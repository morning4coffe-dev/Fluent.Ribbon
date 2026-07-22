# Final facade compatibility pass

The facade forwards portable header templates, definitive click behavior,
typed combo menus, drop-down lifecycle hooks, menu/check/group state,
delegate-based gallery grouping, spinner text conversion, and QAT clone
lifecycle behavior to the current `Ribbon*` controls.

Some WPF presentation capabilities remain metadata-only where the current core
template has no corresponding slot: button-like header template selectors,
combo top-popup presentation, non-light-dismiss drop-downs, and resizable or
split `MenuItem` submenus. Values and bindings are preserved, but the facade
does not claim unsupported rendering behavior.

The focused WPF comparison fell from 139 to 67 gaps. The remaining gaps are
framework-shape contracts rather than portable facade behavior:

- WPF logical-tree and `ItemsControl` container hooks.
- WPF mouse/keyboard/context-menu routed-input overrides.
- WPF routed-event identifiers and `DependencyPropertyKey`.
- WPF `Popup` exposure where the Uno implementation uses `Flyout`.
- Direct WPF base-class relationships that cannot coexist with the current
  `Ribbon*` implementation inheritance.

No placeholder methods or WPF references are added for these contracts.

## Proposed exception fingerprints

All `missing-member` entries below currently have normalized detail:
`The candidate type does not expose this normalized member contract, directly
or through inheritance.` Their shared fingerprint is:

`sha256:d53169444f0fcd402315ca861544690b410cad8fb591749506c012aaac8ec7c5`

### Logical-tree contracts

- `missing-member:P:Fluent.Button::LogicalChildren()`
- `missing-member:P:Fluent.CheckBox::LogicalChildren()`
- `missing-member:P:Fluent.ComboBox::LogicalChildren()`
- `missing-member:P:Fluent.DropDownButton::LogicalChildren()`
- `missing-member:P:Fluent.MenuItem::LogicalChildren()`
- `missing-member:P:Fluent.MenuItem::LogicalParent()`
- `missing-member:P:Fluent.RadioButton::LogicalChildren()`
- `missing-member:P:Fluent.RibbonTabItem::LogicalChildren()`
- `missing-member:P:Fluent.Spinner::LogicalChildren()`
- `missing-member:P:Fluent.SplitButton::LogicalChildren()`
- `missing-member:P:Fluent.TextBox::LogicalChildren()`
- `missing-member:P:Fluent.ToggleButton::LogicalChildren()`

### Item-container and WPF template-selection hooks

- `missing-member:M:Fluent.DropDownButton::GetContainerForItemOverride()`
- `missing-member:M:Fluent.DropDownButton::IsItemItsOwnContainerOverride(System.Object)`
- `missing-member:P:Fluent.DropDownButton::ItemContainerTemplateSelector()`
- `missing-member:P:Fluent.DropDownButton::UsesItemContainerTemplate()`
- `missing-member:M:Fluent.Gallery::GetContainerForItemOverride()`
- `missing-member:M:Fluent.Gallery::IsItemItsOwnContainerOverride(System.Object)`
- `missing-member:M:Fluent.MenuItem::GetContainerForItemOverride()`
- `missing-member:M:Fluent.MenuItem::IsItemItsOwnContainerOverride(System.Object)`

### WPF routed input and context-menu hooks

- `missing-member:M:Fluent.ComboBox::OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)`
- `missing-member:M:Fluent.ComboBox::OnPreviewKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)`
- `missing-member:M:Fluent.DropDownButton::OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)`
- `missing-member:M:Fluent.GalleryItem::OnKeyUp(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)`
- `missing-member:M:Fluent.GalleryItem::OnLostMouseCapture(System.Windows.Input.MouseEventArgs)`
- `missing-member:M:Fluent.GalleryItem::OnMouseEnter(System.Windows.Input.MouseEventArgs)`
- `missing-member:M:Fluent.GalleryItem::OnMouseLeave(System.Windows.Input.MouseEventArgs)`
- `missing-member:M:Fluent.GalleryItem::OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs)`
- `missing-member:M:Fluent.GalleryItem::OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs)`
- `missing-member:M:Fluent.MenuItem::OnContextMenuClosing(Microsoft.UI.Xaml.Controls.ContextMenuEventArgs)`
- `missing-member:M:Fluent.MenuItem::OnContextMenuOpening(Microsoft.UI.Xaml.Controls.ContextMenuEventArgs)`
- `missing-member:M:Fluent.MenuItem::OnIsKeyboardFocusedChanged(Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs)`
- `missing-member:M:Fluent.MenuItem::OnMouseEnter(System.Windows.Input.MouseEventArgs)`
- `missing-member:M:Fluent.MenuItem::OnMouseLeave(System.Windows.Input.MouseEventArgs)`
- `missing-member:M:Fluent.MenuItem::OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs)`
- `missing-member:M:Fluent.MenuItem::OnMouseWheel(System.Windows.Input.MouseWheelEventArgs)`
- `missing-member:M:Fluent.RibbonTabItem::OnGotKeyboardFocus(System.Windows.Input.KeyboardFocusChangedEventArgs)`
- `missing-member:M:Fluent.RibbonTabItem::OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs)`
- `missing-member:M:Fluent.Spinner::OnKeyUp(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)`
- `missing-member:M:Fluent.SplitButton::OnKeyDown(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)`
- `missing-member:M:Fluent.SplitButton::OnPreviewMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs)`
- `missing-member:M:Fluent.TextBox::OnContextMenuClosing(Microsoft.UI.Xaml.Controls.ContextMenuEventArgs)`
- `missing-member:M:Fluent.TextBox::OnContextMenuOpening(Microsoft.UI.Xaml.Controls.ContextMenuEventArgs)`
- `missing-member:M:Fluent.TextBox::OnKeyUp(Microsoft.UI.Xaml.Input.KeyRoutedEventArgs)`

### WPF-only public framework artifacts

- `missing-member:P:Fluent.ComboBox::DropDownPopup()`
- `missing-member:P:Fluent.DropDownButton::DropDownPopup()`
- `missing-member:P:Fluent.MenuItem::DropDownPopup()`
- `missing-member:F:Fluent.Gallery::IsLastItemPropertyKey`
- `missing-member:F:Fluent.GalleryItem::ClickEvent`
- `missing-member:P:Fluent.GalleryItem::IsEnabledCore()`
- `missing-member:F:Fluent.MenuItem::RecognizesAccessKeyProperty`
- `missing-member:M:Fluent.MenuItem::GetRecognizesAccessKey(Microsoft.UI.Xaml.DependencyObject)`
- `missing-member:M:Fluent.MenuItem::SetRecognizesAccessKey(Microsoft.UI.Xaml.DependencyObject,System.Boolean)`
- `missing-member:P:Fluent.MenuItem::RecognizesAccessKey()`
- `missing-member:F:Fluent.SplitButton::CheckedEvent`
- `missing-member:F:Fluent.SplitButton::ClickEvent`
- `missing-member:F:Fluent.SplitButton::IndeterminateEvent`
- `missing-member:F:Fluent.SplitButton::UncheckedEvent`

The routed WPF spinner event contract is also intrinsically incompatible:

- `incompatible-member:E:Fluent.Spinner::ValueChanged`
- fingerprint:
  `sha256:a4b41a0ddb30d3a7110095d29276a79845e50655e545297f4ddb1d106cec17b7`

### Incompatible base shapes

| Exception ID | Fingerprint |
| --- | --- |
| `incompatible-type:Fluent.DropDownButton` | `sha256:2b9f64db29030c021729ed70877aaf4d81883a51620e92e2a938ed8b7ee7e607` |
| `incompatible-type:Fluent.Gallery` | `sha256:08f98d9414817df61d28c9364096f5cac248056df199032eaddbbd22175a11fe` |
| `incompatible-type:Fluent.GalleryItem` | `sha256:90fa8ea64c028672af8736135e8606038b4c09aef3687eddfc31a53045e93b6d` |
| `incompatible-type:Fluent.MenuItem` | `sha256:aee3df75dda4402c960a3efd6a64c4fe6f0f2f2cd882a7748b21969eb81f0e6d` |
| `incompatible-type:Fluent.RibbonTabItem` | `sha256:f14b8a22edc467f3d3b10609aa6df930068e9e98815ee9955d6ff6bd9122c9e4` |
| `incompatible-type:Fluent.Spinner` | `sha256:7540d603bc698c6b59e29e8ef24e2edca9bb416479aacc7932bedb73d91e1736` |
| `incompatible-type:Fluent.SplitButton` | `sha256:076e056eb7ebb5252cea4a5bdb590c6ad3527f26388d93138e29bdda159b9ac3` |
| `incompatible-type:Fluent.StatusBarItem` | `sha256:574986dbe7710f5cfb7f9ee7502d93091346baede8500b6b05aed3ac58c64abd` |
