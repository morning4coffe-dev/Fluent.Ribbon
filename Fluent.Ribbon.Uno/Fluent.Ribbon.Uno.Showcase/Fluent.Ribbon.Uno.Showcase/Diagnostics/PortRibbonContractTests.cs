namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Fluent;
using Fluent.Modern;
using Fluent.Modern.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

/// <summary>Behavioral regressions run on the Showcase's native UI thread, not the reference runtime.</summary>
internal static partial class PortRibbonContractTests
{
    public static async Task VerifyQuickAccessOverloadsAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        var button = new Fluent.Button { Header = "WPF button" };
        var toggle = new Fluent.ToggleButton { Header = "WPF toggle" };
        var dropDown = new Fluent.DropDownButton { Header = "WPF drop-down" };
        ribbon.Tabs[0].Groups[0].Items.Add(button);
        ribbon.Tabs[0].Groups[0].Items.Add(toggle);
        ribbon.Tabs[0].Groups[0].Items.Add(dropDown);
        UIElement element = button;
        IQuickAccessItemProvider provider = button;

        ribbon.AddToQuickAccessToolBar(button);
        ribbon.AddToQuickAccessToolBar(element);
        ribbon.AddToQuickAccessToolBar(provider);
        Check(ribbon.QuickAccessToolBarItems.Count == 1, "Mixed entry points duplicated a button before loading.");
        Check(ribbon.IsInQuickAccessToolBar(button)
              && ribbon.IsInQuickAccessToolBar(element)
              && ribbon.IsInQuickAccessToolBar(provider), "QAT overloads disagree before loading.");
        ribbon.RemoveFromQuickAccessToolBar(button);
        Check(!ribbon.IsInQuickAccessToolBar(provider), "The concrete overload did not remove interface state.");
        ribbon.AddToQuickAccessToolBar(provider);
        ribbon.RemoveFromQuickAccessToolBar(element);
        Check(ribbon.QuickAccessToolBarItems.Count == 0, "UIElement removal left an interface-created copy.");
        ribbon.AddToQuickAccessToolBar(element);
        ribbon.RemoveFromQuickAccessToolBar(provider);

        ribbon.AddToQuickAccessToolBar(toggle);
        Check(ribbon.IsInQuickAccessToolBar(toggle), "The concrete toggle overload failed.");
        ribbon.RemoveFromQuickAccessToolBar(toggle);
        ribbon.AddToQuickAccessToolBar(dropDown);
        Check(ribbon.IsInQuickAccessToolBar(dropDown), "The concrete drop-down overload failed.");
        ribbon.RemoveFromQuickAccessToolBar(dropDown);

        Action<Fluent.Button> add = ribbon.AddToQuickAccessToolBar;
        Func<Fluent.Button, bool> contains = ribbon.IsInQuickAccessToolBar;
        Action<Fluent.Button> remove = ribbon.RemoveFromQuickAccessToolBar;
        add(button);
        Check(contains(button), "A WPF method-group conversion selected an inconsistent QAT overload.");
        remove(button);
        ribbon.AddToQuickAccessToolBar(null);
        Check(!ribbon.IsInQuickAccessToolBar(null), "A null QAT argument must not represent an item.");
        ribbon.RemoveFromQuickAccessToolBar(null);

        var standalone = new CountingProvider();
        ribbon.AddToQuickAccessToolBar(standalone);
        ribbon.AddToQuickAccessToolBar((IQuickAccessItemProvider)standalone);
        Check(standalone.CreationCount == 1 && ribbon.IsInQuickAccessToolBar(standalone),
            "A nonvisual provider was lost or duplicated.");
        var standaloneCopy = ribbon.QuickAccessToolBarItems.Single();
        Check(ribbon.IsInQuickAccessToolBar(standaloneCopy), "A non-provider copy cannot be queried.");
        var removeCommand = (System.Windows.Input.ICommand)Ribbon.RemoveFromQuickAccessCommand;
        Check(removeCommand.CanExecute(standaloneCopy), "An unparented QAT copy did not resolve its owning ribbon.");
        removeCommand.Execute(standaloneCopy);
        Check(!ribbon.IsInQuickAccessToolBar(standalone), "The removal command left a nonvisual source registered.");
        ribbon.AddToQuickAccessToolBar(new EmptyProvider());
        Check(ribbon.QuickAccessToolBarItems.Count == 0, "A provider returning null left a QAT registration.");

        host.Children.Add(ribbon);
        try
        {
            await settle();
            Check(ribbon.TabControl is not null, "The native Ribbon template did not load.");
            add(button);
            Check(ribbon.QuickAccessToolBar?.Items.Count == 1, "A loaded QAT addition did not reach its toolbar.");
            remove(button);
            Check(ribbon.QuickAccessToolBar?.Items.Count == 0, "A loaded QAT removal left a visible copy.");

            var failure = new InvalidOperationException("Expected provider failure");
            try
            {
                ribbon.AddToQuickAccessToolBar(new FailingProvider(failure));
                throw new InvalidOperationException("The QAT API swallowed a provider failure.");
            }
            catch (InvalidOperationException exception) when (ReferenceEquals(exception, failure))
            {
            }

            Check(ribbon.QuickAccessToolBarItems.Count == 0 && ribbon.GetQuickAccessElements().Count == 0,
                "A failed provider left a partial QAT registration.");
        }
        finally
        {
            ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(ribbon);
        }
    }

    public static async Task VerifyQuickAccessBookkeepingAsync(Panel host, Func<Task> settle)
    {
        await VerifyDeferredQuickAccessTargetsAsync(host, settle);
        await VerifyDirectQuickAccessCustomizationAsync(host, settle);
        await VerifySharedQuickAccessTargetsAsync(host, settle);

        var ribbon = CreateRibbon();
        var pinned = new Microsoft.UI.Xaml.Controls.Button { Content = "Authored" };
        var first = new CountingButton { Header = "First" };
        var second = new CountingButton { Header = "Second" };
        var standalone = new CountingProvider();
        var menu = new QuickAccessMenuItem { Header = "First", Target = first, IsChecked = true };
        ribbon.Tabs[0].Groups[0].Items.Add(first);
        ribbon.Tabs[0].Groups[0].Items.Add(second);
        ribbon.QuickAccessToolBarItems.Add(pinned);
        ribbon.QuickAccessItems.Add(menu);
        Check(ribbon.IsInQuickAccessToolBar((IQuickAccessItemProvider)first),
            "An initially checked customization item was ignored before template creation.");
        ribbon.AddToQuickAccessToolBar((UIElement)second);
        ribbon.AddToQuickAccessToolBar(standalone);
        host.Children.Add(ribbon);
        try
        {
            await settle();
            var snapshot = ribbon.GetQuickAccessElements();
            Check(snapshot.Count == 2 && snapshot.ContainsKey(first) && snapshot.ContainsKey(second),
                "The WPF map omitted a source added through the provider or customization API.");
            var firstCopy = snapshot[first];
            var secondCopy = snapshot[second];
            CheckMembership(first, firstCopy, true);
            CheckMembership(second, secondCopy, true);
            snapshot.Clear();
            Check(ribbon.GetQuickAccessElements().Count == 2, "GetQuickAccessElements exposed its live map.");
            ribbon.AddToQuickAccessToolBar(firstCopy);
            Check(first.CreationCount == 1, "Adding a registered copy created a copy of that copy.");
            ribbon.RemoveFromQuickAccessToolBar(firstCopy);
            Check(!menu.IsChecked && !ribbon.IsInQuickAccessToolBar(first), "Copy removal left its menu checked.");
            CheckMembership(first, firstCopy, false);

            ribbon.QuickAccessToolBarItems.Add(firstCopy);
            Check(menu.IsChecked && first.CreationCount == 1 && ribbon.IsInQuickAccessToolBar(first),
                "Reinserting an existing copy did not restore its original association.");
            ribbon.QuickAccessToolBar!.Items.Remove(firstCopy);
            Check(!ribbon.QuickAccessToolBarItems.Contains(firstCopy)
                  && !ribbon.IsInQuickAccessToolBar(first) && !menu.IsChecked,
                "Editing the WPF toolbar Items collection left the source collection or map stale.");
            ribbon.QuickAccessToolBar.Items.Add(firstCopy);
            Check(ribbon.QuickAccessToolBarItems.Contains(firstCopy)
                  && ribbon.IsInQuickAccessToolBar(first) && menu.IsChecked,
                "The WPF toolbar Items collection did not restore an existing copy's registration.");
            ribbon.ShowQuickAccessToolBarAboveRibbon = false;
            await settle();
            Check(ribbon.QuickAccessToolBar!.Items.Contains(firstCopy),
                "The WPF QuickAccessToolBar property did not follow the active toolbar.");
            ribbon.ShowQuickAccessToolBarAboveRibbon = true;
            await settle();
            Check(menu.IsChecked && first.CreationCount == 1, "Moving the QAT recreated or unchecked an existing item.");

            ribbon.RemoveFromQuickAccessToolBar(second);
            menu.Target = second;
            Check(!ribbon.IsInQuickAccessToolBar(first) && ribbon.IsInQuickAccessToolBar(second) && menu.IsChecked,
                "Changing a checked customization target left the previous registration or lost the new one.");
            menu.IsChecked = false;
            Check(!ribbon.IsInQuickAccessToolBar(second), "Unchecking a customization item did not remove its target.");
            ribbon.AddToQuickAccessToolBar((IQuickAccessItemProvider)second);
            Check(menu.IsChecked, "An interface addition did not update its customization check mark.");

            var currentCopy = ribbon.GetQuickAccessElements()[second];
            RibbonCustomizationService.SetItemKey(ribbon.Tabs[0], "port-tab");
            RibbonCustomizationService.SetItemKey(pinned, "port-authored");
            RibbonCustomizationService.SetItemKey(currentCopy, "port-copy");
            var standaloneCopy = ribbon.QuickAccessToolBarItems.Single(
                item => !ReferenceEquals(item, pinned) && !ReferenceEquals(item, currentCopy));
            RibbonCustomizationService.SetItemKey(standaloneCopy, "port-standalone");
            var captured = RibbonCustomizationService.CaptureResult(ribbon);
            Check(captured.Succeeded && captured.Value is not null, "The QAT customization layout could not be captured.");
            var hidden = new RibbonLayout
            {
                Tabs = captured.Value!.Tabs,
                QuickAccessItemKeys = ["port-authored"],
            };
            var hide = RibbonCustomizationService.ApplyResult(ribbon, hidden);
            Check(hide.Succeeded, "Customization could not hide provider-created QAT items.");
            Check(!ribbon.IsInQuickAccessToolBar(second) && !ribbon.IsInQuickAccessToolBar(standalone)
                  && ribbon.GetQuickAccessElements().Count == 0 && !menu.IsChecked,
                "Customization left hidden copies registered or checked.");
            var creationCount = second.CreationCount;
            var restore = RibbonCustomizationService.ApplyResult(ribbon, captured.Value);
            Check(restore.Succeeded, "Customization could not restore existing QAT copies.");
            Check(ribbon.IsInQuickAccessToolBar(second) && ribbon.IsInQuickAccessToolBar(standalone)
                  && ReferenceEquals(ribbon.GetQuickAccessElements()[second], currentCopy)
                  && second.CreationCount == creationCount && menu.IsChecked,
                "Customization restoration lost original identity, cloned again, or left stale checks.");

            ribbon.ClearQuickAccessToolBar();
            await settle();
            Check(ribbon.QuickAccessToolBarItems.SequenceEqual([pinned]),
                "Clear removed a directly authored item or left a provider-created copy.");
            Check(!ribbon.IsInQuickAccessToolBar(standalone) && ribbon.GetQuickAccessElements().Count == 0,
                "Clear omitted a nonvisual provider or left WPF map entries.");
            Check(!menu.IsChecked, "Clear allowed a checked customization item to re-add its target.");
            CheckMembership(second, currentCopy, false);
            Check(!RibbonProperties.GetIsElementInQuickAccessToolBar(standaloneCopy),
                "Clear left a nonvisual provider's copy marked as a QAT item.");
            menu.IsChecked = true;
            ribbon.QuickAccessItems.Clear();
            Check(!ribbon.IsInQuickAccessToolBar(second), "Removing the last customization item retained its target.");
            menu.IsChecked = false;
            menu.IsChecked = true;
            Check(!ribbon.IsInQuickAccessToolBar(second), "A removed customization item retained its callback.");
        }
        finally
        {
            ribbon.QuickAccessItems.Clear();
            ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(ribbon);
        }
    }

    public static async Task VerifyTabCollectionAsync(Panel host, Func<Task> settle)
    {
        await VerifyPendingTabSelectionAsync(host, settle);

        var ribbon = CreateRibbon(withTab: false);
        Check(ribbon.SelectedTab is null && ribbon.SelectedTabIndex == -1,
            "An empty ribbon starts with a nonempty selection index.");
        var hidden = new RibbonTabItem { Header = "Hidden", Visibility = Visibility.Collapsed };
        var disabled = new RibbonTabItem { Header = "Disabled", IsEnabled = false };
        var first = new RibbonTabItem { Header = "A" };
        var second = new RibbonTabItem { Header = "B" };
        ribbon.Tabs.Add(hidden);
        ribbon.Tabs.Add(disabled);
        Check(ribbon.SelectedTab is null && ribbon.SelectedTabIndex == -1,
            "Hidden or disabled tabs were automatically selected before loading.");
        ribbon.Tabs.Add(first);
        ribbon.Tabs.Add(second);
        ribbon.SelectedTabItem = second;
        var beforeLoad = new RibbonTabItem { Header = "Before loading" };
        ribbon.Tabs.Insert(0, beforeLoad);
        Check(ReferenceEquals(ribbon.SelectedTab, second) && ribbon.SelectedTabIndex == 4,
            "Pre-template insertion lost selected identity or its index.");
        host.Children.Add(ribbon);
        try
        {
            await settle();
            AssertTabState(ribbon, second);
            var inserted = new RibbonTabItem { Header = "Inserted" };
            ribbon.Tabs.Insert(1, inserted);
            await settle();
            AssertTabState(ribbon, second);
            ribbon.Tabs.Move(ribbon.Tabs.IndexOf(second), 0);
            await settle();
            AssertTabState(ribbon, second);
            var replacement = new RibbonTabItem { Header = "Replacement" };
            ribbon.Tabs[ribbon.Tabs.IndexOf(first)] = replacement;
            await settle();
            AssertTabState(ribbon, second);
            ribbon.SelectedTab = replacement;
            ribbon.Tabs[ribbon.Tabs.IndexOf(replacement)] = new RibbonTabItem { Header = "Selected replacement" };
            await settle();
            Check(!replacement.IsSelected, "Replacing the selected tab left it selected after removal.");
            AssertTabState(ribbon, second);

            ribbon.IsSimplified = true;
            var simplified = new RibbonTabItem { Header = "Simplified insertion" };
            var group = new RibbonGroupBox { Header = "Simplified group" };
            simplified.Groups.Add(group);
            ribbon.Tabs.Insert(0, simplified);
            await settle();
            Check(simplified.IsSimplified && group.IsSimplified
                  && simplified.GroupsContainer.Height == ribbon.TabControl!.ContentHeight,
                "An inserted tab missed simplified mode or content-height propagation.");
            var lateGroup = new RibbonGroupBox { Header = "Late group" };
            simplified.Groups.Add(lateGroup);
            Check(lateGroup.IsSimplified, "A late group missed its tab's inherited simplified state.");
            ribbon.IsSimplified = false;
            ribbon.ContentHeight = 137;
            Check(ribbon.Tabs.All(tab => tab.GroupsContainer.Height == 137),
                "Changing content height did not reach every current tab.");

            second.IsEnabled = false;
            await settle();
            AssertTabState(ribbon, simplified);
            simplified.Visibility = Visibility.Collapsed;
            await settle();
            Check(ribbon.SelectedTab is { Visibility: Visibility.Visible, IsEnabled: true },
                "Hiding the selected tab did not choose an eligible fallback.");
            foreach (var tab in ribbon.Tabs)
            {
                tab.IsEnabled = false;
            }

            await settle();
            AssertTabState(ribbon, null);
            disabled.IsEnabled = true;
            await settle();
            AssertTabState(ribbon, disabled);
            ribbon.SelectedTabIndex = -1;
            AssertTabState(ribbon, null);
            replacement.Visibility = Visibility.Collapsed;
            replacement.IsEnabled = false;
            replacement.ContextualTabGroupName = "Removed tab";
            Check(ribbon.SelectedTab is null, "A replaced tab still drives its former ribbon's callbacks.");

            ribbon.Tabs.Remove(disabled);
            await settle();
            AssertTabState(ribbon, null);
            disabled.IsEnabled = false;
            disabled.IsEnabled = true;
            Check(ribbon.SelectedTab is null, "A removed tab still drives its former ribbon's callbacks.");
            ribbon.Tabs.Clear();
            await settle();
            AssertTabState(ribbon, null);
            var restored = new RibbonTabItem { Header = "After reset" };
            ribbon.Tabs.Add(restored);
            await settle();
            AssertTabState(ribbon, restored);
        }
        finally
        {
            ribbon.Tabs.Clear();
            host.Children.Remove(ribbon);
        }
    }

    private static async Task VerifyDeferredQuickAccessTargetsAsync(Panel host, Func<Task> settle)
    {
        foreach (var resolveAfterTemplate in new[] { false, true })
        {
            var ribbon = CreateRibbon();
            var targets = Enumerable.Range(0, 5)
                .Select(index => new CountingButton { Header = $"Deferred command {index}" })
                .ToArray();
            var menus = Enumerable.Range(0, targets.Length)
                .Select(index => new QuickAccessMenuItem { Header = $"Deferred menu {index}", IsChecked = true })
                .ToArray();
            foreach (var target in targets)
            {
                ribbon.Tabs[0].Groups[0].Items.Add(target);
            }

            foreach (var menu in menus)
            {
                ribbon.QuickAccessItems.Add(menu);
            }

            Check(menus.All(menu => menu.IsChecked) && ribbon.QuickAccessToolBarItems.Count == 0,
                "Registering checked menus before their targets erased the authored checked intent.");
            if (!resolveAfterTemplate)
            {
                ResolveTargets();
            }

            host.Children.Add(ribbon);
            try
            {
                await settle();
                if (resolveAfterTemplate)
                {
                    Check(menus.All(menu => menu.IsChecked) && ribbon.QuickAccessToolBarItems.Count == 0,
                        "Template loading erased checked intent while targets were still unresolved.");
                    ribbon.ShowQuickAccessToolBarAboveRibbon = false;
                    await settle();
                    Check(menus.All(menu => menu.IsChecked), "QAT relocation erased pending checked requests.");
                    ResolveTargets();
                    await settle();
                }

                Check(ribbon.GetQuickAccessElements().Count == targets.Length
                      && ribbon.QuickAccessToolBar!.Items.Count == targets.Length
                      && targets.All(target => target.CreationCount == 1)
                      && menus.All(menu => menu.IsChecked),
                    "Deferred target assignment did not create each requested QAT copy exactly once.");

                menus[0].Target = null;
                Check(!menus[0].IsChecked && !ribbon.IsInQuickAccessToolBar(targets[0]),
                    "Explicitly clearing a resolved target retained its checked request or copy.");
                var replacement = new CountingButton { Header = "Replacement after explicit clear" };
                menus[0].Target = replacement;
                Check(!menus[0].IsChecked && replacement.CreationCount == 0,
                    "A cleared target's old checked intent was replayed for a later assignment.");
                menus[0].Target = null;
                menus[0].IsChecked = true;
                Check(menus[0].IsChecked, "A new checked request could not wait for a replacement target.");
                menus[0].Target = replacement;
                Check(ribbon.IsInQuickAccessToolBar(replacement) && replacement.CreationCount == 1,
                    "A new explicit checked request was lost before its replacement target arrived.");

                var cancelledTarget = new CountingButton { Header = "Cancelled before resolution" };
                var cancelled = new QuickAccessMenuItem { IsChecked = true };
                ribbon.QuickAccessToolBar!.QuickAccessItems.Add(cancelled);
                cancelled.IsChecked = false;
                cancelled.Target = cancelledTarget;
                Check(!cancelled.IsChecked && cancelledTarget.CreationCount == 0,
                    "Unchecking an unresolved toolbar-authored entry did not cancel its pending request.");
                ribbon.QuickAccessToolBar.QuickAccessItems.Remove(cancelled);

                var removedTarget = new CountingButton { Header = "Removed before resolution" };
                var removed = new QuickAccessMenuItem { IsChecked = true };
                ribbon.QuickAccessItems.Add(removed);
                ribbon.QuickAccessItems.Remove(removed);
                removed.Target = removedTarget;
                removed.IsChecked = false;
                removed.IsChecked = true;
                await settle();
                Check(!ribbon.QuickAccessItems.Contains(removed)
                      && !ribbon.IsInQuickAccessToolBar(removedTarget) && removedTarget.CreationCount == 0,
                    "Resolving or toggling a removed pending menu resurrected its quick-access request.");
            }
            finally
            {
                ribbon.QuickAccessItems.Clear();
                ribbon.ClearQuickAccessToolBar();
                host.Children.Remove(ribbon);
            }

            void ResolveTargets()
            {
                for (var index = 0; index < targets.Length; index++)
                {
                    menus[index].Target = targets[index];
                    Check(ribbon.IsInQuickAccessToolBar(targets[index])
                          && targets[index].CreationCount == 1
                          && menus.All(menu => menu.IsChecked),
                        "Resolving one target lost its request or unchecked another unresolved entry.");
                }
            }
        }

        var boundRibbon = CreateRibbon();
        var boundTarget = new CountingButton { Header = "Bound deferred target" };
        var laterTarget = new CountingButton { Header = "Later bound target" };
        var model = new QuickAccessTargetModel { IsChecked = true };
        var boundMenu = new QuickAccessMenuItem { Header = "Bound checked intent" };
        boundMenu.SetBinding(QuickAccessMenuItem.TargetProperty, new Binding
        {
            Source = model,
            Path = new PropertyPath(nameof(QuickAccessTargetModel.Target)),
            Mode = BindingMode.OneWay,
        });
        boundMenu.SetBinding(QuickAccessMenuItem.IsCheckedProperty, new Binding
        {
            Source = model,
            Path = new PropertyPath(nameof(QuickAccessTargetModel.IsChecked)),
            Mode = BindingMode.TwoWay,
        });
        boundRibbon.QuickAccessItems.Add(boundMenu);
        host.Children.Add(boundRibbon);
        try
        {
            await settle();
            Check(boundMenu.IsChecked && model.IsChecked && boundMenu.Target is null,
                "An unresolved Target binding overwrote the bound checked intent.");
            model.Target = boundTarget;
            await settle();
            Check(boundRibbon.IsInQuickAccessToolBar(boundTarget)
                  && boundTarget.CreationCount == 1 && boundMenu.IsChecked && model.IsChecked
                  && boundMenu.GetBindingExpression(QuickAccessMenuItem.TargetProperty) is not null
                  && boundMenu.GetBindingExpression(QuickAccessMenuItem.IsCheckedProperty) is not null,
                "Resolving a Target binding lost the checked request or replaced a customization binding.");
            model.Target = null;
            await settle();
            Check(!boundRibbon.IsInQuickAccessToolBar(boundTarget) && !boundMenu.IsChecked && !model.IsChecked,
                "A bound clear of a resolved target failed to remove and uncheck the entry.");
            model.Target = laterTarget;
            await settle();
            Check(!boundMenu.IsChecked && laterTarget.CreationCount == 0,
                "A later binding value resurrected checked intent cancelled by a resolved-target clear.");
            model.Target = null;
            model.IsChecked = true;
            await settle();
            Check(boundMenu.IsChecked && model.IsChecked, "A bound checked request was lost while its target was null.");
            model.Target = laterTarget;
            await settle();
            Check(boundRibbon.IsInQuickAccessToolBar(laterTarget) && laterTarget.CreationCount == 1,
                "A newly checked bound request was not applied when its target arrived.");

            var removedModel = new QuickAccessTargetModel();
            var removedMenu = new QuickAccessMenuItem { IsChecked = true };
            removedMenu.SetBinding(QuickAccessMenuItem.TargetProperty, new Binding
            {
                Source = removedModel,
                Path = new PropertyPath(nameof(QuickAccessTargetModel.Target)),
            });
            boundRibbon.QuickAccessToolBar!.QuickAccessItems.Add(removedMenu);
            Check(removedMenu.IsChecked, "A toolbar-authored entry lost its checked intent before binding resolution.");
            boundRibbon.QuickAccessToolBar.QuickAccessItems.Remove(removedMenu);
            var removedTarget = new CountingButton { Header = "Removed bound target" };
            removedModel.Target = removedTarget;
            await settle();
            removedMenu.IsChecked = false;
            removedMenu.IsChecked = true;
            Check(ReferenceEquals(removedMenu.Target, removedTarget)
                  && !boundRibbon.QuickAccessItems.Contains(removedMenu)
                  && !boundRibbon.IsInQuickAccessToolBar(removedTarget)
                  && removedTarget.CreationCount == 0,
                "A late binding update or checked change resurrected a removed pending menu.");
        }
        finally
        {
            boundRibbon.QuickAccessItems.Clear();
            boundRibbon.ClearQuickAccessToolBar();
            host.Children.Remove(boundRibbon);
        }
    }

    private static async Task VerifyDirectQuickAccessCustomizationAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        var first = new CountingButton { Header = "Toolbar-authored target" };
        var second = new CountingButton { Header = "Ribbon-authored target" };
        ribbon.Tabs[0].Groups[0].Items.Add(first);
        ribbon.Tabs[0].Groups[0].Items.Add(second);
        host.Children.Add(ribbon);
        try
        {
            await settle();
            var above = ribbon.QuickAccessToolBar
                        ?? throw new InvalidOperationException("The primary QAT did not load.");
            Check(ribbon.QuickAccessItems.Count == 0, "The direct-customization probe must start without ribbon menus.");
            var direct = new QuickAccessMenuItem { Header = "Direct", Target = first, IsChecked = true };
            above.QuickAccessItems.Add(direct);
            Check(above.QuickAccessItems.Contains(direct) && ribbon.QuickAccessItems.Contains(direct)
                  && ribbon.IsInQuickAccessToolBar(first) && direct.IsChecked && first.CreationCount == 1,
                "A directly authored toolbar customization item erased itself or failed to register.");

            var authored = new QuickAccessMenuItem { Header = "Authored", Target = second, IsChecked = true };
            ribbon.QuickAccessItems.Add(authored);
            above.QuickAccessItems.Move(0, 1);
            Check(ribbon.QuickAccessItems.SequenceEqual([authored, direct])
                  && first.CreationCount == 1 && second.CreationCount == 1,
                "Direct customization reordering lost ribbon-authored entries or recreated copies.");
            ribbon.ShowQuickAccessToolBarAboveRibbon = false;
            await settle();
            var below = ribbon.QuickAccessToolBar
                        ?? throw new InvalidOperationException("The below-ribbon QAT did not load.");
            Check(!ReferenceEquals(above, below) && above.QuickAccessItems.Count == 0
                  && below.QuickAccessItems.SequenceEqual([authored, direct])
                  && direct.IsChecked && authored.IsChecked,
                "QAT relocation discarded or duplicated directly authored customization entries.");

            below.QuickAccessItems.Remove(direct);
            Check(!ribbon.QuickAccessItems.Contains(direct) && !ribbon.IsInQuickAccessToolBar(first)
                  && ribbon.IsInQuickAccessToolBar(second) && authored.IsChecked,
                "Removing a direct customization entry left its target or removed an unrelated target.");
            direct.IsChecked = false;
            direct.IsChecked = true;
            Check(!ribbon.IsInQuickAccessToolBar(first) && first.CreationCount == 1,
                "A removed direct customization entry retained its checked callback.");

            var belowAuthored = new QuickAccessMenuItem { Header = "Below", Target = first, IsChecked = true };
            below.QuickAccessItems.Add(belowAuthored);
            Check(ribbon.IsInQuickAccessToolBar(first) && first.CreationCount == 2,
                "A customization entry added below the ribbon was not registered.");
            ribbon.ShowQuickAccessToolBarAboveRibbon = true;
            await settle();
            Check(above.QuickAccessItems.SequenceEqual([authored, belowAuthored])
                  && below.QuickAccessItems.Count == 0 && first.CreationCount == 2,
                "Moving a below-authored customization entry back above lost or recreated it.");
            ribbon.QuickAccessItems.Remove(authored);
            Check(!above.QuickAccessItems.Contains(authored) && !ribbon.IsInQuickAccessToolBar(second)
                  && ribbon.IsInQuickAccessToolBar(first),
                "Ribbon-side removal did not reconcile the directly editable toolbar collection.");
            above.QuickAccessItems.Clear();
            Check(ribbon.QuickAccessItems.Count == 0 && ribbon.QuickAccessToolBarItems.Count == 0,
                "Clearing direct toolbar customization left logical entries or copies.");
            belowAuthored.IsChecked = false;
            belowAuthored.IsChecked = true;
            Check(!ribbon.IsInQuickAccessToolBar(first) && first.CreationCount == 2,
                "Cleared toolbar customization retained a subscription.");

            var standalone = new QuickAccessToolBar();
            ribbon.ToolBarItems.Add(standalone);
            await settle();
            var reentrantTarget = new CountingButton { Header = "Reentrant customization" };
            var reentrantMenu = new QuickAccessMenuItem
            {
                Header = "Removed during registration",
                Target = reentrantTarget,
                IsChecked = true,
            };
            reentrantTarget.BeforeCreate = () => standalone.QuickAccessItems.Remove(reentrantMenu);
            standalone.QuickAccessItems.Add(reentrantMenu);
            Check(!standalone.QuickAccessItems.Contains(reentrantMenu)
                  && !ribbon.IsInQuickAccessToolBar(reentrantTarget)
                  && reentrantTarget.CreationCount == 1 && ribbon.QuickAccessToolBarItems.Count == 0,
                "Nested customization reconciliation was dropped during provider registration.");
            reentrantMenu.IsChecked = false;
            reentrantMenu.IsChecked = true;
            Check(reentrantTarget.CreationCount == 1 && !ribbon.IsInQuickAccessToolBar(reentrantTarget),
                "A menu removed during registration retained its callback until a later change.");
        }
        finally
        {
            ribbon.ToolBarItems.Clear();
            ribbon.QuickAccessItems.Clear();
            ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(ribbon);
        }
    }

    private static async Task VerifySharedQuickAccessTargetsAsync(Panel host, Func<Task> settle)
    {
        var ribbon = CreateRibbon();
        var first = new CountingButton { Header = "Shared original" };
        var second = new CountingButton { Header = "Retargeted" };
        var firstMenu = new QuickAccessMenuItem { Header = "First owner", Target = first, IsChecked = true };
        var secondMenu = new QuickAccessMenuItem { Header = "Second owner", Target = first, IsChecked = true };
        ribbon.Tabs[0].Groups[0].Items.Add(first);
        ribbon.Tabs[0].Groups[0].Items.Add(second);
        ribbon.QuickAccessItems.Add(firstMenu);
        ribbon.QuickAccessItems.Add(secondMenu);
        host.Children.Add(ribbon);
        try
        {
            await settle();
            var originalCopy = ribbon.GetQuickAccessElements()[first];
            Check(first.CreationCount == 1 && firstMenu.IsChecked && secondMenu.IsChecked,
                "Duplicate customization targets created duplicate copies.");
            firstMenu.Target = second;
            Check(ribbon.IsInQuickAccessToolBar(first) && ribbon.IsInQuickAccessToolBar(second)
                  && firstMenu.IsChecked && secondMenu.IsChecked
                  && ReferenceEquals(ribbon.GetQuickAccessElements()[first], originalCopy)
                  && first.CreationCount == 1 && second.CreationCount == 1,
                "Retargeting one checked menu removed the other menu's target or check state.");
            var retargetedCopy = ribbon.GetQuickAccessElements()[second];
            var thirdMenu = new QuickAccessMenuItem { Header = "Third owner", Target = first, IsChecked = true };
            ribbon.QuickAccessToolBar!.QuickAccessItems.Add(thirdMenu);
            ribbon.QuickAccessItems.Remove(secondMenu);
            Check(ribbon.IsInQuickAccessToolBar(first) && thirdMenu.IsChecked && first.CreationCount == 1,
                "Removing a checked owner ignored another directly authored owner of the same target.");
            var detachedTarget = new CountingButton { Header = "Must remain detached" };
            secondMenu.Target = detachedTarget;
            Check(detachedTarget.CreationCount == 0 && !ribbon.IsInQuickAccessToolBar(detachedTarget),
                "A removed duplicate customization entry retained its target callback.");

            thirdMenu.Target = second;
            Check(!ribbon.IsInQuickAccessToolBar(first) && ribbon.IsInQuickAccessToolBar(second)
                  && firstMenu.IsChecked && thirdMenu.IsChecked
                  && ReferenceEquals(ribbon.GetQuickAccessElements()[second], retargetedCopy)
                  && second.CreationCount == 1,
                "Retargeting the final original owner failed to remove only the original registration.");
            CheckMembership(first, originalCopy, false);
            ribbon.QuickAccessItems.Remove(firstMenu);
            Check(ribbon.IsInQuickAccessToolBar(second) && thirdMenu.IsChecked,
                "Removing one retargeted owner removed the other owner's registration.");
            thirdMenu.Target = null;
            Check(!ribbon.IsInQuickAccessToolBar(second) && !thirdMenu.IsChecked
                  && ribbon.GetQuickAccessElements().Count == 0,
                "The final checked customization owner left a stale registration.");
            CheckMembership(second, retargetedCopy, false);
            firstMenu.IsChecked = false;
            firstMenu.IsChecked = true;
            Check(!ribbon.IsInQuickAccessToolBar(second) && second.CreationCount == 1,
                "A removed shared-target menu re-added the target after its final owner left.");
        }
        finally
        {
            ribbon.QuickAccessItems.Clear();
            ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(ribbon);
        }
    }

    private static async Task VerifyPendingTabSelectionAsync(Panel host, Func<Task> settle)
    {
        foreach (var useAlias in new[] { false, true })
        {
            var ribbon = CreateRibbon(withTab: false);
            var chosen = new RibbonTabItem { Header = "Requested before population" };
            var first = new RibbonTabItem { Header = "Populated first" };
            if (useAlias)
            {
                ribbon.SelectedTabItem = chosen;
            }
            else
            {
                ribbon.SelectedTab = chosen;
            }

            ribbon.Tabs.Add(first);
            host.Children.Add(ribbon);
            try
            {
                await settle();
                Check(ReferenceEquals(ribbon.SelectedTab, chosen) && ribbon.SelectedTabIndex == -1
                      && ribbon.TabControl?.SelectedItem is null && !first.IsSelected,
                    "Partial population or template loading discarded a pending authored tab selection.");
                ribbon.Tabs.Add(chosen);
                await settle();
                AssertTabState(ribbon, chosen);
                ribbon.Tabs.Remove(chosen);
                await settle();
                AssertTabState(ribbon, first);
                ribbon.Tabs.Add(chosen);
                AssertTabState(ribbon, first);

                var cancelled = new RibbonTabItem { Header = "Cancelled request" };
                ribbon.SelectedTab = cancelled;
                ribbon.SelectedTabIndex = -1;
                ribbon.Tabs.Add(cancelled);
                AssertTabState(ribbon, first);
                var resetRequest = new RibbonTabItem { Header = "Request cancelled by reset" };
                ribbon.SelectedTabItem = resetRequest;
                ribbon.Tabs.Clear();
                AssertTabState(ribbon, null);
                ribbon.Tabs.Add(first);
                ribbon.Tabs.Add(resetRequest);
                AssertTabState(ribbon, first);
                var disabledRequest = new RibbonTabItem { Header = "Ineligible request", IsEnabled = false };
                ribbon.SelectedTab = disabledRequest;
                ribbon.Tabs.Add(disabledRequest);
                AssertTabState(ribbon, first);
                disabledRequest.IsEnabled = true;
                AssertTabState(ribbon, first);

                var superseded = new RibbonTabItem { Header = "Superseded item request" };
                ribbon.SelectedTab = superseded;
                await settle();
                ribbon.TabControl!.SelectedItem = first;
                await settle();
                AssertTabState(ribbon, first);
                ribbon.Tabs.Add(superseded);
                await settle();
                AssertTabState(ribbon, first);
            }
            finally
            {
                ribbon.Tabs.Clear();
                host.Children.Remove(ribbon);
            }
        }

        foreach (var useBinding in new[] { false, true })
        {
            var ribbon = CreateRibbon(withTab: false);
            var first = new RibbonTabItem { Header = "Index zero" };
            var second = new RibbonTabItem { Header = "Index one" };
            var chosen = new RibbonTabItem { Header = "Requested index two" };
            var model = new TabSelectionModel { SelectedTabIndex = 2 };
            if (useBinding)
            {
                ribbon.SetBinding(Ribbon.SelectedTabIndexProperty, new Binding
                {
                    Source = model,
                    Path = new PropertyPath(nameof(TabSelectionModel.SelectedTabIndex)),
                    Mode = BindingMode.TwoWay,
                });
            }
            else
            {
                ribbon.SelectedTabIndex = 2;
            }

            ribbon.Tabs.Add(first);
            host.Children.Add(ribbon);
            try
            {
                await settle();
                Check(ribbon.SelectedTabIndex == 2 && ribbon.SelectedTab is null
                      && ribbon.TabControl?.SelectedItem is null && !first.IsSelected,
                    "Partial population discarded or prematurely applied a requested tab index.");
                ribbon.Tabs.Add(second);
                Check(ribbon.SelectedTabIndex == 2 && ribbon.SelectedTab is null,
                    "The requested index was lost before enough tabs were populated.");
                ribbon.Tabs.Add(chosen);
                await settle();
                AssertTabState(ribbon, chosen);
                if (useBinding)
                {
                    Check(model.SelectedTabIndex == 2
                          && ribbon.GetBindingExpression(Ribbon.SelectedTabIndexProperty) is not null,
                        "Resolving a pending index overwrote the selection binding or its source.");
                    model.SelectedTabIndex = 0;
                    await settle();
                    AssertTabState(ribbon, first);
                    model.SelectedTabIndex = 2;
                    await settle();
                    AssertTabState(ribbon, chosen);
                }

                ribbon.Tabs.Remove(chosen);
                await settle();
                AssertTabState(ribbon, first);
                ribbon.Tabs.Add(chosen);
                AssertTabState(ribbon, first);
                ribbon.Tabs.Clear();
                ribbon.SelectedTabIndex = 1;
                ribbon.SelectedTab = null;
                ribbon.Tabs.Add(first);
                ribbon.Tabs.Add(second);
                AssertTabState(ribbon, first);
                ribbon.SelectedTabIndex = 2;
                ribbon.Tabs.Clear();
                ribbon.Tabs.Add(first);
                ribbon.Tabs.Add(second);
                ribbon.Tabs.Add(chosen);
                AssertTabState(ribbon, first);

                if (useBinding)
                {
                    model.SelectedTabIndex = 4;
                }
                else
                {
                    ribbon.SelectedTabIndex = 4;
                }

                await settle();
                ribbon.TabControl!.SelectedItem = second;
                await settle();
                AssertTabState(ribbon, second);
                if (useBinding)
                {
                    Check(model.SelectedTabIndex == 1,
                        "Native selection did not supersede the pending index binding.");
                }

                ribbon.Tabs.Add(new RibbonTabItem { Header = "Index three" });
                ribbon.Tabs.Add(new RibbonTabItem { Header = "Superseded index four" });
                await settle();
                AssertTabState(ribbon, second);
            }
            finally
            {
                ribbon.Tabs.Clear();
                host.Children.Remove(ribbon);
            }
        }

        var boundRibbon = CreateRibbon(withTab: false);
        var boundFirst = new RibbonTabItem { Header = "Binding first" };
        var boundChosen = new RibbonTabItem { Header = "Bound requested tab" };
        var selection = new TabSelectionModel { SelectedTab = boundChosen };
        boundRibbon.SetBinding(Ribbon.SelectedTabItemProperty, new Binding
        {
            Source = selection,
            Path = new PropertyPath(nameof(TabSelectionModel.SelectedTab)),
            Mode = BindingMode.TwoWay,
        });
        boundRibbon.Tabs.Add(boundFirst);
        host.Children.Add(boundRibbon);
        try
        {
            await settle();
            Check(ReferenceEquals(boundRibbon.SelectedTab, boundChosen)
                  && ReferenceEquals(selection.SelectedTab, boundChosen)
                  && boundRibbon.TabControl?.SelectedItem is null,
                "An item binding established before population was replaced by fallback selection.");
            boundRibbon.Tabs.Add(boundChosen);
            await settle();
            AssertTabState(boundRibbon, boundChosen);
            Check(boundRibbon.GetBindingExpression(Ribbon.SelectedTabItemProperty) is not null,
                "Resolving a bound pending item replaced its binding.");
            selection.SelectedTab = boundFirst;
            await settle();
            AssertTabState(boundRibbon, boundFirst);
            var later = new RibbonTabItem { Header = "Later bound request" };
            selection.SelectedTab = later;
            await settle();
            Check(ReferenceEquals(boundRibbon.SelectedTab, later) && ReferenceEquals(selection.SelectedTab, later),
                "A later bound selection was discarded before its tab was added.");
            boundRibbon.Tabs.Add(later);
            await settle();
            AssertTabState(boundRibbon, later);
            boundRibbon.Tabs.Remove(later);
            await settle();
            AssertTabState(boundRibbon, boundFirst);
            selection.SelectedTab = boundChosen;
            await settle();
            AssertTabState(boundRibbon, boundChosen);

            var supersededBoundTab = new RibbonTabItem { Header = "Superseded bound request" };
            selection.SelectedTab = supersededBoundTab;
            await settle();
            boundRibbon.TabControl!.SelectedItem = boundFirst;
            await settle();
            AssertTabState(boundRibbon, boundFirst);
            Check(ReferenceEquals(selection.SelectedTab, boundFirst)
                  && boundRibbon.GetBindingExpression(Ribbon.SelectedTabItemProperty) is not null,
                "Native selection did not supersede and preserve the authored item binding.");
            boundRibbon.Tabs.Add(supersededBoundTab);
            await settle();
            AssertTabState(boundRibbon, boundFirst);
        }
        finally
        {
            boundRibbon.Tabs.Clear();
            host.Children.Remove(boundRibbon);
        }
    }

    public static async Task VerifyContextualGroupsAsync(Panel host, Func<Task> settle)
    {
        var markup = new PortRibbonContextualFixture();
        host.Children.Add(markup);
        try
        {
            await settle();
            Check(ReferenceEquals(markup.Tab.Group, markup.Group)
                  && markup.Tab.GetBindingExpression(RibbonTabItem.GroupProperty) is not null,
                "WPF ElementName markup lost its Group binding during Ribbon loading.");
            markup.Ribbon.ContextualGroups.Remove(markup.Group);
            await settle();
            Check(ReferenceEquals(markup.Tab.Group, markup.Group) && markup.Group.Items.Count == 0,
                "Removing a group destroyed an ElementName binding or kept stale membership.");
            markup.Ribbon.ContextualGroups.Add(markup.Group);
            await settle();
            Check(markup.Group.Items.Contains(markup.Tab),
                "Re-adding a group did not restore ElementName-bound membership.");
        }
        finally
        {
            markup.Ribbon.Tabs.Clear();
            markup.Ribbon.ContextualGroups.Clear();
            host.Children.Remove(markup);
        }

        var ribbon = CreateRibbon();
        var first = new RibbonContextualTabGroup { Header = "First", Visibility = Visibility.Visible };
        var second = new RibbonContextualTabGroup { Header = "Second", Visibility = Visibility.Visible };
        var named = new RibbonContextualTabGroup { Header = "Named", Visibility = Visibility.Visible };
        ribbon.ContextualGroups.Add(first);
        ribbon.ContextualGroups.Add(second);
        ribbon.ContextualGroups.Add(named);
        var model = new GroupModel { Group = first };
        var bound = new RibbonTabItem { Header = "Bound", ContextualTabGroupName = "Named" };
        bound.SetBinding(RibbonTabItem.GroupProperty,
            new Binding { Source = model, Path = new PropertyPath(nameof(GroupModel.Group)) });
        var explicitTab = new RibbonTabItem { Header = "Explicit", Group = first };
        var namedTab = new RibbonTabItem { Header = "Named", ContextualTabGroupName = "Named" };
        var explicitNull = new RibbonTabItem
        {
            Header = "Explicit null",
            Group = null,
            ContextualTabGroupName = "Named",
        };
        ribbon.Tabs.Add(bound);
        ribbon.Tabs.Add(explicitTab);
        ribbon.Tabs.Add(namedTab);
        ribbon.Tabs.Add(explicitNull);
        Check(ReferenceEquals(bound.Group, first) && ReferenceEquals(explicitTab.Group, first),
            "Pre-template linkage overwrote an explicit or bound Group.");
        Check(ReferenceEquals(namedTab.Group, named) && explicitNull.Group is null,
            "Name linking ignored an explicit null or failed to resolve an unassigned Group.");
        host.Children.Add(ribbon);
        try
        {
            await settle();
            Check(bound.GetBindingExpression(RibbonTabItem.GroupProperty) is not null,
                "Ribbon loading replaced the Group binding.");
            Check(first.Items.SequenceEqual([bound, explicitTab]), "Contextual membership did not follow ribbon order.");
            ribbon.Tabs.Move(ribbon.Tabs.IndexOf(explicitTab), 1);
            await settle();
            Check(first.Items.SequenceEqual([explicitTab, bound])
                  && explicitTab.HasLeftGroupBorder && bound.HasRightGroupBorder,
                "Moving contextual tabs left assignment-order membership or stale group borders.");

            model.Group = second;
            await settle();
            Check(ReferenceEquals(bound.Group, second) && !first.Items.Contains(bound) && second.Items.Contains(bound),
                "A live Group binding stopped updating or retained old membership.");
            model.Group = null;
            await settle();
            Check(bound.Group is null && !named.Items.Contains(bound),
                "Name resolution overwrote a live binding that returned null.");
            model.Group = second;
            named.Header = "Renamed";
            await settle();
            Check(namedTab.Group is null && named.Items.Count == 0, "Renaming a group retained a stale name link.");
            namedTab.ContextualTabGroupName = "Renamed";
            await settle();
            Check(ReferenceEquals(namedTab.Group, named), "Changing a tab's group name did not relink it.");
            namedTab.Group = second;
            namedTab.ContextualTabGroupName = "First";
            await settle();
            Check(ReferenceEquals(namedTab.Group, second) && !named.Items.Contains(namedTab),
                "Name changes overwrote a user override of an automatically linked Group.");

            ribbon.ContextualGroups.Remove(second);
            await settle();
            Check(ReferenceEquals(bound.Group, second)
                  && bound.GetBindingExpression(RibbonTabItem.GroupProperty) is not null
                  && second.Items.Count == 0 && !bound.IsContextual && !namedTab.IsContextual,
                "Removing an explicit group destroyed authored values or kept active membership.");
            second.Visibility = Visibility.Collapsed;
            Check(bound.Visibility == Visibility.Visible, "A removed group still controls former tab visibility.");
            ribbon.ContextualGroups.Add(second);
            await settle();
            Check(second.Items.Contains(bound) && bound.Visibility == Visibility.Collapsed,
                "Re-adding an explicitly referenced group did not reactivate its tabs.");
            second.Visibility = Visibility.Visible;
            await settle();
            Check(bound.Visibility == Visibility.Visible, "Reactivated contextual visibility did not restore.");

            ribbon.Tabs.Remove(bound);
            await settle();
            Check(!second.Items.Contains(bound), "Removing a bound contextual tab retained group membership.");
            model.Group = first;
            await settle();
            Check(ReferenceEquals(bound.Group, first) && !first.Items.Contains(bound),
                "A removed tab lost its binding or rejoined a group through stale callbacks.");
            ribbon.Tabs.Insert(1, bound);
            await settle();
            Check(first.Items.SequenceEqual([bound, explicitTab]), "A reinserted bound tab did not restore ordered membership.");

            ribbon.Tabs.Clear();
            await settle();
            Check(ribbon.ContextualGroups.All(group => group.Items.Count == 0),
                "Reset retained contextual tabs or their group visibility callbacks.");
            first.Visibility = Visibility.Collapsed;
            Check(bound.Visibility == Visibility.Visible && explicitTab.Visibility == Visibility.Visible,
                "A group still controls visibility of tabs removed by reset.");
            ribbon.ContextualGroups.Clear();
        }
        finally
        {
            ribbon.Tabs.Clear();
            ribbon.ContextualGroups.Clear();
            host.Children.Remove(ribbon);
        }
    }

    private static Ribbon CreateRibbon(bool withTab = true)
    {
        var ribbon = new Ribbon
        {
            Width = 800,
            AutomaticStateManagement = false,
            IsAutomaticCollapseEnabled = false,
            IsKeyTipHandlingEnabled = false,
        };
        if (withTab)
        {
            var tab = new RibbonTabItem { Header = "Home" };
            tab.Groups.Add(new RibbonGroupBox { Header = "Commands" });
            ribbon.Tabs.Add(tab);
        }

        return ribbon;
    }

    private static void AssertTabState(Ribbon ribbon, RibbonTabItem? selected)
    {
        var tabControl = ribbon.TabControl
                         ?? throw new InvalidOperationException("The native tab control is unavailable.");
        Check(tabControl.TabItems.Cast<object>().SequenceEqual(ribbon.Tabs.Cast<object>()),
            "The realized tab collection differs from Ribbon.Tabs.");
        var index = selected is null ? -1 : ribbon.Tabs.IndexOf(selected);
        Check(ReferenceEquals(ribbon.SelectedTab, selected)
              && ReferenceEquals(ribbon.SelectedTabItem, selected)
              && ReferenceEquals(tabControl.SelectedItem, selected)
              && ribbon.SelectedTabIndex == index && tabControl.SelectedIndex == index,
            "Ribbon and TabControl selection identity/index are inconsistent.");
        Check(ReferenceEquals(tabControl.SelectedContent, selected?.Content),
            "Tab selection left stale selected content.");
        Check(ribbon.Tabs.All(tab => tab.IsSelected == ReferenceEquals(tab, selected)),
            "Tab IsSelected flags disagree with the selected item.");
    }

    private static void CheckMembership(UIElement original, UIElement copy, bool expected)
    {
        Check(RibbonProperties.GetIsElementInQuickAccessToolBar(original) == expected
              && RibbonProperties.GetIsElementInQuickAccessToolBar(copy) == expected,
            "Original/copy QAT membership properties disagree with registration.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed partial class CountingButton : RibbonButton
    {
        public int CreationCount { get; private set; }
        public Action? BeforeCreate { get; set; }

        public override FrameworkElement CreateQuickAccessItem()
        {
            CreationCount++;
            BeforeCreate?.Invoke();
            return new RibbonButton { Header = Header, CanAddToQuickAccessToolBar = false };
        }
    }

    private sealed class CountingProvider : IQuickAccessItemProvider
    {
        public bool CanAddToQuickAccessToolBar { get; set; } = true;
        public int CreationCount { get; private set; }

        public FrameworkElement CreateQuickAccessItem()
        {
            CreationCount++;
            return new Microsoft.UI.Xaml.Controls.Button { Content = "Standalone" };
        }
    }

    private sealed class FailingProvider(Exception exception) : IQuickAccessItemProvider
    {
        public bool CanAddToQuickAccessToolBar { get; set; } = true;
        public FrameworkElement CreateQuickAccessItem() => throw exception;
    }

    private sealed class EmptyProvider : IQuickAccessItemProvider
    {
        public bool CanAddToQuickAccessToolBar { get; set; } = true;
        public FrameworkElement? CreateQuickAccessItem() => null;
    }

    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed class QuickAccessTargetModel : INotifyPropertyChanged
    {
        private UIElement? target;
        private bool isChecked;
        public event PropertyChangedEventHandler? PropertyChanged;

        public UIElement? Target
        {
            get => target;
            set
            {
                if (ReferenceEquals(target, value))
                {
                    return;
                }

                target = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Target)));
            }
        }

        public bool IsChecked
        {
            get => isChecked;
            set
            {
                if (isChecked == value)
                {
                    return;
                }

                isChecked = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked)));
            }
        }
    }

    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed class TabSelectionModel : INotifyPropertyChanged
    {
        private RibbonTabItem? selectedTab;
        private int selectedTabIndex = -1;
        public event PropertyChangedEventHandler? PropertyChanged;

        public RibbonTabItem? SelectedTab
        {
            get => selectedTab;
            set
            {
                if (ReferenceEquals(selectedTab, value))
                {
                    return;
                }

                selectedTab = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTab)));
            }
        }

        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set
            {
                if (selectedTabIndex == value)
                {
                    return;
                }

                selectedTabIndex = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedTabIndex)));
            }
        }
    }

    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed class GroupModel : INotifyPropertyChanged
    {
        private RibbonContextualTabGroup? group;
        public event PropertyChangedEventHandler? PropertyChanged;

        public RibbonContextualTabGroup? Group
        {
            get => group;
            set
            {
                if (ReferenceEquals(group, value))
                {
                    return;
                }

                group = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Group)));
            }
        }
    }
}
