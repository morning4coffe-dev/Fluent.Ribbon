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

## Item binding and gallery selection

`MenuItem`, `StatusBar`, and `RibbonGroupBox` inherit the native `ItemsControl`
binding surface: `ItemsSource`, `ItemTemplate`, `ItemTemplateSelector`,
`ItemContainerStyle`, `ItemContainerStyleSelector`, `DisplayMemberPath`, and
`ItemsPanel`. Generated menu and status containers participate in submenu
ownership and status customization. Source notifications update the displayed
containers, including changes made before the first load or while unloaded
(reconciled on reload).

The existing Uno `Items` API remains an `ObservableCollection<UIElement>` for
directly authored XAML controls and inspecting live containers. Supply models
through `ItemsSource`, or through `((ItemsControl)control).Items` when no source
is assigned. Do not edit the container view while `ItemsSource` is set: edit
the source collection instead. Such direct edits now throw rather than silently
diverging from the source. Native `ItemsControl.Items` retains the source items;
the Fluent control's `ContainerFromItem`, `ContainerFromIndex`,
`ItemFromContainer`, and `IndexFromContainer` methods resolve its live containers.
Call these methods on the Fluent control, not a native `ItemsControl` cast
(WinUI's native lookup methods cannot be overridden). On native WinUI, menus and
galleries also use the source's native generator. A directly authored gallery
visual that is not a `ListBoxItem` is hosted in a native `ListBoxItem` shell;
the Fluent container-view methods still return that original visual. Native
gallery item controls derive from `ListBoxItem` (and therefore `ContentControl`)
so native selector removal can use the required selector-item contract.

Uno's native `ItemCollection` indexer does not emit a vector-change notification
when replacing an item. The portable controls reconcile these silent replacements
at Fluent item/container/selection reads and, while loaded, within a 100 ms UI
dispatcher check, preserving index-slot identity. The check runs only for nonempty
native `Items` views with no `ItemsSource`, performs a nonallocating O(n) scan when
unchanged, and stops on unload, source replacement, or an empty view. Its callback
holds only a weak reference to the control's binding adapter. WinUI and observable
`ItemsSource` notification paths do not use this workaround.
For notification-driven updates, use an observable `ItemsSource` or the Fluent
`Items` collection instead.

`Gallery`/`RibbonGallery.SelectedItem` and `SelectionChanged` payloads now expose
source models, not generated `GalleryItem`/`RibbonGalleryItem` containers.
Directly authored UI elements still select as themselves. Selection survives
source reordering and resets that retain the selected model, and is cleared when
that model is removed. `SelectionChanged` now uses WinUI's
`SelectionChangedEventHandler`; handlers receive `SelectionChangedEventArgs` and
read `AddedItems`/`RemovedItems`, replacing the former Uno-only
`EventHandler<object?>` payload.

## Popup item ownership and custom templates

On native WinUI, dropdowns, split buttons, menus, and galleries use their source
control's item generator. A dropdown or submenu's `Popup` and `ItemsPresenter`
belong to the same source `ControlTemplate`; the presenter is not moved into a
different control's template or an independently owned flyout. The popup does not assign
the same authored UI elements to a second native `ItemsControl`. Native WinUI
can corrupt the retiring visual tree when one item belongs to both native
owners, even after the popup reports `Unloaded`. Do not mirror a source's
`Items` into another native items host or manually detach its generated
containers.

Custom WinUI control templates must preserve the canonical presenter contract used
by the default dropdown, split-button, and menu templates:

```xaml
<Grid x:Name="PART_ItemsOwner"
      Width="0" Height="0"
      Visibility="Collapsed" IsHitTestVisible="False">
    <ItemsPresenter x:Name="ItemsPresenter" />
</Grid>
```

Place this panel inside the source control's template inheritance chain, and
retain an `ItemsPanel` template, such as a `StackPanel`. The control initializes
the presenter there before moving it into that same template's popup. Native
dropdown/split templates also retain `PART_Popup`, `PART_PopupContentControl`,
`PART_ScrollViewer`, and `PART_PopupItemsPanel` from the default template, along
with its header/gallery slots when those features are used. Menu templates retain
the first three popup parts; their scroll viewer hosts the canonical presenter.
Templates without the required parts fail explicitly instead of falling back
to unsafe duplicate ownership.

Ordinary reopen preserves the current popup/root. Replacing a source template
retires that template's popup and admits any pending QAT anchor through the new
template, without replacing original item content. Preparing a never-loaded
source uses an owned temporary default-template binding; it does not mount the
source or overwrite a caller template binding, and normal styling resumes on
the first real load.

Native gallery grouping arranges source-generated items without moving them
between group panels. Group headings use a separate template layer, and filters
change visibility rather than container ownership. Custom gallery templates
retain `ItemsPresenter` and `PART_GroupHeaders` when group headings are needed.

Uno's managed rendering targets retain their container-view adapter and managed
popup items host. Their generator callback ordering and presenter reparenting
differ from WinUI, so WinUI's container reservations are not applied to those
targets. Custom managed menu templates retain `PART_SubmenuItemsHost` as an
`ItemsControl`; the default shared templates select the appropriate host with
Uno's `win` and `not_win` XAML prefixes.

## Input and command availability

Gallery Enter/Space activation is committed once on the matching unhandled
key release. The portable protected down/up handlers are also used by native
event overrides; facade input hooks remain overridable. Pointer, key-tip, UIA,
and facade `RaiseClick` routes share the same command gate.

Menu items, gallery items, and the `RibbonControl` base observe command and
parameter changes. Command disabling is an effective-value constraint, not a
replacement for an authored or bound `IsEnabled` value. Observations detach on
unload and refresh on reload. Split buttons constrain only the primary action:
`IsButtonEnabled` remains the user's permission, while `IsPrimaryActionEnabled`
reports combined primary availability. Their dropdown remains usable unless
the whole control is disabled. Loaded quick-access split clones observe their
own synchronized command and parameter even while the original control is
unloaded; execution is forwarded weakly to the original exactly once.

On Uno, applying an animation value can write back through two-way bindings, and
removing that value can detach the binding. Effective constraints suppress source
updates only during their own writes, restore the original binding when releasing
the native overlay, and retain the latest local/bound intent. Locked backstage
open state uses the same mechanism; effective values are not substituted into
the user's view model.

Native WinUI bindings are immutable once attached. For an automatic two-way
binding, the constraint temporarily installs an equivalent `Explicit`-update
binding for the entire hold, then restores the original binding instance and
latest authored value on release. `GetBindingExpression(...).ParentBinding`
therefore has a different identity during that hold. The held animation is
applied synchronously so direct dependency-property writes cannot temporarily
bypass a locked state.

Spinner editor commits now use one conversion pipeline for Enter and focus loss.
Custom `TextToValueConverter` instances receive the original edited string,
before range coercion or formatting. `Convert` maps text to a number; `ConvertBack`
formats the effective number. Invalid conversion results revert to the formatted
current value, and Escape cancels without parsing. Converter/format/value changes
use that same formatter.

## Editor header presentation

The default ComboBox, TextBox, and Spinner templates render headers through
`ContentPresenter` rather than converting every header to plain text. Set
`HeaderTemplate`, or use `HeaderTemplateSelector` on controls that expose that
property. An explicit template takes precedence over a selector. Changing the
header model, template, or selector updates the rendered header; clearing the
templates restores ordinary text presentation.
Quick-access editor copies retain these templates and selectors and follow live
header-model and presentation changes.

The default presenters select templates without replacing the requested
template/selector values or their consumer bindings. The `HeaderText` part name
is retained for size states and automation labeling; custom control templates
keep their own header bindings.

`TextBox` and `RibbonTextBox` share Fluent's `HeaderTemplate` and
`HeaderTemplateSelector` dependency properties. Native WinUI rejects an ordinary
CLR `Header` model when its own `HeaderTemplate` is null. The port therefore keeps
an explicit template or a real content-presenting fallback in the native slot
while preserving the original `Header` object and binding. Nullable template
bindings must target Fluent's property, not
`Microsoft.UI.Xaml.Controls.TextBox.HeaderTemplateProperty`. Direct native
bindings remain caller-owned and retain that framework limitation.

An authored TextBox template can use `TemplateBinding Header` normally. A native
`TemplateBinding HeaderTemplate` observes the protective projection; use
`Binding HeaderTemplate` and `Binding HeaderTemplateSelector` with
`RelativeSource TemplatedParent` when the template needs the requested values.

## Ribbon display and customization options

`AreTabHeadersVisible` controls the tab-header strip rather than the individual
tabs. Selection and selected group content remain available when headers are
hidden. `IsDisplayOptionsButtonVisible` controls the localized display-options
button; `CanMinimize` and `CanUseSimplified` govern the actions offered there.
Root ribbon options propagate to its tab control without replacing values or
bindings explicitly authored on that tab control.

`IsDefaultContextMenuEnabled` and the QAT customization/location permissions
govern generated user menus, checklist entries, and their commands. Retained
menu commands recheck permissions before changing state. Explicitly authored
context menus and programmatic QAT edits remain available.

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
