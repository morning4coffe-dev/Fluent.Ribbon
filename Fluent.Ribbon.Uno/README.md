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
- **Medium** (`Middle` in the WPF-compatible API): Small icon (16x16) with text beside
- **Small**: Small icon only

## Installation

### NuGet (Coming Soon)

```bash
dotnet add package Fluent.Ribbon.Uno
```

### From Source

1. Clone the repository
2. Add a reference to `Fluent.Ribbon.Uno.Controls.csproj`
3. For WPF type names and XAML compatibility, also reference
   `Fluent.Ribbon.Uno.Compatibility.csproj`

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
├── Fluent.Ribbon.Uno.Compatibility/ # WPF type-name and member compatibility facade
├── ApiCompatibility/                # Metadata comparison tool and exception ledger
├── Fluent.Ribbon.Uno.Showcase/      # Demo application
│   └── Fluent.Ribbon.Uno.Showcase/  # Main app project
│
└── Fluent.Ribbon.Uno.sln            # Solution file
```

## WPF-compatible API

The core assembly keeps the additive Uno-native `Ribbon*` control names. The
`Fluent.Ribbon.Uno.Compatibility` assembly restores WPF names such as
`Fluent.Button`, `Fluent.ToggleButton`, and `Fluent.RibbonTabItem` without
shadowing WinUI controls inside the core implementation. Both assemblies use the
same `xmlns:fluent="using:Fluent"` XAML namespace.

When building from source, reference both projects for WPF-style XAML. The
`Fluent.Ribbon.Uno.Compatibility.XamlTests` project is a compile-time fixture for
that contract. `ApiCompatibility` compares the WPF and Uno metadata and keeps
intrinsically WPF-only exceptions explicit. See [MIGRATION.md](MIGRATION.md) for
the current type mapping and platform-exception policy.

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

# Build the WPF compatibility facade and XAML fixture
dotnet build Fluent.Ribbon.Uno.Compatibility/Fluent.Ribbon.Uno.Compatibility.csproj -f net10.0-desktop
dotnet build Fluent.Ribbon.Uno.Compatibility.XamlTests/Fluent.Ribbon.Uno.Compatibility.XamlTests.csproj -f net10.0-desktop

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

### Local validation

Run the deterministic validation entry point from the Uno directory:

```powershell
.\Validate.ps1          # builds, API report, contract tests, XAML fixture, Desktop UI smoke
.\Validate.ps1 -SkipUi  # same validation without launching the Showcase
.\ValidatePackages.ps1  # packs Core + facade and builds a package-only consumer
```

For repeatable WPF/WinUI visual comparisons on Windows, run:

```powershell
.\CompareShowcases.ps1
```

The script builds both Showcases, resizes them to the same physical dimensions,
and writes matching Toolbars, Insert, Galleries, Resizing, ComboBox popup, and
Backstage screenshots under `artifacts\visual-parity`.

The GitHub Actions matrix builds the controls, compatibility facade, WPF-style
XAML fixture, and Showcase for WinUI, Desktop/Skia on Windows/Linux/macOS,
WebAssembly, Android, and iOS. Desktop additionally runs the contract, API, and
out-of-process UI smoke tests.

## Differences from WPF Fluent.Ribbon

This Uno Platform version is a **reimplementation** of the WPF library. Most controls
are present, but several advanced behaviours are intentionally simplified:

| Feature | WPF Version | Uno Version |
|---------|-------------|-------------|
| Window Chrome | ControlzEx WindowChrome | Not supported (use platform windowing) |
| Quick Access Toolbar | Add/remove from controls, customization menu, state persistence (`IQuickAccessItemProvider`) | Visual toolbar with overflow and add/remove support for provider controls; item persistence is still incomplete |
| Backstage / App Menu | Adorner overlay with open/close animations | Implemented, simplified (no adorner/animation) |
| KeyTips | Full Alt-key navigation tree | Alt/F10 navigation and activation implemented; WPF focus restoration and all nested scopes are still being hardened |
| Galleries | Grouping, filtering, live hover preview | Implemented for `RibbonGallery` and `InRibbonGallery`; Uno uses a custom control instead of WPF `Selector` inheritance |
| ComboBox popup | WPF-sized long list with optional `TopPopupContent` | Selection/editing and compact list popup are implemented; native WinUI currently constrains the visible viewport, and `TopPopupContent` is not used by the Showcase until its native layout is stable |
| ColorGallery | Standard/theme/recent colors + custom-color dialog | Standard/theme/recent colors with injectable cross-platform picker and WinUI `ContentDialog` fallback |
| ScreenTip | Rich tooltip + F1 help hook | `Title`/`Text`/`DisableReason` with F1 help integration |
| Spinner | `TextToValueConverter`, full validation | Simplified numeric spinner |
| Theming | Theme generator (many themes) | Light / Dark / High Contrast via WinUI `ThemeDictionaries` |

## Modern extensions (beyond WPF)

Modern extensions are additive Fluent.Ribbon.Uno features that go beyond the original WPF Fluent.Ribbon and remain isolated from the core `Fluent` namespace. Consumers opt in by merging `Themes/Modern/Modern.xaml`; coordination lives in [Fluent.Ribbon.Uno.Controls/Modern/README.md](Fluent.Ribbon.Uno.Controls/Modern/README.md).

## Roadmap

Implemented (functional, some simplified — see the table above):

- [x] Ribbon, tabs, groups with size reduction (Large/Medium/Small/Collapsed)
- [x] Buttons: Button, ToggleButton, SplitButton, DropDownButton, Check/Radio
- [x] Quick Access Toolbar (visual toolbar, overflow, add/remove provider controls)
- [x] Backstage / Application Menu (simplified)
- [x] RibbonGallery grouping, filtering, selection, and live preview
- [x] InRibbonGallery filtering, popup sizing/state, selection, live preview, and QAT clone behavior
- [x] ColorGallery standard/theme/recent colors
- [x] ColorGallery custom-color picker accept/cancel flow
- [x] KeyTip Alt/F10 navigation
- [x] ScreenTip with F1 help integration
- [x] Contextual tabs
- [x] Minimized ribbon state
- [x] Simplified ribbon mode
- [x] Status bar, ToolBar, Spinner, ComboBox, TextBox, TwoLineLabel
- [x] Theming (Light / Dark / High Contrast)
- [x] Localization (20 languages)

Planned enhancements (parity with WPF):

- [ ] Quick Access Toolbar state persistence and broader provider coverage
- [ ] KeyTip focus restore and complete Backstage/ApplicationMenu/StartScreen scope routing
- [ ] Backstage open/close animations

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License - see [LICENSE](../License.txt) for details.

## Acknowledgments

- Original [Fluent.Ribbon](https://github.com/fluentribbon/Fluent.Ribbon) project
- [Uno Platform](https://platform.uno/) team
