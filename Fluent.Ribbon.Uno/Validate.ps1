[CmdletBinding()]
param(
    [ValidateSet("report", "enforce")]
    [string]$ApiMode = "report",

    [switch]$SkipUi
)

$ErrorActionPreference = "Stop"
$unoRoot = $PSScriptRoot
$repositoryRoot = Split-Path -Parent $unoRoot

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

Push-Location $repositoryRoot
try {
    Invoke-DotNet @(
        "build",
        ".\Fluent.Ribbon\Fluent.Ribbon.csproj",
        "-c", "Release",
        "-f", "net8.0-windows",
        "--nologo"
    )

    Invoke-DotNet @(
        "test",
        ".\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Tests\Fluent.Ribbon.Uno.Tests.csproj",
        "-c", "Release",
        "--nologo"
    )

    Invoke-DotNet @(
        "test",
        ".\Fluent.Ribbon.Uno\ApiCompatibility\Fluent.Ribbon.ApiCompatibility.Tests\Fluent.Ribbon.ApiCompatibility.Tests.csproj",
        "-c", "Release",
        "--nologo"
    )

    Invoke-DotNet @(
        "build",
        ".\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Controls\Fluent.Ribbon.Uno.Controls.csproj",
        "-c", "Release",
        "-f", "net10.0-desktop",
        "--nologo"
    )

    Invoke-DotNet @(
        "build",
        ".\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Compatibility\Fluent.Ribbon.Uno.Compatibility.csproj",
        "-c", "Release",
        "-f", "net10.0-desktop",
        "--nologo"
    )

    Invoke-DotNet @(
        "build",
        ".\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Compatibility.XamlTests\Fluent.Ribbon.Uno.Compatibility.XamlTests.csproj",
        "-c", "Release",
        "-f", "net10.0-desktop",
        "--nologo"
    )

    Invoke-DotNet @(
        "build",
        ".\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase.csproj",
        "-c", "Release",
        "-f", "net10.0-desktop",
        "--nologo"
    )

    Invoke-DotNet @(
        "run",
        "--project", ".\Fluent.Ribbon.Uno\ApiCompatibility\Fluent.Ribbon.ApiCompatibility\Fluent.Ribbon.ApiCompatibility.csproj",
        "-c", "Release",
        "--no-build",
        "--",
        "--reference", ".\bin\Fluent.Ribbon\Release\net8.0-windows\Fluent.dll",
        "--candidate", ".\bin\Fluent.Ribbon.Uno.Controls\Release\net10.0-desktop\Fluent.Ribbon.Uno.dll",
        "--candidate", ".\bin\Fluent.Ribbon.Uno.Compatibility\Release\net10.0-desktop\Fluent.Ribbon.Uno.Compatibility.dll",
        "--exceptions", ".\Fluent.Ribbon.Uno\ApiCompatibility\exceptions.wpf-only.json",
        "--mode", $ApiMode
    )

    if (-not $SkipUi) {
        $previousUiSetting = $env:RUN_UNO_UI_TESTS
        try {
            $env:RUN_UNO_UI_TESTS = "1"
            Invoke-DotNet @(
                "test",
                ".\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.UITests\Fluent.Ribbon.Uno.UITests.csproj",
                "-c", "Release",
                "--nologo",
                "--filter", "TestCategory=UI"
            )
        }
        finally {
            $env:RUN_UNO_UI_TESTS = $previousUiSetting
        }
    }
}
finally {
    Pop-Location
}
