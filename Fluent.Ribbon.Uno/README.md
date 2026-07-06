# Fluent.Ribbon.Uno

A cross-platform Ribbon UI control library for [Uno Platform](https://platform.uno/), inspired by [Fluent.Ribbon](https://github.com/fluentribbon/Fluent.Ribbon) for WPF.

## Overview

This library provides Office-like Ribbon controls that work across multiple platforms:

- **Windows** (WinUI 3)
- **WebAssembly** (Browser)
- **iOS**
- **Android**
- **macOS**
- **Linux** (via Skia)

## Features

### Core Controls

| Control | Description |
|---------|-------------|
| `Ribbon` | The main container for tabs, quick access toolbar, and application menu |
| `RibbonTab` | A tab containing groups of controls |
| `RibbonGroupBox` | A group of related controls with a header |
| `RibbonButton` | A button with large/medium/small size support |
| `RibbonToggleButton` | A toggle button with size scaling |
| `RibbonSplitButton` | A button with dropdown menu |
| `RibbonSeparator` | A visual separator |

### Control Sizes

All ribbon controls support three sizes that automatically adjust their appearance:

- **Large**: Full icon (32x32) with text below
- **Medium**: Small icon (16x16) with text beside
- **Small**: Small icon only

## Installation

### NuGet (Coming Soon)

```bash
dotnet add package Fluent.Ribbon.Uno
```

### From Source

1. Clone the repository
2. Add a reference to `Fluent.Ribbon.Uno.Controls.csproj`

## Quick Start

### 1. Add the namespace

```xaml
xmlns:fluent="using:Fluent"
```

### 2. Add resources to App.xaml

```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <ResourceDictionary Source="ms-appx:///Fluent.Ribbon.Uno/Themes/Generic.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 3. Use the Ribbon

```xaml
<fluent:Ribbon Title="My Application">
    <fluent:Ribbon.Tabs>
        <fluent:RibbonTab Header="Home">
            <fluent:RibbonTab.Groups>
                <fluent:RibbonGroupBox Header="Clipboard">
                    <fluent:RibbonGroupBox.Items>
                        <fluent:RibbonButton Header="Paste" Size="Large" />
                        <fluent:RibbonButton Header="Cut" Size="Medium" />
                        <fluent:RibbonButton Header="Copy" Size="Medium" />
                    </fluent:RibbonGroupBox.Items>
                </fluent:RibbonGroupBox>
            </fluent:RibbonTab.Groups>
        </fluent:RibbonTab>
    </fluent:Ribbon.Tabs>
</fluent:Ribbon>
```

## Project Structure

```
Fluent.Ribbon.Uno/
├── Fluent.Ribbon.Uno.Controls/     # The control library
│   ├── Controls/                    # Control implementations
│   ├── Converters/                  # Value converters
│   ├── Enumerations/                # Enums (RibbonControlSize, etc.)
│   ├── Helpers/                     # Helper classes
│   └── Themes/                      # XAML styles and templates
│
├── Fluent.Ribbon.Uno.Showcase/      # Demo application
│   └── Fluent.Ribbon.Uno.Showcase/  # Main app project
│
└── Fluent.Ribbon.Uno.sln            # Solution file
```

## Building

### Prerequisites

- **.NET 10 SDK** or later
- Uno Platform workload: `dotnet workload install uno`
- **For the Windows / WinUI 3 head only:** Visual Studio 2022 (or the standalone
  [Build Tools for Visual Studio](https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022))
  with the **Windows 10/11 SDK** (10.0.26100). A full `MSBuild.exe` is required —
  see the note below.

### Build Commands

For non-Windows targets (Desktop/Skia, WebAssembly, Android, iOS) you can use the
.NET CLI directly:

```bash
# Restore packages
dotnet restore

# Build the Desktop (Skia) head — works on Windows, Linux and macOS
dotnet build -f net10.0-desktop

# Run the showcase (Desktop)
dotnet run --project Fluent.Ribbon.Uno.Showcase/Fluent.Ribbon.Uno.Showcase -f net10.0-desktop
```

#### Windows / WinUI 3 target

Building the `net10.0-windows10.0.26100` (WinUI 3) head with `dotnet build` is **not
supported** — Uno stops it with error `UNOB0008` because the WinUI XAML compiler is a
.NET Framework tool that must run under a full `MSBuild.exe`. Build it with `msbuild`
from a *Developer Command Prompt for VS 2022* instead:

```powershell
# Controls library
msbuild Fluent.Ribbon.Uno.Controls\Fluent.Ribbon.Uno.Controls.csproj /r /t:Build /p:TargetFramework=net10.0-windows10.0.26100

# Showcase app
msbuild Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase.csproj /r /t:Build /p:TargetFramework=net10.0-windows10.0.26100
```

In CI, use a `windows-latest` runner (which ships with the Windows SDK) together with
[`microsoft/setup-msbuild`](https://github.com/microsoft/setup-msbuild) and invoke
`msbuild` as shown above.

## Differences from WPF Fluent.Ribbon

This Uno Platform version has some differences from the original WPF library:

| Feature | WPF Version | Uno Version |
|---------|-------------|-------------|
| Window Chrome | ControlzEx WindowChrome | Not supported (use platform windowing) |
| Backstage | Full backstage support | Simplified menu |
| KeyTips | Full keyboard navigation | Basic support |
| Theming | Theme generator | Uses WinUI theming |
| Gallery | Full gallery with grouping | Simplified version |

## Roadmap

- [ ] Quick Access Toolbar
- [ ] Backstage/Application Menu
- [ ] Gallery control
- [ ] ScreenTip (enhanced tooltips)
- [ ] KeyTip keyboard navigation
- [ ] Contextual tabs
- [ ] Minimized ribbon state
- [ ] Simplified ribbon mode
- [ ] Theme customization

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License - see [LICENSE](../License.txt) for details.

## Acknowledgments

- Original [Fluent.Ribbon](https://github.com/fluentribbon/Fluent.Ribbon) project
- [Uno Platform](https://platform.uno/) team
