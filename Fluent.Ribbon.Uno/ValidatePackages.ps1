[CmdletBinding()]
param(
    [string]$Version = "0.1.0-local"
)

$ErrorActionPreference = "Stop"
$unoRoot = $PSScriptRoot
$repositoryRoot = Split-Path -Parent $unoRoot
$packageDirectory = Join-Path $repositoryRoot "artifacts\Fluent.Ribbon.Uno.Packages"
$packageCache = Join-Path $repositoryRoot "artifacts\Fluent.Ribbon.Uno.PackageCache\$PID"
$packageBuildDirectory = Join-Path $repositoryRoot "artifacts\Fluent.Ribbon.Uno.PackageBuild\$PID"
$consumerDirectory = Join-Path $unoRoot "PackageValidation\Fluent.Ribbon.Uno.PackageConsumer"
$consumerProject = Join-Path $consumerDirectory "Fluent.Ribbon.Uno.PackageConsumer.csproj"
$consumerFixture = Join-Path $consumerDirectory "PackageFixture.xaml"
$consumerNuGetConfig = Join-Path $unoRoot "PackageValidation\NuGet.Config"
$previousPackageCache = $env:NUGET_PACKAGES

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-PackageEntries {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        return @($archive.Entries | ForEach-Object FullName)
    }
    finally {
        $archive.Dispose()
    }
}

function Get-PackageNuspec {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $archive = [System.IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entry = $archive.Entries |
            Where-Object { $_.FullName -like "*.nuspec" } |
            Select-Object -First 1
        if ($null -eq $entry) {
            throw "'$Path' does not contain a nuspec."
        }

        $reader = [System.IO.StreamReader]::new($entry.Open())
        try {
            return [xml]$reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-PackageEntry {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Entries,

        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if (-not ($Entries | Where-Object { $_ -match $Pattern })) {
        throw "Package is missing $Description (expected entry matching '$Pattern')."
    }
}

if (Test-Path $packageDirectory) {
    Remove-Item $packageDirectory -Recurse -Force
}

if (Test-Path $packageCache) {
    Remove-Item $packageCache -Recurse -Force
}

if (Test-Path $packageBuildDirectory) {
    Remove-Item $packageBuildDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $packageDirectory | Out-Null
New-Item -ItemType Directory -Path $packageCache | Out-Null
New-Item -ItemType Directory -Path $packageBuildDirectory | Out-Null
$env:NUGET_PACKAGES = $packageCache

Add-Type -AssemblyName System.IO.Compression.FileSystem

Push-Location $repositoryRoot
try {
    $packProperties = @(
        "-p:Version=$Version",
        "-p:TargetFramework=net10.0-desktop",
        "-p:TargetFrameworks=net10.0-desktop"
    )

    Invoke-DotNet -Arguments (@(
        "pack",
        ".\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Controls\Fluent.Ribbon.Uno.Controls.csproj",
        "-c", "Release",
        "-o", $packageDirectory,
        "--artifacts-path", $packageBuildDirectory,
        "--nologo"
    ) + $packProperties)

    Invoke-DotNet -Arguments (@(
        "pack",
        ".\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Compatibility\Fluent.Ribbon.Uno.Compatibility.csproj",
        "-c", "Release",
        "-o", $packageDirectory,
        "--artifacts-path", $packageBuildDirectory,
        "--nologo"
    ) + $packProperties)

    $facadePackage = Join-Path $packageDirectory "Fluent.Ribbon.Uno.$Version.nupkg"
    $corePackage = Join-Path $packageDirectory "Fluent.Ribbon.Uno.Core.$Version.nupkg"
    if (-not (Test-Path $facadePackage) -or -not (Test-Path $corePackage)) {
        throw "Expected facade and core packages were not produced."
    }

    $facadeEntries = Get-PackageEntries -Path $facadePackage
    Assert-PackageEntry $facadeEntries '^lib/[^/]+/Fluent\.Ribbon\.Uno\.Compatibility\.dll$' "the compatibility assembly"
    Assert-PackageEntry $facadeEntries '^README\.md$' "README.md"
    Assert-PackageEntry $facadeEntries '^MIGRATION\.md$' "MIGRATION.md"
    Assert-PackageEntry $facadeEntries '^license/License\.txt$' "the license"
    Assert-PackageEntry $facadeEntries '^Logo\.png$' "the package icon"

    $facadeNuspec = Get-PackageNuspec -Path $facadePackage
    $coreDependency = $facadeNuspec.SelectSingleNode(
        "//*[local-name()='dependency' and @id='Fluent.Ribbon.Uno.Core']")
    if ($null -eq $coreDependency) {
        throw "The facade package does not depend on Fluent.Ribbon.Uno.Core."
    }

    if ($coreDependency.version -ne "[$Version]") {
        throw "The facade must depend on exactly Fluent.Ribbon.Uno.Core $Version; actual range is '$($coreDependency.version)'."
    }

    $coreEntries = Get-PackageEntries -Path $corePackage
    Assert-PackageEntry $coreEntries '^lib/[^/]+/Fluent\.Ribbon\.Uno\.dll$' "the core assembly"
    Assert-PackageEntry $coreEntries '^lib/[^/]+/Fluent\.Ribbon\.Uno/Themes/Generic\.xaml$' "the core Generic.xaml resource"
    Assert-PackageEntry $coreEntries '^lib/[^/]+/Fluent\.Ribbon\.Uno/Themes/.+\.xaml$' "the core theme resource layout"
    Assert-PackageEntry $coreEntries '^README\.md$' "README.md"
    Assert-PackageEntry $coreEntries '^MIGRATION\.md$' "MIGRATION.md"
    Assert-PackageEntry $coreEntries '^license/License\.txt$' "the license"
    Assert-PackageEntry $coreEntries '^Logo\.png$' "the package icon"

    $generatedEntries = @(
        $coreEntries |
            Where-Object {
                $_ -match '^(content|contentFiles)/(?:[^/]+/)*(obj|bin)/' -or
                $_ -match '^(obj|bin)/'
            }
    )
    if ($generatedEntries.Count -gt 0) {
        throw "The core package contains a generated obj/bin entry: $($generatedEntries[0])."
    }

    $themeRoot = Join-Path $unoRoot "Fluent.Ribbon.Uno.Controls\Themes"
    foreach ($themeFile in Get-ChildItem $themeRoot -Recurse -Filter "*.xaml") {
        $relativeThemePath = $themeFile.FullName.Substring($themeRoot.Length).TrimStart("\")
        $relativeThemePath = $relativeThemePath.Replace("\", "/")
        $packageSuffix = "/Fluent.Ribbon.Uno/Themes/$relativeThemePath"
        if (-not ($coreEntries | Where-Object { $_.EndsWith($packageSuffix, [System.StringComparison]::Ordinal) })) {
            throw "The core package is missing theme resource '$relativeThemePath'."
        }
    }

    [xml]$consumerXml = Get-Content $consumerProject -Raw
    $consumerPackageReferences = @(
        $consumerXml.Project.ItemGroup.PackageReference |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_.Include) }
    )
    if ($consumerPackageReferences.Count -ne 1 -or
        $consumerPackageReferences[0].Include -ne "Fluent.Ribbon.Uno") {
        throw "The package consumer must reference only Fluent.Ribbon.Uno."
    }

    $fixtureText = Get-Content $consumerFixture -Raw
    if ($fixtureText -notmatch 'SizeDefinition="Large Middle Small"') {
        throw "The package consumer must compile the WPF-style SizeDefinition string."
    }

    if ($fixtureText -notmatch '<fluent:ContextMenu') {
        throw "The package consumer must compile Fluent.ContextMenu usage."
    }

    if ($fixtureText -notmatch 'ms-appx:///Fluent\.Ribbon\.Uno/Themes/Generic\.xaml') {
        throw "The package consumer must resolve the core Generic.xaml resource URI."
    }

    Invoke-DotNet -Arguments @(
        "restore",
        $consumerProject,
        "--configfile", $consumerNuGetConfig,
        "--force",
        "--no-cache",
        "-p:FluentRibbonUnoPackageVersion=$Version"
    )

    $assetsPath = Join-Path $consumerDirectory "obj\project.assets.json"
    $assets = Get-Content $assetsPath -Raw | ConvertFrom-Json
    $resolvedLibraries = @($assets.libraries.PSObject.Properties.Name)
    foreach ($packageId in "Fluent.Ribbon.Uno/$Version", "Fluent.Ribbon.Uno.Core/$Version") {
        if ($resolvedLibraries -notcontains $packageId) {
            throw "Consumer restore did not resolve '$packageId' from the package graph."
        }
    }

    Invoke-DotNet -Arguments @(
        "build",
        $consumerProject,
        "-c", "Release",
        "-f", "net10.0-desktop",
        "--no-restore",
        "--nologo",
        "-p:FluentRibbonUnoPackageVersion=$Version"
    )

    $consumerOutput = Join-Path $repositoryRoot "bin\Fluent.Ribbon.Uno.PackageConsumer\Release\net10.0-desktop"
    foreach ($assembly in "Fluent.Ribbon.Uno.PackageConsumer.dll", "Fluent.Ribbon.Uno.Compatibility.dll", "Fluent.Ribbon.Uno.dll") {
        if (-not (Test-Path (Join-Path $consumerOutput $assembly))) {
            throw "Consumer output is missing '$assembly'."
        }
    }

    Write-Host "Package validation passed for Fluent.Ribbon.Uno $Version and exact Core dependency $Version."
}
finally {
    Pop-Location
    $env:NUGET_PACKAGES = $previousPackageCache
    Remove-Item $packageCache -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $packageBuildDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
