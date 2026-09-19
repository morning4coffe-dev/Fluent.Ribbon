[CmdletBinding()]
param(
    [ValidateSet('desktop', 'browserwasm', 'android', 'ios')]
    [string]$Platform = 'browserwasm',
    [string]$ArtifactRoot = (Join-Path $PSScriptRoot '..\..\artifacts\UnoBackport'),
    [string]$RestoreConfigFile = (Join-Path $PSScriptRoot 'NuGet.Source.Config'),
    [string]$RestoreFallbackPackages,
    [switch]$PrepareOnly,
    [switch]$BuildOnly,
    [switch]$CompileRuntimeTests
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$pinFile = Join-Path $PSScriptRoot 'backport.json'
$pinHash = (Get-FileHash $pinFile -Algorithm SHA256).Hash
$pin = Get-Content $pinFile -Raw | ConvertFrom-Json
$sdkPinHash = (Get-FileHash (Join-Path $PSScriptRoot 'sdk-manifest.json') -Algorithm SHA256).Hash
$root = [IO.Path]::GetFullPath($ArtifactRoot)
$source = Join-Path $root 'source'
$feed = Join-Path $root "feed\$Platform"
$logs = Join-Path $root "logs\$Platform"
$helper = Join-Path $PSScriptRoot 'backport.py'
$platformPin = $pin.platforms.$Platform
$version = "$($pin.packageVersionPrefix).$Platform"
$state = [ordered]@{ platform = $Platform; commit = $pin.commit; version = $version; stage = 'prepare'; result = 'running'; runtimeTestsCompiled = $false; runtimeQualified = $false }
$originalEnvironment = @{}
foreach ($name in @('NUGET_PACKAGES', 'NUGET_HTTP_CACHE_PATH', 'UNO_SOURCE_TREE')) {
    $originalEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

function Invoke-Checked([string]$Program, [string[]]$Arguments, [string]$LogName) {
    Write-Host "$Program $($Arguments -join ' ')"
    & $Program @Arguments 2>&1 | Tee-Object -FilePath (Join-Path $logs $LogName)
    if ($LASTEXITCODE -ne 0) {
        throw "$Program failed with exit code $LASTEXITCODE. See $(Join-Path $logs $LogName)"
    }
}

Push-Location $PSScriptRoot
$lock = $null
try {
    # Check output ownership before creating anything, including the build lock.
    & python -c 'import sys; from backport import owned_path; owned_path(sys.argv[1])' $root
    if ($LASTEXITCODE -ne 0) { throw 'Invalid backport artifact root.' }
    New-Item -ItemType Directory -Path $logs -Force | Out-Null
    $lock = [IO.File]::Open((Join-Path $root 'build.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    $drive = [IO.DriveInfo]::new([IO.Path]::GetPathRoot($root))
    Write-Host "Backport volume free space: $([math]::Round($drive.AvailableFreeSpace / 1GB, 1)) GiB."
    Invoke-Checked python @($helper, 'prepare', '--source', $source, '--root', $root, '--platform', $Platform) 'prepare.log'
    $env:UNO_SOURCE_TREE = $source
    Invoke-Checked node @('--test', (Join-Path $PSScriptRoot 'tests\semantic-elements.test.cjs')) 'semantic-elements.tap'
    if ($PrepareOnly) {
        $state.result = 'source-verified'
        return
    }
    if ($Platform -eq 'ios' -and ![Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([Runtime.InteropServices.OSPlatform]::OSX)) {
        throw 'The native UIKit source/package gate requires macOS and its supported Xcode/iOS workload.'
    }
    $sdk = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0 -or $sdk -ne $pin.dotnetSdk) {
        throw "This backport is pinned to .NET SDK $($pin.dotnetSdk), resolved '$sdk'."
    }
    $existingFeed = Test-Path (Join-Path $feed 'build-receipt.json')
    if ($existingFeed) {
        Invoke-Checked python @($helper, 'verify-feed', '--root', $root, '--platform', $Platform) 'verify-feed.log'
        if (!$CompileRuntimeTests -and (Test-Path (Join-Path $root "sdk\$Platform\$version\build-receipt.json"))) {
            Invoke-Checked python @($helper, 'verify-sdk', '--root', $root, '--platform', $Platform) 'verify-sdk.log'
            $state.result = 'existing-build-verified'
            return
        }
    }
    if ((Test-Path $feed) -and !$existingFeed) {
        throw "An incomplete feed exists at $feed. Preserve its evidence and use a fresh artifact root; package identities must not be overwritten."
    }

    $env:NUGET_PACKAGES = Join-Path $root 'source-packages'
    $env:NUGET_HTTP_CACHE_PATH = Join-Path $root 'http-cache'
    $config = [IO.Path]::GetFullPath($RestoreConfigFile)
    $buildProperties = @(
        '-p:ManagePackageVersionsCentrally=false',
        '-p:UnoNugetOverrideVersion=',
        '-p:GeneratePackageOnBuild=false',
        "-p:RestoreConfigFile=$config",
        "-p:RestoreFallbackFolders=$RestoreFallbackPackages"
    )
    if ($RestoreFallbackPackages) {
        Write-Host "Reading exact, unchanged dependency versions from fallback cache: $RestoreFallbackPackages. No cache replacement targets are enabled."
    }
    $override = @"
<Project>
  <PropertyGroup>
    <UnoTargetFrameworkOverride>$($platformPin.sourceFramework)</UnoTargetFrameworkOverride>
    <PackageVersion>$($pin.release)</PackageVersion>
    <PackageVersion Condition="'`$(MSBuildProjectName)' == 'Uno.UI.Skia' Or '`$(MSBuildProjectName)' == 'Uno.UI.Reference' Or '`$(MSBuildProjectName)' == 'Uno.UI.netcoremobile' Or '`$(MSBuildProjectName)' == 'Uno.UI.Runtime.Skia.WebAssembly.Browser'">$version</PackageVersion>
    <RepositoryCommit>$($pin.commit)</RepositoryCommit>
    <RepositoryBranch>fluent-accessibility-backport</RepositoryBranch>
    <InformationalVersion>$version+$($pin.commit)</InformationalVersion>
  </PropertyGroup>
</Project>
"@
    $overridePath = Join-Path $source 'src\crosstargeting_override.props'
    if (!(Test-Path $overridePath) -or [IO.File]::ReadAllText($overridePath) -ne $override) {
        [IO.File]::WriteAllText($overridePath, $override, [Text.UTF8Encoding]::new($false))
    }
    $state.stage = 'sdk'
    if (Test-Path (Join-Path $root "sdk\$Platform\$version\build-receipt.json")) {
        Invoke-Checked python @($helper, 'verify-sdk', '--root', $root, '--platform', $Platform) 'verify-sdk.log'
    }
    else {
        Invoke-Checked python @($helper, 'prepare-sdk', '--root', $root, '--platform', $Platform) 'prepare-sdk.log'
        $sdkFeed = Join-Path $root "sdk-feed\$Platform"
        if (Test-Path (Join-Path $sdkFeed "Uno.Sdk.Private.$version.nupkg")) {
            throw "An incomplete private SDK package already exists in $sdkFeed; refusing to overwrite its identity."
        }
        New-Item -ItemType Directory -Path $sdkFeed -Force | Out-Null
        # CI=true is Uno's official switch which prevents its developer SDK pack
        # target from deleting existing SDK packages/caches.
        Invoke-Checked dotnet (@(
            'pack', (Join-Path $source 'src\Uno.Sdk\Uno.Sdk.csproj'), '-c', 'Release', '-m:1',
            "-p:PackageVersion=$version", '-p:CI=true',
            "-p:UnoSdkPackageManifest=$(Join-Path $root "sdk-input\$Platform\packages.json")",
            "-p:PackageOutputPath=$sdkFeed", '-v:minimal'
        ) + $buildProperties) 'build-sdk.log'
        Invoke-Checked python @($helper, 'seal-sdk', '--source', $source, '--root', $root, '--platform', $Platform) 'seal-sdk.log'
    }
    $state.stage = 'compile'
    $index = 0
    $projects = if ($existingFeed) { @() } else { $platformPin.projects }
    foreach ($project in $projects) {
        $projectPath = Join-Path $source ($project.Replace('/', [IO.Path]::DirectorySeparatorChar))
        Invoke-Checked dotnet (@('build', $projectPath, '-c', 'Release', '-m:1', '-v:minimal') + $buildProperties) "build-$index.log"
        $index++
    }
    if ($CompileRuntimeTests) {
        $state.stage = 'compile-runtime-tests'
        $testVariant = if ($Platform -in @('desktop', 'browserwasm')) { 'Skia' } else { 'netcoremobile' }
        $testProject = Join-Path $source "src\Uno.UI.RuntimeTests\Uno.UI.RuntimeTests.$testVariant.csproj"
        Invoke-Checked dotnet (@('build', $testProject, '-c', 'Release', '-m:1', '-v:minimal') + $buildProperties) 'build-runtime-tests.log'
        $state.runtimeTestsCompiled = $true
    }
    Invoke-Checked python @($helper, 'verify-source', '--source', $source, '--root', $root, '--platform', $Platform) 'verify-source.log'
    if ($existingFeed) {
        $state.result = if ($state.runtimeTestsCompiled) { 'existing-build-and-fixture-compile-verified' } else { 'existing-runtime-and-sdk-verified' }
        return
    }
    if ($BuildOnly) {
        $state.result = 'compiled-not-runtime-qualified'
        return
    }

    $state.stage = 'pack'
    Invoke-Checked python @($helper, 'prepare-pack', '--source', $source, '--root', $root, '--platform', $Platform) 'prepare-pack.log'
    New-Item -ItemType Directory -Path $feed | Out-Null
    $packRoot = Join-Path $root "pack\$Platform"
    if ($Platform -eq 'browserwasm') {
        # Uno's pack target builds embedded project references, even after an
        # earlier build. Pack it first so the core nuspec reads the final outputs.
        Invoke-Checked dotnet (@(
            'pack', (Join-Path $source 'src\Uno.UI.Runtime.Skia.WebAssembly.Browser\Uno.UI.Runtime.Skia.WebAssembly.Browser.csproj'),
            '-c', 'Release', '-m:1', "-p:PackageOutputPath=$feed", '-v:minimal'
        ) + $buildProperties) 'pack-browser.log'
    }
    Invoke-Checked dotnet @(
        'restore', (Join-Path $PSScriptRoot 'Pack.csproj'),
        "-p:BaseIntermediateOutputPath=$(Join-Path $packRoot 'obj')$([IO.Path]::DirectorySeparatorChar)",
        "-p:RestoreConfigFile=$config", '-v:minimal'
    ) 'restore-pack.log'
    Invoke-Checked dotnet @(
        'pack', (Join-Path $PSScriptRoot 'Pack.csproj'), '--no-build', '-c', 'Release',
        "-p:NuspecFile=$(Join-Path $packRoot 'Uno.WinUI.nuspec')",
        "-p:PackageOutputPath=$feed", "-p:PackageVersion=$version",
        "-p:BaseIntermediateOutputPath=$(Join-Path $packRoot 'obj')$([IO.Path]::DirectorySeparatorChar)",
        '-v:minimal'
    ) 'pack-core.log'
    if ((Get-FileHash $pinFile -Algorithm SHA256).Hash -ne $pinHash -or
        (Get-FileHash (Join-Path $PSScriptRoot 'sdk-manifest.json') -Algorithm SHA256).Hash -ne $sdkPinHash) {
        throw 'The source/package pin changed during this build; refusing to seal mixed inputs.'
    }
    Invoke-Checked python @($helper, 'seal-feed', '--source', $source, '--root', $root, '--platform', $Platform) 'seal-feed.log'
    $state.result = 'packaged-not-runtime-qualified'
    Write-Host "Verified opt-in packages: $feed. Run the unchanged platform runtime gates before promotion."
}
catch {
    $state.result = 'failed'
    $state.error = $_.Exception.Message
    throw
}
finally {
    if (Test-Path $logs) {
        $state | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $logs 'result.json') -Encoding utf8
    }
    if ($lock) { $lock.Dispose() }
    foreach ($name in $originalEnvironment.Keys) {
        [Environment]::SetEnvironmentVariable($name, $originalEnvironment[$name], 'Process')
    }
    Pop-Location
}
