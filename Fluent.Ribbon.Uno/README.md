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

- .NET 8.0 SDK or later
- Uno Platform workload: `dotnet workload install uno-platform`

### Build Commands

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run showcase (desktop)
dotnet run --project Fluent.Ribbon.Uno.Showcase/Fluent.Ribbon.Uno.Showcase
```

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
