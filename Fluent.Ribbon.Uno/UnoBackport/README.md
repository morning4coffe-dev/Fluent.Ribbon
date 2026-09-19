# Pinned Uno accessibility backport (opt-in, not qualified)

This is a reproducible source/package integration, **not a released dependency
or a platform qualification pass**. Normal Fluent.Ribbon builds continue to use
Uno.Sdk `6.6.42` and its implicit Uno runtime `6.6.184`.

| Input | Pin |
| --- | --- |
| Upstream | <https://github.com/unoplatform/uno> |
| Source commit | `c29204c9b2cb5798dbdc023b23a25b3f6d87c600` |
| Source build SDK | .NET `10.0.102` |
| Private package version | `6.6.185-fluent-a11y.7.<platform>` |
| Candidate build SDK | source-built `Uno.Sdk.Private`, matching the private runtime |
| Supported opt-ins | `desktop`, `browserwasm`, `android`, `ios` |

The next-patch prerelease identity sorts above the released `6.6.184`
dependencies without impersonating that release. Platform suffixes prevent
different platform subsets from sharing a NuGet identity. Nothing is published.
Uno's Apache license and third-party/MIT notices are retained beside the patches.

The released SDK's `UNOB0004`/`UNOB0005` version checks intentionally reject a
different runtime identity. They are **not disabled or replaced**. The official
`src/Uno.Sdk/Uno.Sdk.csproj` builds a private SDK with matching checks instead.
`sdk-manifest.json` pins the source build-input patch and the dependency manifest
extracted from the hashed, released `Uno.Sdk.6.6.42.nupkg`. Only the backported
runtime IDs move to the private Core group; every other package version and
framework override is retained. Rebuilding the upstream development manifest
unchanged would introduce unrelated development-package upgrades.

## Scope and remaining gates

The browser patch seeds **realized** ListView/ItemsRepeater peers, fixes built-in
TabView/tablist and SplitButton roles, and revokes container subscriptions.
Deferred DOM updates retain container generations, node incarnations, ordering,
geometry, selection, and the latest item count. Two additional regressions cover
stale counts and removal of a replacement node.
The `.5` candidate also projects custom selectors' realized logical item peers
instead of assuming each visual has its own peer. Selection invokes the
exported provider, and template actions remain exposed outside the listbox's
option-only subtree. Data sources are not scanned or forcibly realized.

The native patch resolves advertised `GetPattern` providers for activation and
Android node state. UIKit named containers expose their real child `UIView`
owners through generated Objective-C selectors. Native visibility/attachment
is checked using UIKit, not the Skia-only shared `IsOffscreen` implementation.

The own-container lifecycle patch keeps application-owned local or bound
`DataContext` values when `ItemsControl.CleanUpContainer` handles authored
containers. Generated containers still receive explicit local null **last**,
preserving the inherited-context protection for
[unoplatform/uno#12845](https://github.com/unoplatform/uno/issues/12845).
Regressions cover local/bound cleanup, generated-container null precedence, and
three real TabView unload/reload cycles without replacing tab/content/selection
identities or DataContext bindings.

The patches do **not** fabricate accessible names, hide visible controls, or
waive axe rules. Remaining browser families include unnamed commands/inputs,
custom menu/list composite hierarchy, roleless text labels, and unsupported
Thumb/range or nested-template semantics. UIKit selector registration, container
navigation/focus, VoiceOver behavior, and Android conditional providers still
require real platform execution. Default mobile framework peer child traversal
also remains outside this targeted named-container correction.

`Given_WebAssemblySemanticContainers.cs` and `Given_NativeAutomationBridge.cs`
are upstream runtime regression fixtures in the patches. They are not run by
the pure JavaScript or build-wiring tests. A build, a successful patch application,
or their mere presence does not count as a runtime pass.

## Build and verify

Use PowerShell 7, Python 3.10+, Node.js 22.13+ (24 recommended), Git, and the
pinned .NET SDK. Android needs the matching .NET Android workload; UIKit needs
macOS, Xcode and the matching iOS workload. Inspect free disk space before
fetching/building Uno; its source, dependencies and outputs occupy several GB.
All commands below start at the Fluent.Ribbon repository root.

```powershell
# Local contracts, including actual MSBuild target-ordering probes (no UI).
python -m unittest discover -s .\Fluent.Ribbon.Uno\UnoBackport\tests -p 'test_*.py' -v

# Fetch the exact source, check/apply the hashed patches, run the pure TS tests.
.\Fluent.Ribbon.Uno\UnoBackport\Build.ps1 -Platform browserwasm -PrepareOnly

# Compile and package the browser candidate. Android/iOS use their platform names.
.\Fluent.Ribbon.Uno\UnoBackport\Build.ps1 -Platform browserwasm -CompileRuntimeTests
```

`-BuildOnly` stops after source compilation; it does not create an activatable
feed. `-CompileRuntimeTests` also compiles the upstream platform test project;
CI enables it, but compilation is not test execution. `-RestoreConfigFile`
selects an explicit dependency source configuration.
`-RestoreFallbackPackages` can read exact **unchanged** dependencies from an
existing cache; it is never used for consumer/backport runtime resolution and
does not enable Uno's cache-replacement targets.

`Build.ps1` serializes its lane with an exclusive lock. It uses Uno's official
source projects and `PrepareNuGetPackage` target, followed by .NET/NuGet packing.
The latter target operates on packaging-only copies. The official core nuspec
is restricted to the chosen platform; the browser package retains both the
`lib/net10.0` reference surface and the `uno-runtime/net10.0/skia` implementation.
Build tools, themes and runtime assemblies come from that source build, not
assembly replacements in an existing package.

Outputs remain under `artifacts\UnoBackport`:

- `source`: verified upstream checkout with the patches applied;
- `source-packages`, `http-cache`: isolated source restore data;
- `feed\<platform>`: private packages, hash receipt and exact-ID source mapping;
- `sdk-feed\<platform>` and `sdk\<platform>\<version>`: the private SDK package
  and verified local SDK imports, with a separate hash receipt;
- `consumer-packages\<platform>`: separate consumer restore cache;
- `consumer\<platform>\bin` and `obj`: isolated application/project outputs;
- `logs\<platform>`: command logs and a result explicitly marked unqualified.

An existing complete feed is verified, not overwritten. An incomplete feed
cannot activate. Preserve failure evidence and use another `-ArtifactRoot`
below `artifacts\UnoBackport` when retrying an incomplete feed. No script performs
broad cleanup, modifies the global cache, publishes packages, or pushes changes.

## Consume only for qualification

After the matching feed has been built:

```powershell
dotnet build .\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase.csproj `
    -c Release -f net10.0-browserwasm -p:TargetFrameworks=net10.0-browserwasm `
    -p:FluentUnoBackport=browserwasm
```

For another artifact root, also pass its absolute path with
`-p:FluentUnoBackportRoot=...`. Mobile uses `net10.0-android` / `android` or
`net10.0-ios` / `ios`; supply the ordinary emulator/simulator RID and packaging
properties required by the existing native test runners.

Desktop Skia is an explicit profile, not native WinUI:

```powershell
.\Fluent.Ribbon.Uno\UnoBackport\Build.ps1 -Platform desktop
dotnet build .\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase.csproj `
    -c Release -f net10.0-desktop -p:TargetFrameworks=net10.0-desktop -p:FluentUnoBackport=desktop
```

It packages the reference assemblies and source-built Skia implementation under
the distinct `.desktop` identity. Platform hosts retain their pinned released
versions. The executable is under
`artifacts\UnoBackport\consumer\desktop\bin\Fluent.Ribbon.Uno.Showcase\Release\net10.0-desktop`.
Neither the build script nor the opt-in Desktop CI job launches the app.

Project SDK imports use the ordinary `Uno.Sdk` resolution when the opt-in is
empty, and the verified private SDK when it is set. They do not modify the
global SDK/package cache. Only old **generated files** in project-local `bin`
and `obj` are excluded when using isolated output paths; those directories
are not deleted or rewritten.

The exact-version hooks run **after `UnoImplicitPackages`**, not by changing root
central package versions or bypassing `UnoSdkVersionCheck`. Only `Uno.WinUI` and, on browser
heads, `Uno.WinUI.Runtime.Skia.WebAssembly.Browser` change. Other packages retain
their released versions. Exact version ranges, feed/package hashes, runtime
entries, source-build hashes, extracted assemblies, SDK files, and resolved
consumer caches are checked.
Missing packages, released-package fallback, mixed-platform builds and stale
receipts fail closed. Packing Fluent.Ribbon with the unqualified opt-in is
explicitly rejected.

The existing `fluent-ribbon-uno.yml` workflow has an optional `uno-backport`
dispatch input, default **false**. When true, it builds each candidate before
the existing Playwright/axe, tablet Android hierarchy/crash, and iOS XCTest
gates. Their assertions and execution remain unchanged. Native Windows and
ordinary Desktop jobs do not use these platform-subset packages; the separate
opt-in Desktop job only builds an isolated executable. Contract checks alone
cannot promote the candidate or certify an unexecuted platform.

For upstream managed fixtures, use Uno's runtime-test harness with filters
`Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.Given_WebAssemblySemanticContainers`
or `Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.Given_NativeAutomationBridge`.
The pinned source's runtime-test documentation describes the real SamplesApp
hosts and base64 filter transport. Keep `UnoNugetOverrideVersion` empty and use
the isolated source cache. A fixture compile is separate from its execution.

## Revising the patch

Edit a verified source checkout, regenerate only the relevant patch with
`git diff --binary`, and explicitly run
`python .\Fluent.Ribbon.Uno\UnoBackport\backport.py refresh-hashes`.
Review the changed source hashes and increment the private version in
`backport.json` and `Version.props` before reusing
the integration. Build/verify commands never accept new checksums automatically.
Promotion requires recorded passes of the real platform gates, review of the
remaining families above, and an explicit dependency/default-policy change.

## Verification recorded on 2026-09-17

The earlier `fluent-a11y.2.browserwasm` candidate was built from the pinned source,
including the private SDK, core/browser packages, and the managed
`Uno.UI.RuntimeTests.Skia` regression project. The WPF-style XAML fixture and
Showcase compiled against it. Deployed `Uno.UI.dll` and browser-exporter DLL
hashes matched the source-built package contents.

The 28 integration/provenance contracts, eight pure TypeScript tests, and
existing browser-source/Android-runner contracts passed. The **actual unchanged
Playwright run executed all four tests: two passed and two failed**. Representative
semantics, customization names and split-button roles passed. Interaction still
failed because the Insert gallery exposed no option; axe still found
`aria-prohibited-attr`, `aria-required-attr`, `aria-required-children`,
`aria-required-parent`, `button-name`, and `label` violations. None were waived.

Managed fixture compilation is not fixture execution. Android/iOS source builds,
device/simulator execution and VoiceOver qualification were not performed in
this local pass. Their CI source/runtime gates remain enabled. The candidate
therefore remains opt-in and must not be the shipped default.

The `.3` candidate adds the own-container lifecycle patch and must be identified
separately from that `.2` runtime evidence.

For that lifecycle fix, the pinned Uno headless unit-test target (`net9.0`)
reproduced both authored local/bound DataContext failures before the change,
while the generated-container null-cleanup check passed. After the change,
all three focused cases and the complete 21-test ItemsControl class passed
with zero skips. The opt-in browser CI lane executes the three focused cases
and verifies their TRX execution counts. The TabView local/bound reload tests
are separate real-UI runtime fixtures; compilation alone does not qualify them.
The `.3.browserwasm` source/private-SDK packages and the managed TabView
regression project compiled successfully. Actual TabView reload fixture
execution and `.3` browser/mobile runtime qualification remain open; no
consumer regression assertions were changed.

### Bounded Desktop/browser pass

The `.5.desktop` source core/private SDK and isolated Desktop Showcase built
successfully. Its deployed `Uno.UI.dll` hash matched the source-built Skia
package. The executable was **not launched**; authored-ribbon reload and
Desktop UI qualification remain for the coordinator/independent QA.

The `.5.browserwasm` source packages, logical-selection regression fixture and
Showcase compiled. All 31 integration contracts and eight pure TypeScript
tests passed. The single post-change **unchanged** browser suite executed four
tests: two passed, two failed, zero skipped. The separate direct gallery check
also still found no Insert gallery option. Consequently, the logical-peer
projection change is an **unqualified candidate, not a verified gallery fix**.
The six axe families listed above remain. No source/SDK identity guard or axe
assertion was disabled, and no additional axe redesign was attempted.

### Reviewed geometry correction

The `.6` revision is limited to realized-item geometry. Creation, ordinary
position updates, and realized-container refresh now share a managed
transform-aware rectangle calculation. Refresh uses the actual semantic
container, including transformed width/height, instead of overwriting a scaled
or rotated node with raw layout dimensions.

The managed fixture covers an 80x24 tab scaled to 160x48, its 90-degree rotated
48x160 bounds, an intervening translated visual panel, a transformed semantic
parent, and refresh without node replacement. It computes expectations through
the real `TransformToVisual(...).TransformBounds(...)` API. That fixture
compiles; its real-UI execution remains a separate qualification step.
All 32 integration/wiring contracts and 10 pure TypeScript tests pass.
No new gallery/axe iteration is part of this revision.

The coordinator's actual `.5.desktop` 17-case cumulative parity pass is
preserved with its binary/runtime under `artifacts\UnoBackport\history\v5`.
That evidence belongs to `.5`, not the rebuilt `.6` candidate. The backport
remains opt-in; no default promotion or publication has occurred.

The `.6` Desktop and browser source/private-SDK packages and Showcase consumers
also built successfully. Independent review accepted the geometry correction;
the coordinator reran all 32 integration contracts and 10 TypeScript tests.
The managed transformed-item fixture remains compile-only evidence.

Actual `.6` browser execution remains two passed and two failed, with zero
skipped tests: Insert gallery options and the six axe families above are still
unresolved. The `.6` Desktop cumulative run completed all 17 cases, but failed
`wpf-keytip-scopes` and `ribbon-inert-options` on presentation readiness.
That run was concurrent with browser qualification; this does not establish
the cause of either failure. A serialized repeat was blocked by the locked
desktop. Do not promote the earlier `.5` pass to `.6` qualification or waive
the remaining runtime assertions.

### Reviewed mobile selection correction

The `.7` revision retains advertised selection providers as the authoritative
state source, with a direct `SelectorItem.IsSelected` fallback for built-in
containers whose peers do not advertise `SelectionItem`. This preserves
Android's selected node state and UIKit's selected trait without querying an
unadvertised provider. The native bridge fixture now checks an ordinary
`ListViewItem` through unselected, selected, and unselected states.
Earlier candidate evidence does not qualify these rebuilt mobile packages.

The `.7` Android source/private-SDK package, native regression fixtures, and
self-contained x86_64 Debug and Release Showcase APKs compiled. A fresh full
review and a focused re-review accepted the selection-state correction.
The `.7.desktop` source/private-SDK package and Showcase also built, and the
serialized, unlocked Desktop run passed all 17 cumulative parity cases with
exit code zero.

Actual Android verification was attempted on isolated hardware-accelerated
API 36 and API 34 tablet emulators. The application rendered, but Android
system/System UI ANR dialogs prevented the unchanged foreground/accessibility
gate from passing. Reboots, alternate renderer/resource settings, a supported
older image, and a production Release APK did not produce a passing run.
The lower-resolution API 34 device retained the same 1280x800 logical tablet
viewport. Assertions and runtime timeouts were not relaxed. These attempts
remain failures, not mobile qualification; the built-in selector fixture is
compile-only evidence until it executes in a native test host.
No browser or UIKit runtime pass is inferred from the `.7` Desktop result.
