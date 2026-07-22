# Compatibility facade runtime

Quick-access clones use direct `DependencyProperty` contracts and property-change
callbacks. No member lookup or string property paths are used, and subclasses of
the facade wrappers inherit the corresponding contract. This keeps QAT behavior
compatible with trimming and AOT.

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
