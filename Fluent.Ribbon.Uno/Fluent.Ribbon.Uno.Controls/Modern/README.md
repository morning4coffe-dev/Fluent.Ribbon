# Fluent.Ribbon.Uno — Modern Extensions (beyond WPF)

Modern extensions are additive Fluent.Ribbon.Uno features that go beyond the original WPF Fluent.Ribbon surface. They must be cleanly isolated so existing `Fluent` consumers remain unaffected unless they explicitly opt in.

## Golden rules

Every contributor must follow this checklist:

- [ ] **Namespace:** every new public modern type lives in `Fluent.Modern` or a sub-namespace (`Fluent.Modern.Controls`, `Fluent.Modern.Commands`, `Fluent.Modern.Icons`, `Fluent.Modern.Media`, `Fluent.Modern.Helpers`). Never add modern types to namespace `Fluent`.
- [ ] **Folders:** all modern library code lives under `Modern\` in the Controls project, mirroring the existing layout. All modern XAML lives under `Themes\Modern\`.
- [ ] **Opt-in styles:** aggregate modern styles in `Themes\Modern\Modern.xaml`. Do not merge it into `Themes\Generic.xaml`; consumers opt in explicitly.
- [ ] **Marker + banner:** every modern public type carries `[ModernExtension]` and includes the `<b>Modern extension</b>` XML-doc banner line. Phase 0 self-marks `ModernExtensionAttribute` with `[ModernExtension]` so the runtime guard is self-consistent.
- [ ] **One-way dependency:** no type in namespace `Fluent` may reference `Fluent.Modern`. Modern may use `Fluent`, never the reverse.

> ⚠️ **Scope discipline (enforced).** Modern work must ONLY add/modify files under `Modern\`, `Themes\Modern\`, the two Showcase `MainPage.Modern*.cs` partials, and this ledger. Do **not** restyle, re-theme, re-tune palettes, or "modernize" the look of any core file (`Themes\*.xaml` outside `Themes\Modern\`, `MainPage.xaml`, core control `.cs`). A prior agent's unauthorized core "Fluent redesign" (palette + template + `MainPage.xaml`) was reverted because it violated this rule and corrupted file encodings. If you believe a core change is genuinely required, STOP and report instead of making it.

## Target structure

```text
Fluent.Ribbon.Uno.Controls\
├── Modern\
│   ├── Controls\              # Fluent.Modern.Controls
│   ├── Commands\              # Fluent.Modern.Commands
│   ├── Icons\                 # Fluent.Modern.Icons
│   ├── Media\                 # Fluent.Modern.Media
│   ├── Helpers\               # Fluent.Modern.Helpers
│   ├── ModernExtensionAttribute.cs
│   └── README.md
└── Themes\
    └── Modern\
        ├── Modern.xaml        # opt-in aggregator
        └── *.xaml             # per-feature dictionaries
```

## How to opt in

```xaml
<ResourceDictionary Source="ms-appx:///Fluent.Ribbon.Uno/Themes/Modern/Modern.xaml" />
```

## Validation

Build the Desktop/Skia head from the Showcase project directory:

```powershell
cd D:\Projects\Fluent.Ribbon\Fluent.Ribbon.Uno\Fluent.Ribbon.Uno.Showcase\Fluent.Ribbon.Uno.Showcase
dotnet build -f net10.0-desktop
```

Run the env-gated Showcase auto-test:

```powershell
$env:SHOWCASE_AUTOTEST = '1'
$env:SHOWCASE_AUTOTEST_EXIT = '1'
$env:SHOWCASE_AUTOTEST_LOG = "$env:TEMP\modern_p0_autotest.log"
dotnet run -f net10.0-desktop
```

The log must contain `COMPLETE` and `MODERN-GUARD PASS`, and must not contain `THREW`, `FATAL`, or `MODERN-GUARD FAIL`.

## Status ledger

| Feature | Phase | Status | Owner | Key files | Notes for next agent |
|---------|-------|--------|-------|-----------|----------------------|
| Foundation scaffold | P0 | ✅ done | Phase 0 agent | `Modern\`, `Themes\Modern\Modern.xaml` | `Modern.xaml` is opt-in only; do not merge it into `Generic.xaml`. |
| ModernExtension marker | P0 | ✅ done | Phase 0 agent | `Modern\ModernExtensionAttribute.cs` | Attribute is self-marked with `[ModernExtension]`; keep future public modern types marked too. |
| Guard check | P0 | ✅ done | Phase 0 agent | Showcase `MainPage.ModernAutoTest.cs`, `MainPage.AutoTest.cs` | Guard reflects over `Fluent.Ribbon.Uno` public `Fluent.Modern*` types and logs pass/fail. |
| Showcase Modern tab + autotest | P0 | ✅ done | Phase 0 agent | Showcase `MainPage.ModernShowcase.cs`, `MainPage.xaml.cs`, `App.xaml` | Add future demos to the Modern tab from modern-specific partials. |
| Docs/ledger | P0 | ✅ done | Phase 0 agent | `Modern\README.md`, root `README.md` | Update this ledger as each phase starts and completes. |
| RibbonInvoker shared helper | P1 | ✅ done | Copilot | `Modern\Commands\RibbonInvoker.cs`, `Modern\README.md` | `Fluent.Modern.Commands.RibbonInvoker` is the shared invoke path; KeyboardAccelerator MUST reuse it instead of duplicating UI Automation/Command fallback logic. |
| Tell-Me command search | P1 | ✅ done | Copilot | `Modern\Commands\RibbonCommandDescriptor.cs`, `Modern\Commands\RibbonCommandCatalog.cs`, `Modern\Controls\RibbonSearchBox.cs`, `Themes\Modern\RibbonSearchBox.xaml`, `Themes\Modern\Modern.xaml`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | Catalog now observes ribbon tabs, tab groups, group items, and recursively nested menu collections; mutations coalesce through the dispatcher, `Search` flushes pending work, and `Dispose`/RibbonSearchBox unload or ribbon replacement removes every subscription. `MODERN-SEARCH-DYNAMIC PASS` covers add/remove and cleanup. |
| First-class IconSource/SVG icons | P1 | ✅ done | Copilot | `Modern\Controls\ModernIconPresenter.cs`, `Modern\Controls\ModernRibbonButton.cs`, `Themes\Modern\ModernIconPresenter.xaml`, `Themes\Modern\ModernRibbonButton.xaml`, `Themes\Modern\Modern.xaml`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, Showcase `Assets\Modern\star.svg`, Showcase `Fluent.Ribbon.Uno.Showcase.csproj`, `Modern\README.md` | Implemented as an opt-in `ModernRibbonButton` subclass to avoid changing core `Fluent` controls or `Generic.xaml`. Reuse `ModernIconPresenter` for any `IconSource` (`FontIconSource`, `PathIconSource`, bitmap/image/SVG, animated sources); SVG is enabled via the Showcase `Svg` UnoFeature so support stays aligned with the current Uno SDK. |
| KeyboardAccelerator | P1 | ✅ done | Copilot | `Modern\Commands\RibbonAccelerator.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | Attached-property gesture parser creates/removes one WinUI `KeyboardAccelerator`, formats `AcceleratorText`, and invokes through shared `RibbonInvoker`; ScreenTip integration appends the shortcut via existing `ScreenTip.Attach` (falling back to tooltip text). Future P4 XYFocus/KeyTip work should coordinate accelerator precedence without adding `Fluent` → `Fluent.Modern` dependencies. |
| Adaptive/visuals: responsive breakpoints | P2 | ✅ done | Copilot | `Modern\Helpers\RibbonAdaptiveBehavior.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | Opt-in attached behavior defaults to 900px simplified / 500px minimized and owns `Ribbon.IsSimplified`/`Ribbon.IsMinimized` while enabled; disabling restores the captured original state. |
| Adaptive/visuals: touch density | P2 | ✅ done | Copilot | `Modern\Helpers\RibbonInputMode.cs`, `Themes\Modern\RibbonTouchDensity.xaml`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | `RibbonInputMode.InputMode=Touch` merges `RibbonTouchDensity.xaml` on the target element only; Mouse removes it. The dictionary overrides available keyed sizes and exposes modern touch keys for demos without changing core controls. |
| Adaptive/visuals: Mica/Acrylic backdrop | P2 | ✅ done | Copilot | `Modern\Media\RibbonBackdrop.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | `RibbonBackdrop.TryApply(Window, kind)` sets Mica/MicaAlt/DesktopAcrylic/None and returns false instead of throwing when unsupported; Mica is Windows-only and is expected to report unsupported on the Skia desktop head. |
| Adaptive/visuals: ThemeShadow | P2 | ✅ done | Copilot | `Modern\Media\RibbonElevation.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | `RibbonElevation.Depth` applies a shared `ThemeShadow` plus `Translation.Z` for opt-in elevated modern surfaces and clears both at depth 0. |
| Adaptive/visuals: Composition/ConnectedAnimation | P2 | ✅ done | Copilot | `Modern\Media\RibbonAnimations.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | `RibbonAnimations.EnableImplicitTransitions` installs/removes composition implicit Offset/Opacity transitions; connected-animation helpers wrap `ConnectedAnimationService` and return success without throwing. |
| Data-driven RibbonBuilder | P3 | ✅ done | Copilot | `Modern\Model\RibbonModel.cs`, `Modern\RibbonBuilder.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | Lightweight command model builds tabs/groups/items without core edits; `RibbonBuilder` uses `ModernRibbonButton` for `IconSource`, applies `RibbonAccelerator` gestures, and autotests command execution through `RibbonInvoker`. |
| RibbonCustomizationService (JSON persistence) | P3 | ✅ done | Copilot | `Modern\RibbonCustomizationService.cs`, `Modern\Model\RibbonLayout.cs`, `Modern\Model\RibbonCustomizationResult.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | Header text is no longer a key source. Keys prefer explicit `ItemKey`, x:Name, or AutomationId, then use a localization-independent structural fingerprint; ambiguous duplicates fail before capture/apply. Result APIs expose warnings/errors/exceptions for capture, apply, JSON, and durable storage while legacy methods remain no-throw wrappers. `MODERN-CUSTOMIZE PASS` covers localization stability, duplicate rejection, failure visibility, round-trip, persistence, and restoration. |
| TeachingTip coach-marks + InfoBar host | P3 | ✅ done | Copilot | `Modern\Controls\RibbonCoachMark.cs`, `Modern\Controls\RibbonInfoBarHost.cs`, `Themes\Modern\RibbonInfoBarHost.xaml`, `Themes\Modern\Modern.xaml`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | `RibbonCoachMark.Show/Close` is no-throw and attaches TeachingTips to the nearest panel when available; `RibbonInfoBarHost` wraps WinUI InfoBar with TemplateBinding. Autotest log: `MODERN-ONBOARDING PASS`. |
| RTL/FlowDirection | P4 | ✅ done | Copilot | `Modern\Helpers\RibbonFlow.cs`, `Themes\Modern\ModernRibbonButton.xaml`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | `RibbonFlow.IsRightToLeft` applies subtree `FlowDirection` with no-throw accessors; `ModernRibbonButton` medium layout now uses StackPanel spacing instead of a physical left/right icon margin for RTL mirroring. Autotest log: `MODERN-RTL-ACCENT PASS`. |
| XYFocus/gamepad | P4 | ✅ done | Copilot | `Modern\Helpers\RibbonFocus.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | `RibbonFocus.EnableXYFocus` sets XYFocus keyboard navigation to Enabled plus directional strategies and reverts to Auto without throwing. Autotest log: `MODERN-A11Y PASS`. |
| AutomationPeers | P4 | ✅ done | Copilot | `Modern\Automation\RibbonSearchBoxAutomationPeer.cs`, `Modern\Automation\RibbonInfoBarHostAutomationPeer.cs`, `Modern\Automation\ModernRibbonButtonAutomationPeer.cs`, `Modern\Controls\RibbonSearchBox.cs`, `Modern\Controls\RibbonInfoBarHost.cs`, `Modern\Controls\ModernRibbonButton.cs`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | RibbonSearchBox now exposes Edit control type plus `IValueProvider` value/set/read-only semantics, mirrors a public `Text` property to the inner AutoSuggestBox, and places the inner peer in the Raw accessibility view to avoid duplicate control-view nodes. Showcase supplies a unique AutomationId/name; `MODERN-A11Y PASS` validates metadata and automation values. |
| Accent-color brushes | P4 | ✅ done | Copilot | `Themes\Modern\ModernAccentBrushes.xaml`, `Themes\Modern\Modern.xaml`, Showcase `MainPage.ModernShowcase.cs`, Showcase `MainPage.ModernAutoTest.cs`, `Modern\README.md` | Accent brushes are merged only through the opt-in `Modern.xaml` aggregator and use system accent resources with high-contrast system color fallbacks. Autotest log: `MODERN-RTL-ACCENT PASS`. |

## Update protocol

Each subsequent agent must:

1. Set its row Status to 🔄 in-progress when starting and ✅ done when complete.
2. List every file it added or changed in `Key files`.
3. Leave `Notes for next agent` with gotchas, follow-ups, and validation notes.
4. Never edit another feature's files without noting it in that feature's ledger row.
