# NuGet package validation

Run from the repository root:

```powershell
.\Fluent.Ribbon.Uno\ValidatePackages.ps1
```

The command creates local desktop packages, verifies their dependency and file
layout, restores the fixture using only `Fluent.Ribbon.Uno`, and compiles its
WPF-style XAML against the packages.

The fixture is intentionally a library because a reliable desktop UI smoke
requires an initialized platform window and UI thread. The remaining manual
runtime check is:

1. Reference only the generated `Fluent.Ribbon.Uno` package from a desktop Uno app.
2. Merge `ms-appx:///Fluent.Ribbon.Uno/Themes/Generic.xaml` in application resources.
3. Put `PackageFixture` in an activated `Window`.
4. On that window's UI thread, call `CompatibilityRuntimeSmoke.Run(rootPanel)`.
5. Verify `Succeeded` is `true` and inspect `Failures` otherwise.
