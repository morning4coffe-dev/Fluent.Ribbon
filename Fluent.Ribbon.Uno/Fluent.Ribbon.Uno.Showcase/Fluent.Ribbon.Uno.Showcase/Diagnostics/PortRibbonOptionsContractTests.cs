namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;

internal static class PortRibbonOptionsContractTests
{
    public static async Task VerifyAsync(Panel host, Func<Task> settle)
    {
        await VerifyRibbonChromeAsync(host, settle);
        await VerifyStandaloneTabChromeAsync(host, settle);
        await VerifyAuthoredTabOptionsAsync(host, settle, bound: false);
        await VerifyAuthoredTabOptionsAsync(host, settle, bound: true);
        await VerifyQuickAccessContextAsync(host, settle);
        await VerifyCompatibilityContextAsync(host, settle);
        await VerifyQuickAccessMenuAsync(host, settle);
        await VerifyQuickAccessMenuRetirementAsync(host, settle);
        await VerifyQuickAccessOverflowTransitionsAsync(host, settle);
        await VerifyStandaloneQuickAccessLocationAsync(host, settle);
    }

    private static async Task VerifyRibbonChromeAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        var expected = CaptureTabPresentation(ribbon.Tabs);
        ribbon.IsQuickAccessToolBarVisible = false;
        var headers = new ToggleSwitch { IsOn = true };
        var options = new ToggleSwitch { IsOn = true };
        var headerBinding = BindSwitch(ribbon, Ribbon.AreTabHeadersVisibleProperty, headers);
        var optionsBinding = BindSwitch(ribbon, Ribbon.IsDisplayOptionsButtonVisibleProperty, options);
        host.Children.Add(ribbon);
        try
        {
            await settle();
            var tabs = ribbon.TabControl ?? throw new InvalidOperationException("The ribbon template did not create its tab control.");
            await VerifyChromeAsync(ribbon, tabs, value => headers.IsOn = value, value => options.IsOn = value, expected, settle);
            Require(ReferenceEquals(ribbon.GetBindingExpression(Ribbon.AreTabHeadersVisibleProperty)?.ParentBinding, headerBinding)
                    && ReferenceEquals(ribbon.GetBindingExpression(Ribbon.IsDisplayOptionsButtonVisibleProperty)?.ParentBinding, optionsBinding),
                "Ribbon option changes replaced the caller's bindings.");
            Require(headers.IsOn && options.IsOn, "Template synchronization wrote back into the option models.");
        }
        finally
        {
            if (ribbon.TabControl is { } tabs)
            {
                tabs.IsDropDownOpen = false;
            }
            host.Children.Remove(ribbon);
            await settle();
        }
    }

    private static async Task VerifyStandaloneTabChromeAsync(Panel host, Func<Task> settle)
    {
        var tabs = new RibbonTabControl { Width = 700 };
        tabs.TabItems.Add(CreateTab("Standalone A"));
        tabs.TabItems.Add(CreateTab("Standalone B"));
        tabs.SelectedItem = tabs.TabItems[0];
        var expected = CaptureTabPresentation(tabs.TabItems.Cast<RibbonTabItem>());
        var headers = new ToggleSwitch { IsOn = true };
        var options = new ToggleSwitch { IsOn = true };
        var headerBinding = BindSwitch(tabs, RibbonTabControl.AreTabHeadersVisibleProperty, headers);
        var optionsBinding = BindSwitch(tabs, RibbonTabControl.IsDisplayOptionsButtonVisibleProperty, options);
        host.Children.Add(tabs);
        try
        {
            await settle();
            await VerifyChromeAsync(null, tabs, value => headers.IsOn = value, value => options.IsOn = value, expected, settle);
            Require(ReferenceEquals(tabs.GetBindingExpression(RibbonTabControl.AreTabHeadersVisibleProperty)?.ParentBinding, headerBinding)
                    && ReferenceEquals(tabs.GetBindingExpression(RibbonTabControl.IsDisplayOptionsButtonVisibleProperty)?.ParentBinding, optionsBinding),
                "Standalone tab chrome replaced authored bindings.");
        }
        finally
        {
            tabs.IsDropDownOpen = false;
            host.Children.Remove(tabs);
            await settle();
        }
    }

    private static async Task VerifyChromeAsync(
        Ribbon? ribbon,
        RibbonTabControl tabs,
        Action<bool> showHeaders,
        Action<bool> showOptions,
        IReadOnlyDictionary<RibbonTabItem, (object? Content, object? Model)> expected,
        Func<Task> settle)
    {
        var originalTabs = tabs.TabItems.Cast<RibbonTabItem>().ToArray();
        Require(originalTabs.Length == expected.Count && originalTabs.All(expected.ContainsKey),
            "Initial template realization replaced authored tab identities.");
        var first = originalTabs[0];
        var second = originalTabs[1];
        var headerStrip = Part(tabs, "TabListView");
        var optionsButton = Part(tabs, "PART_DisplayOptionsButton");
        var menu = new NativeButton { Content = "Application menu" };
        var footer = new NativeButton { Content = "Authored tab toolbar" };
        var titleAction = new NativeButton { Content = "Title action" };
        tabs.TabStripFooter = footer;
        if (ribbon is not null)
        {
            ribbon.Menu = menu;
            ribbon.ToolBarItems.Add(titleAction);
        }
        else
        {
            tabs.TabStripHeader = menu;
        }
        await settle();
        Require(HasVisibleBounds(headerStrip) && HasVisibleBounds(optionsButton),
            "The actual header strip or display-options button was not rendered.");
        Require(AutomationProperties.GetName(optionsButton)
                == RibbonLocalization.Current.Localization.DisplayOptionsButtonScreenTipTitle,
            "Display-options chrome lost its localized accessible name.");
        AssertTabPresented(first);
        var firstModel = (PortEditorHeaderModel)expected[first].Model!;
        firstModel.Title += " updated";
        await settle();
        AssertTabPresented(first);

        showHeaders(false);
        await settle();
        Require(!tabs.AreTabHeadersVisible && headerStrip.Visibility == Visibility.Collapsed,
            "AreTabHeadersVisible did not hide the native header strip.");
        Require(originalTabs.All(tab => tab.Visibility == Visibility.Visible),
            "Hiding the strip rewrote tab visibility instead of preserving selectable tabs.");
        Require(HasVisibleBounds(optionsButton) && HasVisibleBounds(menu) && HasVisibleBounds(footer),
            "Hiding headers also hid the application menu, toolbar, or display-options button.");
        AssertTabPresented(first);
        Select(second);
        await settle();
        Require(headerStrip.Visibility == Visibility.Collapsed && !tabs.AreTabHeadersVisible,
            "Preparing an unseen tab's content exposed its hidden header strip.");
        AssertTabPresented(second);
        var secondModel = (PortEditorHeaderModel)expected[second].Model!;
        secondModel.Title += " updated";
        await settle();
        AssertTabPresented(second);

        showOptions(false);
        await settle();
        Require(!tabs.IsDisplayOptionsButtonVisible && optionsButton.Visibility == Visibility.Collapsed,
            "IsDisplayOptionsButtonVisible did not hide the actual options button.");
        Require(HasVisibleBounds(menu) && HasVisibleBounds(footer)
                && (ribbon is null || HasVisibleBounds(titleAction)),
            "Hiding display options collapsed unrelated toolbar/application chrome.");
        Require(OpenOptions(tabs) is null, "Hidden display options still accepted a user opening request.");
        AssertTabPresented(second);

        tabs.TabStripFooter = null;
        if (ribbon is not null)
        {
            ribbon.Menu = null;
        }
        else
        {
            tabs.TabStripHeader = null;
        }
        showHeaders(true);
        await settle();
        var presenter = Presenter(tabs);
        var withHeaders = presenter.TransformToVisual(tabs).TransformPoint(default).Y;
        var heightWithHeaders = tabs.ActualHeight;
        Require(HasVisibleBounds(headerStrip), "Header restoration did not restore real strip geometry.");
        showHeaders(false);
        await settle();
        Require(presenter.TransformToVisual(tabs).TransformPoint(default).Y < withHeaders - 1
                && tabs.ActualHeight < heightWithHeaders - 1,
            "Hiding all header-row chrome left an empty header row in the rendered layout.");
        AssertTabPresented(second);

        SetMinimized(true);
        await settle();
        var keyTip = first.OnKeyTipPressed();
        await settle();
        Require(keyTip.PressedElementOpenedPopup && tabs.DropDownPopup?.IsOpen == true,
            "Hidden headers prevented minimized KeyTip navigation from opening the selected tab.");
        AssertTabPresented(first);
        Select(second);
        await settle();
        Require(tabs.DropDownPopup?.IsOpen == true, "Hidden-header selection closed the minimized content popup.");
        AssertTabPresented(second);
        showOptions(true);
        showHeaders(true);
        await settle();
        Require(tabs.DropDownPopup?.IsOpen == true && HasVisibleBounds(optionsButton),
            "Restoring display chrome closed or replaced the minimized content presentation.");
        AssertTabPresented(second);
        tabs.IsDropDownOpen = false;
        SetMinimized(false);
        await settle();

        var displayMenu = RequireMenu(OpenOptions(tabs), "The display-options button did not open its real menu.");
        await settle();
        AssertMenuRendered(displayMenu);
        var minimize = MenuAction(displayMenu, RibbonLocalization.Current.Localization.MinimizeRibbon);
        Execute(minimize);
        await settle();
        Require(tabs.IsMinimized && (ribbon is null || ribbon.IsMinimized),
            "The display-options action did not update the standalone/root minimization state.");
        displayMenu = RequireMenu(OpenOptions(tabs), "Minimized ribbons lost their display-options menu.");
        await settle();
        Execute(MenuAction(displayMenu, RibbonLocalization.Current.Localization.ExpandRibbon));
        await settle();
        Require(!tabs.IsMinimized && (ribbon is null || !ribbon.IsMinimized),
            "The expand display option did not restore ribbon content.");
        AssertTabPresented(second);

        SetCanUseSimplified(true);
        await settle();
        displayMenu = RequireMenu(OpenOptions(tabs), "Simplified layout options were not available.");
        await settle();
        var simplified = MenuAction(displayMenu, RibbonLocalization.Current.Localization.UseSimplifiedRibbon);
        Execute(simplified);
        await settle();
        Require(tabs.IsSimplified && second.Groups[0].IsSimplified && (ribbon is null || ribbon.IsSimplified),
            "The display-options menu changed no actual ribbon/group layout state.");
        displayMenu = RequireMenu(OpenOptions(tabs), "Classic layout could not be restored.");
        await settle();
        Execute(MenuAction(displayMenu, RibbonLocalization.Current.Localization.UseClassicRibbon));
        await settle();
        Require(!tabs.IsSimplified && !second.Groups[0].IsSimplified && (ribbon is null || !ribbon.IsSimplified),
            "Restoring classic layout left simplified group or root state.");

        displayMenu = RequireMenu(OpenOptions(tabs), "The display-options permissions fixture did not open.");
        await settle();
        minimize = MenuAction(displayMenu, RibbonLocalization.Current.Localization.MinimizeRibbon);
        simplified = MenuAction(displayMenu, RibbonLocalization.Current.Localization.UseSimplifiedRibbon);
        SetCanMinimize(false);
        SetCanUseSimplified(false);
        ExecuteBlocked(minimize);
        ExecuteBlocked(simplified);
        await settle();
        Require(!tabs.IsMinimized && !tabs.IsSimplified && OpenOptions(tabs) is null,
            "A retained display command bypassed changed permissions or opened an empty menu.");
        SetCanMinimize(true);
        SetCanUseSimplified(true);
        await settle();
        displayMenu = RequireMenu(OpenOptions(tabs), "Restoring display permissions did not restore options.");
        var delayed = MenuAction(displayMenu, RibbonLocalization.Current.Localization.MinimizeRibbon);
        showOptions(false);
        ExecuteBlocked(delayed);
        await settle();
        Require(!tabs.IsMinimized && displayMenu.Items.OfType<MenuFlyoutItem>().All(item => !item.IsLoaded),
            "A deferred display-menu opening survived its button being hidden.");
        showOptions(true);
        await settle();
        Require(tabs.TabItems.Cast<RibbonTabItem>().SequenceEqual(originalTabs),
            "Option toggles replaced or reordered authored tab models.");
        AssertTabPresented(second);

        void AssertTabPresented(RibbonTabItem selected) =>
            AssertPresented(tabs, selected, ribbon, expected[selected]);

        void Select(RibbonTabItem tab)
        {
            if (ribbon is null) tabs.SelectedItem = tab;
            else ribbon.SelectedTab = tab;
        }

        void SetMinimized(bool value)
        {
            if (ribbon is null) tabs.IsMinimized = value;
            else ribbon.IsMinimized = value;
        }

        void SetCanMinimize(bool value)
        {
            if (ribbon is null) tabs.CanMinimize = value;
            else ribbon.CanMinimize = value;
        }

        void SetCanUseSimplified(bool value)
        {
            if (ribbon is null) tabs.CanUseSimplified = value;
            else ribbon.CanUseSimplified = value;
        }
    }

    private static async Task VerifyAuthoredTabOptionsAsync(Panel host, Func<Task> settle, bool bound)
    {
        var ribbon = CreateRibbon();
        var expected = CaptureTabPresentation(ribbon.Tabs);
        var model = new ToggleSwitch();
        ribbon.DataContext = bound ? null : model;
        var value = bound ? "{Binding IsOn, Mode=OneWay}" : "False";
        ribbon.Template = (ControlTemplate)XamlReader.Load($$"""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:fluent="using:Fluent"
                             TargetType="fluent:Ribbon">
                <fluent:RibbonTabControl x:Name="PART_RibbonTabControl"
                                         AreTabHeadersVisible="{{value}}"
                                         IsDisplayOptionsButtonVisible="{{value}}" />
            </ControlTemplate>
            """);
        host.Children.Add(ribbon);
        try
        {
            await settle();
            var tabs = ribbon.TabControl ?? throw new InvalidOperationException("The authored tab template was not applied.");
            var headerBinding = tabs.GetBindingExpression(RibbonTabControl.AreTabHeadersVisibleProperty)?.ParentBinding;
            var optionsBinding = tabs.GetBindingExpression(RibbonTabControl.IsDisplayOptionsButtonVisibleProperty)?.ParentBinding;
            if (bound)
            {
                Require(headerBinding is not null && optionsBinding is not null,
                    "Root synchronization replaced unresolved authored option bindings.");
                ribbon.DataContext = model;
                await settle();
            }
            AssertPresented(tabs, ribbon.Tabs[0], ribbon, expected[ribbon.Tabs[0]]);
            Require(!tabs.AreTabHeadersVisible && !tabs.IsDisplayOptionsButtonVisible,
                "Root defaults overwrote the tab-control template's authored options.");
            Require(Part(tabs, "TabListView").Visibility == Visibility.Collapsed
                    && Part(tabs, "PART_DisplayOptionsButton").Visibility == Visibility.Collapsed,
                "Authored tab-control option values did not affect rendered chrome.");
            ribbon.AreTabHeadersVisible = false;
            ribbon.IsDisplayOptionsButtonVisible = false;
            ribbon.AreTabHeadersVisible = true;
            ribbon.IsDisplayOptionsButtonVisible = true;
            await settle();
            Require(!tabs.AreTabHeadersVisible && !tabs.IsDisplayOptionsButtonVisible,
                "A root option update replaced an authored tab-control value.");
            AssertPresented(tabs, ribbon.Tabs[0], ribbon, expected[ribbon.Tabs[0]]);
            if (bound)
            {
                Require(headerBinding is not null && optionsBinding is not null,
                    "The authored option bindings were not created.");
                model.IsOn = true;
            }
            else
            {
                Require(headerBinding is null && optionsBinding is null,
                    "Root synchronization installed bindings over local template values.");
                tabs.AreTabHeadersVisible = true;
                tabs.IsDisplayOptionsButtonVisible = true;
            }
            ribbon.AreTabHeadersVisible = false;
            ribbon.IsDisplayOptionsButtonVisible = false;
            await settle();
            Require(tabs.AreTabHeadersVisible && tabs.IsDisplayOptionsButtonVisible
                    && HasVisibleBounds(Part(tabs, "TabListView"))
                    && HasVisibleBounds(Part(tabs, "PART_DisplayOptionsButton")),
                "Independent local/bound tab options stopped controlling native chrome.");
            Require(ReferenceEquals(tabs.GetBindingExpression(RibbonTabControl.AreTabHeadersVisibleProperty)?.ParentBinding, headerBinding)
                    && ReferenceEquals(tabs.GetBindingExpression(RibbonTabControl.IsDisplayOptionsButtonVisibleProperty)?.ParentBinding, optionsBinding),
                "Root updates replaced authored option binding identities.");
            AssertPresented(tabs, ribbon.Tabs[0], ribbon, expected[ribbon.Tabs[0]]);
        }
        finally
        {
            host.Children.Remove(ribbon);
            await settle();
        }
    }

    private static async Task VerifyQuickAccessContextAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        ribbon.CanQuickAccessLocationChanging = false;
        var source = new Fluent.Button { Header = "Context command" };
        ribbon.Tabs[0].Groups[0].Items.Add(source);
        var entry = new QuickAccessMenuItem { Header = "Context command", Target = source };
        ribbon.QuickAccessItems.Add(entry);
        host.Children.Add(ribbon);
        MenuFlyout? activeMenu = null;
        try
        {
            await settle();
            activeMenu = RequireMenu(OpenContext(source), "The source context menu did not open.");
            await settle();
            AssertMenuRendered(activeMenu);
            var add = MenuAction(activeMenu, RibbonLocalization.Current.Localization.RibbonContextMenuAddItem);
            ribbon.IsDefaultContextMenuEnabled = false;
            ExecuteBlocked(add);
            Require(OpenContext(source) is null && !ribbon.IsInQuickAccessToolBar(source),
                "Disabling the default context menu left an active source customization path.");
            await settle();
            ribbon.IsDefaultContextMenuEnabled = true;
            activeMenu = RequireMenu(OpenContext(source), "The default menu did not recover after re-enabling.");
            ribbon.IsDefaultContextMenuEnabled = false;
            await settle();
            Require(activeMenu.Items.OfType<MenuFlyoutItem>().All(item => !item.IsLoaded),
                "A queued right-tap menu appeared after default menus were disabled.");

            ribbon.CanCustomizeQuickAccessToolBarItems = false;
            var addCommand = (ICommand)Ribbon.AddToQuickAccessCommand;
            Require(!addCommand.CanExecute(source), "The add command ignored item-customization permission.");
            addCommand.Execute(source);
            Require(!ribbon.IsInQuickAccessToolBar(source), "Direct command execution bypassed item-customization permission.");
            ribbon.AddToQuickAccessToolBar(source);
            var copy = (FrameworkElement)ribbon.GetQuickAccessElements()[source];
            Require(entry.IsChecked && ribbon.IsInQuickAccessToolBar(copy),
                "User permissions incorrectly blocked programmatic QAT registration.");
            InvokeEntry(entry);
            Require(entry.IsChecked && ReferenceEquals(ribbon.GetQuickAccessElements()[source], copy),
                "A user checklist action bypassed permission or changed canonical QAT identity.");
            ribbon.RemoveFromQuickAccessToolBar(copy);
            Require(!entry.IsChecked && !ribbon.IsInQuickAccessToolBar(source),
                "User permissions blocked programmatic copy removal.");
            ribbon.CanCustomizeQuickAccessToolBarItems = true;
            source.CanAddToQuickAccessToolBar = false;
            Require(!addCommand.CanExecute(source), "The user add command ignored the provider's CanAdd permission.");
            addCommand.Execute(source);
            Require(!ribbon.IsInQuickAccessToolBar(source), "Execute bypassed the provider's CanAdd permission.");
            source.CanAddToQuickAccessToolBar = true;
            ribbon.IsDefaultContextMenuEnabled = true;

            activeMenu = RequireMenu(OpenContext(source), "Source customization did not recover.");
            await settle();
            Execute(MenuAction(activeMenu, RibbonLocalization.Current.Localization.RibbonContextMenuAddItem));
            await settle();
            copy = (FrameworkElement)ribbon.GetQuickAccessElements()[source];
            Require(entry.IsChecked && ReferenceEquals(ribbon.QuickAccessToolBar!.Items.Single(), copy),
                "Source context customization did not use the canonical QAT registration.");
            activeMenu = RequireMenu(OpenContext(copy), "The QAT clone did not receive its removal menu. "
                + $"loaded={copy.IsLoaded}, visible={HasVisibleBounds(copy)}, enabled={(copy as Control)?.IsEnabled}, "
                + $"context={copy.ContextFlyout}, local={copy.ReadLocalValue(FrameworkElement.ContextFlyoutProperty)}, "
                + $"binding={copy.GetBindingExpression(FrameworkElement.ContextFlyoutProperty)?.ParentBinding}, "
                + $"sourceEnabled={source.IsEnabled}, canCustomize={ribbon.CanCustomizeQuickAccessToolBarItems}, "
                + $"defaultMenu={ribbon.IsDefaultContextMenuEnabled}, qatVisible={ribbon.IsQuickAccessToolBarVisible}, "
                + $"path={DescribeVisualPath(copy)}");
            await settle();
            AssertMenuRendered(activeMenu);
            var remove = MenuAction(activeMenu, RibbonLocalization.Current.Localization.RibbonContextMenuRemoveItem);
            ribbon.CanCustomizeQuickAccessToolBarItems = false;
            ExecuteBlocked(remove);
            var removeCommand = (ICommand)Ribbon.RemoveFromQuickAccessCommand;
            Require(!removeCommand.CanExecute(copy), "The copy's command ignored changed customization permission.");
            removeCommand.Execute(copy);
            Require(ReferenceEquals(ribbon.GetQuickAccessElements()[source], copy) && entry.IsChecked,
                "A delayed clone command bypassed permission or recreated the provider copy.");
            ribbon.IsDefaultContextMenuEnabled = false;
            Require(OpenContext(source) is null && OpenContext(copy) is null,
                "Default-menu disabling did not cover both source and clone.");
            ribbon.CanCustomizeQuickAccessToolBarItems = true;
            ribbon.IsDefaultContextMenuEnabled = true;
            source.IsEnabled = false;
            await settle();
            Require(!removeCommand.CanExecute(copy) && OpenContext(source) is null,
                "A disabled source retained a user customization path through its clone.");
            removeCommand.Execute(copy);
            Require(ReferenceEquals(ribbon.GetQuickAccessElements()[source], copy),
                "A disabled source was removed by a posted clone command.");
            source.IsEnabled = true;
            await settle();

            activeMenu = RequireMenu(OpenContext(copy), "The clone removal menu did not recover.");
            await settle();
            remove = MenuAction(activeMenu, RibbonLocalization.Current.Localization.RibbonContextMenuRemoveItem);
            var authored = new MenuFlyout();
            authored.Items.Add(new MenuFlyoutItem { Text = "Authored context action" });
            copy.ContextFlyout = authored;
            ExecuteBlocked(remove);
            Require(OpenContext(copy) is null && ReferenceEquals(copy.ContextFlyout, authored)
                    && ReferenceEquals(ribbon.GetQuickAccessElements()[source], copy),
                "Default customization replaced or bypassed a newly authored clone context menu.");
            activeMenu.Hide();
            copy.ClearValue(FrameworkElement.ContextFlyoutProperty);
            await settle();

            var menuModel = new ContentControl { Content = authored };
            var binding = new Binding { Source = menuModel, Path = new PropertyPath(nameof(ContentControl.Content)), Mode = BindingMode.OneWay };
            source.SetBinding(FrameworkElement.ContextFlyoutProperty, binding);
            ribbon.IsDefaultContextMenuEnabled = false;
            ribbon.CanCustomizeQuickAccessToolBarItems = false;
            ContextMenuService.Coerce(source);
            Require(ReferenceEquals(source.ContextFlyout, authored)
                    && ReferenceEquals(source.GetBindingExpression(FrameworkElement.ContextFlyoutProperty)?.ParentBinding, binding)
                    && authored.Items.Count == 1 && OpenContext(source) is null,
                "Default-menu policy replaced an explicitly authored context-menu binding.");
            authored.ShowAt(source);
            activeMenu = authored;
            await settle();
            AssertMenuRendered(authored);
            authored.Hide();
            await settle();
            ribbon.IsDefaultContextMenuEnabled = true;
            ribbon.CanCustomizeQuickAccessToolBarItems = true;
            menuModel.Content = null;
            await settle();
            Require(OpenContext(source) is null
                    && ReferenceEquals(source.GetBindingExpression(FrameworkElement.ContextFlyoutProperty)?.ParentBinding, binding),
                "A null authored context-menu binding was replaced by a generated menu.");
            source.ClearValue(FrameworkElement.ContextFlyoutProperty);
            ribbon.ContextFlyout = authored;
            Require(OpenContext(source) is null, "An ancestor's authored context menu was ignored.");
            ribbon.ClearValue(FrameworkElement.ContextFlyoutProperty);

            activeMenu = RequireMenu(OpenContext(copy), "Clone customization did not restore after custom menus were removed.");
            await settle();
            Execute(MenuAction(activeMenu, RibbonLocalization.Current.Localization.RibbonContextMenuRemoveItem));
            await settle();
            Require(!entry.IsChecked && ribbon.GetQuickAccessElements().Count == 0
                    && ribbon.QuickAccessToolBarItems.Count == 0,
                "Clone context removal left a stale source map or customization check.");
        }
        finally
        {
            activeMenu?.Hide();
            ribbon.QuickAccessItems.Clear();
            ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(ribbon);
            await settle();
        }
    }

    private static async Task VerifyCompatibilityContextAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        var source = new Fluent.Button { Header = "Compatibility context" };
        ribbon.Tabs[0].Groups[0].Items.Add(source);
        host.Children.Add(ribbon);
        MenuFlyout? menu = null;
        try
        {
            await settle();
            ContextMenuService.Coerce(source);
            menu = RequireMenu(source.ContextFlyout as MenuFlyout, "Compatibility coercion did not create a default context menu.");
            menu.ShowAt(source);
            await settle();
            AssertMenuRendered(menu);
            var add = MenuAction(menu, RibbonLocalization.Current.Localization.RibbonContextMenuAddItem);
            ribbon.IsDefaultContextMenuEnabled = false;
            ExecuteBlocked(add);
            await settle();
            Require(!ribbon.IsInQuickAccessToolBar(source),
                "A compatibility context command bypassed default-menu disabling.");
            ContextMenuService.Coerce(source);
            Require(source.ContextFlyout is null, "Coercion retained a disabled generated context menu.");
            ribbon.IsDefaultContextMenuEnabled = true;
            ContextMenuService.Coerce(source);
            menu = RequireMenu(source.ContextFlyout as MenuFlyout, "Re-enabling default menus left a generated null local value blocking coercion.");
            menu.ShowAt(source);
            await settle();
            Execute(MenuAction(menu, RibbonLocalization.Current.Localization.RibbonContextMenuAddItem));
            await settle();
            var copy = (FrameworkElement)ribbon.GetQuickAccessElements()[source];
            ContextMenuService.Coerce(copy);
            menu = RequireMenu(copy.ContextFlyout as MenuFlyout, "Compatibility coercion ignored a QAT copy whose CanAdd is false.");
            menu.ShowAt(copy);
            await settle();
            AssertMenuRendered(menu);
            var remove = MenuAction(menu, RibbonLocalization.Current.Localization.RibbonContextMenuRemoveItem);
            ribbon.CanCustomizeQuickAccessToolBarItems = false;
            ExecuteBlocked(remove);
            await settle();
            Require(ReferenceEquals(ribbon.GetQuickAccessElements()[source], copy),
                "A retained compatibility removal command bypassed item-customization permission.");
            ribbon.RemoveFromQuickAccessToolBar(source);
            Require(ribbon.GetQuickAccessElements().Count == 0,
                "Compatibility user permissions incorrectly blocked programmatic removal.");
        }
        finally
        {
            menu?.Hide();
            ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(ribbon);
            await settle();
        }
    }

    private static async Task VerifyQuickAccessMenuAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        var source = new Fluent.Button { Header = "Checklist command" };
        ribbon.Tabs[0].Groups[0].Items.Add(source);
        var entry = new QuickAccessMenuItem { Header = "Checklist command", Target = source };
        ribbon.QuickAccessItems.Add(entry);
        var customizeRequests = 0;
        ribbon.CustomizeQuickAccessToolbar += (_, _) => customizeRequests++;
        host.Children.Add(ribbon);
        Flyout? menu = null;
        try
        {
            await settle();
            var toolbar = ribbon.QuickAccessToolBar ?? throw new InvalidOperationException("The active QAT was not created.");
            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "The QAT checklist did not open.");
            await settle();
            Require(HasVisibleBounds(entry) && !ribbon.CanCustomizeQuickAccessToolBar,
                "Full-toolbar customization permission incorrectly hid the independent item checklist.");
            InvokeEntry(entry);
            Require(entry.IsChecked && ribbon.IsInQuickAccessToolBar(source),
                "The real checklist input path did not register its source.");
            new RibbonMenuItemAutomationPeer(entry).Invoke();
            Require(!entry.IsChecked && !ribbon.IsInQuickAccessToolBar(source),
                "The checklist's automation path changed a different checked property or bypassed bookkeeping.");

            var enabled = new ToggleSwitch();
            var enabledBinding = BindSwitch(entry, Control.IsEnabledProperty, enabled);
            await settle();
            InvokeEntry(entry);
            Require(!entry.IsChecked, "An explicitly disabled checklist item was invoked.");
            enabled.IsOn = true;
            await settle();
            InvokeEntry(entry);
            var copy = ribbon.GetQuickAccessElements()[source];
            ribbon.CanCustomizeQuickAccessToolBarItems = false;
            InvokeEntry(entry);
            Require(entry.IsChecked && ReferenceEquals(ribbon.GetQuickAccessElements()[source], copy)
                    && ReferenceEquals(entry.GetBindingExpression(Control.IsEnabledProperty)?.ParentBinding, enabledBinding),
                "A delayed checklist action bypassed policy or policy changes replaced IsEnabled binding state.");
            var customizeCommand = (ICommand)Ribbon.CustomizeQuickAccessToolbarCommand;
            Require(!customizeCommand.CanExecute(ribbon), "Disabled full-toolbar customization remained executable.");
            customizeCommand.Execute(ribbon);
            Require(customizeRequests == 0, "Direct command execution bypassed full-toolbar customization permission.");
            ribbon.CanCustomizeQuickAccessToolBar = true;
            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "Full-toolbar customization did not appear.");
            await settle();
            var panel = (Panel)menu.Content;
            Require(!panel.Children.Contains(entry), "Disabled item customization remained in the generated checklist.");
            var customize = ActionButton(panel, "QuickAccessCustomizeButton");
            Require(HasVisibleBounds(customize), "Full-toolbar customization was only stored, not rendered.");
            ribbon.CanCustomizeQuickAccessToolBar = false;
            ExecuteBlocked(customize);
            Require(customizeRequests == 0, "A posted toolbar-customize command bypassed changed permission.");
            ribbon.CanCustomizeQuickAccessToolBar = true;
            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "Toolbar customization did not recover.");
            await settle();
            Execute(ActionButton((Panel)menu.Content, "QuickAccessCustomizeButton"));
            Require(customizeRequests == 1, "The generated customization action did not raise exactly one request.");

            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "The QAT location menu did not open.");
            await settle();
            var location = ActionButton((Panel)menu.Content, "QuickAccessLocationButton");
            ribbon.CanQuickAccessLocationChanging = false;
            ExecuteBlocked(location);
            var showBelow = (ICommand)Ribbon.ShowQuickAccessBelowCommand;
            Require(!showBelow.CanExecute(ribbon), "The location command ignored location-change permission.");
            showBelow.Execute(ribbon);
            Require(ribbon.ShowQuickAccessToolBarAboveRibbon, "A delayed/direct user location command bypassed permission.");
            ribbon.ShowQuickAccessToolBarAboveRibbon = false;
            await settle();
            toolbar = ribbon.QuickAccessToolBar!;
            Require(!toolbar.ShowAboveRibbon && ReferenceEquals(ribbon.GetQuickAccessElements()[source], copy)
                    && toolbar.Items.Contains(copy),
                "Location policy blocked programmatic positioning or moving the QAT recreated its copy.");
            ribbon.CanQuickAccessLocationChanging = true;
            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "Restoring location permission did not restore its action.");
            await settle();
            Execute(ActionButton((Panel)menu.Content, "QuickAccessLocationButton"));
            await settle();
            Require(ribbon.ShowQuickAccessToolBarAboveRibbon && ReferenceEquals(ribbon.GetQuickAccessElements()[source], copy),
                "The generated location action did not move the canonical toolbar.");
            toolbar = ribbon.QuickAccessToolBar!;
            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "The menu visibility fixture did not open.");
            var retainedLocation = ActionButton((Panel)menu.Content, "QuickAccessLocationButton");
            ribbon.IsQuickAccessToolBarMenuDropDownVisible = false;
            ExecuteBlocked(retainedLocation);
            await settle();
            Require(Part(toolbar, "PART_MenuButton").Visibility == Visibility.Collapsed
                    && HasVisibleBounds((FrameworkElement)copy) && OpenQuickAccessMenu(toolbar) is null,
                "Hiding the QAT menu button left its input active or hid unrelated toolbar items.");
            ribbon.IsQuickAccessToolBarMenuDropDownVisible = true;
            ribbon.CanCustomizeQuickAccessToolBarItems = true;
            ribbon.IsDefaultContextMenuEnabled = false;
            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "Default-context policy incorrectly disabled the normal QAT menu.");
            await settle();
            Require(HasVisibleBounds(entry) && enabled.IsOn, "Restoring checklist permission lost its content or authored enabled state.");
            InvokeEntry(entry);
            Require(!entry.IsChecked && !ribbon.IsInQuickAccessToolBar(source),
                "A normal QAT checklist could not customize while only default context menus were disabled.");
            entry.IsChecked = true;
            ribbon.CanCustomizeQuickAccessToolBarItems = false;
            entry.IsChecked = false;
            Require(!ribbon.IsInQuickAccessToolBar(source), "User policy blocked programmatic checklist state edits.");
        }
        finally
        {
            menu?.Hide();
            ribbon.QuickAccessItems.Clear();
            ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(ribbon);
            await settle();
        }
    }

    private static async Task VerifyQuickAccessMenuRetirementAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        var source = new Fluent.Button { Header = "Reusable checklist command" };
        ribbon.Tabs[0].Groups[0].Items.Add(source);
        var entry = new QuickAccessMenuItem { Header = source.Header, Target = source };
        ribbon.QuickAccessItems.Add(entry);
        host.Children.Add(ribbon);
        Flyout? menu = null;
        try
        {
            await settle();
            foreach (var useToolbarCollection in new[] { false, true })
            {
                foreach (var clear in new[] { false, true })
                {
                    var toolbar = ribbon.QuickAccessToolBar!;
                    menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "The removable checklist did not open.");
                    await settle();
                    var previousPanel = (Panel)menu.Content;
                    Require(previousPanel.Children.Contains(entry) && HasVisibleBounds(entry),
                        "The removable checklist row was not rendered.");
                    IList<QuickAccessMenuItem> items = useToolbarCollection ? toolbar.QuickAccessItems : ribbon.QuickAccessItems;
                    if (clear)
                    {
                        items.Clear();
                    }
                    else
                    {
                        Require(items.Remove(entry), "The active checklist entry was not removed.");
                    }
                    menu.Hide();
                    await settle();
                    Require(!previousPanel.Children.Contains(entry) && VisualTreeHelper.GetParent(entry) is null,
                        "Removing a checklist entry left it parented beneath a retiring popup.");
                    Require(!ribbon.QuickAccessItems.Contains(entry) && !toolbar.QuickAccessItems.Contains(entry),
                        "Removing a displayed checklist entry did not synchronize the canonical collections.");
                    ribbon.QuickAccessItems.Add(entry);
                    menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "The removed checklist row could not be reused.");
                    await settle();
                    Require(((Panel)menu.Content).Children.Contains(entry) && HasVisibleBounds(entry),
                        "Reusing a removed checklist row did not produce live popup content.");
                    menu.Hide();
                    await settle();
                }
            }

            foreach (var showAbove in new[] { false, true })
            {
                menu = RequireFlyout(OpenQuickAccessMenu(ribbon.QuickAccessToolBar!),
                    "The checklist did not open before programmatic migration.");
                await settle();
                var previousPanel = (Panel)menu.Content;
                ribbon.ShowQuickAccessToolBarAboveRibbon = showAbove;
                await settle();
                Require(!previousPanel.Children.Contains(entry),
                    "Programmatic QAT migration retained checklist content in the old popup.");
                menu = RequireFlyout(OpenQuickAccessMenu(ribbon.QuickAccessToolBar!),
                    "The checklist did not reopen after programmatic migration.");
                await settle();
                Require(ribbon.QuickAccessToolBar!.ShowAboveRibbon == showAbove
                        && ((Panel)menu.Content).Children.Contains(entry) && HasVisibleBounds(entry),
                    "Programmatic QAT migration lost the canonical checklist row.");
                menu.Hide();
                await settle();
            }
        }
        finally
        {
            menu?.Hide();
            ribbon.QuickAccessItems.Clear();
            ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(ribbon);
            await settle();
        }
    }

    private static async Task VerifyQuickAccessOverflowTransitionsAsync(Panel host, Func<Task> settle)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var toolbar = new QuickAccessToolBar { Width = 130 };
        var first = new Fluent.Button { Header = "First overflow command", Width = 80 };
        var second = new Fluent.Button { Header = "Second overflow command", Width = 80 };
        var hidden = new Fluent.Button { Header = "Authored hidden command", Width = 80, Visibility = Visibility.Collapsed };
        toolbar.Items.Add(first);
        toolbar.Items.Add(second);
        toolbar.Items.Add(hidden);
        panel.Children.Add(toolbar);
        host.Children.Add(panel);
        try
        {
            for (var cycle = 0; cycle < 2; cycle++)
            {
                toolbar.Width = 130;
                await settle();
                Require(toolbar.HasOverflowItems
                        && Part(toolbar, "PART_OverflowButton").Visibility == Visibility.Visible
                        && new[] { first, second }.Any(item => item.Visibility == Visibility.Collapsed),
                    "A finite-width QAT did not render its overflow affordance and hide overflowing items.");
                toolbar.ClearValue(FrameworkElement.WidthProperty);
                await settle();
                Require(!toolbar.HasOverflowItems
                        && Part(toolbar, "PART_OverflowButton").Visibility == Visibility.Collapsed
                        && HasVisibleBounds(first) && HasVisibleBounds(second)
                        && hidden.Visibility == Visibility.Collapsed,
                    "Returning a QAT to natural width retained overflow or revealed an authored hidden item.");
            }
        }
        finally
        {
            host.Children.Remove(panel);
            await settle();
        }
    }

    private static async Task VerifyStandaloneQuickAccessLocationAsync(Panel host, Func<Task> settle)
    {
        var toolbar = new QuickAccessToolBar { Width = 300 };
        host.Children.Add(toolbar);
        Flyout? menu = null;
        try
        {
            await settle();
            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "The standalone QAT location menu did not open.");
            await settle();
            var location = ActionButton((Panel)menu.Content, "QuickAccessLocationButton");
            toolbar.CanQuickAccessLocationChanging = false;
            ExecuteBlocked(location);
            Require(toolbar.ShowAboveRibbon && OpenQuickAccessMenu(toolbar) is null,
                "Standalone location disabling left an active command or an empty generated menu.");
            toolbar.ShowAboveRibbon = false;
            toolbar.CanQuickAccessLocationChanging = true;
            menu = RequireFlyout(OpenQuickAccessMenu(toolbar), "Standalone QAT location permission did not restore.");
            await settle();
            Execute(ActionButton((Panel)menu.Content, "QuickAccessLocationButton"));
            Require(toolbar.ShowAboveRibbon, "The standalone QAT location action had no effect.");
        }
        finally
        {
            menu?.Hide();
            host.Children.Remove(toolbar);
            await settle();
        }
    }

    private static Ribbon CreateRibbon()
    {
        var ribbon = new Ribbon
        {
            Width = 700,
            AutomaticStateManagement = false,
            IsAutomaticCollapseEnabled = false,
            IsKeyTipHandlingEnabled = false,
        };
        ribbon.Tabs.Add(CreateTab("Options A"));
        ribbon.Tabs.Add(CreateTab("Options B"));
        return ribbon;
    }

    private static RibbonTabItem CreateTab(string header)
    {
        var model = new PortEditorHeaderModel { Title = header + " model" };
        var tab = new RibbonTabItem { Header = header, DataContext = model };
        var group = new RibbonGroupBox { Header = header + " commands" };
        group.Items.Add(new Fluent.Button { Header = header + " action" });
        var probe = new TextBlock();
        probe.SetBinding(TextBlock.TextProperty, new Binding
        {
            Path = new PropertyPath(nameof(PortEditorHeaderModel.Title)),
            Mode = BindingMode.OneWay,
        });
        group.Items.Add(probe);
        tab.Groups.Add(group);
        return tab;
    }

    private static Binding BindSwitch(FrameworkElement target, DependencyProperty property, ToggleSwitch source)
    {
        var binding = new Binding { Source = source, Path = new PropertyPath(nameof(ToggleSwitch.IsOn)), Mode = BindingMode.OneWay };
        target.SetBinding(property, binding);
        return target.GetBindingExpression(property)?.ParentBinding
               ?? throw new InvalidOperationException("The option binding was not installed.");
    }

    private static IReadOnlyDictionary<RibbonTabItem, (object? Content, object? Model)> CaptureTabPresentation(
        IEnumerable<RibbonTabItem> tabs) =>
        tabs.ToDictionary(tab => tab, tab => (Content: (object?)tab.Content, Model: (object?)tab.DataContext));

    private static void AssertPresented(
        RibbonTabControl tabs,
        RibbonTabItem selected,
        Ribbon? ribbon,
        (object? Content, object? Model) expected)
    {
        var presenter = Presenter(tabs);
        var group = selected.Groups.FirstOrDefault();
        var contentRoot = selected.Content as FrameworkElement;
        var probe = group?.Items.OfType<TextBlock>().SingleOrDefault();
        var selectedMatches = ReferenceEquals(tabs.SelectedItem, selected);
        var contentMatches = ReferenceEquals(selected.Content, expected.Content);
        var modelMatches = ReferenceEquals(selected.DataContext, expected.Model);
        var selectedContentMatches = ReferenceEquals(tabs.SelectedContent, expected.Content);
        var presentedContentMatches = ReferenceEquals(presenter.Content, expected.Content);
        var contentModelMatches = contentRoot is not null && ReferenceEquals(contentRoot.DataContext, expected.Model);
        var groupModelMatches = group is not null && ReferenceEquals(group.DataContext, expected.Model);
        var probeModelMatches = probe is not null && ReferenceEquals(probe.DataContext, expected.Model);
        var expectedProbeValue = (expected.Model as PortEditorHeaderModel)?.Title;
        var probeTextMatches = probe is not null && expectedProbeValue is string expectedText
                               && string.Equals(probe.Text, expectedText, StringComparison.Ordinal);
        var probeVisible = probe is not null && HasVisibleBounds(probe);
        // Source changes above verify the live binding's observable result on both heads.
        // A native BindingExpression wrapper is diagnostic information, not rendered behavior.
        var probeBindingInspectable = probe?.GetBindingExpression(TextBlock.TextProperty) is not null;
        var boundModelMatches = probeModelMatches && probeTextMatches && probeVisible;
        var presenterVisible = HasVisibleBounds(presenter);
        var groupVisible = group is not null && HasVisibleBounds(group);
        if (!selectedMatches || !selected.IsSelected || !contentMatches || !modelMatches
            || !selectedContentMatches || !presentedContentMatches
            || !contentModelMatches || !groupModelMatches || !boundModelMatches
            || !presenterVisible || !groupVisible)
        {
            Require(false,
                "Option changes lost selected identity, model context, or actual group content. "
                + $"headers={tabs.AreTabHeadersVisible}, options={tabs.IsDisplayOptionsButtonVisible}, "
                + $"minimized={tabs.IsMinimized}, popup={tabs.DropDownPopup?.IsOpen == true}, "
                + $"selectionMatches={selectedMatches}, nativeSelected={selected.IsSelected}, "
                + $"contentMatches={contentMatches}, modelMatches={modelMatches}, "
                + $"selectedContentMatches={selectedContentMatches}, presentedContentMatches={presentedContentMatches}, "
                + $"contentModelMatches={contentModelMatches}, groupModelMatches={groupModelMatches}, boundModelMatches={boundModelMatches}, "
                + $"probeModelMatches={probeModelMatches}, probeTextMatches={probeTextMatches}, "
                + $"probeVisible={probeVisible}, probeBindingInspectable={probeBindingInspectable}, "
                + $"presenterVisible={presenterVisible}, groupVisible={groupVisible}; "
                + $"expectedTab={Describe(selected)}, actualTab={Describe(tabs.SelectedItem)}, index={tabs.SelectedIndex}; "
                + $"expectedContent={Describe(expected.Content)}, tabContent={Describe(selected.Content)}, "
                + $"selectedContent={Describe(tabs.SelectedContent)}, presentedContent={Describe(presenter.Content)}; "
                + $"expectedModel={Describe(expected.Model)}, tabModel={Describe(selected.DataContext)}, "
                + $"contentModel={Describe(contentRoot?.DataContext)}, groupModel={Describe(group?.DataContext)}, "
                + $"probeModel={Describe(probe?.DataContext)}, probeText={probe?.Text}, presenterContext={Describe(presenter.DataContext)}; "
                + $"expectedProbeValue={expectedProbeValue}, expectedProbeValueType={Describe(expectedProbeValue)}; "
                + $"groupsHostContent={Describe(selected.GroupsContainer.Content)}, "
                + $"groupsHostChildren={VisualTreeHelper.GetChildrenCount(selected.GroupsContainer)}; "
                + $"groupItemState={DescribeGroupItems(group)}; "
                + $"contentPath={DescribeVisualPath(contentRoot)}; "
                + $"presenterPath={DescribeVisualPath(presenter)}; groupPath={DescribeVisualPath(group)}; "
                + $"probePath={DescribeVisualPath(probe)}.");
        }
        Require(ribbon is null || (ReferenceEquals(ribbon.SelectedTab, selected)
                                  && ribbon.SelectedTabIndex == ribbon.Tabs.IndexOf(selected)),
            $"Option changes left root and native tab selection out of sync: root={Describe(ribbon?.SelectedTab)}, "
            + $"rootIndex={ribbon?.SelectedTabIndex}, native={Describe(tabs.SelectedItem)}, nativeIndex={tabs.SelectedIndex}.");
    }

    private static string Describe(object? value) =>
        value is null ? "null" : $"{value.GetType().Name}#{RuntimeHelpers.GetHashCode(value):X}";

    private static string DescribeGroupItems(RibbonGroupBox? group)
    {
        if (group is null) return "none";
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var panel = typeof(RibbonGroupBox).GetField("_itemsPanel", flags)?.GetValue(group) as Panel;
        var binding = typeof(RibbonGroupBox).GetField("itemsBinding", flags)?.GetValue(group);
        var suspended = binding?.GetType().GetProperty("IsSuspended", flags)?.GetValue(binding);
        return $"{Describe(panel)},loaded={panel?.IsLoaded},suspended={suspended},children="
               + (panel is null ? "none" : string.Join("|", panel.Children.Select(child =>
                   $"{Describe(child)}:{(child as FrameworkElement)?.IsLoaded}")));
    }

    private static string DescribeVisualPath(DependencyObject? element)
    {
        var path = new List<string>();
        for (var current = element; current is not null && path.Count < 12; current = VisualTreeHelper.GetParent(current))
        {
            path.Add(current is FrameworkElement visual
                ? $"{Describe(visual)}[{visual.Name},loaded={visual.IsLoaded},visibility={visual.Visibility},"
                  + $"actual={visual.ActualWidth:R}x{visual.ActualHeight:R},desired={visual.DesiredSize.Width:R}x{visual.DesiredSize.Height:R}]"
                : Describe(current));
        }
        return path.Count == 0 ? "null" : string.Join(" > ", path);
    }

    private static ContentPresenter Presenter(RibbonTabControl tabs) =>
        tabs.SelectedContentPresenter as ContentPresenter
        ?? throw new InvalidOperationException("Selected tab content has no presenter.");

    private static FrameworkElement Part(DependencyObject root, string name) =>
        Descendants(root).OfType<FrameworkElement>().FirstOrDefault(element => element.Name == name)
        ?? throw new InvalidOperationException($"Missing rendered template part: {name}.");

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, index)))
            {
                yield return child;
            }
        }
    }

    private static bool HasVisibleBounds(FrameworkElement element)
    {
        if (!element.IsLoaded || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is UIElement { Visibility: Visibility.Collapsed })
            {
                return false;
            }
        }
        return true;
    }

    private static void AssertMenuRendered(MenuFlyout menu) =>
        Require(menu.Items.OfType<MenuFlyoutItem>().Any(HasVisibleBounds), "The generated menu had no rooted, rendered action.");

    private static MenuFlyout RequireMenu(MenuFlyout? menu, string message) =>
        menu ?? throw new InvalidOperationException(message);

    private static Flyout RequireFlyout(Flyout? menu, string message) =>
        menu ?? throw new InvalidOperationException(message);

    private static MenuFlyoutItem MenuAction(MenuFlyout menu, string label) =>
        menu.Items.OfType<MenuFlyoutItem>().Single(item => item.Text == label && item.Command is not null);

    private static NativeButton ActionButton(Panel panel, string id) =>
        panel.Children.OfType<NativeButton>().Single(button => AutomationProperties.GetAutomationId(button) == id);

    private static void Execute(MenuFlyoutItem item) => Execute(item.Command, item.CommandParameter, blocked: false);
    private static void Execute(NativeButton item) => Execute(item.Command, item.CommandParameter, blocked: false);
    private static void ExecuteBlocked(MenuFlyoutItem item) => Execute(item.Command, item.CommandParameter, blocked: true);
    private static void ExecuteBlocked(NativeButton item) => Execute(item.Command, item.CommandParameter, blocked: true);

    private static void Execute(ICommand? command, object? parameter, bool blocked)
    {
        Require(command is not null && command.CanExecute(parameter) != blocked,
            blocked ? "A retained user command ignored its changed permission." : "An enabled menu action was not executable.");
        command!.Execute(parameter);
    }

    // These are the shared handlers called by the actual Click/RightTapped routes.
    // Public controls avoid native XAML metadata problems with private probe subclasses.
    private static readonly MethodInfo OpenOptionsMethod = Primitive(
        typeof(RibbonTabControl), "OpenDisplayOptions", typeof(MenuFlyout), isStatic: false);
    private static readonly MethodInfo OpenContextMethod = Primitive(
        typeof(Ribbon).Assembly.GetType("Fluent.QuickAccessHelper", throwOnError: true)!,
        "OpenContextMenu", typeof(MenuFlyout), true,
        typeof(FrameworkElement), typeof(DependencyObject), typeof(Point));
    private static readonly MethodInfo OpenQuickAccessMenuMethod = Primitive(
        typeof(QuickAccessToolBar), "OpenCustomizationMenu", typeof(Flyout), isStatic: false);
    private static readonly MethodInfo InvokeEntryMethod = Primitive(
        typeof(QuickAccessMenuItem), "OnInvoke", typeof(void), isStatic: false);

    private static MenuFlyout? OpenOptions(RibbonTabControl tabs) => (MenuFlyout?)Invoke(OpenOptionsMethod, tabs);
    private static MenuFlyout? OpenContext(FrameworkElement element) =>
        (MenuFlyout?)Invoke(OpenContextMethod, null, element, element, new Point(4, 4));
    private static Flyout? OpenQuickAccessMenu(QuickAccessToolBar toolbar) =>
        (Flyout?)Invoke(OpenQuickAccessMenuMethod, toolbar);
    private static void InvokeEntry(QuickAccessMenuItem item) => Invoke(InvokeEntryMethod, item);

    private static MethodInfo Primitive(Type type, string name, Type returnType, bool isStatic, params Type[] parameters)
    {
        var method = type.GetMethod(name,
            BindingFlags.NonPublic | BindingFlags.DeclaredOnly | (isStatic ? BindingFlags.Static : BindingFlags.Instance),
            binder: null, parameters, modifiers: null) ?? throw new MissingMethodException(type.FullName, name);
        Require(method.ReturnType == returnType && (method.IsAssembly || method.IsFamily),
            $"The shared input primitive {type.FullName}.{name} changed its contract.");
        return method;
    }

    private static object? Invoke(MethodInfo method, object? target, params object?[] parameters)
    {
        try
        {
            return method.Invoke(target, parameters);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("Ribbon options: " + message);
        }
    }
}
