namespace FluentRibbon.Uno.Showcase;

using System.Collections;
using System.Reflection;
using System.Windows.Input;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Button = Microsoft.UI.Xaml.Controls.Button;

public sealed partial class MainPage
{
    private async Task RunKeyboardFocusAutoTestAsync()
    {
        AutoLog("KEYBOARD-FOCUS BEGIN");
        await SettleAsync(4, 150);

        if (Content is not Grid pageRoot
            || XamlRoot is not { } xamlRoot
            || MainRibbon.Tabs.FirstOrDefault() is not RibbonTab firstTab
            || firstTab.Groups.FirstOrDefault() is not RibbonGroupBox firstGroup)
        {
            AutoLog("  FAIL KEYBOARD-FOCUS required Showcase surfaces were not found");
            return;
        }

        var service = (KeyTipService?)typeof(Ribbon)
            .GetField("_keyTipService", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(MainRibbon);
        if (service is null)
        {
            AutoLog("  FAIL KEYBOARD-FOCUS Ribbon KeyTipService was not found");
            return;
        }

        var focusProbe = new Button
        {
            Width = 1,
            Height = 1,
            Opacity = 0.01,
            Content = "Focus probe",
        };
        AutomationProperties.SetAutomationId(focusProbe, "KeyboardFocusProbe");
        Grid.SetRow(focusProbe, 1);
        Canvas.SetZIndex(focusProbe, -1);

        var commandInvoked = false;
        var firstTabKeys = KeyTip.GetKeys(firstTab) ?? firstTab.KeyTip;
        if (string.IsNullOrWhiteSpace(firstTabKeys))
        {
            AutoLog("  FAIL KEYBOARD-FOCUS first Showcase tab has no KeyTip");
            return;
        }

        var enabledButton = new RibbonButton
        {
            Header = "Keyboard test",
            KeyTip = "ZZ",
        };
        var disabledButton = new RibbonButton
        {
            Header = "Disabled keyboard test",
            KeyTip = "ZX",
            IsEnabled = false,
        };
        var hiddenButton = new RibbonButton
        {
            Header = "Hidden keyboard test",
            KeyTip = "ZV",
            Visibility = Visibility.Collapsed,
        };
        var menuNavigationHost = new RibbonMenuItem
        {
            Header = "Menu navigation test",
            Width = 180,
            Opacity = 0.01,
            IsDefinitive = false,
        };
        var disabledMenuItem = new RibbonMenuItem
        {
            Header = "Disabled menu item",
            IsEnabled = false,
        };
        var firstEnabledMenuItem = new RibbonMenuItem { Header = "First enabled menu item" };
        var lastEnabledMenuItem = new RibbonMenuItem { Header = "Last enabled menu item" };
        menuNavigationHost.Items.Add(disabledMenuItem);
        menuNavigationHost.Items.Add(new GroupSeparatorMenuItem { Header = "Menu group" });
        menuNavigationHost.Items.Add(firstEnabledMenuItem);
        menuNavigationHost.Items.Add(lastEnabledMenuItem);
        Grid.SetRow(menuNavigationHost, 1);
        Canvas.SetZIndex(menuNavigationHost, 60);

        var originalKeys = MainRibbon.KeyTipKeys.ToArray();
        var originalSelectedTab = MainRibbon.SelectedTab;
        var originalMinimized = MainRibbon.IsMinimized;
        var originalCollapsed = MainRibbon.IsCollapsed;
        var originalGroupState = firstGroup.State;
        var originalStartScreen = MainRibbon.StartScreen;
        Control? dynamicallyDisabledProbe = null;
        var applicationMenu = MainRibbon.Menu as ApplicationMenu;
        var originalApplicationMenuKeyTip = applicationMenu?.KeyTip;
        var firstMenuItem = applicationMenu?.Items.OfType<RibbonMenuItem>().FirstOrDefault();
        var originalMenuItemKeyTip = firstMenuItem?.KeyTip;
        var originalMenuItemCommand = firstMenuItem?.Command;
        var startScreen = new StartScreen
        {
            KeyTip = "98",
            Content = new Button
            {
                Content = "Create",
                IsTabStop = true,
            },
        };
        AutomationProperties.SetAutomationId(startScreen, "KeyboardStartScreen");
        AutomationProperties.SetAutomationId((Button)startScreen.Content, "KeyboardStartScreenCreate");
        Grid.SetRowSpan(startScreen, 3);
        Canvas.SetZIndex(startScreen, 50);

        try
        {
            if (applicationMenu is not null
                && string.IsNullOrWhiteSpace(originalApplicationMenuKeyTip))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS application menu has no default localized KeyTip");
                return;
            }

            pageRoot.Children.Add(focusProbe);
            firstGroup.Items.Add(enabledButton);
            firstGroup.Items.Add(disabledButton);
            firstGroup.Items.Add(hiddenButton);
            pageRoot.Children.Add(menuNavigationHost);
            MainRibbon.SelectedTab = firstTab;
            MainRibbon.IsMinimized = false;
            MainRibbon.IsCollapsed = false;
            await SettleAsync(2);

            MainRibbon.KeyTipKeys.Clear();
            MainRibbon.KeyTipKeys.Add(VirtualKey.F12);
            if (!service.KeyTipKeys.SequenceEqual(new[] { VirtualKey.F12 }))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS custom activation keys did not synchronize");
                return;
            }

            MainRibbon.KeyTipKeys.Clear();
            foreach (var key in originalKeys)
            {
                MainRibbon.KeyTipKeys.Add(key);
            }

            focusProbe.Focus(FocusState.Programmatic);
            service.Show();
            var rootTargets = GetKeyTipTargets(service);
            if (!service.IsActive
                || !service.AreAnyKeyTipsVisible
                || rootTargets.All(target => !ReferenceEquals(target.Element, firstTab))
                || !ReferenceEquals(
                    Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot),
                    firstTab))
            {
                AutoLog(
                    $"  FAIL KEYBOARD-FOCUS Alt/F10 root scope did not expose ribbon tabs "
                    + $"active={service.IsActive} visible={service.AreAnyKeyTipsVisible} "
                    + $"scope={GetKeyTipScopeNames(service)} targets="
                    + string.Join(",", rootTargets.Select(target => $"{target.Element.GetType().Name}:{target.Keys}")));
                return;
            }

            InvokeKeyTipDismissal(service, "DismissForPointerInput");
            if (service.IsActive)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS pointer dismissal left KeyTips active");
                return;
            }

            focusProbe.Focus(FocusState.Programmatic);
            DismissPopupReason? dismissalReason = null;
            EventHandler<DismissPopupEventArgs> dismissalHandler = (_, args) =>
                dismissalReason = args.DismissReason;
            PopupService.DismissPopup += dismissalHandler;
            try
            {
                service.Show();
                InvokeKeyTipDismissal(service, "DismissForWindowDeactivation");
                if (service.IsActive
                    || dismissalReason != DismissPopupReason.ApplicationLostFocus)
                {
                    AutoLog("  FAIL KEYBOARD-FOCUS window deactivation dismissal was incomplete");
                    return;
                }
            }
            finally
            {
                PopupService.DismissPopup -= dismissalHandler;
            }

            focusProbe.Focus(FocusState.Programmatic);
            service.Show();
            service.Hide();
            if (!ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot), focusProbe))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS focus was not restored after dismissal");
                return;
            }

            firstTab.Focus(FocusState.Programmatic);
            service.Show();
            service.Hide();
            if (!ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot), firstTab))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS in-ribbon focus was not restored after dismissal");
                return;
            }

            service.Show();
            var partialCandidates = GetKeyTipTargets(service);
            var partialTarget = partialCandidates.FirstOrDefault(target =>
                target.Keys.Length > 1
                && partialCandidates.All(candidate =>
                    !string.Equals(
                        candidate.Keys,
                        target.Keys[..1],
                        StringComparison.OrdinalIgnoreCase)));
            if (partialTarget is null)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS no unambiguous partial KeyTip target was available");
                return;
            }

            InvokeKeyTipCharacter(service, partialTarget.Keys[0]);
            if (!service.IsActive
                || GetKeyTipTargets(service).All(target => !target.IsVisible))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS partial KeyTip matching hid every candidate");
                return;
            }

            service.Hide();
            service.Show();
            var unusedCharacter = "QJY9876543210"
                .First(character => GetKeyTipTargets(service)
                    .All(target => !target.Keys.StartsWith(character.ToString(), StringComparison.OrdinalIgnoreCase)));
            InvokeKeyTipCharacter(service, unusedCharacter);
            if (service.IsActive)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS invalid first KeyTip character did not dismiss");
                return;
            }

            MainRibbon.SelectedTab = firstTab;
            await SettleAsync(1);
            service.Show();
            InvokeKeyTipRebuild(service);
            InvokeKeyTipTarget(service, firstTab);

            await SettleAsync(1);
            var tabTargets = GetKeyTipTargets(service);
            if (!service.IsActive
                || tabTargets.Count == 0
                || tabTargets.Any(target => ReferenceEquals(target.Element, disabledButton))
                || tabTargets.Any(target => ReferenceEquals(target.Element, hiddenButton)))
            {
                AutoLog(
                    $"  FAIL KEYBOARD-FOCUS tab scope did not filter disabled/hidden controls "
                    + $"active={service.IsActive} scope={GetKeyTipScopeNames(service)} targets="
                    + string.Join(",", tabTargets.Select(target =>
                        $"{target.Element.GetType().Name}:{target.Keys}:enabled="
                        + (target.Element is Control control && control.IsEnabled))));
                return;
            }

            dynamicallyDisabledProbe = tabTargets
                .Select(target => target.Element)
                .OfType<Control>()
                .FirstOrDefault(control => control.IsEnabled);
            if (dynamicallyDisabledProbe is null)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS tab scope had no enabled control to filter");
                return;
            }

            dynamicallyDisabledProbe.IsEnabled = false;
            InvokeKeyTipRebuild(service);
            if (GetKeyTipTargets(service).Any(
                    target => ReferenceEquals(target.Element, dynamicallyDisabledProbe)))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS a control remained after becoming disabled");
                return;
            }

            dynamicallyDisabledProbe.IsEnabled = true;
            dynamicallyDisabledProbe = null;
            service.Hide();

            firstGroup.State = RibbonGroupBoxState.Collapsed;
            service.Show();
            InvokeKeyTipRebuild(service);
            InvokeKeyTipTarget(service, firstTab);
            await SettleAsync(1);
            if (GetKeyTipTargets(service).Any(
                    target => ReferenceEquals(target.Element, enabledButton)))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS collapsed Ribbon group exposed child KeyTips");
                return;
            }

            service.Hide();
            firstGroup.State = originalGroupState;
            await SettleAsync(1);

            var contextualTab = MainRibbon.Tabs.FirstOrDefault(tab => tab.IsContextual);
            if (contextualTab is not null)
            {
                service.Show();
                if (GetKeyTipTargets(service).Any(target => ReferenceEquals(target.Element, contextualTab)))
                {
                    AutoLog("  FAIL KEYBOARD-FOCUS hidden contextual tab was included");
                    return;
                }

                service.Hide();
            }

            MainRibbon.IsMinimized = true;
            service.Show();
            InvokeKeyTipRebuild(service);
            InvokeKeyTipTarget(service, firstTab);

            await SettleAsync(1);
            var tabControl = FindDescendant<RibbonTabControl>(MainRibbon);
            if (!MainRibbon.IsMinimized
                || tabControl?.IsDropDownOpen != true
                || !service.IsActive)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS minimized tab activation did not open its dropdown scope");
                return;
            }

            InvokeKeyTipBack(service);
            if (tabControl.IsDropDownOpen)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS Escape/back did not close the minimized tab scope");
                return;
            }

            service.Hide();
            MainRibbon.IsMinimized = false;

            MainRibbon.IsCollapsed = true;
            service.Show();
            if (service.IsActive)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS collapsed Ribbon entered KeyTip mode");
                return;
            }

            MainRibbon.IsCollapsed = false;

            if (applicationMenu is not null && firstMenuItem is not null)
            {
                applicationMenu.KeyTip = "99";
                firstMenuItem.KeyTip = "N";
                firstMenuItem.Command = new AutoTestCommand(() => commandInvoked = true);
                service.Show();
                InvokeKeyTipCharacter(service, '9');
                InvokeKeyTipCharacter(service, '9');
                await SettleAsync(2);

                if (!applicationMenu.IsDropDownOpen
                    || !service.IsActive
                    || GetKeyTipScopeDepth(service) < 2
                    || GetKeyTipTargets(service).All(
                        target => !ReferenceEquals(target.Element, firstMenuItem)))
                {
                    AutoLog("  FAIL KEYBOARD-FOCUS application-menu nested scope was not established");
                    return;
                }

                InvokeKeyTipBack(service);
                await SettleAsync(1);
                if (applicationMenu.IsDropDownOpen
                    || GetKeyTipScopeDepth(service) != 1)
                {
                    AutoLog("  FAIL KEYBOARD-FOCUS application-menu Escape/back did not return to root");
                    return;
                }

                service.Hide();
                service.Show();
                InvokeKeyTipCharacter(service, '9');
                InvokeKeyTipCharacter(service, '9');
                await SettleAsync(2);
                InvokeKeyTipCharacter(service, 'N');
                if (!commandInvoked || service.IsActive)
                {
                    AutoLog("  FAIL KEYBOARD-FOCUS exact nested KeyTip did not invoke its command");
                    return;
                }

                applicationMenu.Close();
            }

            menuNavigationHost.FlowDirection = FlowDirection.LeftToRight;
            menuNavigationHost.Focus(FocusState.Keyboard);
            InvokeMenuNavigation(menuNavigationHost, VirtualKey.Right);
            await SettleAsync(3);
            if (menuNavigationHost.IsDropDownOpen is false
                || menuNavigationHost.DropDownPopup?.IsOpen != true
                || !ReferenceEquals(
                    Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot),
                    firstEnabledMenuItem))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS submenu Right navigation did not focus the first enabled item");
                return;
            }

            InvokeMenuNavigation(firstEnabledMenuItem, VirtualKey.Down);
            if (!ReferenceEquals(
                    Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot),
                    lastEnabledMenuItem))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS menu Down navigation did not skip disabled/separator items");
                return;
            }

            InvokeMenuNavigation(lastEnabledMenuItem, VirtualKey.Home);
            InvokeMenuNavigation(firstEnabledMenuItem, VirtualKey.End);
            if (!ReferenceEquals(
                    Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot),
                    lastEnabledMenuItem))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS menu Home/End navigation did not reach enabled endpoints");
                return;
            }

            InvokeMenuNavigation(lastEnabledMenuItem, VirtualKey.Left);
            await SettleAsync(2);
            if (menuNavigationHost.IsDropDownOpen
                || !ReferenceEquals(
                    Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot),
                    menuNavigationHost))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS submenu Left navigation did not close and restore parent focus");
                return;
            }

            menuNavigationHost.FlowDirection = FlowDirection.RightToLeft;
            InvokeMenuNavigation(menuNavigationHost, VirtualKey.Left);
            await SettleAsync(3);
            InvokeMenuNavigation(firstEnabledMenuItem, VirtualKey.Escape);
            await SettleAsync(2);
            if (menuNavigationHost.IsDropDownOpen
                || !ReferenceEquals(
                    Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot),
                    menuNavigationHost))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS RTL submenu entry or Escape focus restoration failed");
                return;
            }

            focusProbe.Focus(FocusState.Programmatic);
            BackstageView.OnKeyTipPressed();
            await SettleAsync(1);
            if (!BackstageView.IsOpen || !MainRibbon.IsBackstageOrStartScreenOpen)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS backstage did not synchronize open state");
                return;
            }

            service.Show();
            var backstageTargets = GetKeyTipTargets(service);
            var hasHomeTarget = backstageTargets.Any(
                target => ReferenceEquals(target.Element, backstageHomeButton));
            var hasRecentTarget = backstageTargets.Any(
                target => ReferenceEquals(target.Element, backstageRecentButton));
            if (!hasHomeTarget || hasRecentTarget)
            {
                AutoLog(
                    "  FAIL KEYBOARD-FOCUS Backstage scope included the wrong tab content "
                    + $"home={hasHomeTarget} recent={hasRecentTarget} "
                    + $"targets={string.Join(',', backstageTargets.Select(target => $"{target.Element.GetType().Name}:{target.Keys}"))}");
                return;
            }

            InvokeKeyTipDismissal(service, "DismissForPointerInput");
            BackstageView.OnKeyTipBack();
            var closingSurfaceRemainedVisible =
                !BackstageView.AreAnimationsEnabled
                || BackstageView.AdornerLayer?.Visibility == Visibility.Visible;
            await SettleAsync(1);
            if (BackstageView.IsOpen
                || !closingSurfaceRemainedVisible
                || BackstageView.AdornerLayer?.Visibility != Visibility.Collapsed
                || !ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot), focusProbe))
            {
                AutoLog(
                    "  FAIL KEYBOARD-FOCUS backstage did not animate closed and restore focus "
                    + $"closingVisible={closingSurfaceRemainedVisible} "
                    + $"visibility={BackstageView.AdornerLayer?.Visibility}");
                return;
            }

            pageRoot.Children.Add(startScreen);
            MainRibbon.StartScreen = startScreen;
            focusProbe.Focus(FocusState.Programmatic);
            service.Show();
            var startScreenTargets = GetKeyTipTargets(service);
            if (startScreenTargets.All(target => !ReferenceEquals(target.Element, startScreen)))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS root scope did not expose the configured StartScreen");
                return;
            }

            InvokeKeyTipTarget(service, startScreen);
            await SettleAsync(1);
            if (!startScreen.IsOpen
                || !startScreen.Shown
                || !MainRibbon.IsBackstageOrStartScreenOpen
                || !service.IsActive
                || GetKeyTipScopeDepth(service) < 2)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS StartScreen nested scope was not established");
                return;
            }

            InvokeKeyTipBack(service);
            await SettleAsync(1);
            if (startScreen.IsOpen
                || GetKeyTipScopeDepth(service) != 1
                || !ReferenceEquals(
                    Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot),
                    firstTab))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS StartScreen back navigation did not return to root");
                return;
            }

            service.Hide();
            if (!ReferenceEquals(
                    Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(xamlRoot),
                    focusProbe))
            {
                AutoLog("  FAIL KEYBOARD-FOCUS StartScreen final dismissal did not restore focus");
                return;
            }

            var informationProbe = new Button();
            var information = new KeyTipInformation("I", informationProbe, hide: false);
            informationProbe.IsEnabled = false;
            var disabledState = information.IsEnabled;
            informationProbe.IsEnabled = true;
            var enabledState = information.IsEnabled;
            informationProbe.Visibility = Visibility.Collapsed;
            var hiddenState = information.IsEnabled;
            if (disabledState || !enabledState || hiddenState)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS KeyTipInformation did not track live enabled/visible state");
                return;
            }

            object? helpSender = new object();
            object? helpTopic = null;
            EventHandler<ScreenTipHelpEventArgs> helpHandler = (sender, args) =>
            {
                helpSender = sender;
                helpTopic = args.HelpTopic;
            };
            var screenTip = new ScreenTip
            {
                HelpTopic = "keyboard-help",
                Width = 0,
                Height = 0,
                Opacity = 0,
                IsHitTestVisible = false,
            };
            ScreenTip.HelpPressed += helpHandler;
            try
            {
                pageRoot.Children.Add(screenTip);
                await SettleAsync(1);
                var handled = InvokePrivateResult<bool>(
                    screenTip,
                    "TryHandleHelpKey",
                    VirtualKey.F1,
                    false);
                var alreadyHandled = InvokePrivateResult<bool>(
                    screenTip,
                    "TryHandleHelpKey",
                    VirtualKey.F1,
                    true);
                if (!handled
                    || alreadyHandled
                    || helpSender is not null
                    || !Equals(helpTopic, "keyboard-help")
                    || screenTip.HelpLabelActualVisibility != Visibility.Visible
#if WINDOWS
                    || !Equals(GetScreenTipAcceleratorKey(screenTip), "F1")
#endif
                    )
                {
                    AutoLog(
                        "  FAIL KEYBOARD-FOCUS ScreenTip F1/help semantics were incorrect "
                        + $"handled={handled} alreadyHandled={alreadyHandled} "
                        + $"senderNull={helpSender is null} topic={helpTopic ?? "<null>"} "
                        + $"helpVisibility={screenTip.HelpLabelActualVisibility} "
                        + $"isLoaded={screenTip.IsLoaded}");
                    return;
                }

                var keyboardRootField = typeof(ScreenTip)
                    .GetField("_keyboardRoot", BindingFlags.Instance | BindingFlags.NonPublic);
                if (keyboardRootField?.GetValue(screenTip) is null)
                {
                    AutoLog("  FAIL KEYBOARD-FOCUS ScreenTip did not attach its active F1 handler");
                    return;
                }

                pageRoot.Children.Remove(screenTip);
                await SettleAsync(1);
                if (keyboardRootField.GetValue(screenTip) is not null)
                {
                    AutoLog("  FAIL KEYBOARD-FOCUS ScreenTip retained its F1 handler after unload");
                    return;
                }
            }
            finally
            {
                pageRoot.Children.Remove(screenTip);
                ScreenTip.HelpPressed -= helpHandler;
            }

            service.Show();
            service.Detach();
            var retainedRoot = typeof(KeyTipService)
                .GetField("_rootElement", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(service);
            if (service.IsActive || retainedRoot is not null)
            {
                AutoLog("  FAIL KEYBOARD-FOCUS unload cleanup retained active KeyTip state");
                return;
            }

            service.Attach();
            AutoLog("  PASS activation, matching, nested scopes, focus, filtering, help, and cleanup");
        }
        catch (Exception ex)
        {
            AutoLog($"  KEYBOARD-FOCUS THREW {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            service.Hide();
            if (dynamicallyDisabledProbe is not null)
            {
                dynamicallyDisabledProbe.IsEnabled = true;
            }

            applicationMenu?.Close();
            BackstageView.IsOpen = false;
            startScreen.IsOpen = false;
            MainRibbon.StartScreen = originalStartScreen;
            MainRibbon.IsMinimized = originalMinimized;
            MainRibbon.IsCollapsed = originalCollapsed;
            firstGroup.State = originalGroupState;
            MainRibbon.SelectedTab = originalSelectedTab;

            if (applicationMenu is not null)
            {
                applicationMenu.KeyTip = originalApplicationMenuKeyTip;
            }

            if (firstMenuItem is not null)
            {
                firstMenuItem.KeyTip = originalMenuItemKeyTip ?? string.Empty;
                firstMenuItem.Command = originalMenuItemCommand;
            }

            MainRibbon.KeyTipKeys.Clear();
            foreach (var key in originalKeys)
            {
                MainRibbon.KeyTipKeys.Add(key);
            }

            firstGroup.Items.Remove(enabledButton);
            firstGroup.Items.Remove(disabledButton);
            firstGroup.Items.Remove(hiddenButton);
            menuNavigationHost.IsDropDownOpen = false;
            pageRoot.Children.Remove(menuNavigationHost);
            pageRoot.Children.Remove(startScreen);
            pageRoot.Children.Remove(focusProbe);
        }

        AutoLog("KEYBOARD-FOCUS END");
    }

    private static IReadOnlyList<KeyTipTargetSnapshot> GetKeyTipTargets(KeyTipService service)
    {
        var targets = (IEnumerable?)typeof(KeyTipService)
            .GetField("_targets", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(service);
        if (targets is null)
        {
            return Array.Empty<KeyTipTargetSnapshot>();
        }

        var result = new List<KeyTipTargetSnapshot>();
        foreach (var target in targets)
        {
            if (target is null)
            {
                continue;
            }

            var type = target.GetType();
            if (type.GetProperty("Element")?.GetValue(target) is FrameworkElement element
                && type.GetProperty("Keys")?.GetValue(target) is string keys
                && type.GetProperty("Visual")?.GetValue(target) is FrameworkElement visual)
            {
                result.Add(new KeyTipTargetSnapshot(
                    element,
                    keys,
                    visual.Visibility == Visibility.Visible));
            }
        }

        return result;
    }

    private static int GetKeyTipScopeDepth(KeyTipService service)
    {
        var stack = typeof(KeyTipService)
            .GetField("_scopeStack", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(service);
        return (int?)stack?.GetType().GetProperty("Count")?.GetValue(stack) ?? 0;
    }

    private static string GetKeyTipScopeNames(KeyTipService service)
    {
        var stack = (IEnumerable?)typeof(KeyTipService)
            .GetField("_scopeStack", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(service);
        if (stack is null)
        {
            return string.Empty;
        }

        return string.Join(
            ">",
            stack.Cast<object>().Select(frame =>
                frame.GetType().GetProperty("Container")?.GetValue(frame)?.GetType().Name ?? "?"));
    }

    private static void InvokeKeyTipCharacter(KeyTipService service, char character)
    {
        typeof(KeyTipService)
            .GetMethod("AppendKey", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(service, new object[] { character });
    }

    private static void InvokeKeyTipBack(KeyTipService service)
    {
        typeof(KeyTipService)
            .GetMethod("NavigateBack", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(service, null);
    }

    private static void InvokeMenuNavigation(MenuItem item, VirtualKey key)
    {
        typeof(MenuItem)
            .GetMethod(
                "HandleMenuNavigationKey",
                BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(item, new object[] { key });
    }

    private static void InvokeKeyTipDismissal(KeyTipService service, string methodName)
    {
        typeof(KeyTipService)
            .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(service, null);
    }

    private static object? GetScreenTipAcceleratorKey(ScreenTip screenTip)
    {
        // WinRT projects AutomationProperties.AcceleratorKeyProperty as a static *property*,
        // not a field (unlike WPF), so probe both before giving up — otherwise this always
        // reads null and the assertion compares null against "F1".
        var type = typeof(AutomationProperties);
        var property = type
            .GetProperty("AcceleratorKeyProperty", BindingFlags.Public | BindingFlags.Static)
            ?.GetValue(null) as DependencyProperty
            ?? type
                .GetField("AcceleratorKeyProperty", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null) as DependencyProperty;

        return property is null ? null : screenTip.GetValue(property);
    }

    private static void InvokeKeyTipRebuild(KeyTipService service)
    {
        typeof(KeyTipService)
            .GetMethod("RebuildTargets", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(service, null);
    }

    private static void InvokeKeyTipTarget(KeyTipService service, FrameworkElement element)
    {
        var targets = (IEnumerable?)typeof(KeyTipService)
            .GetField("_targets", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(service);
        var target = targets?.Cast<object>().FirstOrDefault(candidate =>
            ReferenceEquals(
                candidate.GetType().GetProperty("Element")?.GetValue(candidate),
                element));
        if (target is null)
        {
            throw new InvalidOperationException(
                $"No KeyTip target exists for {element.GetType().Name}.");
        }

        typeof(KeyTipService)
            .GetMethod("Activate", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.Invoke(service, new[] { target });
    }

    private sealed record KeyTipTargetSnapshot(
        FrameworkElement Element,
        string Keys,
        bool IsVisible);

    private sealed class AutoTestCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
