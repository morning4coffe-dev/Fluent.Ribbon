[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",

    [ValidateRange(800, 3840)]
    [int]$Width = 1600,

    [ValidateRange(600, 2160)]
    [int]$Height = 1000,

    [bool]$IncludeVisualMatrix = $true,

    [ValidateRange(800, 1600)]
    [int]$NarrowWidth = 900,

    [ValidateRange(600, 1200)]
    [int]$NarrowHeight = 700,

    [switch]$CaptureHighContrast,

    [string]$OutputDirectory = (Join-Path $PSScriptRoot "artifacts\visual-parity")
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$winUiTargetFramework = "net10.0-windows10.0.26100"
$winUiProject = Join-Path $PSScriptRoot "Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase.csproj"
$wpfProject = Join-Path $repositoryRoot "Fluent.Ribbon.Showcase\Fluent.Ribbon.Showcase.csproj"
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)

Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class ShowcaseWindowNativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}
"@
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing

function Invoke-Checked {
    param(
        [Parameter(Mandatory)]
        [scriptblock]$Command,

        [Parameter(Mandatory)]
        [string]$Description
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$Description failed with exit code $LASTEXITCODE."
    }
}

function Get-MSBuildPath {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path -LiteralPath $vswhere)) {
        throw "Visual Studio Build Tools were not found."
    }

    $path = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" |
        Select-Object -First 1
    if (-not $path) {
        throw "MSBuild.exe was not found."
    }

    return $path
}

function Wait-ForMainWindow {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId
    )

    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        $process = Get-Process -Id $ProcessId -ErrorAction Stop
        $process.Refresh()
        if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
            return $process.MainWindowHandle
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Process $ProcessId did not create a main window."
}

function Set-ComparisonWindowSize {
    param(
        [Parameter(Mandatory)]
        [IntPtr]$WindowHandle,

        [int]$TargetWidth = $Width,

        [int]$TargetHeight = $Height
    )

    $noMove = 0x0002
    $noZOrder = 0x0004
    $noActivate = 0x0010
    $result = [ShowcaseWindowNativeMethods]::SetWindowPos(
        $WindowHandle,
        [IntPtr]::Zero,
        0,
        0,
        $TargetWidth,
        $TargetHeight,
        $noMove -bor $noZOrder -bor $noActivate)
    if (-not $result) {
        throw "Could not resize comparison window."
    }
}

function Capture-WpfWindow {
    param(
        [Parameter(Mandatory)]
        [IntPtr]$WindowHandle,

        [Parameter(Mandatory)]
        [string]$Path
    )

    $rect = [ShowcaseWindowNativeMethods+RECT]::new()
    if (-not [ShowcaseWindowNativeMethods]::GetWindowRect($WindowHandle, [ref]$rect)) {
        throw "Could not read WPF comparison window bounds."
    }

    $captureWidth = $rect.Right - $rect.Left
    $captureHeight = $rect.Bottom - $rect.Top
    if ($captureWidth -le 0 -or $captureHeight -le 0) {
        throw "WPF comparison window bounds are empty."
    }

    [ShowcaseWindowNativeMethods]::SetForegroundWindow($WindowHandle) | Out-Null
    Start-Sleep -Milliseconds 150

    $bitmap = [System.Drawing.Bitmap]::new($captureWidth, $captureHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.CopyFromScreen(
            $rect.Left,
            $rect.Top,
            0,
            0,
            [System.Drawing.Size]::new($captureWidth, $captureHeight))

        $sampledColors = [System.Collections.Generic.HashSet[int]]::new()
        for ($x = 0; $x -lt $captureWidth; $x += 16) {
            for ($y = 0; $y -lt $captureHeight; $y += 16) {
                $sampledColors.Add($bitmap.GetPixel($x, $y).ToArgb()) | Out-Null
            }
        }
        if ($sampledColors.Count -lt 4) {
            throw "WPF capture is blank; run visual comparison in an interactive desktop session."
        }

        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Get-WinUiProcessId {
    param(
        [Parameter(Mandatory)]
        [string]$OutputPath,

        [string]$LaunchArguments
    )

    $winAppArguments = @("run", $OutputPath, "--clean", "--detach", "--json")
    if ($LaunchArguments) {
        $winAppArguments += @("--args", $LaunchArguments)
    }

    $json = (& winapp @winAppArguments | Out-String).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "winapp run failed with exit code $LASTEXITCODE."
    }

    $launch = $json | ConvertFrom-Json
    foreach ($propertyName in "processId", "pid", "PID") {
        if ($launch.PSObject.Properties.Name -contains $propertyName) {
            return [int]$launch.$propertyName
        }
    }

    if ($json -match '"(?:processId|pid|PID)"\s*:\s*(\d+)') {
        return [int]$Matches[1]
    }

    throw "Could not read the WinUI process ID from winapp output: $json"
}

function Restart-WinUiShowcase {
    param(
        [int]$CurrentProcessId,

        [Parameter(Mandatory)]
        [string]$OutputPath,

        [string]$LaunchArguments,

        [int]$TargetWidth = $Width,

        [int]$TargetHeight = $Height
    )

    if ($CurrentProcessId -gt 0 -and
        (Get-Process -Id $CurrentProcessId -ErrorAction SilentlyContinue)) {
        Stop-Process -Id $CurrentProcessId
        Wait-Process -Id $CurrentProcessId -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
    }

    $processId = Get-WinUiProcessId `
        -OutputPath $OutputPath `
        -LaunchArguments $LaunchArguments
    Set-ComparisonWindowSize `
        -WindowHandle (Wait-ForMainWindow -ProcessId $processId) `
        -TargetWidth $TargetWidth `
        -TargetHeight $TargetHeight
    Start-Sleep -Seconds 1
    return $processId
}

function Get-AutomationElement {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$AutomationId
    )

    $processCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId)
    $automationIdCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    $nameCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $AutomationId)
    $selectorCondition = [System.Windows.Automation.OrCondition]::new(
        $automationIdCondition,
        $nameCondition)
    $condition = [System.Windows.Automation.AndCondition]::new(
        $processCondition,
        $selectorCondition)
    $element = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        $condition)
    if (-not $element) {
        throw "Could not find automation element '$AutomationId' in process $ProcessId."
    }

    return $element
}

function Set-ExpandCollapseState {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$AutomationId,

        [Parameter(Mandatory)]
        [bool]$Expand
    )

    $element = Get-AutomationElement -ProcessId $ProcessId -AutomationId $AutomationId
    $pattern = $element.GetCurrentPattern(
        [System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    if ($Expand) {
        $pattern.Expand()
    }
    else {
        $pattern.Collapse()
    }
}

function Invoke-ShowcaseElement {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$Selector
    )

    & winapp ui wait-for $Selector -a $ProcessId -t 10000 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not find '$Selector' in process $ProcessId."
    }

    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    $lastError = $null
    do {
        try {
            $element = Get-AutomationElement -ProcessId $ProcessId -AutomationId $Selector
            $patterns = $element.GetSupportedPatterns()
            if ($patterns -contains [System.Windows.Automation.ExpandCollapsePattern]::Pattern) {
                $pattern = $element.GetCurrentPattern(
                    [System.Windows.Automation.ExpandCollapsePattern]::Pattern)
                if ($pattern.Current.ExpandCollapseState -ne
                    [System.Windows.Automation.ExpandCollapseState]::Expanded) {
                    $pattern.Expand()
                }
                return
            }

            if ($patterns -contains [System.Windows.Automation.InvokePattern]::Pattern) {
                $element.GetCurrentPattern(
                    [System.Windows.Automation.InvokePattern]::Pattern).Invoke()
                return
            }

            if ($patterns -contains [System.Windows.Automation.TogglePattern]::Pattern) {
                $element.GetCurrentPattern(
                    [System.Windows.Automation.TogglePattern]::Pattern).Toggle()
                return
            }
        }
        catch {
            $lastError = $_
        }

        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    & winapp ui invoke $Selector -a $ProcessId -q 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        return
    }

    throw "Could not invoke '$Selector' in process $ProcessId. $lastError"
}

function Select-ShowcaseItem {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$OwnerSelector,

        [Parameter(Mandatory)]
        [string]$ItemName
    )

    Invoke-ShowcaseElement -ProcessId $ProcessId -Selector $OwnerSelector
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        Start-Sleep -Milliseconds 100
        $ownerElement = $null
        try {
            $ownerElement = Get-AutomationElement -ProcessId $ProcessId -AutomationId $OwnerSelector
        }
        catch {
            # Some selectors are names rather than automation IDs.
        }

        $processCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
            $ProcessId)
        $nameCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            $ItemName)
        $condition = [System.Windows.Automation.AndCondition]::new(
            $processCondition,
            $nameCondition)
        $candidates = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            $condition) |
            Sort-Object { $_.Current.IsOffscreen }

        foreach ($candidate in $candidates) {
            try {
                $patterns = $candidate.GetSupportedPatterns()
                if ($patterns -contains [System.Windows.Automation.SelectionItemPattern]::Pattern) {
                    $selectionPattern = $candidate.GetCurrentPattern(
                        [System.Windows.Automation.SelectionItemPattern]::Pattern)
                    $selectionPattern.Select()
                    Close-ShowcaseItemOwner `
                        -ProcessId $ProcessId `
                        -OwnerSelector $OwnerSelector `
                        -OwnerElement $ownerElement
                    return
                }

                if ($patterns -contains [System.Windows.Automation.InvokePattern]::Pattern) {
                    $invokePattern = $candidate.GetCurrentPattern(
                        [System.Windows.Automation.InvokePattern]::Pattern)
                    $invokePattern.Invoke()
                    Close-ShowcaseItemOwner `
                        -ProcessId $ProcessId `
                        -OwnerSelector $OwnerSelector `
                        -OwnerElement $ownerElement
                    return
                }
            }
            catch {
                # Theme changes can recreate the item while it is being queried.
            }
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Could not select '$ItemName' in process $ProcessId."
}

function Close-ShowcaseItemOwner {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$OwnerSelector,

        [System.Windows.Automation.AutomationElement]$OwnerElement
    )

    $owners = @()
    if ($OwnerElement) {
        $owners += $OwnerElement
    }

    try {
        $currentOwner = Get-AutomationElement `
            -ProcessId $ProcessId `
            -AutomationId $OwnerSelector
        if ($owners -notcontains $currentOwner) {
            $owners += $currentOwner
        }
    }
    catch {
        # The owner can be recreated by a theme change.
    }

    foreach ($owner in $owners) {
        try {
            $patterns = $owner.GetSupportedPatterns()
            if ($patterns -contains [System.Windows.Automation.ExpandCollapsePattern]::Pattern) {
                $expandPattern = $owner.GetCurrentPattern(
                    [System.Windows.Automation.ExpandCollapsePattern]::Pattern)
                if ($expandPattern.Current.ExpandCollapseState -eq
                    [System.Windows.Automation.ExpandCollapseState]::Expanded) {
                    $expandPattern.Collapse()
                }
            }
        }
        catch {
            # Continue past stale elements recreated by the selected state.
        }
    }

    Start-Sleep -Milliseconds 350
}

function Capture-OpenSurface {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$Selector,

        [Parameter(Mandatory)]
        [string]$Prefix,

        [Parameter(Mandatory)]
        [string]$State,

        [switch]$Click,

        [switch]$ExpandCollapse,

        [switch]$VerifyExpanded
    )

    & winapp ui wait-for $Selector -a $ProcessId -t 10000 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Could not find '$Selector' in process $ProcessId."
    }

    if ($ExpandCollapse) {
        Set-ExpandCollapseState -ProcessId $ProcessId -AutomationId $Selector -Expand $true
    }
    elseif ($Click) {
        & winapp ui click $Selector -a $ProcessId | Out-Null
    }
    else {
        & winapp ui invoke $Selector -a $ProcessId | Out-Null
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Could not open '$State' in process $ProcessId."
    }

    Start-Sleep -Milliseconds 500
    $windowHandle = [long](Wait-ForMainWindow -ProcessId $ProcessId)

    if ($VerifyExpanded) {
        $deadline = [DateTime]::UtcNow.AddSeconds(5)
        do {
            if ($ExpandCollapse) {
                $element = Get-AutomationElement -ProcessId $ProcessId -AutomationId $Selector
                $pattern = $element.GetCurrentPattern(
                    [System.Windows.Automation.ExpandCollapsePattern]::Pattern)
                $isExpanded =
                    $pattern.Current.ExpandCollapseState -eq
                    [System.Windows.Automation.ExpandCollapseState]::Expanded
            }
            else {
                $searchResult = (& winapp ui search $Selector -w $windowHandle --json 2>$null | Out-String) |
                    ConvertFrom-Json
                $isExpanded = @(
                    $searchResult.matches |
                        Where-Object { $_.expandState -eq "expanded" }
                ).Count -gt 0
            }

            if ($isExpanded) {
                break
            }

            Start-Sleep -Milliseconds 100
        } while ([DateTime]::UtcNow -lt $deadline)

        if (-not $isExpanded) {
            throw "'$State' did not leave '$Selector' expanded in process $ProcessId."
        }
    }

    $path = Join-Path $OutputDirectory "$Prefix-$State.png"
    if ($Prefix -eq "wpf") {
        Capture-WpfWindow `
            -WindowHandle (Wait-ForMainWindow -ProcessId $ProcessId) `
            -Path $path
    }
    else {
        & winapp ui screenshot -a $ProcessId -o $path | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not capture '$State' for process $ProcessId."
        }
    }
}

function Select-ShowcaseTab {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$State
    )

    $processCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
        $ProcessId)
    $nameCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $State)
    $tabCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::TabItem)
    $condition = [System.Windows.Automation.AndCondition]::new(
        $processCondition,
        $nameCondition,
        $tabCondition)

    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        $candidates = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            $condition)

        foreach ($candidate in $candidates) {
            try {
                $patterns = $candidate.GetSupportedPatterns()
                if ($patterns -contains [System.Windows.Automation.SelectionItemPattern]::Pattern) {
                    $pattern = $candidate.GetCurrentPattern(
                        [System.Windows.Automation.SelectionItemPattern]::Pattern)
                    if (-not $pattern.Current.IsSelected) {
                        $pattern.Select()
                        Start-Sleep -Milliseconds 250
                    }

                    return
                }
                elseif ($patterns -contains [System.Windows.Automation.InvokePattern]::Pattern) {
                    $candidate.GetCurrentPattern(
                        [System.Windows.Automation.InvokePattern]::Pattern).Invoke()
                    return
                }
            }
            catch {
                # Continue past stale tab elements recreated during layout changes.
            }
        }

        Start-Sleep -Milliseconds 100
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Could not find or select '$State' in process $ProcessId."
}

function Capture-ShowcaseState {
    param(
        [Parameter(Mandatory)]
        [int]$ProcessId,

        [Parameter(Mandatory)]
        [string]$Prefix,

        [Parameter(Mandatory)]
        [string]$State,

        [switch]$SelectState
    )

    if ($SelectState) {
        Select-ShowcaseTab -ProcessId $ProcessId -State $State
    }

    $safeState = $State -replace "[^A-Za-z0-9]+", "-"
    $path = Join-Path $OutputDirectory "$Prefix-$safeState.png"
    if ($Prefix -eq "wpf") {
        Capture-WpfWindow `
            -WindowHandle (Wait-ForMainWindow -ProcessId $ProcessId) `
            -Path $path
    }
    else {
        & winapp ui screenshot -a $ProcessId -o $path | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "Could not capture '$State' for process $ProcessId."
        }
    }
}

if (-not (Get-Command winapp -ErrorAction SilentlyContinue)) {
    throw "winapp CLI is required. Run /winui-setup before using this script."
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$msbuild = Get-MSBuildPath
$wpfProcess = $null
$winUiProcessId = $null

Push-Location $repositoryRoot
try {
    Invoke-Checked -Description "WPF Showcase build" -Command {
        dotnet build $wpfProject -f net8.0-windows -c $Configuration --nologo
    }

    Invoke-Checked -Description "WinUI Showcase build" -Command {
        & $msbuild $winUiProject /restore /t:Build /nologo /v:m `
            "/p:Configuration=$Configuration" `
            /p:Platform=x64 `
            /p:PublishTrimmed=false `
            "/p:TargetFramework=$winUiTargetFramework" `
            "/p:TargetFrameworks=$winUiTargetFramework"
    }

    $wpfExecutable = Join-Path $repositoryRoot "bin\Fluent.Ribbon.Showcase\$Configuration\net8.0-windows\Fluent.Ribbon.Showcase.exe"
    $winUiOutput = Join-Path $PSScriptRoot "Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase\bin\x64\$Configuration\$winUiTargetFramework\win-x64"

    $wpfProcess = Start-Process -FilePath $wpfExecutable -PassThru
    $winUiProcessId = Get-WinUiProcessId `
        -OutputPath $winUiOutput `
        -LaunchArguments "--showcase-tab=0 --showcase-state=classic"

    Set-ComparisonWindowSize -WindowHandle (Wait-ForMainWindow -ProcessId $wpfProcess.Id)
    Set-ComparisonWindowSize -WindowHandle (Wait-ForMainWindow -ProcessId $winUiProcessId)
    Start-Sleep -Seconds 1

    Add-Type -AssemblyName PresentationFramework
    $isHighContrast = [System.Windows.SystemParameters]::HighContrast
    if ($CaptureHighContrast) {
        if (-not $isHighContrast) {
            throw "High Contrast capture requires Windows Contrast Themes to be enabled before running the script."
        }

        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "Toolbars" -SelectState
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "Toolbars" -SelectState
        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "HighContrast-Toolbars"
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "HighContrast-Toolbars"
        Write-Host "High Contrast screenshots written to $OutputDirectory"
        return
    }

    if ($isHighContrast) {
        throw "Disable Windows Contrast Themes for normal baselines, or use -CaptureHighContrast for a High Contrast-only run."
    }

    $states = @("Toolbars", "Insert", "Galleries", "Resizing & Screentips")
    foreach ($state in $states) {
        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State $state -SelectState
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State $state -SelectState
    }

    if ($IncludeVisualMatrix) {
        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "Toolbars" -SelectState
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "Toolbars" -SelectState

        Select-ShowcaseItem -ProcessId $wpfProcess.Id -OwnerSelector "BaseColors" -ItemName "Dark"
        Select-ShowcaseTab -ProcessId $wpfProcess.Id -State "Toolbars"
        $winUiProcessId = Restart-WinUiShowcase `
            -CurrentProcessId $winUiProcessId `
            -OutputPath $winUiOutput `
            -LaunchArguments "--showcase-tab=0 --showcase-state=dark"
        Start-Sleep -Milliseconds 500
        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "Dark-Toolbars"
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "Dark-Toolbars"

        Select-ShowcaseItem -ProcessId $wpfProcess.Id -OwnerSelector "BaseColors" -ItemName "Light"
        Select-ShowcaseTab -ProcessId $wpfProcess.Id -State "Toolbars"

        Select-ShowcaseItem -ProcessId $wpfProcess.Id -OwnerSelector "Window flow direction" -ItemName "RightToLeft"
        Select-ShowcaseTab -ProcessId $wpfProcess.Id -State "Toolbars"
        $winUiProcessId = Restart-WinUiShowcase `
            -CurrentProcessId $winUiProcessId `
            -OutputPath $winUiOutput `
            -LaunchArguments "--showcase-tab=0 --showcase-state=rtl"
        Start-Sleep -Milliseconds 500
        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "RTL-Toolbars"
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "RTL-Toolbars"

        Select-ShowcaseItem -ProcessId $wpfProcess.Id -OwnerSelector "Window flow direction" -ItemName "LeftToRight"
        Select-ShowcaseTab -ProcessId $wpfProcess.Id -State "Toolbars"

        Select-ShowcaseItem -ProcessId $wpfProcess.Id -OwnerSelector "Ribbon Display Options" -ItemName "Use Simplified Ribbon"
        Select-ShowcaseTab -ProcessId $wpfProcess.Id -State "Toolbars"
        $winUiProcessId = Restart-WinUiShowcase `
            -CurrentProcessId $winUiProcessId `
            -OutputPath $winUiOutput `
            -LaunchArguments "--showcase-tab=0 --showcase-state=simplified"
        Start-Sleep -Milliseconds 500
        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "Simplified-Toolbars"
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "Simplified-Toolbars"

        Select-ShowcaseItem -ProcessId $wpfProcess.Id -OwnerSelector "Ribbon Display Options" -ItemName "Use Classic Ribbon"
        Select-ShowcaseTab -ProcessId $wpfProcess.Id -State "Toolbars"

        Invoke-ShowcaseElement -ProcessId $wpfProcess.Id -Selector "IsMinimized"
        $winUiProcessId = Restart-WinUiShowcase `
            -CurrentProcessId $winUiProcessId `
            -OutputPath $winUiOutput `
            -LaunchArguments "--showcase-tab=0 --showcase-state=minimized"
        Start-Sleep -Milliseconds 500
        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "Minimized-Toolbars"
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "Minimized-Toolbars"

        Invoke-ShowcaseElement -ProcessId $wpfProcess.Id -Selector "IsMinimized"

        Set-ComparisonWindowSize `
            -WindowHandle (Wait-ForMainWindow -ProcessId $wpfProcess.Id) `
            -TargetWidth $NarrowWidth `
            -TargetHeight $NarrowHeight
        $winUiProcessId = Restart-WinUiShowcase `
            -CurrentProcessId $winUiProcessId `
            -OutputPath $winUiOutput `
            -TargetWidth $NarrowWidth `
            -TargetHeight $NarrowHeight
        Start-Sleep -Milliseconds 500
        Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "Narrow-Toolbars"
        Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "Narrow-Toolbars"

        Set-ComparisonWindowSize -WindowHandle (Wait-ForMainWindow -ProcessId $wpfProcess.Id)
        $winUiProcessId = Restart-WinUiShowcase `
            -CurrentProcessId $winUiProcessId `
            -OutputPath $winUiOutput `
            -LaunchArguments "--showcase-tab=0 --showcase-state=classic"

    }

    Capture-ShowcaseState -ProcessId $wpfProcess.Id -Prefix "wpf" -State "Toolbars" -SelectState
    Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "Toolbars" -SelectState
    Capture-OpenSurface -ProcessId $wpfProcess.Id -Selector "comboBoxFontSize" -Prefix "wpf" -State "ComboBoxPopup" -ExpandCollapse -VerifyExpanded
    Set-ExpandCollapseState -ProcessId $wpfProcess.Id -AutomationId "comboBoxFontSize" -Expand $false
    Start-Sleep -Milliseconds 250

    Capture-OpenSurface -ProcessId $wpfProcess.Id -Selector "Backstage" -Prefix "wpf" -State "Backstage"
    Capture-OpenSurface -ProcessId $winUiProcessId -Selector "Open Backstage" -Prefix "winui" -State "Backstage"

    Stop-Process -Id $winUiProcessId
    Wait-Process -Id $winUiProcessId -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    $winUiProcessId = Get-WinUiProcessId `
        -OutputPath $winUiOutput `
        -LaunchArguments "--showcase-tab=0 --showcase-state=classic"
    Set-ComparisonWindowSize -WindowHandle (Wait-ForMainWindow -ProcessId $winUiProcessId)
    Start-Sleep -Seconds 1
    Capture-ShowcaseState -ProcessId $winUiProcessId -Prefix "winui" -State "Toolbars" -SelectState
    Capture-OpenSurface `
        -ProcessId $winUiProcessId `
        -Selector "fontSizeCombo" `
        -Prefix "winui" `
        -State "ComboBoxPopup" `
        -ExpandCollapse `
        -VerifyExpanded

    Write-Host "Visual parity screenshots written to $OutputDirectory"
}
finally {
    if ($wpfProcess -and -not $wpfProcess.HasExited) {
        Stop-Process -Id $wpfProcess.Id
    }

    if ($winUiProcessId -and (Get-Process -Id $winUiProcessId -ErrorAction SilentlyContinue)) {
        Stop-Process -Id $winUiProcessId
    }

    Pop-Location
}
