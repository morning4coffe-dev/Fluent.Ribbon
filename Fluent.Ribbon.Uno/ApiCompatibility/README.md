# Fluent.Ribbon API compatibility baseline

This directory contains a metadata-only API comparison tool. It reads ECMA-335
metadata directly, so the WPF and Uno assemblies and their runtime dependencies
do not have to be loaded into the tool process.

The WPF `Fluent.dll` assembly is the reference. Public and protected symbols in
`Fluent` namespaces are compared with one or more candidate assemblies.
Reviewed framework substitutions (`System.Windows.*` to WinUI or
`Windows.Foundation`) are normalized on the reference side only. Candidate
metadata stays literal, so an accidental WPF framework reference in an Uno
assembly is reported rather than normalized away.

The comparison includes enum and `const` literal values and optional-parameter
default values. Accessibility is strict except for reviewed framework
substitutions in `FrameworkCompatibilityPolicy`; for example, WPF's public
`OnApplyTemplate()` may map to WinUI's protected override contract. This is not
a global public-to-protected relaxation.

## Build and test

From the repository root:

```powershell
dotnet test .\Fluent.Ribbon.Uno\ApiCompatibility\Fluent.Ribbon.ApiCompatibility.Tests\Fluent.Ribbon.ApiCompatibility.Tests.csproj -c Release
```

Build the inputs when needed:

```powershell
dotnet build .\Fluent.Ribbon\Fluent.Ribbon.csproj -c Release -f net8.0-windows
dotnet build .\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Controls\Fluent.Ribbon.Uno.Controls.csproj -c Release -f net10.0-desktop
dotnet build .\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Compatibility\Fluent.Ribbon.Uno.Compatibility.csproj -c Release -f net10.0
```

## Report mode

Report mode prints every gap but returns exit code `0`:

```powershell
dotnet run --project .\Fluent.Ribbon.Uno\ApiCompatibility\Fluent.Ribbon.ApiCompatibility\Fluent.Ribbon.ApiCompatibility.csproj -c Release -- `
  --reference .\bin\Fluent.Ribbon\Release\net8.0-windows\Fluent.dll `
  --candidate .\bin\Fluent.Ribbon.Uno.Controls\Release\net10.0-desktop\Fluent.Ribbon.Uno.dll `
  --candidate .\bin\Fluent.Ribbon.Uno.Compatibility\Release\net10.0\Fluent.Ribbon.Uno.Compatibility.dll `
  --exceptions .\Fluent.Ribbon.Uno\ApiCompatibility\exceptions.wpf-only.json `
  --mode report
```

Repeat `--candidate` to merge public `Fluent` types from split candidate
assemblies. Existing single-candidate commands remain valid. A duplicate public
type name across candidates is rejected as invalid input with exit code `2`.

## Enforce mode

Change the final argument to `--mode enforce`. Enforce mode returns exit code
`1` if any gap is not approved by the ledger. Invalid input or an invalid ledger
returns exit code `2`.

The checked-in `exceptions.wpf-only.json` ledger uses schema version 2. Every
entry requires an exact diagnostic ID, a reason, and a normalized
`detailFingerprint` (`sha256:...`). The fingerprint binds approval to the
specific incompatibility details, so a later, different incompatibility on the
same symbol is not silently approved. A mismatch is reported with the current
fingerprint for review and ledger migration. Schema version 1 remains readable
for CLI compatibility but new approvals should use version 2.

Add entries only for APIs that are intrinsically WPF-only; do not use the
ledger to hide ordinary porting gaps. Stale entries and fingerprint mismatches
are reported so approvals can be reviewed. Reports also group unapproved gaps
by issue category and affected type, ordered by gap count for prioritization.
