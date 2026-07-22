# Migrating Fluent.Ribbon from WPF to Uno

## Compatibility target

Fluent.Ribbon.Uno preserves the portable WPF `Fluent` type and member names while
using Uno/WinUI framework types. The existing Uno-native `Ribbon*` controls remain
available as additive APIs.

From source, reference both projects:

```xml
<ProjectReference Include="..\Fluent.Ribbon.Uno.Controls\Fluent.Ribbon.Uno.Controls.csproj" />
<ProjectReference Include="..\Fluent.Ribbon.Uno.Compatibility\Fluent.Ribbon.Uno.Compatibility.csproj" />
```

The `Fluent.Ribbon.Uno` NuGet package is the compatibility-facing package and
depends on `Fluent.Ribbon.Uno.Core`, which carries the controls and XAML
resources. Consumers install only the main package:

```powershell
dotnet add package Fluent.Ribbon.Uno
```

The XAML namespace remains:

```xaml
xmlns:fluent="using:Fluent"
```

## Control names

| WPF name | Uno implementation |
|---|---|
| `Fluent.Button` | Compatibility facade over `RibbonButton` |
| `Fluent.ToggleButton` | Compatibility facade over `RibbonToggleButton` |
| `Fluent.CheckBox` | Compatibility facade over `RibbonCheckBox` |
| `Fluent.RadioButton` | Compatibility facade over `RibbonRadioButton` |
| `Fluent.ComboBox` | Compatibility facade over `RibbonComboBox` |
| `Fluent.TextBox` | Compatibility facade over `RibbonTextBox` |
| `Fluent.DropDownButton` | Compatibility facade over `RibbonDropDownButton` |
| `Fluent.SplitButton` | Compatibility facade over `RibbonSplitButton` |
| `Fluent.Gallery` / `GalleryItem` | Compatibility facade over `RibbonGallery` / `RibbonGalleryItem` |
| `Fluent.RibbonTabItem` | Compatibility facade over `RibbonTab` |
| `Fluent.StatusBar` / `StatusBarItem` | Compatibility facade over `RibbonStatusBar` / `RibbonStatusBarItem` |

The compatibility XAML fixture in
`Fluent.Ribbon.Uno.Compatibility.XamlTests` compiles representative WPF-style
markup against every declared Uno target.

For runtime facade diagnostics, icon conversion outcomes, and QAT clone checks,
see [Fluent.Ribbon.Uno.Compatibility/RUNTIME.md](Fluent.Ribbon.Uno.Compatibility/RUNTIME.md).

## Framework substitutions

Common WPF framework types map to their Uno/WinUI equivalents:

- `System.Windows.DependencyObject` to `Microsoft.UI.Xaml.DependencyObject`
- `System.Windows.UIElement` to `Microsoft.UI.Xaml.UIElement`
- `System.Windows.Controls.*` to `Microsoft.UI.Xaml.Controls.*` where an equivalent exists
- `System.Windows.Media.*` to `Microsoft.UI.Xaml.Media.*`
- `System.Windows.Point`, `Rect`, and `Size` to `Windows.Foundation`
- `System.Windows.Media.Color` to `Windows.UI.Color`
- WPF routed commands to `XamlUICommand`

The metadata comparison tool under `ApiCompatibility` owns the complete,
executable normalization list.

## Intentional platform exceptions

The following WPF concepts do not have a source-compatible cross-platform Uno
implementation:

- ControlzEx/WPF window chrome, `RibbonWindow`, and HWND steering helpers
- WPF `AdornerLayer` rendering
- WPF pixel-shader effects
- ControlzEx runtime theme-provider integration
- WPF `ItemContainerGenerator` and internal automation-peer reflection helpers
- WPF Microsoft.Xaml.Behaviors collection types
- Custom WPF routed-event registration; portable services expose direct events and
  methods instead of a WinUI routed-event token

Every approved exception is listed with a reason in
`ApiCompatibility\exceptions.wpf-only.json`. Ordinary porting gaps must not be
added to that ledger.

## Validation

From `Fluent.Ribbon.Uno`:

```powershell
.\Validate.ps1
.\ValidatePackages.ps1
```

Use `-ApiMode enforce` only after the API report contains no unapproved gaps.
The default `report` mode keeps the remaining work visible while allowing
incremental compatibility changes.
