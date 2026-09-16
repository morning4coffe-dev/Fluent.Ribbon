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
.\Validate.ps1                         # builds, API report, contracts, XAML, cumulative parity + UI smoke
.\Validate.ps1 -PortParityPhase 3      # parity cases through phase 3, plus existing UI smoke
.\Validate.ps1 -ApiMode enforce       # reject unapproved metadata compatibility gaps
.\Validate.ps1 -SkipUi                # same non-UI validation without launching the Showcase
.\ValidatePackages.ps1                # packs Core + facade and builds a package-only consumer
```

`PortParityPhase` selects a cumulative runtime gate: API/data contracts (1),
input/application state (2), QAT/popups (3), and presentation/configuration (4,
the default). These gates exercise real controls; metadata comparison alone
does not cover inherited framework APIs, rendered templates, or input behavior.
Each run requires every registered case to complete and rejects crashes or
diagnostic failures.

The out-of-process tests normally launch the built Desktop Showcase. Set
`SHOWCASE_AUTOTEST_APP` to a freshly built unpackaged WinUI Showcase executable
to run the same native regressions against WinUI instead. This override does
not build the executable; build the matching target first.

For focused presentation/configuration QA, run the `DesktopPresentationContracts`
test (and use the same executable override for WinUI). It covers both phase-4
findings without claiming that earlier phases passed. Direct launches can use
`--port-parity-phase=4 --port-parity-only-phase=4`; focused runs have a distinct
`PORT-PARITY FOCUSED COMPLETE` marker and cannot satisfy the cumulative gate.

An interactive driver can also launch the Showcase directly, including through
a Windows shortcut, without changing the user's environment:

```powershell
.\Fluent.Ribbon.Uno.Showcase.exe --autotest=1 --port-parity-phase=3 `
    --autotest-exit=1 --autotest-log="C:\QA\phase3.log"
```

These switches configure only the Showcase process. Existing environment
variables take precedence. `--native-popup-external-input=1` enables the native
outside-pointer rendezvous when an approved input driver and a connected,
unlocked desktop are available; it does not generate pointer input itself.
An accessibility Invoke action is not a physical pointer click.
In external-input mode, the popup-options case runs first so the two physical
clicks can be completed promptly. Every other selected case still runs afterward
in its original order; the default automated order is unchanged.
For a human-assisted run, `--native-popup-input-timeout=900` allows up to 15 minutes
to respond at each pointer stage. This changes only the external response deadline,
not layout settling, pointer requirements, or control behavior; the default is
120 seconds and values outside 1–1800 seconds are rejected.

For repeatable WPF/WinUI visual comparisons on Windows, run:

```powershell
.\CompareShowcases.ps1
```

The script builds both Showcases, resizes them to the same physical dimensions,
and writes matching Toolbars, Insert, Galleries, Resizing, ComboBox popup, and
Backstage screenshots under `artifacts\visual-parity`.
Use `-CaptureHighContrast` while a Windows Contrast Theme is active for a
separate High Contrast-only capture; normal baselines require Contrast Themes
to be disabled.

The Showcase diagnostic options also work as WebAssembly query parameters. This
allows deterministic browser baselines without UI automation, for example:

```text
http://localhost:5000/?showcase-tab=0&showcase-state=dark
http://localhost:5000/?showcase-tab=0&showcase-state=rtl
http://localhost:5000/?showcase-tab=0&showcase-state=simplified
http://localhost:5000/?showcase-tab=5&showcase-state=touch
```

The GitHub Actions matrix builds the controls, compatibility facade, WPF-style
XAML fixture, and Showcase for WinUI, Desktop/Skia on Windows/Linux/macOS,
WebAssembly, Android, and iOS. Desktop additionally runs the contract, API, and
diagnostic-option tests, cumulative phase-4 parity, and out-of-process UI smoke.
WinUI also runs the focused presentation/configuration contracts; this is not
approval of the separate native pointer/lifetime gate.

The Android accessibility job requires hardware acceleration. Its Linux runner
grants the current runner group read/write access to `/dev/kvm` and checks that
access before starting the emulator; it does not fall back to slow software
emulation or extend accessibility-test timeouts.
The full-ribbon Android hierarchy contract runs on a tablet viewport, where its
Clipboard and Font commands are expanded. A phone viewport legitimately reduces
those groups to popup buttons; the tablet gate does not certify phone-layout
accessibility.

### Native validation limitations

Desktop and reference-target passes do not certify native WinUI input or
mobile-platform execution. Full native cumulative qualification still requires
the real outside-pointer stages and repeatable native lifetime collection.
Those checks remain explicit failures when input is unavailable; focused
phase-4 validation does not waive them.

The strict WPF metadata exception ledger is validated against the Desktop
assemblies. Native WinUI has additional framework-shape differences, including
the sealed `ScrollViewer` wrapper and selector projections; a Desktop report of
zero unapproved gaps must not be reported as an equivalent native result.

## Differences from WPF Fluent.Ribbon

This Uno Platform version is a **reimplementation** of the WPF library. Most controls
are present, but several advanced behaviours are intentionally simplified:

| Feature | WPF Version | Uno Version |
|---------|-------------|-------------|
| Window Chrome | ControlzEx WindowChrome | Not supported (use platform windowing) |
| Quick Access Toolbar | Add/remove from controls, customization menu, state persistence (`IQuickAccessItemProvider`) | Provider-backed add/remove, overflow, customization, checked-item persistence, and WPF-compatible provider clones |
| Backstage / App Menu | Adorner overlay with open/close animations | Portable overlay with opt-out fade transitions; no WPF `AdornerLayer` dependency |
| KeyTips | Full Alt-key navigation tree | Alt/F10 navigation, focus restoration, and nested Backstage scopes implemented |
| Galleries | Grouping, filtering, live hover preview | Source-model selection, grouping, filtering and preview; native galleries keep one source-owned `ListBox` generator |
| ComboBox popup | WPF-sized long list with optional `TopPopupContent` | Long-list sizing, top content, selection/item automation, and editable value automation are implemented; placement and open/close animation remain platform-native |
| ColorGallery | Standard/theme/recent colors + custom-color dialog | Standard/theme/recent colors with injectable cross-platform picker and WinUI `ContentDialog` fallback |
| ScreenTip | Rich tooltip + F1 help hook | `Title`/`Text`/`DisableReason` with F1 help integration |
| Spinner | `TextToValueConverter`, full validation | Original-text conversion on Enter/focus loss, range coercion, formatting and Escape cancellation |
| Theming | Theme generator (many themes) | Light / Dark / High Contrast via WinUI `ThemeDictionaries` |

### Quick access provider content

Menu providers choose the same action shape as WPF when the copy is created:
`Button`, `RibbonToggleButton`, `DropDownButton`, or `RibbonSplitButton`.
The `Ribbon*` names are the public core counterparts of the facade controls.
Primary actions forward to the original command and event exactly once; a visible
copy continues observing command availability when its original tab is unloaded.

Drop-downs, split menus, groups, and in-ribbon galleries retain their original
interactive content, data containers, templates, and selection. Managed copies
borrow the provider's whole content host rather than copying child controls or
displaying text substitutes. Native dropdown and menu copies instead reanchor
the source's own template popup, keeping its presenter in the source template.
Groups have an independent `QuickAccess` state;
opening the copy does not require collapsing or selecting the source group.
Closing, unloading, or removing a copy returns its content after native unload
completion. Reinserting the same copy reconnects its live property bindings.
Source collection observation is reference-counted while a presentation needs
it. Cancellation and failed preparation release their leases; the last release
restores the unloaded source's normal callback and polling-timer cleanup.

For independently rendered headers and icons, prefer data values with templates,
image sources, icon sources, or the supported native icon/text elements. A visual
presentation that cannot be duplicated safely raises `NotSupportedException`
instead of becoming a noninteractive text placeholder. This does not restrict
arbitrary interactive elements inside the borrowed drop-down content.

## Modern extensions (beyond WPF)

Modern extensions are additive Fluent.Ribbon.Uno features that go beyond the original WPF Fluent.Ribbon and remain isolated from the core `Fluent` namespace. Consumers opt in by merging `Themes/Modern/Modern.xaml`; coordination lives in [Fluent.Ribbon.Uno.Controls/Modern/README.md](Fluent.Ribbon.Uno.Controls/Modern/README.md).

## Roadmap

Implemented (functional, some simplified — see the table above):

- [x] Ribbon, tabs, groups with size reduction (Large/Medium/Small/Collapsed). All heads reduce groups under space pressure and re-enlarge them symmetrically when the window grows again, following the `ReduceOrder`; resize is debounced so groups resize once the width settles.
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

The 17 audited port gaps have dedicated cumulative runtime regressions. The
intentional framework-specific API exceptions are listed above and enforced by
`ApiCompatibility\exceptions.wpf-only.json`; passing that metadata ledger alone
does not establish runtime or native-platform parity.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

MIT License - see [LICENSE](../License.txt) for details.

## Acknowledgments

- Original [Fluent.Ribbon](https://github.com/fluentribbon/Fluent.Ribbon) project
- [Uno Platform](https://platform.uno/) team
