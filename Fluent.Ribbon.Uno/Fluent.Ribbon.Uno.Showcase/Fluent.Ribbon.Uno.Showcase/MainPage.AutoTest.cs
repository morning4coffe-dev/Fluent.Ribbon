// Opt-in runtime diagnostics / auto-test harness for the Showcase.
//
// This file adds NO behavior to normal runs: everything below is gated on
// environment variables and is a no-op unless they are set.
//
//   SHOWCASE_TAB=<index>        Select a single ribbon tab at startup (handy for screenshots).
//   SHOWCASE_AUTOTEST=1         Walk every tab, open every dropdown/gallery, exercise
//                               Enlarge/Reduce + Simplified/Minimized toggles, logging each step.
//   SHOWCASE_AUTOTEST_LOG=path  Append the [AUTOTEST] log lines to this file (also written to stdout).
//   SHOWCASE_AUTOTEST_EXIT=1    Exit the process once the walk completes (so a runner can detect "done").
//
// The walker deliberately drives controls programmatically because synthetic
// mouse/keyboard input does not reach the Uno Skia window. Any layout crash that
// fires on a dispatcher tick is surfaced either as a "THREW" line here or as an
// Uno "NativeDispatcher unhandled exception" on stdout, which the runner greps for.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Threading.Tasks;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace FluentRibbon.Uno.Showcase;

public sealed partial class MainPage
{
    private static bool autoTestFailed;
    private bool autoTestStarted;

    private static bool AutoTestEnabled =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHOWCASE_AUTOTEST"));

    private static void AutoLog(string message)
    {
        if (message.Contains("FAIL", StringComparison.Ordinal)
            || message.Contains("THREW", StringComparison.Ordinal)
            || message.Contains("FATAL", StringComparison.Ordinal))
        {
            autoTestFailed = true;
        }

        var line = $"[AUTOTEST] {DateTime.Now:HH:mm:ss.fff} {message}";
        Console.WriteLine(line);
        try
        {
            var path = Environment.GetEnvironmentVariable("SHOWCASE_AUTOTEST_LOG");
            if (!string.IsNullOrEmpty(path))
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // logging must never throw
        }
    }

    // Called from the constructor. Wires opt-in diagnostics only when the relevant
    // environment variables are present; otherwise returns immediately.
    private void InitializeDiagnostics()
    {
        var tabVar = Environment.GetEnvironmentVariable("SHOWCASE_TAB");
        var autotest = AutoTestEnabled;
        if (string.IsNullOrEmpty(tabVar) && !autotest)
        {
            return;
        }

        this.Loaded += async (_, _) => await RunConfiguredDiagnosticsAsync(tabVar, autotest);
    }

    internal void StartAutoTestFromHost()
    {
        if (!AutoTestEnabled)
        {
            return;
        }

        var queued = DispatcherQueue.TryEnqueue(
            async () => await RunConfiguredDiagnosticsAsync(
                Environment.GetEnvironmentVariable("SHOWCASE_TAB"),
                autotest: true));
        App.LogAutoTestStartup($"AUTOTEST DISPATCH QUEUED {queued}");
    }

    private async Task RunConfiguredDiagnosticsAsync(string? tabVar, bool autotest)
    {
        if (autoTestStarted)
        {
            return;
        }

        autoTestStarted = true;
        if (int.TryParse(tabVar, out var idx) && idx >= 0 && idx < MainRibbon.Tabs.Count)
        {
            MainRibbon.SelectedTabIndex = idx;
        }

        if (!autotest)
        {
            return;
        }

        if (string.Equals(
                Environment.GetEnvironmentVariable("SHOWCASE_KEYBOARD_AUTOTEST_ONLY"),
                "1",
                StringComparison.Ordinal))
        {
            await RunKeyboardOnlyAutoTestAsync();
        }
        else
        {
            await RunAutoTestAsync();
        }
    }

    private async Task SettleAsync(int cycles = 3, int delayMs = 150)
    {
        for (var i = 0; i < cycles; i++)
        {
            try
            {
                this.UpdateLayout();
            }
            catch (Exception ex)
            {
                AutoLog($"  THREW during UpdateLayout: {ex.GetType().Name}: {ex.Message}");
            }

            await Task.Delay(delayMs);
        }
    }

    private async Task RunAutoTestAsync()
    {
        autoTestFailed = false;
        AutoLog("START");
        try
        {
            var tabCount = MainRibbon.Tabs.Count;
            AutoLog($"Ribbon has {tabCount} tabs");

            for (var i = 0; i < tabCount; i++)
            {
                var header = MainRibbon.Tabs[i].Header?.ToString() ?? "?";
                AutoLog($"TAB {i} '{header}' BEGIN");
                MainRibbon.SelectedTabIndex = i;
                await SettleAsync();

                var targets = new List<Control>();
                CollectDropdownControls(this, targets);
                AutoLog($"TAB {i} '{header}' found {targets.Count} dropdown-bearing controls");

                foreach (var control in targets)
                {
                    await ExerciseControlAsync(control);
                }

                AutoLog($"TAB {i} '{header}' END");
            }

            await ExerciseTogglesAsync();
            await VerifyTabGroupLayoutParityAsync();
            await VerifyToolbarStatusParityAsync();

            // RibbonGallery is not instantiated anywhere in the Showcase XAML, so exercise it here to keep
            // regression coverage over the non-virtualizing UniformItemsPanel migration (measure + grouping
            // + filter rebuild + item mutation), which is where the ItemsRepeater ElementManager crash lived.
            await VerifyRibbonGalleryAsync();
            await VerifyInRibbonGalleryParityAsync();
            await VerifyColorGalleryParityAsync();

            // Regression: hiding the contextual group that owns the currently selected tab must fall back
            // to a visible tab instead of leaving stale content with no selected header.
            await VerifyContextualTabRemovalAsync();

            VerifyRibbonStateStorageLifecycle();
            VerifyCompatibilityRuntimeSmoke();

            await RunKeyboardFocusAutoTestAsync();
            await RunModernAutoTestAsync();

            AutoLog("COMPLETE");
        }
        catch (Exception ex)
        {
            AutoLog($"FATAL {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }

        await FinishAutoTestAsync();
    }

    private void VerifyCompatibilityRuntimeSmoke()
    {
        AutoLog("COMPAT-RUNTIME BEGIN");
        if (Content is not Panel host)
        {
            AutoLog("  FAIL COMPAT-RUNTIME Showcase content is not a Panel");
            return;
        }

        var result = Fluent.CompatibilityRuntimeSmoke.Run(host);
        if (!result.Succeeded)
        {
            AutoLog($"  FAIL COMPAT-RUNTIME {string.Join(" | ", result.Failures)}");
            return;
        }

        AutoLog(
            $"COMPAT-RUNTIME PASS wrappers={result.WrapperCount} "
            + $"templates={result.AppliedTemplateCount} "
            + $"qat={result.QuickAccessCloneCount} "
            + $"commands={result.CommandExecutionCount}");
    }

    private async Task RunKeyboardOnlyAutoTestAsync()
    {
        autoTestFailed = false;
        AutoLog("START");
        try
        {
            await RunKeyboardFocusAutoTestAsync();
            AutoLog("COMPLETE");
        }
        catch (Exception ex)
        {
            AutoLog($"FATAL {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }

        await FinishAutoTestAsync();
    }

    private async Task FinishAutoTestAsync()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHOWCASE_AUTOTEST_EXIT")))
        {
            var exitCode = autoTestFailed ? 1 : 0;
            AutoLog(exitCode == 0 ? "RESULT PASS" : "RESULT FAIL");
            Environment.ExitCode = exitCode;
            await Task.Delay(500);
            AutoLog("EXIT");

            if (!OperatingSystem.IsBrowser()
                && !OperatingSystem.IsAndroid()
                && !OperatingSystem.IsIOS())
            {
                Environment.Exit(exitCode);
            }

            try
            {
                Application.Current.Exit();
            }
            catch
            {
                // ignore
            }
        }
    }

    private void VerifyRibbonStateStorageLifecycle()
    {
        try
        {
            if (!MainRibbon.RibbonStateStorage.IsLoaded)
            {
                AutoLog("  FAIL Ribbon state storage was not loaded with the Ribbon lifecycle");
                return;
            }

            using var storage = new RibbonStateStorage
            {
                IsMinimized = true,
                ShowQuickAccessToolBarBelowRibbon = true,
                IsSimplified = true,
            };
            storage.SaveTemporary();
            storage.IsMinimized = false;
            storage.ShowQuickAccessToolBarBelowRibbon = false;
            storage.IsSimplified = false;
            storage.LoadTemporary();

            if (!storage.IsMinimized
                || !storage.ShowQuickAccessToolBarBelowRibbon
                || !storage.IsSimplified)
            {
                AutoLog("  FAIL Ribbon state temporary round-trip did not restore all values");
                return;
            }

            AutoLog("  OK Ribbon state lifecycle and temporary round-trip");
        }
        catch (Exception ex)
        {
            AutoLog($"  THREW Ribbon state lifecycle: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void CollectDropdownControls(DependencyObject root, List<Control> sink)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            switch (child)
            {
                case InRibbonGallery:
                case RibbonDropDownButton:
                case RibbonComboBox:
                case ColorGallery:
                    sink.Add((Control)child);
                    break;
            }

            CollectDropdownControls(child, sink);
        }
    }

    private async Task ExerciseControlAsync(Control control)
    {
        var name = (control as FrameworkElement)?.Name ?? string.Empty;
        var kind = control.GetType().Name;
        AutoLog($"  OPEN {kind} '{name}'");
        try
        {
            switch (control)
            {
                case InRibbonGallery gallery:
                    // The popup (expandedRepeater) is the real crash suspect; open it the
                    // same way a click would, then hide it.
                    InvokePrivate(gallery, "ShowPopup");
                    await SettleAsync(3);
                    Require(gallery.IsDropDownOpen, "gallery popup did not open");
                    Require(
                        GetPrivateFieldValue<Popup>(gallery, "_popup")?.IsOpen == true,
                        "gallery popup was not visible");
                    HidePrivateFlyout(gallery, "_popup");
                    await SettleAsync(1);
                    Require(!gallery.IsDropDownOpen, "gallery popup did not close");
                    AutoLog($"  ENLARGE/REDUCE {kind} '{name}'");
                    gallery.Enlarge();
                    await SettleAsync(1);
                    gallery.Enlarge();
                    await SettleAsync(1);
                    gallery.Reduce();
                    await SettleAsync(1);
                    gallery.Reduce();
                    await SettleAsync(1);
                    break;

                case RibbonDropDownButton dropDown:
                    dropDown.OnKeyTipPressed();
                    await SettleAsync(3);
#if WINDOWS
                    Require(dropDown.IsDropDownOpen, "drop-down flyout did not open");
#endif
                    dropDown.CloseDropDown();
#if WINDOWS
                    await SettleAsync(3);
                    Require(!dropDown.IsDropDownOpen, "drop-down flyout did not close");
#else
                    await SettleAsync(1);
#endif
                    break;

                case RibbonComboBox combo:
#if WINDOWS
                    combo.IsDropDownOpen = true;
                    await SettleAsync(2);
                    Require(combo.IsDropDownOpen, "combo box popup did not open");
                    combo.IsDropDownOpen = false;
                    await SettleAsync(1);
                    Require(!combo.IsDropDownOpen, "combo box popup did not close");
#else
                    if (FindDescendant<ComboBox>(combo) is ComboBox inner)
                    {
                        inner.IsDropDownOpen = true;
                        await SettleAsync(2);
                        inner.IsDropDownOpen = false;
                        await SettleAsync(1);
                    }
#endif
                    break;

                case ColorGallery:
                    await SettleAsync(1);
                    break;
            }

            AutoLog($"  OK {kind} '{name}'");
        }
        catch (Exception ex)
        {
            AutoLog($"  THREW {kind} '{name}': {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void InvokePrivate(object target, string methodName)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method?.Invoke(target, null);
    }

    private static void InvokePrivate(object target, string methodName, params object[] arguments)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method?.Invoke(target, arguments);
    }

    private static T? GetPrivateFieldValue<T>(object target, string fieldName)
        where T : class
    {
        return target.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(target) as T;
    }

    private static void HidePrivateFlyout(object target, string fieldName)
    {
        var value = GetPrivateFieldValue<object>(target, fieldName);
        if (value is FlyoutBase flyout)
        {
            flyout.Hide();
        }
        else if (value is Popup popup)
        {
            popup.IsOpen = false;
        }
    }

    private async Task ExerciseTogglesAsync()
    {
        AutoLog("TOGGLES BEGIN");
        try
        {
            MainRibbon.SelectedTabIndex = 0;
            await SettleAsync();

            AutoLog("  Simplified ON");
            MainRibbon.IsSimplified = true;
            await SettleAsync();
            AutoLog("  Simplified OFF");
            MainRibbon.IsSimplified = false;
            await SettleAsync();

            AutoLog("  Minimized ON");
            MainRibbon.IsMinimized = true;
            await SettleAsync();
            AutoLog("  Minimized OFF");
            MainRibbon.IsMinimized = false;
            await SettleAsync();
        }
        catch (Exception ex)
        {
            AutoLog($"  TOGGLES THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("TOGGLES END");
    }

    // Regression for the contextual-tab-removal bug: when the currently selected tab belongs to a
    // contextual group that is toggled off, the ribbon must fall back to a visible tab (like WPF)
    // instead of leaving the now-hidden tab selected with stale content and no visible header.
    private async Task VerifyContextualTabRemovalAsync()
    {
        AutoLog("CTXTEST BEGIN");
        try
        {
            RibbonTabItem? designTab = null;
            foreach (var t in MainRibbon.Tabs)
            {
                if (string.Equals(t.Header?.ToString(), "Design", StringComparison.Ordinal))
                {
                    designTab = t;
                    break;
                }
            }

            if (designTab is null)
            {
                AutoLog("CTXTEST SKIP (no Design contextual tab found)");
                return;
            }

            // Show the contextual group and select its tab.
            TableToolsGroup.Visibility = Visibility.Visible;
            await SettleAsync();
            MainRibbon.SelectedTab = designTab;
            await SettleAsync();
            AutoLog($"  selected='{MainRibbon.SelectedTab?.Header}' designVisible={designTab.Visibility}");

            // Hide the group -> the selected (Design) tab becomes hidden.
            TableToolsGroup.Visibility = Visibility.Collapsed;
            await SettleAsync();

            var selected = MainRibbon.SelectedTab;
            var ok = selected is not null
                     && selected != designTab
                     && selected.Visibility == Visibility.Visible;
            AutoLog($"CTXTEST after-hide selected='{selected?.Header}' designVisible={designTab.Visibility} => {(ok ? "PASS" : "FAIL")}");
        }
        catch (Exception ex)
        {
            AutoLog($"  CTXTEST THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("CTXTEST END");
    }

    private async Task VerifyTabGroupLayoutParityAsync()
    {
        AutoLog("TABGROUPTEST BEGIN");
        try
        {
            var tabControl = FindDescendant<RibbonTabControl>(MainRibbon);
            if (tabControl is null)
            {
                AutoLog("  FAIL TABGROUPTEST no RibbonTabControl found");
                return;
            }

            tabControl.IsMinimized = false;
            tabControl.SelectFirstTab();
            await SettleAsync(1);

            if (tabControl.SelectedItem is not RibbonTab { Visibility: Visibility.Visible })
            {
                AutoLog("  FAIL TABGROUPTEST first visible tab was not selected");
                return;
            }

            var originalSelection = tabControl.SelectedItem;
            var navigated = InvokePrivateResult<bool>(tabControl, "SelectRelativeTab", 1, null);
            await SettleAsync(1);
            if (!navigated || ReferenceEquals(originalSelection, tabControl.SelectedItem))
            {
                AutoLog("  FAIL TABGROUPTEST keyboard-style tab navigation did not advance");
                return;
            }

            var selectedTab = tabControl.SelectedItem as RibbonTab;
            var group = selectedTab?.Groups.FirstOrDefault()
                        ?? MainRibbon.Tabs.SelectMany(tab => tab.Groups).FirstOrDefault();
            if (group is not null)
            {
                var reduced = group.StateDefinition.ReduceState(RibbonGroupBoxState.Large);
                var enlarged = group.StateDefinition.EnlargeState(reduced);
                if (reduced == RibbonGroupBoxState.Large || enlarged != RibbonGroupBoxState.Large)
                {
                    AutoLog("  FAIL TABGROUPTEST group reduction transitions are invalid");
                    return;
                }

                group.State = reduced;
                group.TryClearCacheAndResetStateAndScaleAndNotifyParentRibbonGroupsContainer();
            }

            var layoutPanel = new RibbonTabsContainer();
            layoutPanel.Children.Add(new RibbonTab { Header = "One" });
            layoutPanel.Children.Add(new RibbonTab { Header = "Two" });
            layoutPanel.Measure(new Size(120, 40));
            layoutPanel.Arrange(new Rect(0, 0, 120, 40));
            if (double.IsNaN(layoutPanel.DesiredSize.Width)
                || double.IsInfinity(layoutPanel.DesiredSize.Width))
            {
                AutoLog("  FAIL TABGROUPTEST tab layout produced a non-finite size");
                return;
            }

            AutoLog("  PASS tab selection, keyboard navigation, group reduction, and layout");
        }
        catch (Exception ex)
        {
            AutoLog($"  TABGROUPTEST THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("TABGROUPTEST END");
    }

    private async Task VerifyToolbarStatusParityAsync()
    {
        AutoLog("TOOLBARSTATUSTEST BEGIN");
        try
        {
            if (Content is not Panel root)
            {
                AutoLog("  FAIL TOOLBARSTATUSTEST page content is not a panel");
                return;
            }

            var testHost = new StackPanel
            {
                Width = 320,
                Opacity = 0,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            root.Children.Add(testHost);

            try
            {
                var quickAccessToolBar = new QuickAccessToolBar
                {
                    Width = 72,
                };
                AutomationProperties.SetAutomationId(
                    quickAccessToolBar,
                    "AutoTestQuickAccessToolBar");
                testHost.Children.Add(quickAccessToolBar);
                quickAccessToolBar.ApplyTemplate();

                var itemsChanged = 0;
                quickAccessToolBar.ItemsChanged += (_, _) => itemsChanged++;
                var firstQuickItem = new RibbonButton
                {
                    Header = "First",
                    Width = 64,
                };
                var secondQuickItem = new RibbonButton
                {
                    Header = "Second",
                    Width = 64,
                };
                AutomationProperties.SetAutomationId(firstQuickItem, "AutoTestQatFirst");
                AutomationProperties.SetAutomationId(secondQuickItem, "AutoTestQatSecond");
                quickAccessToolBar.Items.Add(firstQuickItem);
                quickAccessToolBar.Items.Add(secondQuickItem);
                var defaultKeyTip = KeyTip.GetKeys(firstQuickItem);
                var locationChanges = 0;
                quickAccessToolBar.ShowAboveRibbonChanged += (_, _) => locationChanges++;
                quickAccessToolBar.ShowAboveRibbon = false;

                quickAccessToolBar.QuickAccessItems.Add(
                    new QuickAccessMenuItem
                    {
                        Header = "First command",
                        Target = firstQuickItem,
                    });
                var customizationItemsPanel = new StackPanel();
                InvokePrivateResult<object?>(
                    quickAccessToolBar,
                    "AddQuickAccessCustomizationItems",
                    customizationItemsPanel);

                var customKeyTipsUpdated = false;
                quickAccessToolBar.UpdateKeyTipsAction =
                    _ => customKeyTipsUpdated = true;
                quickAccessToolBar.Measure(new Size(72, 32));
                quickAccessToolBar.Arrange(new Rect(0, 0, 72, 32));
                quickAccessToolBar.Refresh();
                await SettleAsync(1);

                if (itemsChanged < 2
                    || !customKeyTipsUpdated
                    || defaultKeyTip != "1"
                    || locationChanges != 1
                    || customizationItemsPanel.Children.Count != 1
                    || !quickAccessToolBar.HasOverflowItems)
                {
                    AutoLog(
                        $"  FAIL TOOLBARSTATUSTEST QAT changed={itemsChanged} "
                        + $"keytips={customKeyTipsUpdated}/{defaultKeyTip} "
                        + $"overflow={quickAccessToolBar.HasOverflowItems}");
                    return;
                }

                var toolbar = new RibbonToolBar();
                AutomationProperties.SetAutomationId(toolbar, "AutoTestRibbonToolBar");
                var toolbarButton = new RibbonButton
                {
                    Name = "LayoutTarget",
                    Header = "Layout target",
                };
                AutomationProperties.SetAutomationId(toolbarButton, "AutoTestToolbarButton");
                toolbar.Children.Add(toolbarButton);

                var row = new RibbonToolBarRow();
                row.Children.Add(
                    new RibbonToolBarControlDefinition
                    {
                        Target = toolbarButton.Name,
                        Size = RibbonControlSize.Small,
                        Width = 42,
                    });
                var layout = new RibbonToolBarLayoutDefinition
                {
                    Size = RibbonControlSize.Small,
                    RowCount = 1,
                };
                layout.Rows.Add(row);
                toolbar.LayoutDefinitions.Add(layout);
                RibbonProperties.SetSize(toolbar, RibbonControlSize.Small);
                testHost.Children.Add(toolbar);
                toolbar.ApplyTemplate();
                toolbar.Measure(new Size(180, 48));
                toolbar.Arrange(new Rect(0, 0, 180, 48));
                await SettleAsync(1);

                if (Math.Abs(toolbarButton.Width - 42) > 0.1
                    || !ReferenceEquals(toolbar.Children, toolbar.Items))
                {
                    AutoLog("  FAIL TOOLBARSTATUSTEST toolbar layout definition was not applied");
                    return;
                }

                var statusBar = new RibbonStatusBar();
                AutomationProperties.SetAutomationId(statusBar, "AutoTestRibbonStatusBar");
                var leftStatus = new RibbonStatusBarItem
                {
                    Title = "Left status",
                    Content = "Ready",
                };
                var rightStatus = new RibbonStatusBarItem
                {
                    Title = "Right status",
                    Content = "100%",
                };
                statusBar.Items.Add(leftStatus);
                statusBar.RightItems.Add(rightStatus);
                testHost.Children.Add(statusBar);
                statusBar.ApplyTemplate();

                leftStatus.IsChecked = false;
                if (leftStatus.Visibility != Visibility.Collapsed)
                {
                    AutoLog("  FAIL TOOLBARSTATUSTEST unchecked status item remained visible");
                    return;
                }

                leftStatus.IsChecked = true;
                var statusMenuItem = new StatusBarMenuItem(leftStatus);
                InvokePrivateResult<object?>(statusMenuItem, "InvokeForAutomation");
                if (leftStatus.IsChecked || statusMenuItem.IsChecked)
                {
                    AutoLog("  FAIL TOOLBARSTATUSTEST status menu did not synchronize checked state");
                    return;
                }

                InvokePrivateResult<object?>(statusBar, "RebuildCustomizationMenu");
                if (statusBar.ContextFlyout is not Flyout
                    {
                        Content: StackPanel customizationPanel
                    }
                    || customizationPanel.Children
                        .OfType<StatusBarMenuItem>()
                        .Count() != 2)
                {
                    AutoLog("  FAIL TOOLBARSTATUSTEST customization menu did not mirror status items");
                    return;
                }

                var statusPanel = new StatusBarPanel();
                var leftPanelItem = new Border
                {
                    Width = 60,
                    Height = 20,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                var rightPanelItem = new Border
                {
                    Width = 60,
                    Height = 20,
                    HorizontalAlignment = HorizontalAlignment.Right,
                };
                statusPanel.Children.Add(leftPanelItem);
                statusPanel.Children.Add(rightPanelItem);
                statusPanel.Measure(new Size(70, 24));
                statusPanel.Arrange(new Rect(0, 0, 70, 24));

                if (leftPanelItem.DesiredSize.Width != 0
                    || rightPanelItem.DesiredSize.Width <= 0)
                {
                    AutoLog("  FAIL TOOLBARSTATUSTEST status overflow priority is invalid");
                    return;
                }

                AutoLog(
                    "  PASS QAT overflow/add-remove/keytips, toolbar definitions, "
                    + "status checks/menu synchronization, and status overflow");
            }
            finally
            {
                root.Children.Remove(testHost);
            }
        }
        catch (Exception ex)
        {
            AutoLog($"  TOOLBARSTATUSTEST THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("TOOLBARSTATUSTEST END");
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : class
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is T deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    private static T? InvokePrivateResult<T>(object target, string methodName, params object?[] arguments)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        return method is null ? default : (T?)method.Invoke(target, arguments);
    }

    // Standalone runtime exercise of RibbonGallery (which the Showcase XAML never instantiates).
    private async Task VerifyRibbonGalleryAsync()
    {
        AutoLog("RGTEST BEGIN");
        Popup? popup = null;
        try
        {
            var flat = new RibbonGallery { ItemWidth = 32, ItemHeight = 32, Width = 240, Height = 120 };
            for (var i = 0; i < 10; i++)
            {
                flat.Items.Add(MakeRgItem(i, null));
            }

            var grouped = new RibbonGallery
            {
                ItemWidth = 32,
                ItemHeight = 32,
                Width = 240,
                Height = 160,
                IsGrouped = true,
            };
            var groupNames = new[] { "Alpha", "Beta", "Gamma" };
            for (var i = 0; i < 15; i++)
            {
                grouped.Items.Add(MakeRgItem(i, groupNames[i % groupNames.Length]));
            }

            var filterAB = new GalleryGroupFilter { Title = "A+B", Groups = "Alpha,Beta" };
            var filterG = new GalleryGroupFilter { Title = "G", Groups = "Gamma" };
            grouped.Filters.Add(filterAB);
            grouped.Filters.Add(filterG);

            var host = new StackPanel { Spacing = 8 };
            host.Children.Add(flat);
            host.Children.Add(grouped);

            popup = new Popup { Child = host };
            if (this.Content is Panel rootPanel)
            {
                rootPanel.Children.Add(popup);
            }

            popup.IsOpen = true;
            await SettleAsync(4, 150);

            AutoLog("  RGTEST apply filter A+B");
            grouped.SelectedFilter = filterAB;
            await SettleAsync(3, 150);

            AutoLog("  RGTEST apply filter G");
            grouped.SelectedFilter = filterG;
            await SettleAsync(3, 150);

            AutoLog("  RGTEST clear filter");
            grouped.SelectedFilter = null;
            await SettleAsync(3, 150);

            AutoLog("  RGTEST mutate items");
            grouped.Items.Add(MakeRgItem(99, "Alpha"));
            grouped.Items.RemoveAt(0);
            await SettleAsync(3, 150);

            AutoLog("  RGTEST OK");
        }
        catch (Exception ex)
        {
            AutoLog($"  RGTEST THREW {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                if (popup is not null)
                {
                    popup.IsOpen = false;
                    if (this.Content is Panel rootPanel)
                    {
                        rootPanel.Children.Remove(popup);
                    }
                }
            }
            catch
            {
                // ignore teardown errors
            }
        }

        AutoLog("RGTEST END");
    }

    private async Task VerifyInRibbonGalleryParityAsync()
    {
        AutoLog("IRGTEST BEGIN");
        try
        {
            var previewCommand = new GalleryAutoTestCommand();
            var cancelPreviewCommand = new GalleryAutoTestCommand();
            var alpha = new RibbonGalleryItem
            {
                Group = "Alpha",
                Content = new TextBlock { Text = "Alpha" },
                PreviewCommand = previewCommand,
                CancelPreviewCommand = cancelPreviewCommand,
                CommandParameter = "Alpha",
            };
            var beta = new RibbonGalleryItem
            {
                Group = "Beta",
                Content = new TextBlock { Text = "Beta" },
            };
            var gallery = new InRibbonGallery
            {
                Header = "Parity",
                ItemWidth = 48,
                ItemHeight = 32,
                MinItemsInRow = 1,
                MaxItemsInRow = 3,
                MinItemsInDropDownRow = 1,
                MaxItemsInDropDownRow = 4,
                DropDownWidth = 260,
                DropDownHeight = 180,
                MaxDropDownWidth = 420,
                MaxDropDownHeight = 320,
                ResizeMode = ContextMenuResizeMode.Both,
                CanCollapseToButton = true,
            };
            gallery.Items.Add(alpha);
            gallery.Items.Add(beta);

            var alphaFilter = new GalleryGroupFilter { Title = "Alpha only", Groups = "Alpha" };
            var betaFilter = new GalleryGroupFilter { Title = "Beta only", Groups = "Beta" };
            gallery.Filters.Add(alphaFilter);
            gallery.Filters.Add(betaFilter);
            gallery.SelectedFilter = alphaFilter;

            Require(gallery.HasFilter, "filter metadata was not enabled");
            Require(gallery.SelectedFilterTitle == "Alpha only", "selected filter title did not synchronize");
            Require(gallery.SelectedFilterGroups == "Alpha", "selected filter groups did not synchronize");
            Require(alpha.Visibility == Visibility.Visible, "allowed filter item was hidden");
            Require(beta.Visibility == Visibility.Collapsed, "excluded filter item remained visible");

            var selectionEvents = 0;
            gallery.SelectionChanged += (_, _) => selectionEvents++;
            gallery.SelectedIndex = 1;
            Require(ReferenceEquals(gallery.SelectedItem, beta), "selected index did not synchronize selected item");
            Require(beta.IsSelected && !alpha.IsSelected, "gallery item selection flags did not synchronize");
            Require(selectionEvents == 1, "selection event was not raised exactly once");

            var opened = 0;
            var closed = 0;
            gallery.DropDownOpened += (_, _) => opened++;
            gallery.DropDownClosed += (_, _) => closed++;
            gallery.IsDropDownOpen = true;
            gallery.IsDropDownOpen = false;
            Require(opened == 1 && closed == 1, "open/close events did not track dropdown state");

            InvokePrivate(gallery, "PreviewItemForAutomation", alpha);
            InvokePrivate(gallery, "CancelPreviewForAutomation");
            Require(previewCommand.ExecuteCount == 1, "live preview command was not executed");
            Require(cancelPreviewCommand.ExecuteCount == 1, "cancel preview command was not executed");

            var quickAccess = gallery.CreateQuickAccessItem() as InRibbonGallery;
            Require(quickAccess is not null, "quick access clone was not created");
            quickAccess!.IsDropDownOpen = true;
            Require(gallery.IsFrozen, "source gallery was not frozen while the QAT clone was open");
            Require(quickAccess.Items.Count == gallery.Items.Count, "QAT clone did not receive source items");
            quickAccess.IsDropDownOpen = false;
            Require(!gallery.IsFrozen && quickAccess.Items.Count == 0, "QAT clone did not restore source state");

            var scaled = 0;
            gallery.Scaled += (_, _) => scaled++;
            gallery.Reduce();
            gallery.IsCollapsed = true;
            gallery.ResetScale();
            Require(scaled >= 2, "scale events did not cover reduce/reset");
            Require(!gallery.IsCollapsed, "ResetScale did not restore the inline state");

            await SettleAsync(2, 75);
            AutoLog("  IRGTEST OK");
        }
        catch (Exception ex)
        {
            AutoLog($"  IRGTEST THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("IRGTEST END");
    }

    private async Task VerifyColorGalleryParityAsync()
    {
        AutoLog("CGTEST BEGIN");
        Popup? popup = null;
        try
        {
            ColorGallery.RecentColors.Clear();
            var gallery = new ColorGallery
            {
                Columns = 3,
                Mode = ColorGalleryMode.ThemeColors,
                ThemeColorGridRows = 4,
                StandardColorGridRows = 3,
                ThemeColorsSource =
                [
                    Windows.UI.Color.FromArgb(255, 40, 90, 180),
                    Windows.UI.Color.FromArgb(255, 180, 80, 40),
                    Windows.UI.Color.FromArgb(255, 50, 150, 90),
                ],
            };

            if (this.Content is Panel rootPanel)
            {
                popup = new Popup { Child = gallery };
                rootPanel.Children.Add(popup);
                popup.IsOpen = true;
                gallery.ApplyTemplate();
                await SettleAsync(2, 75);
            }

            Require(ColorGallery.HighlightColors.Length == 15, "highlight palette count differed from WPF");
            Require(ColorGallery.StandardColors.Length == 30, "standard palette count differed from WPF");
            Require(ColorGallery.StandardThemeColors.Length == 10, "standard theme palette count differed from WPF");
            Require(
                ColorGallery.HighlightColors[0] == Windows.UI.Color.FromArgb(255, 255, 255, 0),
                "highlight palette values differed from WPF");
            Require(
                ColorGallery.HighlightColors[^1] == Windows.UI.Color.FromArgb(255, 0, 0, 0)
                && ColorGallery.StandardColors[15] == Windows.UI.Color.FromArgb(255, 102, 102, 102)
                && ColorGallery.StandardColors[^1] == Windows.UI.Color.FromArgb(255, 156, 133, 192)
                && ColorGallery.StandardThemeColors[^1] == Windows.UI.Color.FromArgb(255, 112, 48, 160),
                "static palette values differed from WPF");
            Require(gallery.MutableStandardColors.Count == ColorGallery.StandardColors.Length, "mutable standard palette was not initialized");
            gallery.MutableStandardColors.Add(Windows.UI.Color.FromArgb(255, 1, 2, 3));
            Require(ColorGallery.StandardColors.Length == 30, "mutable palette changed the WPF static palette");

            Require(gallery.ThemeGradients?.Length == 12, "theme gradient dimensions were incorrect");
            Require(gallery.StandardGradients?.Length == 9, "standard gradient dimensions were incorrect");
            Require(
                GetColorBrightness(gallery.ThemeGradients![0]) > GetColorBrightness(gallery.ThemeGradients[^3]),
                "theme gradients were not ordered light-to-dark");

            gallery.Mode = ColorGalleryMode.StandardColors;
            Require(gallery.ThemeGradients is null && gallery.StandardGradients is null, "standard mode retained theme gradients");
            gallery.Mode = ColorGalleryMode.HighlightColors;
            Require(gallery.ThemeGradients is null && gallery.StandardGradients is null, "highlight mode retained gradients");
            gallery.Mode = ColorGalleryMode.ThemeColors;

            var routedEvents = 0;
            var valueEvents = 0;
            gallery.SelectedColorChanged += (_, _) => routedEvents++;
            gallery.SelectedColorValueChanged += (_, color) =>
            {
                if (color.HasValue)
                {
                    valueEvents++;
                }
            };
            gallery.SelectedColor = Windows.UI.Color.FromArgb(255, 10, 20, 30);
            Require(routedEvents == 1 && valueEvents == 1, "selection events did not preserve direct and value semantics");
            gallery.SelectedColor = gallery.ThemeColors[0];
            await SettleAsync(1, 50);
            var selectedSwatch = InvokePrivateResult<Button>(
                gallery,
                "GetSelectedSwatchForAutomation");
            Require(
                selectedSwatch is not null,
                "selected color did not synchronize the rendered swatch");

            var acceptedColor = Windows.UI.Color.FromArgb(255, 11, 22, 33);
            var picker = new AutoTestColorPicker(
                ColorGalleryCustomColorPickerResult.Accepted(acceptedColor));
            gallery.CustomColorPicker = picker;
            await InvokePrivateResult<Task>(gallery, "ExecuteMoreColorsAsync")!;
            Require(picker.RequestCount == 1, "injected picker was not invoked");
            Require(gallery.SelectedColor == acceptedColor, "accepted custom color was not selected");
            Require(ColorGallery.RecentColors.FirstOrDefault() == acceptedColor, "accepted custom color was not added to recent colors");

            var recentCount = ColorGallery.RecentColors.Count;
            gallery.CustomColorPicker = new AutoTestColorPicker(
                ColorGalleryCustomColorPickerResult.Canceled(acceptedColor));
            await InvokePrivateResult<Task>(gallery, "ExecuteMoreColorsAsync")!;
            Require(ColorGallery.RecentColors.Count == recentCount, "canceled custom color changed recent colors");

            var hostHookColor = Windows.UI.Color.FromArgb(255, 90, 100, 110);
            var hostHookGallery = new ColorGallery();
            hostHookGallery.CustomColorPickerRequested += (_, args) =>
                args.Picker = new AutoTestColorPicker(
                    ColorGalleryCustomColorPickerResult.Accepted(hostHookColor));
            await InvokePrivateResult<Task>(hostHookGallery, "ExecuteMoreColorsAsync")!;
            Require(hostHookGallery.SelectedColor == hostHookColor, "host picker hook was not honored");

            var eventGallery = new ColorGallery();
            var eventColor = Windows.UI.Color.FromArgb(255, 60, 70, 80);
            eventGallery.MoreColorsExecuting += (_, args) => args.Color = eventColor;
            await InvokePrivateResult<Task>(eventGallery, "ExecuteMoreColorsAsync")!;
            Require(eventGallery.SelectedColor == eventColor, "MoreColorsExecuting accepted color was not selected");

            var cancelGallery = new ColorGallery();
            cancelGallery.MoreColorsExecuting += (_, args) =>
            {
                args.Color = eventColor;
                args.Canceled = true;
            };
            await InvokePrivateResult<Task>(cancelGallery, "ExecuteMoreColorsAsync")!;
            Require(cancelGallery.SelectedColor is null, "canceled MoreColorsExecuting changed selection");

            AutoLog("  CGTEST OK");
        }
        catch (Exception ex)
        {
            AutoLog($"  CGTEST THREW {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            if (popup is not null)
            {
                popup.IsOpen = false;
                if (this.Content is Panel rootPanel)
                {
                    rootPanel.Children.Remove(popup);
                }
            }

            ColorGallery.RecentColors.Clear();
        }

        AutoLog("CGTEST END");
    }

    private static int GetColorBrightness(Windows.UI.Color color) =>
        color.R + color.G + color.B;

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class GalleryAutoTestCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add
            {
            }

            remove
            {
            }
        }

        public int ExecuteCount { get; private set; }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
            ExecuteCount++;
        }
    }

    private sealed class AutoTestColorPicker : IColorGalleryCustomColorPicker
    {
        private readonly ColorGalleryCustomColorPickerResult _result;

        public AutoTestColorPicker(ColorGalleryCustomColorPickerResult result)
        {
            _result = result;
        }

        public int RequestCount { get; private set; }

        public Task<ColorGalleryCustomColorPickerResult> PickColorAsync(
            ColorGalleryCustomColorPickerContext context,
            CancellationToken cancellationToken = default)
        {
            RequestCount++;
            return Task.FromResult(_result);
        }
    }

    private static RibbonGalleryItem MakeRgItem(int i, string? group)
    {
        var hue = (i * 36) % 360;
        var color = HsvToColor(hue);
        return new RibbonGalleryItem
        {
            Group = group ?? string.Empty,
            Content = new Border
            {
                Width = 28,
                Height = 28,
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(3),
            },
        };
    }

    private static Windows.UI.Color HsvToColor(double hue)
    {
        var h = hue / 60.0;
        var x = 1 - Math.Abs((h % 2) - 1);
        double r = 0, g = 0, b = 0;
        switch ((int)h)
        {
            case 0: r = 1; g = x; break;
            case 1: r = x; g = 1; break;
            case 2: g = 1; b = x; break;
            case 3: g = x; b = 1; break;
            case 4: r = x; b = 1; break;
            default: r = 1; b = x; break;
        }

        return Windows.UI.Color.FromArgb(255, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
}
