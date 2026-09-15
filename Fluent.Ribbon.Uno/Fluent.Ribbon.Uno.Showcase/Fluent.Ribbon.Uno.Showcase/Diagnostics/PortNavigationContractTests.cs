namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

internal static class PortNavigationContractTests
{
    public static async Task VerifyKeyTipScopesAsync(Panel host, Func<Task> settle)
    {
        await VerifyKeyTipHostCoexistenceAsync(host, settle);

        var ribbon = CreateRibbon();
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Navigation focus backup" };
        var surface = CreateSurface(ribbon, editor);
        var leafInvocations = 0;
        var launcherInvocations = 0;
        var aliasInvocations = 0;
        var tab = new RibbonTabItem { Header = "WPF tab", KeyTip = "H" };
        var alias = new RibbonTab { Header = "Uno alias", KeyTip = "U" };
        var group = new RibbonGroupBox
        {
            Header = "Commands",
            KeyTip = "G",
            IsLauncherVisible = true,
            LauncherKeys = "L",
            LauncherCommand = new ProbeCommand(() => launcherInvocations++),
        };
        var button = new Fluent.Button { Header = "Regular", KeyTip = "R" };
        var disabled = new Fluent.Button { Header = "Disabled", KeyTip = "X", IsEnabled = false };
        var hidden = new Fluent.Button { Header = "Hidden", KeyTip = "Y", Visibility = Visibility.Collapsed };
        var dropDown = new Fluent.DropDownButton { Header = "Nested", KeyTip = "D" };
        var parent = new Fluent.MenuItem { Header = "WPF submenu", KeyTip = "M" };
        var leaf = new Fluent.MenuItem
        {
            Header = "Run",
            KeyTip = "N",
            Command = new ProbeCommand(() => leafInvocations++),
        };
        var aliasMenu = new RibbonMenuItem
        {
            Header = "Uno menu alias",
            KeyTip = "A",
            Command = new ProbeCommand(() => aliasInvocations++),
        };
        parent.Items.Add(leaf);
        dropDown.Items.Add(parent);
        dropDown.Items.Add(aliasMenu);
        group.Items.Add(button);
        group.Items.Add(disabled);
        group.Items.Add(hidden);
        group.Items.Add(dropDown);
        tab.Groups.Add(group);
        var aliasButton = new Fluent.Button { Header = "Alias command", KeyTip = "B" };
        var aliasGroup = new RibbonGroupBox { Header = "Alias commands" };
        aliasGroup.Items.Add(aliasButton);
        alias.Groups.Add(aliasGroup);
        ribbon.Tabs.Add(tab);
        ribbon.Tabs.Add(alias);
        var service = GetField<KeyTipService>(ribbon, "_keyTipService");
        host.Children.Add(surface);
        try
        {
            await settle();
            editor.Focus(FocusState.Programmatic);
            var applicationInputRoot = GetField<FrameworkElement>(service, "_rootElement");
            Check(RouteKey(service, applicationInputRoot, VirtualKey.F10),
                "The registered application input route did not activate KeyTips.");
            Check(HasTarget(service, tab) && HasTarget(service, alias) && !HasTarget(service, button),
                "Root KeyTips did not expose both tab contracts or leaked a tab's commands.");
            await Press(service, "H", settle);
            Check(service.IsActive && ReferenceEquals(ribbon.SelectedTab, tab) && ribbon.SelectedTabIndex == 0
                  && HasTarget(service, button) && !HasTarget(service, aliasButton)
                  && !HasTarget(service, disabled) && !HasTarget(service, hidden),
                "The WPF tab did not enter an eligible, selected-tab-only KeyTip scope. "
                + $"Active={service.IsActive}, Selected={ribbon.SelectedTab?.Header}, Index={ribbon.SelectedTabIndex}, "
                + $"Targets={DescribeTargets(service)}");
            foreach (var expandedState in new[]
                     {
                         RibbonGroupBoxState.Large,
                         RibbonGroupBoxState.Medium,
                         RibbonGroupBoxState.Small,
                     })
            {
                group.State = expandedState;
                Invoke(service, "RebuildTargets");
                await settle();
                Check(service.IsActive && HasTarget(service, button) && HasTarget(service, dropDown)
                      && !HasTarget(service, disabled) && !HasTarget(service, hidden),
                    $"Expanded group state {expandedState} hid commands or admitted ineligible targets: {DescribeTargets(service)}");
            }

            group.State = RibbonGroupBoxState.Large;
            await Press(service, "G", settle);
            Check(ScopeDepth(service) == 3 && HasTarget(service, button),
                "An expanded ribbon group did not establish its logical scope.");
            Back(service);
            await settle();
            Check(ScopeDepth(service) == 2, "Expanded-group back navigation did not return to its tab.");
            await Press(service, "L", settle);
            Check(launcherInvocations == 1 && !service.IsActive, "The WPF launcher KeyTip did not invoke its command.");

            editor.Focus(FocusState.Programmatic);
            service.Show();
            await Press(service, "H", settle);
            await Press(service, "D", settle);
            Check(dropDown.IsDropDownOpen && HasTarget(service, parent) && HasTarget(service, aliasMenu)
                  && !HasTarget(service, leaf),
                "A closed WPF submenu leaked descendants or failed to expose its own KeyTip. "
                + $"Open={dropDown.IsDropDownOpen}, ParentTarget={HasTarget(service, parent)}, "
                + $"AliasTarget={HasTarget(service, aliasMenu)}, LeafTarget={HasTarget(service, leaf)}, "
                + $"Parent={DescribeElement(parent)}, Alias={DescribeElement(aliasMenu)}, Leaf={DescribeElement(leaf)}, "
                + $"Scope={DescribeScope(service)}, Targets={DescribeTargets(service)}");
            await Press(service, "M", settle);
            Check(parent.IsDropDownOpen && parent.DropDownPopup?.IsOpen == true
                  && HasTarget(service, leaf) && !HasTarget(service, aliasMenu),
                "The WPF MenuItem did not enter a real, isolated submenu scope.");
            Back(service);
            await settle();
            Check(!parent.IsDropDownOpen && dropDown.IsDropDownOpen
                  && ReferenceEquals(CurrentFocus(ribbon), parent),
                "Submenu back navigation failed to close and restore focus to its WPF owner.");
            await Press(service, "M", settle);
            await Press(service, "N", settle);
            Check(leafInvocations == 1 && !service.IsActive,
                "A nested WPF KeyTip did not invoke exactly once. "
                + $"Invocations={leafInvocations}, Active={service.IsActive}, Scope={DescribeScope(service)}, "
                + $"Targets={DescribeTargets(service)}, Leaf={DescribeElement(leaf)}");
            dropDown.IsDropDownOpen = false;
            parent.IsDropDownOpen = false;
            await settle();

            service.Show();
            await Press(service, "U", settle);
            Check(ReferenceEquals(ribbon.SelectedTab, alias) && HasTarget(service, aliasButton),
                "Repairing WPF scopes regressed the Uno RibbonTab alias.");
            Back(service);
            service.Hide();
            ribbon.SelectedTab = tab;
            await settle();
            service.Show();
            await Press(service, "H", settle);
            await Press(service, "D", settle);
            await Press(service, "A", settle);
            Check(aliasInvocations == 1, "The RibbonMenuItem alias no longer participates in dropdown scopes.");
            dropDown.IsDropDownOpen = false;
            await settle();

            group.State = RibbonGroupBoxState.Collapsed;
            await settle();
            editor.Focus(FocusState.Programmatic);
            service.Show();
            await Press(service, "H", settle);
            Check(HasTarget(service, group) && !HasTarget(service, button),
                "A closed collapsed group leaked its commands into the tab scope.");
            await Press(service, "G", settle);
            Check(group.IsDropDownOpen && group.DropDownPopup?.IsOpen == true && HasTarget(service, button),
                "The collapsed group's actual popup did not establish its command scope. "
                + $"Requested={group.IsDropDownOpen}, Actual={group.DropDownPopup?.IsOpen}, State={group.State}, "
                + $"Group={DescribeElement(group)}, Button={DescribeElement(button)}, Scope={DescribeScope(service)}, "
                + $"Targets={DescribeTargets(service)}, Pending={GetField<bool>(service, "_scopeRefreshPending")}, "
                + $"Eligible={Invoke(service, "IsEligible", button)}, "
                + $"PopupContainsButton={(group.DropDownPopup?.Child is DependencyObject popupChild && IsDescendant(button, popupChild))}, "
                + $"ButtonPath={DescribeVisualPath(button)}");
            Back(service);
            await settle();
            Check(!group.IsDropDownOpen && ScopeDepth(service) == 2, "Collapsed-group back navigation failed.");
            Back(service);
            service.Hide();
            Check(ReferenceEquals(CurrentFocus(ribbon), editor),
                "Leaving nested KeyTips did not restore the pre-KeyTip focus.");

            group.State = RibbonGroupBoxState.Large;
            ribbon.IsMinimized = true;
            await settle();
            service.Show();
            await Press(service, "H", settle);
            Check(ribbon.TabControl?.DropDownPopup?.IsOpen == true && HasTarget(service, dropDown),
                "A minimized WPF tab did not expose commands from its actual popup.");
            await Press(service, "D", settle);
            Check(ribbon.TabControl!.DropDownPopup!.IsOpen && dropDown.IsDropDownOpen,
                "Opening a nested dropdown closed its minimized-tab parent popup.");
            Back(service);
            await settle();
            Check(!dropDown.IsDropDownOpen && ribbon.TabControl.DropDownPopup.IsOpen,
                "Backing out of a nested dropdown closed the parent tab popup.");
            Back(service);
            await settle();
            Check(!ribbon.TabControl.IsDropDownOpen && ScopeDepth(service) == 1,
                "Backing out of a minimized tab left its popup or nested scope open.");
            service.Hide();

            service.Show();
            host.Children.Remove(surface);
            await settle();
            Check(!service.IsActive && GetField<object?>(service, "_rootElement") is null,
                "Unloading the ribbon retained active KeyTip input or scope state.");
            Check(!RouteKey(service, applicationInputRoot, VirtualKey.F10) && !service.IsActive,
                "A detached input root still activates this ribbon's KeyTip service.");
            host.Children.Add(surface);
            await settle();
            ribbon.IsMinimized = false;
            service.Show();
            await Press(service, "H", settle);
            Check(HasTarget(service, button), "Reloading the ribbon did not restore WPF KeyTip navigation.");
            await VerifyTabPresentationRefreshAsync(
                ribbon, service, tab, alias, button, aliasButton, editor, settle);
        }
        finally
        {
            service.Hide();
            parent.IsDropDownOpen = false;
            dropDown.IsDropDownOpen = false;
            group.IsDropDownOpen = false;
            if (ribbon.TabControl is { } tabControl)
            {
                tabControl.IsDropDownOpen = false;
            }

            host.Children.Remove(surface);
            await settle();
        }
    }

    public static async Task VerifyMinimizedContentAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        ribbon.ContentHeight = 134;
        ribbon.IsMinimized = true;
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Popup focus backup" };
        var surface = CreateSurface(ribbon, editor);
        var first = new RibbonTabItem { Header = "First" };
        var second = new RibbonTabItem { Header = "Second" };
        var firstButton = new Fluent.Button { Header = "First popup command" };
        var secondButton = new Fluent.Button { Header = "Second popup command" };
        var firstGroup = new RibbonGroupBox { Header = "First group" };
        var secondGroup = new RibbonGroupBox { Header = "Second group" };
        firstGroup.Items.Add(firstButton);
        secondGroup.Items.Add(secondButton);
        first.Groups.Add(firstGroup);
        second.Groups.Add(secondGroup);
        ribbon.Tabs.Add(first);
        ribbon.Tabs.Add(second);
        host.Children.Add(surface);
        try
        {
            await settle();
            var tabs = ribbon.TabControl ?? throw new InvalidOperationException("No rooted RibbonTabControl.");
            Check(first.GroupsContainer.Height == 134 && tabs.ContentHeight == 134,
                "Minimizing erased authored content height before popup creation.");
            var compactHeight = ribbon.ActualHeight;
            editor.Focus(FocusState.Programmatic);
            tabs.IsDropDownOpen = true;
            await settle();
            AssertPopupGeometry(tabs.DropDownPopup, ribbon.XamlRoot, firstButton);
            var closedDuringTransfer = 0;
            var reopenedDuringTransfer = 0;
            tabs.DropDownClosed += (_, _) => closedDuringTransfer++;
            tabs.DropDownOpened += (_, _) => reopenedDuringTransfer++;
            Check(Math.Abs(ribbon.ActualHeight - compactHeight) < 3,
                "The transient popup expanded the ribbon's inline layout.");
            Check(ReferenceEquals(tabs.SelectedContent, first.Content)
                  && tabs.SelectedContentPresenter is ContentPresenter presenter
                  && ReferenceEquals(presenter.Content, first.Content),
                "The public selected-content surface does not describe the popup's real content.");
            var inline = FindVisual<ContentPresenter>(tabs, element => element.Name == "PART_ContentPresenter");
            Check(inline is { Visibility: Visibility.Collapsed } && inline.Content is null,
                "Minimized content is still parented or visible in the inline presenter.");
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(first);
            var expand = peer?.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
            Check(expand?.ExpandCollapseState == ExpandCollapseState.Expanded,
                "Tab automation did not report the real open popup.");

            ribbon.SelectedTab = second;
            await settle();
            AssertPopupGeometry(tabs.DropDownPopup, ribbon.XamlRoot, secondButton);
            var secondPopupContent = tabs.DropDownPopup?.Child
                                     ?? throw new InvalidOperationException("The second tab has no popup content.");
            Check(!IsDescendant(firstButton, secondPopupContent)
                  && first.Content is UIElement firstContent
                  && VisualTreeHelper.GetParent(firstContent) is null,
                "Switching an open popup retained or double-parented the previous tab content.");
            var secondContentReady = Invoke(tabs, "IsContentPresented", second);
            Check(closedDuringTransfer == 0 && reopenedDuringTransfer == 0 && tabs.IsDropDownOpen
                  && Math.Abs(ribbon.ActualHeight - compactHeight) < 3
                  && CurrentFocus(ribbon) is DependencyObject switchedFocus
                  && IsDescendant(switchedFocus, secondPopupContent),
                "A selected-tab transfer dismissed the popup, expanded the ribbon, or restored background focus. "
                + $"Closed={closedDuringTransfer}, Reopened={reopenedDuringTransfer}, Requested={tabs.IsDropDownOpen}, "
                + $"Actual={tabs.DropDownPopup?.IsOpen}, Height={ribbon.ActualHeight}, Baseline={compactHeight}, "
                + $"Delta={ribbon.ActualHeight - compactHeight}, Focus={DescribeFocus(ribbon)}, "
                + $"Selected={ribbon.SelectedTab?.Header}, Ready={secondContentReady}");

            tabs.SelectedItem = null;
            tabs.SelectedItem = second;
            await settle();
            AssertPopupGeometry(tabs.DropDownPopup, ribbon.XamlRoot, secondButton);
            Check(closedDuringTransfer == 0,
                "A transient null selection was treated as final popup dismissal.");

            var originalSecondContent = second.Content;
            var replacementContent = new Fluent.Button { Header = "Live content replacement" };
            second.Content = replacementContent;
            await settle();
            AssertPopupGeometry(tabs.DropDownPopup, ribbon.XamlRoot, replacementContent);
            Check(closedDuringTransfer == 0 && Math.Abs(ribbon.ActualHeight - compactHeight) < 3,
                "Replacing selected content dismissed or expanded the minimized ribbon.");
            second.Content = originalSecondContent;
            await settle();
            AssertPopupGeometry(tabs.DropDownPopup, ribbon.XamlRoot, secondButton);
            Check(GetField<IDictionary>(tabs, "_releasingContent").Count == 0,
                "A completed content handoff retained native unload subscriptions.");

            ribbon.ContentHeight = 180;
            await settle();
            Check(second.GroupsContainer.Height == 180 && ribbon.ContentHeight == 180,
                "Changing content height while open did not reach the popup.");
            ribbon.IsSimplified = true;
            await settle();
            Check(second.IsSimplified && second.GroupsContainer.Height > 0
                  && second.GroupsContainer.Height < 100 && ribbon.ContentHeight == 180,
                "Simplified popup presentation erased the authored classic height or became zero-height.");
            AssertPopupGeometry(tabs.DropDownPopup, ribbon.XamlRoot, secondButton);
            ribbon.IsSimplified = false;
            await settle();
            Check(second.GroupsContainer.Height == 180, "Leaving simplified mode lost the authored height.");

            tabs.DropDownPopup!.IsOpen = false;
            await settle();
            Check(!tabs.IsDropDownOpen && !tabs.DropDownPopup.IsOpen
                  && closedDuringTransfer == 1
                  && ReferenceEquals(CurrentFocus(ribbon), editor),
                "Light dismissal did not synchronize state and restore focus.");
            tabs.IsDropDownOpen = true;
            await settle();
            Check((bool)Invoke(tabs, "TryClosePopupOnEscape")!, "Escape did not close the minimized popup.");
            await settle();
            Check(!tabs.IsDropDownOpen && !tabs.DropDownPopup.IsOpen, "Escape left a live popup behind.");

            tabs.IsDropDownOpen = true;
            await settle();
            ribbon.IsMinimized = false;
            await settle();
            Check(!tabs.IsDropDownOpen && !tabs.DropDownPopup.IsOpen
                  && inline!.Visibility == Visibility.Visible
                  && ReferenceEquals(inline.Content, second.Content)
                  && ribbon.ActualHeight > compactHeight + 80,
                "Unminimizing did not restore the same content to the expanded inline presenter.");
            ribbon.IsMinimized = true;
            tabs.IsDropDownOpen = true;
            await settle();
            ribbon.Tabs.Remove(second);
            await settle();
            Check(!tabs.IsDropDownOpen && !tabs.DropDownPopup.IsOpen
                  && !second.IsSelected && second.Content is UIElement secondContent
                  && VisualTreeHelper.GetParent(secondContent) is null,
                "Removing the selected tab retained its transient popup or content ownership.");
            tabs.IsDropDownOpen = true;
            await settle();
            var popup = tabs.DropDownPopup;
            host.Children.Remove(surface);
            await settle();
            Check(popup?.IsOpen != true && !tabs.IsDropDownOpen,
                "Unloading the ribbon left a native popup open.");
            host.Children.Add(surface);
            await settle();
            tabs = ribbon.TabControl!;
            tabs.IsDropDownOpen = true;
            await settle();
            AssertPopupGeometry(tabs.DropDownPopup, ribbon.XamlRoot, firstButton);
        }
        finally
        {
            if (ribbon.TabControl is { } tabs)
            {
                tabs.IsDropDownOpen = false;
            }

            host.Children.Remove(surface);
            await settle();
        }

        var pendingTab = new RibbonTabItem { Header = "Preloaded popup" };
        var pendingButton = new Fluent.Button { Header = "Ready after load" };
        var pendingGroup = new RibbonGroupBox { Header = "Pending" };
        pendingGroup.Items.Add(pendingButton);
        pendingTab.Groups.Add(pendingGroup);
        var standalone = new RibbonTabControl { IsMinimized = true, ContentHeight = 120 };
        standalone.TabItems.Add(pendingTab);
        standalone.SelectedItem = pendingTab;
        standalone.IsDropDownOpen = true;
        host.Children.Add(standalone);
        try
        {
            await settle();
            AssertPopupGeometry(standalone.DropDownPopup, standalone.XamlRoot, pendingButton);
        }
        finally
        {
            standalone.IsDropDownOpen = false;
            host.Children.Remove(standalone);
            await settle();
        }
    }

    private static async Task VerifyTabPresentationRefreshAsync(
        Ribbon ribbon,
        KeyTipService service,
        RibbonTabItem first,
        RibbonTabItem second,
        FrameworkElement firstCommand,
        FrameworkElement secondCommand,
        FrameworkElement editor,
        Func<Task> settle)
    {
        var tabs = ribbon.TabControl ?? throw new InvalidOperationException("The tab presentation probe must be rooted.");
        var inputRoot = GetField<FrameworkElement>(service, "_rootElement");
        foreach (var minimized in new[] { false, true })
        {
            service.Hide();
            ribbon.IsMinimized = minimized;
            editor.Focus(FocusState.Programmatic);
            service.Show();
            for (var round = 0; round < 4; round++)
            {
                if (ScopeDepth(service) > 1)
                {
                    Back(service);
                }

                Check(RouteKey(service, inputRoot, (VirtualKey)first.KeyTip[0]),
                    "The rapid WPF-tab request was not handled by the registered root.");
                Back(service);
                Check(RouteKey(service, inputRoot, (VirtualKey)second.KeyTip[0]),
                    "The rapid Uno-tab request was not handled by the registered root.");
                Back(service);

                var selected = round % 2 == 0 ? first : second;
                var expected = round % 2 == 0 ? firstCommand : secondCommand;
                var excluded = round % 2 == 0 ? secondCommand : firstCommand;
                await Press(service, selected.KeyTip, settle);
                Check(service.IsActive && ScopeDepth(service) == 2
                      && ReferenceEquals(ribbon.SelectedTab, selected)
                      && ribbon.SelectedTabIndex == ribbon.Tabs.IndexOf(selected)
                      && HasTarget(service, expected) && !HasTarget(service, excluded),
                    $"A superseded {(minimized ? "popup" : "inline")} transfer published stale KeyTips: {DescribeTargets(service)}. "
                    + $"Round={round}, Wanted={selected.Header}, Selected={ribbon.SelectedTab?.Header}, "
                    + $"ControlSelection={(tabs.SelectedItem as RibbonTabItem)?.Header}, Depth={ScopeDepth(service)}, "
                    + $"Requested={tabs.IsDropDownOpen}, Actual={tabs.DropDownPopup?.IsOpen}, Scope={DescribeScope(service)}");
                Check(CurrentFocus(ribbon) is DependencyObject focused
                      && (ReferenceEquals(focused, selected)
                          || (selected.Content is DependencyObject content && IsDescendant(focused, content))),
                    "Completing a tab transfer restored focus into a stale scope.");
                var subscriptions = GetField<Delegate?>(tabs, "ContentPresentationChanged");
                Check(subscriptions?.GetInvocationList().Length == 1,
                    "Rapid scope changes stacked tab-content readiness subscriptions.");
                Check(GetField<object?>(tabs, "_contentReadinessRoot") is null,
                    "A completed presentation retained its layout-readiness observation.");
                if (minimized)
                {
                    AssertPopupGeometry(tabs.DropDownPopup, ribbon.XamlRoot, expected);
                }
            }

            Back(service);
            service.Hide();
            Check(GetField<Delegate?>(tabs, "ContentPresentationChanged") is null,
                "Leaving tab KeyTips retained a content-presentation subscription.");
            Check(ReferenceEquals(CurrentFocus(ribbon), editor),
                "Cancelling queued tab scopes lost the original focus backup.");
        }

        ribbon.IsMinimized = false;
    }

    private static async Task VerifyKeyTipHostCoexistenceAsync(Panel host, Func<Task> settle)
    {
        var unrelatedAction = new Fluent.Button { Header = "Unrelated inline action", KeyTip = "Z" };
        var unrelated = new StartScreen
        {
            Content = unrelatedAction,
            IsOpen = true,
            AreAnimationsEnabled = false,
        };
        var inlineHost = new Grid { Height = 90 };
        inlineHost.Children.Add(unrelated);
        var ribbon = CreateRibbon();
        var tab = new RibbonTabItem { Header = "Owned tab", KeyTip = "H" };
        ribbon.Tabs.Add(tab);
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Coexisting host focus" };
        var surface = CreateSurface(ribbon, editor);
        var service = GetField<KeyTipService>(ribbon, "_keyTipService");
        host.Children.Add(inlineHost);
        host.Children.Add(surface);
        try
        {
            await settle();
            Check(unrelated.IsOpen && unrelated.IsLoaded && unrelatedAction.ActualHeight > 0,
                "The coexistence probe must include a genuinely open, rooted inline StartScreen.");
            editor.Focus(FocusState.Programmatic);
            service.Show();
            Check(ScopeDepth(service) == 1 && HasTarget(service, tab) && !HasTarget(service, unrelatedAction),
                "Root KeyTips selected an unrelated inline StartScreen in the same XamlRoot.");
            service.Hide();

            var backstageAction = new Fluent.Button { Header = "Owned Backstage action", KeyTip = "B" };
            var backstage = new Backstage { Content = backstageAction, AreAnimationsEnabled = false };
            ribbon.Menu = backstage;
            await settle();
            backstage.IsOpen = true;
            await settle();
            service.Show();
            Check(ScopeDepth(service) == 2 && HasTarget(service, backstageAction)
                  && !HasTarget(service, unrelatedAction) && !HasTarget(service, tab),
                "Restricting initial scope discovery lost the ribbon's explicitly owned Backstage.");
            service.Hide();
            backstage.IsOpen = false;
            ribbon.Menu = null;

            var startupAction = new Fluent.Button { Header = "Owned startup action", KeyTip = "S" };
            var owned = new StartScreen
            {
                Content = startupAction,
                IsOpen = true,
                AreAnimationsEnabled = false,
            };
            ribbon.StartScreen = owned;
            await settle();
            service.Show();
            await settle();
            Check(HasTarget(service, startupAction) && !HasTarget(service, unrelatedAction)
                  && !HasTarget(service, tab),
                "Coexisting inline content displaced the ribbon's configured application StartScreen scope.");
            service.Hide();
            ribbon.StartScreen = null;
            await settle();
            Check(unrelated.IsOpen, "Application-surface navigation changed an unrelated inline demonstration.");
        }
        finally
        {
            service.Hide();
            ribbon.StartScreen = null;
            if (ribbon.Menu is Backstage backstage)
            {
                backstage.IsOpen = false;
            }

            unrelated.IsOpen = false;
            host.Children.Remove(surface);
            host.Children.Remove(inlineHost);
            await settle();
        }
    }

    public static async Task VerifyStartScreenHostingAsync(Panel host, Func<Task> settle)
    {
        await VerifyLockedOpenStateAsync(host, settle);
        await VerifySurfaceHandoffAndInlineAnimationAsync(host, settle);
        await VerifyNestedApplicationPopupsAsync(host, settle);

        var ribbon = CreateRibbon();
        ribbon.Tabs.Add(new RibbonTabItem { Header = "Home" });
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "StartScreen focus backup" };
        var surface = CreateSurface(ribbon, editor);
        var create = new Fluent.Button { Header = "Create", KeyTip = "C" };
        var recent = new Fluent.Button { Header = "Recent", KeyTip = "R" };
        var screen = new StartScreen { IsOpen = true, AreAnimationsEnabled = false, KeyTip = "S" };
        screen.LeftPaneContent = recent;
        screen.Content = create;
        ribbon.StartScreen = screen;
        Check(screen.IsOpen && ((Backstage)screen).IsOpen
              && ReferenceEquals(((Backstage)screen).Content, create),
            "Early StartScreen state/content diverged from the inherited Backstage contract.");
        host.Children.Add(surface);
        var service = GetField<KeyTipService>(ribbon, "_keyTipService");
        try
        {
            await settle();
            var popup = GetField<Popup>(ribbon, "_startScreenPopup");
            AssertWindowOverlay(popup, ribbon.XamlRoot, screen, create);
            Check(screen.Shown && ribbon.IsBackstageOrStartScreenOpen
                  && ribbon.ActualHeight < ((FrameworkElement)popup.Child).ActualHeight,
                "Ribbon.StartScreen was not shown as a window-level overlay.");
            Check(ReferenceEquals(StartScreen.IsOpenProperty, Backstage.IsOpenProperty)
                  && ReferenceEquals(StartScreen.CloseOnEscProperty, Backstage.CloseOnEscProperty),
                "StartScreen still exposes a separate base open/escape state.");
            var automation = FrameworkElementAutomationPeer.CreatePeerForElement(screen)
                ?.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider;
            Check(automation?.ExpandCollapseState == ExpandCollapseState.Expanded,
                "StartScreen automation does not describe the shown overlay.");
            var popupInputRoot = GetField<FrameworkElement>(ribbon, "_startScreenPopupRoot");
            Check(RouteKey(service, popupInputRoot, VirtualKey.F10),
                "The registered StartScreen popup input route did not activate KeyTips.");
            await settle();
            Check(HasTarget(service, create) && HasTarget(service, recent)
                  && !HasTarget(service, ribbon.Tabs[0]),
                "An application StartScreen did not isolate its KeyTip scope from the ribbon.");
            service.Hide();

            screen.CanChangeIsOpen = false;
            ((Backstage)screen).IsOpen = false;
            Check(screen.IsOpen && popup.IsOpen, "StartScreen ignored CanChangeIsOpen through the base contract.");
            screen.CanChangeIsOpen = true;
            ((Backstage)screen).CloseOnEsc = false;
            Check(!screen.CloseOnEsc && !(bool)Invoke(screen, "TryCloseOnEscape")! && screen.IsOpen,
                "StartScreen ignored the inherited CloseOnEsc contract.");
            screen.CloseOnEsc = true;
            Check((bool)Invoke(screen, "TryCloseOnEscape")!, "Escape did not close the StartScreen overlay.");
            await settle();
            Check(!screen.IsOpen && !((Backstage)screen).IsOpen && !popup.IsOpen
                  && !ribbon.IsBackstageOrStartScreenOpen,
                "Closing StartScreen left base state, popup, or parent-ribbon state open.");
            Check(!RouteKey(service, popupInputRoot, VirtualKey.F10),
                "A closed StartScreen popup retained its routed-input registration.");
            screen.IsOpen = true;
            await settle();
            Check(!screen.IsOpen && !popup.IsOpen, "Shown did not suppress a repeated application startup screen.");
            editor.Focus(FocusState.Programmatic);
            screen.Shown = false;
            screen.IsOpen = true;
            await settle();
            AssertWindowOverlay(popup, ribbon.XamlRoot, screen, create);
            screen.IsOpen = false;
            await settle();
            Check(ReferenceEquals(CurrentFocus(ribbon), editor),
                "Closing an application StartScreen did not restore the previous focus.");

            var late = new StartScreen { IsOpen = true, AreAnimationsEnabled = false };
            ribbon.StartScreen = late;
            await settle();
            var lateContent = new Fluent.Button { Header = "Late base content" };
            ((Backstage)late).Content = lateContent;
            await settle();
            Check(ReferenceEquals(late.Content, lateContent) && late.IsOpen && ((Backstage)late).IsOpen,
                "Late base Content assignment did not satisfy the early StartScreen open request.");
            popup = GetField<Popup>(ribbon, "_startScreenPopup");
            AssertWindowOverlay(popup, ribbon.XamlRoot, late, lateContent);
            var replacement = new StartScreen
            {
                Content = new Fluent.Button { Header = "Replacement" },
                IsOpen = true,
                AreAnimationsEnabled = false,
            };
            ribbon.StartScreen = replacement;
            await settle();
            Check(!late.IsOpen && VisualTreeHelper.GetParent(late) is null
                  && replacement.IsOpen && ribbon.IsBackstageOrStartScreenOpen,
                "Replacing an open StartScreen retained the old state or visual owner. "
                + $"OldOpen={late.IsOpen}, OldParent={VisualTreeHelper.GetParent(late)?.GetType().Name}, "
                + $"OldLoaded={late.IsLoaded}, NewOpen={replacement.IsOpen}, ParentOpen={ribbon.IsBackstageOrStartScreenOpen}, "
                + $"NewParent={VisualTreeHelper.GetParent(replacement)?.GetType().Name}, Popup={popup.IsOpen}");
            AssertWindowOverlay(popup, ribbon.XamlRoot, replacement, (FrameworkElement)replacement.Content!);
            replacement.CanChangeIsOpen = false;
            ribbon.StartScreen = null;
            await settle();
            Check(!replacement.IsOpen && !popup.IsOpen && !ribbon.IsBackstageOrStartScreenOpen
                  && VisualTreeHelper.GetParent(replacement) is null,
                "Clearing a locked StartScreen failed to tear down its overlay.");

            replacement.CanChangeIsOpen = true;
            replacement.Shown = false;
            replacement.IsOpen = true;
            ribbon.StartScreen = replacement;
            await settle();
            host.Children.Remove(surface);
            await settle();
            Check(!popup.IsOpen && !replacement.IsOpen && !ribbon.IsBackstageOrStartScreenOpen,
                "Unloading a ribbon retained its StartScreen overlay or open state.");
            host.Children.Add(surface);
            await settle();
            replacement.Shown = false;
            replacement.IsOpen = true;
            await settle();
            AssertWindowOverlay(popup, ribbon.XamlRoot, replacement, (FrameworkElement)replacement.Content!);
            ribbon.StartScreen = null;
            await settle();

            var inline = new StartScreen
            {
                Content = new Fluent.Button { Header = "Inline demonstration" },
                Height = 160,
                AreAnimationsEnabled = false,
            };
            Grid.SetRow(inline, 1);
            surface.Children.Add(inline);
            await settle();
            inline.IsOpen = true;
            await settle();
            Check(inline.IsOpen && !ribbon.IsBackstageOrStartScreenOpen,
                "An inline StartScreen incorrectly claimed application-level ribbon state.");
            inline.IsOpen = false;
            inline.IsOpen = true;
            await settle();
            Check(inline.IsOpen && ReferenceEquals(VisualTreeHelper.GetParent(inline), surface),
                "Application-level hosting broke explicitly authored inline StartScreen demonstrations.");
            inline.IsOpen = false;
            surface.Children.Remove(inline);
        }
        finally
        {
            service.Hide();
            ribbon.StartScreen = null;
            host.Children.Remove(surface);
            await settle();
        }
    }

    private static async Task VerifyLockedOpenStateAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        ribbon.Tabs.Add(new RibbonTabItem { Header = "Lock state" });
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Lock focus" };
        var surface = CreateSurface(ribbon, editor);
        var model = new OpenStateModel();
        var screen = new StartScreen
        {
            Content = new Fluent.Button { Header = "Interactive while locked" },
            AreAnimationsEnabled = false,
        };
        screen.SetBinding(Backstage.IsOpenProperty, new Binding
        {
            Source = model,
            Path = new PropertyPath(nameof(OpenStateModel.IsOpen)),
            Mode = BindingMode.OneWay,
        });
        ribbon.StartScreen = screen;
        host.Children.Add(surface);
        try
        {
            await settle();
            model.IsOpen = true;
            await settle();
            var popup = GetField<Popup>(ribbon, "_startScreenPopup");
            var layer = screen.AdornerLayer
                        ?? throw new InvalidOperationException("The open screen has no realized presentation layer.");
            screen.CanChangeIsOpen = false;
            model.IsOpen = false;
            await settle();
            Check(!model.IsOpen && screen.IsOpen && ((Backstage)screen).IsOpen
                  && (bool)screen.GetValue(Backstage.IsOpenProperty)
                  && layer.IsHitTestVisible && popup.IsOpen
                  && screen.GetBindingExpression(Backstage.IsOpenProperty) is not null,
                "A locked bound write diverged the effective DP, CLR state, template hit testing, or binding.");
            screen.CanChangeIsOpen = true;
            await settle();
            Check(!screen.IsOpen && !(bool)screen.GetValue(Backstage.IsOpenProperty)
                  && !popup.IsOpen && !ribbon.IsBackstageOrStartScreenOpen
                  && screen.GetBindingExpression(Backstage.IsOpenProperty) is not null,
                "Unlocking did not apply the pending bound close without replacing its binding.");
            screen.Shown = false;
            screen.CanChangeIsOpen = false;
            model.IsOpen = true;
            await settle();
            Check(model.IsOpen && !screen.IsOpen && !(bool)screen.GetValue(Backstage.IsOpenProperty) && !popup.IsOpen,
                "A locked bound open changed effective template or popup state.");
            screen.CanChangeIsOpen = true;
            await settle();
            Check(screen.IsOpen && popup.IsOpen && (bool)screen.GetValue(Backstage.IsOpenProperty),
                "Unlocking did not apply a pending bound open.");
            model.IsOpen = false;
            await settle();

            screen.ClearValue(Backstage.IsOpenProperty);
            screen.Shown = false;
            screen.CanChangeIsOpen = false;
            screen.SetValue(Backstage.IsOpenProperty, true);
            Check(!screen.IsOpen && !(bool)screen.GetValue(Backstage.IsOpenProperty),
                "A direct SetValue bypassed the closed-state lock.");
            screen.CanChangeIsOpen = true;
            await settle();
            Check(screen.IsOpen && popup.IsOpen, "A pending direct open was lost on unlock.");
            screen.CanChangeIsOpen = false;
            screen.SetValue(Backstage.IsOpenProperty, false);
            Check(screen.IsOpen && (bool)screen.GetValue(Backstage.IsOpenProperty)
                  && screen.AdornerLayer?.IsHitTestVisible == true,
                "A direct SetValue disabled hit testing on a locked open screen.");
            screen.CanChangeIsOpen = true;
            await settle();
            Check(!screen.IsOpen && !popup.IsOpen, "A pending direct close was lost on unlock.");
        }
        finally
        {
            screen.CanChangeIsOpen = true;
            ribbon.StartScreen = null;
            host.Children.Remove(surface);
            await settle();
        }
    }

    private static async Task VerifySurfaceHandoffAndInlineAnimationAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        var tab = new RibbonTabItem { Header = "Application handoff" };
        ribbon.Tabs.Add(tab);
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Original application focus" };
        var surface = CreateSurface(ribbon, editor);
        var backstage = new Backstage { Content = new Fluent.Button { Header = "Backstage action" } };
        ribbon.Menu = backstage;
        var screen = new StartScreen { Content = new Fluent.Button { Header = "Startup action" }, KeyTip = "I" };
        var service = GetField<KeyTipService>(ribbon, "_keyTipService");
        host.Children.Add(surface);
        try
        {
            await settle();
            editor.Focus(FocusState.Programmatic);
            backstage.IsOpen = true;
            await settle();
            Check(backstage.IsOpen && ribbon.IsBackstageOrStartScreenOpen
                  && backstage.AdornerLayer?.Visibility == Visibility.Visible,
                "The rooted Backstage did not establish its ribbon application surface.");
            ribbon.StartScreen = screen;
            screen.IsOpen = true;
            await settle();
            Check(!backstage.IsOpen && backstage.AdornerLayer?.Visibility == Visibility.Collapsed
                  && screen.IsOpen && ribbon.IsBackstageOrStartScreenOpen,
                "Opening StartScreen did not exclusively hand off the existing Backstage surface.");
            var popup = GetField<Popup>(ribbon, "_startScreenPopup");
            var closingLayer = screen.AdornerLayer
                               ?? throw new InvalidOperationException("The active StartScreen has no layer.");
            Check(screen.AreAnimationsEnabled, "The animation regression must exercise the default setting.");
            screen.IsOpen = false;
            Check(closingLayer.Visibility == Visibility.Visible && ribbon.IsBackstageOrStartScreenOpen,
                "Default closing animation discarded its visible application surface prematurely.");
            await settle();
            await settle();
            Check(!popup.IsOpen && !ribbon.IsBackstageOrStartScreenOpen
                  && closingLayer.Visibility == Visibility.Collapsed
                  && ReferenceEquals(CurrentFocus(ribbon), editor),
                "Animated close after handoff left stale state or restored focus into the retired Backstage.");

            screen.Shown = false;
            screen.IsOpen = true;
            await settle();
            backstage.IsOpen = true;
            await settle();
            Check(!screen.IsOpen && !popup.IsOpen && backstage.IsOpen && ribbon.IsBackstageOrStartScreenOpen,
                "Reverse application-surface handoff retained StartScreen or lost Backstage accounting.");
            screen.IsOpen = false;
            Check(backstage.IsOpen && ribbon.IsBackstageOrStartScreenOpen,
                "Closing the retired StartScreen cleared another open Backstage's ribbon state.");
            backstage.IsOpen = false;
            await settle();
            await settle();
            Check(!ribbon.IsBackstageOrStartScreenOpen && ReferenceEquals(CurrentFocus(ribbon), editor),
                "Closing the final animated surface did not restore original focus and ribbon state.");

            ribbon.StartScreen = null;
            var inline = new StartScreen
            {
                Content = new Fluent.Button { Header = "Explicit inline keyboard host", KeyTip = "A" },
                KeyTip = "I",
            };
            Grid.SetRowSpan(inline, 2);
            surface.Children.Add(inline);
            await settle();
            ribbon.StartScreen = inline;
            editor.Focus(FocusState.Programmatic);
            service.Show();
            await Press(service, "I", settle);
            Check(inline.IsOpen && inline.AreAnimationsEnabled
                  && ReferenceEquals(VisualTreeHelper.GetParent(inline), surface),
                "The existing explicitly inline application host was reparented or failed to open.");
            var inlineLayer = inline.AdornerLayer
                              ?? throw new InvalidOperationException("The inline screen did not realize its layer.");
            Back(service);
            Check(!inline.IsOpen && inlineLayer.Visibility == Visibility.Visible,
                "Inline KeyTip back did not begin the default closing animation.");
            await settle();
            await settle();
            Check(ScopeDepth(service) == 1 && inlineLayer.Visibility == Visibility.Collapsed
                  && ReferenceEquals(CurrentFocus(ribbon), tab),
                "Animated inline back navigation failed to restore the root tab focus.");
            service.Hide();
            Check(ReferenceEquals(CurrentFocus(ribbon), editor),
                "Final inline KeyTip dismissal lost the original editor focus.");
            ribbon.StartScreen = null;
            surface.Children.Remove(inline);
        }
        finally
        {
            service.Hide();
            backstage.CanChangeIsOpen = true;
            backstage.IsOpen = false;
            ribbon.StartScreen = null;
            host.Children.Remove(surface);
            await settle();
        }
    }

    private static async Task VerifyNestedApplicationPopupsAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        ribbon.Tabs.Add(new RibbonTabItem { Header = "Nested application menus" });
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Nested popup focus" };
        var surface = CreateSurface(ribbon, editor);
        var parent = new Fluent.DropDownButton { Header = "Flyout-backed parent" };
        var nested = new Fluent.MenuItem { Header = "Nested child" };
        var executions = 0;
        var leaf = new Fluent.MenuItem { Header = "Leaf command", Command = new ProbeCommand(() => executions++) };
        nested.Items.Add(leaf);
        parent.Items.Add(nested);
        var screen = new StartScreen { Content = parent, AreAnimationsEnabled = false };
        ribbon.StartScreen = screen;
        host.Children.Add(surface);
        try
        {
            await settle();
            screen.IsOpen = true;
            await settle();
            parent.IsDropDownOpen = true;
            PopupService.RaiseDismissPopupEvent(parent, DismissPopupMode.Always);
            await settle();
            Check(screen.IsOpen && parent.IsDropDownOpen && nested.IsLoaded,
                "An opening child dropdown was dismissed by the StartScreen broadcast subscriber.");
            Check(((IDropDownControl)parent).DropDownPopup is null,
                "The regression must exercise a Flyout-backed ancestor without a Popup property.");
            nested.IsDropDownOpen = true;
            PopupService.RaiseDismissPopupEvent(nested, DismissPopupMode.Always);
            await settle();
            Check(screen.IsOpen && parent.IsDropDownOpen && nested.DropDownPopup?.IsOpen == true
                  && leaf.IsLoaded && leaf.ActualHeight > 0,
                "Nested opening closed a real Flyout parent or its containing StartScreen.");
            ((IKeyTipedControl)leaf).OnKeyTipPressed();
            await settle();
            await settle();
            Check(executions == 1 && !nested.IsDropDownOpen && !parent.IsDropDownOpen
                  && !screen.IsOpen && !ribbon.IsBackstageOrStartScreenOpen,
                "Ordinary leaf-command dismissal did not close the complete popup/application chain.");
        }
        finally
        {
            nested.IsDropDownOpen = false;
            parent.IsDropDownOpen = false;
            ribbon.StartScreen = null;
            host.Children.Remove(surface);
            await settle();
        }
    }

    private static Ribbon CreateRibbon() => new()
    {
        Width = 760,
        AutomaticStateManagement = false,
        IsAutomaticCollapseEnabled = false,
    };

    private static Grid CreateSurface(Ribbon ribbon, FrameworkElement editor)
    {
        var grid = new Grid { Height = 380 };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(ribbon);
        Grid.SetRow(editor, 1);
        grid.Children.Add(editor);
        return grid;
    }

    private static async Task Press(KeyTipService service, string keys, Func<Task> settle)
    {
        foreach (var character in keys)
        {
            var upper = char.ToUpperInvariant(character);
            var key = upper is >= 'A' and <= 'Z'
                ? (VirtualKey)((int)VirtualKey.A + upper - 'A')
                : (VirtualKey)((int)VirtualKey.Number0 + upper - '0');
            Check(RouteKey(service, InputRootForFocus(service), key),
                $"The active input route did not handle KeyTip character '{character}'.");
        }

        await settle();
    }

    private static void Back(KeyTipService service) =>
        Check(RouteKey(service, InputRootForFocus(service), VirtualKey.Escape),
            "The active popup/root input route did not handle KeyTip back navigation.");

    private static bool RouteKey(KeyTipService service, FrameworkElement root, VirtualKey key) =>
        (bool)(Invoke(service, "ProcessKeyDown", root, key, false, false)
               ?? throw new InvalidOperationException("The input route returned no handled result."));

    private static FrameworkElement InputRootForFocus(KeyTipService service)
    {
        var mainRoot = GetField<FrameworkElement>(service, "_rootElement");
        var focused = CurrentFocus(mainRoot) as DependencyObject;
        return GetField<IEnumerable>(service, "_attachedInputRoots").Cast<FrameworkElement>()
                   .FirstOrDefault(root => focused is not null && IsDescendant(focused, root))
               ?? mainRoot;
    }

    private static bool HasTarget(KeyTipService service, FrameworkElement target) =>
        GetField<IEnumerable>(service, "_targets").Cast<object>().Any(entry =>
            ReferenceEquals(entry.GetType().GetProperty("Element")!.GetValue(entry), target));

    private static string DescribeTargets(KeyTipService service) =>
        string.Join(", ", GetField<IEnumerable>(service, "_targets").Cast<object>().Select(entry =>
            $"{entry.GetType().GetProperty("Keys")?.GetValue(entry)}:"
            + $"{entry.GetType().GetProperty("Element")?.GetValue(entry)?.GetType().Name}"));

    private static string DescribeElement(FrameworkElement element) =>
        $"{element.GetType().Name}({element.Name}, loaded={element.IsLoaded}, rooted={element.XamlRoot is not null}, "
        + $"visible={element.Visibility}, enabled={(element is Control control ? control.IsEnabled : true)}, "
        + $"size={element.ActualWidth}x{element.ActualHeight})";

    private static string DescribeScope(KeyTipService service)
    {
        var frame = GetField<IEnumerable>(service, "_scopeStack").Cast<object>().FirstOrDefault();
        if (frame is null)
        {
            return "none";
        }

        var owner = frame.GetType().GetProperty("Container")?.GetValue(frame) as FrameworkElement;
        var presentation = Invoke(service, "GetScopePresentationRoot", frame) as FrameworkElement;
        return $"{(owner is null ? "none" : DescribeElement(owner))} -> "
               + (presentation is null ? "none" : DescribeElement(presentation));
    }

    private static string DescribeFocus(FrameworkElement owner)
    {
        return DescribeVisualPath(CurrentFocus(owner) as DependencyObject);
    }

    private static string DescribeVisualPath(DependencyObject? element)
    {
        var path = new List<string>();
        for (var current = element; current is not null && path.Count < 8;
             current = VisualTreeHelper.GetParent(current))
        {
            path.Add(current is FrameworkElement frameworkElement
                ? DescribeElement(frameworkElement)
                : current.GetType().Name);
        }

        return path.Count == 0 ? "none" : string.Join(" > ", path);
    }

    private static int ScopeDepth(KeyTipService service) =>
        GetField<IEnumerable>(service, "_scopeStack").Cast<object>().Count();

    private static T GetField<T>(object owner, string name) =>
        (T)(owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing regression observation field '{name}'.")).GetValue(owner)!;

    private static object? Invoke(object owner, string method, params object[] arguments)
    {
        try
        {
            for (var type = owner.GetType(); type is not null; type = type.BaseType)
            {
                if (type.GetMethod(method,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } member)
                {
                    return member.Invoke(owner, arguments);
                }
            }

            throw new InvalidOperationException($"Missing navigation path '{method}'.");
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static T? FindVisual<T>(DependencyObject root, Func<T, bool> predicate)
        where T : DependencyObject
    {
        if (root is T value && predicate(value))
        {
            return value;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            if (FindVisual(VisualTreeHelper.GetChild(root, index), predicate) is { } match)
            {
                return match;
            }
        }

        return default;
    }

    private static bool IsDescendant(DependencyObject element, DependencyObject ancestor)
    {
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    private static object? CurrentFocus(FrameworkElement owner)
    {
        var root = owner.XamlRoot ?? throw new InvalidOperationException("The focus probe must be rooted.");
        return FocusManager.GetFocusedElement(root);
    }

    private static void AssertPopupGeometry(Popup? popup, XamlRoot? root, FrameworkElement command)
    {
        if (root is null)
        {
            throw new InvalidOperationException("The popup geometry probe must have a XamlRoot.");
        }

        var openPopup = popup ?? throw new InvalidOperationException("The minimized ribbon has no native popup.");
        var child = openPopup.Child as FrameworkElement
                    ?? throw new InvalidOperationException("The minimized ribbon popup has no framework content.");
        Check(openPopup.IsOpen, "The minimized ribbon popup is not open.");
        Check(child.ActualWidth > 20 && child.ActualHeight > 20
              && openPopup.HorizontalOffset >= -1 && openPopup.VerticalOffset >= -1
              && openPopup.HorizontalOffset + child.ActualWidth <= root.Size.Width + 2
              && openPopup.VerticalOffset + child.ActualHeight <= root.Size.Height + 2,
            "The minimized popup is zero-sized or extends beyond the XamlRoot viewport.");
        var owningGroup = FindVisual<RibbonGroupBox>(child, group => group.Items.Contains(command));
        Check(IsDescendant(command, child) && command.IsLoaded
              && command.ActualWidth > 0 && command.ActualHeight > 0,
            "The actual command content is not visibly realized inside the popup. "
            + $"Command={DescribeElement(command)}, Descendant={IsDescendant(command, child)}, "
            + $"Parent={VisualTreeHelper.GetParent(command)?.GetType().Name}, Root={DescribeElement(child)}, "
            + $"Focus={DescribeFocus(child)}, Group={(owningGroup is null ? "none" : DescribeGroup(owningGroup))}");
    }

    private static string DescribeGroup(RibbonGroupBox group)
    {
        var panel = GetField<Panel?>(group, "_itemsPanel");
        var binding = GetField<object>(group, "itemsBinding");
        var suspended = binding.GetType().GetProperty("IsSuspended",
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(binding);
        return $"{DescribeElement(group)}, items={group.Items.Count}, panelChildren={panel?.Children.Count}, "
               + $"suspended={suspended}, panel={(panel is null ? "none" : DescribeElement(panel))}";
    }

    private static void AssertWindowOverlay(Popup popup, XamlRoot? root, StartScreen screen, FrameworkElement content)
    {
        if (root is null)
        {
            throw new InvalidOperationException("The StartScreen geometry probe must have a XamlRoot.");
        }

        var child = popup.Child as FrameworkElement
                    ?? throw new InvalidOperationException("The StartScreen popup has no framework content.");
        Check(popup.IsOpen && ReferenceEquals(popup.XamlRoot, root),
            "Ribbon.StartScreen did not open an XamlRoot-owned popup.");
        Check(Math.Abs(child.ActualWidth - root.Size.Width) < 3
              && Math.Abs(child.ActualHeight - root.Size.Height) < 3
              && IsDescendant(screen, child) && IsDescendant(content, child)
              && content.IsLoaded && content.ActualWidth > 0 && content.ActualHeight > 0,
            "StartScreen content is not visibly hosted across the actual window viewport.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class ProbeCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute();
    }

    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed class OpenStateModel : INotifyPropertyChanged
    {
        private bool isOpen;
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsOpen
        {
            get => isOpen;
            set
            {
                if (isOpen == value)
                {
                    return;
                }

                isOpen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
            }
        }
    }
}
