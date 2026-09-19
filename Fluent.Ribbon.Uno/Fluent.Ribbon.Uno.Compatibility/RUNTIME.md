# Compatibility facade runtime

Quick-access clones use direct `DependencyProperty` contracts and property-change
callbacks. No member lookup or string property paths are used, and subclasses of
the facade wrappers inherit the corresponding contract. This keeps QAT behavior
compatible with trimming and AOT.

Managed borrowed quick-access popup content is prepared before showing the flyout and
released before an accepted hide/light-dismiss, while the content is still
rooted. A pending transfer can postpone presentation without raising
`DropDownOpened`; canceled light dismissal retains the borrowed content.
Canonical item observation and child ownership stay active while a clone
presents the body, including when the source control is unloaded.

Native WinUI dropdown/split presentation uses a `Popup`, resize root and canonical
`ItemsPresenter` owned by the source's current `ControlTemplate`. The presenter
is initialized in `PART_ItemsOwner` and moved only to `PART_PopupItemsPanel`
inside that same template's `PART_PopupContentControl`/`PART_ScrollViewer`.
Native Menu QAT copies reanchor the source menu's own template popup; they do
not borrow its canonical presenter into a different control template.
Ordinary reopen reuses the current popup/root. Replacing the source template
retires its popup and handlers, then admits the still-requested active anchor
after the native template setter completes. A never-loaded source can initialize
its default template without being mounted; the temporary owned binding is
released on first real Loading and never replaces a caller template binding.
Native generation remains responsible for removing its visual children;
logical data reconciliation must not detach them during a generator update.
Custom native dropdown/split templates must provide `PART_ItemsOwner`,
`ItemsPresenter`, `PART_Popup`, `PART_PopupContentControl`, `PART_ScrollViewer`,
and `PART_PopupItemsPanel` in the same source template. Default templates also
provide the menu-header, gallery and separator slots.
Uno's managed targets retain the container-view adapter and managed popup host,
rather than using WinUI's different generator reservation/teardown protocol.

The persistent Showcase native lifetime regression is invoked by the existing
`qat-clones` finding case. Real outside-pointer verification is a separate
external-input rendezvous in `inert-popup-properties`: set
`SHOWCASE_NATIVE_POPUP_EXTERNAL_INPUT=1` only when an interactive desktop and an
approved input driver are available. The log identifies the test process,
stage and `NativePopupOutsideDismissTarget` automation ID. The test itself does
not activate/reconnect desktops or inject system input. Without that mode it
completes automated nonpointer contracts and reports the pointer case blocked;
this is not full native gate approval.
The persistent-popup rendezvous waits for a matching pointer press and release
before checking the button's Click and popup state. A held pointer or an
accessibility Invoke alone cannot complete that stage.
Then invoke the target's `Prepare light-dismiss` action to arm the second stage
after the input driver has finished restoring focus. This is setup, not pointer
evidence. The light-dismiss stage requires both a new native content-island
pointer press within the target and popup closure; focus loss or programmatic
closure alone fails the gate.

`InRibbonGallery` copies expose the source's canonical logical `Items` and
source-model `SelectedItem`/`SelectedIndex` from creation, not only while their
popup is open. Selection and filter bindings remain bidirectional while the
copy's binding session is active. Closed copies do not populate another native
items host or take visual item ownership; popup opening borrows the whole source
panel and closing returns it intact. Source data observation is suspended when
the last copy deactivates and reconciled when a retained copy is loaded again.

Gallery automation peers cache non-gallery-item peers by weak item identity.
Live items keep their canonical peers, but the cache does not keep removed
authored items alive for the lifetime of the gallery.
Tab group-sizing cancellation also detaches the dispatcher timer's Tick handler
and invalidates queued sizing work, so retired timers cannot retain a tab or
rewrite an unloaded presentation.

Object icon values can be inspected explicitly:

```csharp
var result = CompatibilityIconAdapter.Convert(icon);
```

`ImageSource`, URI strings, `Uri`, `ImageIconSource`, and `BitmapIconSource`
values are converted to an `ImageSource` where possible. Other `IconSource`
values are preserved as an `IconElement`. Existing `IconElement` and `UIElement`
content is also preserved. Current core ribbon templates expose image-source
slots, so preserved WinUI content is not assigned as `null`; the facade retains
it and reports `PreservedWinUIContent` with an explanatory `Error`. Unknown
objects report `Unsupported`. A wrapper's latest result is available through
`CompatibilityIconAdapter.GetLastResult(wrapper)`.

Applications and the Showcase can run a package-level smoke check after WinUI
resources initialize:

```csharp
var result = CompatibilityRuntimeSmoke.Run(rootPanel);
```

Call it on the UI thread and pass a live panel from the active XAML tree. It
temporarily attaches every facade wrapper, applies templates, creates QAT
clones, and verifies command execution. `Failures` contains independent
diagnostics instead of stopping after the first wrapper.

## Popup options and menu presentation

`DropDownButton.DismissOnClickOutside` controls native light dismissal only.
Explicit close, Escape, and definitive leaf commands still close the appropriate
popup chain. `ClosePopupOnMouseDown` listens inside the dropdown (including
handled pointer input), excludes resize handles, and closes that dropdown after
`Math.Max(100, ClosePopupOnMouseDownDelay)` milliseconds. As in WPF, the property
retains the supplied delay; the minimum is applied when scheduling. Closing,
unloading, or disabling the option cancels a pending close.

Dropdowns, native editable/non-editable combo popups, and submenus use
`ResizeableContentControl`. `ResizeMode.None` hides the handles, `Vertical`
changes height only, and `Both` changes both dimensions. Pointer dragging,
keyboard arrows (Shift accelerates), and the UIA Transform pattern share the
same constraints. Initial and maximum dropdown heights update live; popup
dimensions and submenu placement are bounded by the current viewport.
Temporary popup constraints do not replace the requested size. Relaxing a
constraint restores the accepted user size; explicitly assigning
`DropDownButton.DropDownHeight` again resets it, including an unchanged value.
Submenu close/reopen requests are serialized across native teardown, preserving
the new request and keyboard-focus target when an older close completes late.
Combo selection, editing, top content, and menu footer remain native.
The core menu-footer dependency property drives the actual combo presenter on
both core and facade instances. User resize limits apply to the complete popup,
not only the platform's list-height calculation. Submenu placement uses the
popup's transformed origin in physical coordinates; content direction is
independent, so RTL does not mirror the placement twice.
`ContextMenu` retains its native `MenuFlyoutPresenter` and menu items, wrapping
the existing scrolling content when resizing is requested. Presenter styles
continue to supply its minimum/maximum constraints.

Default menu items show check, indeterminate, and mutually exclusive radio
states. Split items have separate primary and submenu hit targets; an
unavailable primary command does not disable the submenu. The menu automation
peer exposes the corresponding Invoke, ExpandCollapse, Toggle, and Selection
patterns. With `RecognizesAccessKey`, the first unescaped `_` in an ordinary
string header supplies a native access key (`__` displays a literal underscore).
Custom header templates are preserved, and an explicitly assigned native
`AccessKey` is never replaced by generated text.
Generated access keys use a tracked binding rather than string-object identity,
which is not stable across WinRT marshalling. Turning recognition off restores
the original literal header and clears only a generated key; consumer header
and access-key bindings remain active.
