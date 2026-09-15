namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

internal static class PortQuickAccessCloneTests
{
    public static async Task VerifyClonesAsync(Panel host, Func<Task> settle)
    {
        await Run("menu", () => VerifyMenuActions(host, settle));
        await Run("drop-down", () => VerifyDropDownContent(host, settle, new Fluent.DropDownButton { Header = "Drop-down" }));
        await Run("core split", () => VerifyDropDownContent(host, settle, new RibbonSplitButton { Header = "Core split" }));
        await Run("facade split", () => VerifyDropDownContent(host, settle, new Fluent.SplitButton { Header = "Facade split" }));
        await Run("group", () => VerifyGroup(host, settle));
        await Run("gallery", () => VerifyGallery(host, settle));
        await Run("presentation leases", () => VerifyPresentationLeases(host, settle));
#if WINDOWS
        await PortNativePopupContractTests.VerifyAsync(host, settle);
#endif

        static async Task Run(string name, Func<Task> verify)
        {
            App.LogAutoTestStartup($"QAT CLONES BEGIN {name}");
            await verify();
            App.LogAutoTestStartup($"QAT CLONES COMPLETE {name}");
        }
    }

    private static async Task VerifyMenuActions(Panel host, Func<Task> settle)
    {
        var fixture = new Fixture(host);
        App.LogAutoTestStartup("QAT MENU fixture constructed");
        var command = new CloneCommand();
        var leaf = new Fluent.MenuItem { Header = "Action", Command = command, CommandParameter = "leaf", IsDefinitive = false };
        var check = new Fluent.MenuItem { Header = "Checked action", IsCheckable = true, Command = command, IsDefinitive = false };
        var choices = new Fluent.MenuItem { Header = "Grouped choices" };
        var peer = new Fluent.MenuItem { Header = "Other choice", IsCheckable = true, GroupName = "qat-choices", IsChecked = true };
        check.GroupName = "qat-choices";
        choices.Items.Add(check);
        choices.Items.Add(peer);
        fixture.Sources.Children.Add(choices);
        var split = new Fluent.MenuItem { Header = "Menu split", IsSplit = true, IsCheckable = true, Command = command, IsDefinitive = false };
        var submenu = new Fluent.MenuItem { Header = "Submenu" };
        var nested = new Fluent.MenuItem { Header = "Nested action", Command = command, IsDefinitive = false };
        var editor = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Live submenu editor", Width = 180 };
        var splitChild = new Fluent.MenuItem { Header = "Split child", Command = command, IsDefinitive = false };
        submenu.Items.Add(nested);
        submenu.Items.Add(editor);
        split.Items.Add(splitChild);
        var clicks = 0;
        leaf.Click += (_, _) => clicks++;
        check.Click += (_, _) => clicks++;
        split.Click += (_, _) => clicks++;
        nested.Click += (_, _) => clicks++;
        splitChild.Click += (_, _) => clicks++;
        foreach (var item in new[] { leaf, check, split, submenu })
        {
            if (!ReferenceEquals(item, check))
            {
                fixture.Sources.Children.Add(item);
            }
            fixture.Ribbon.AddToQuickAccessToolBar(item);
        }
        App.LogAutoTestStartup("QAT MENU sources and copies registered");

        try
        {
            await settle();
            App.LogAutoTestStartup("QAT MENU initial layout complete");
            var leafCopy = Copy<Fluent.Button>(fixture.Ribbon, leaf);
            var checkCopy = Copy<RibbonToggleButton>(fixture.Ribbon, check);
            var splitCopy = Copy<RibbonSplitButton>(fixture.Ribbon, split);
            var submenuCopy = Copy<Fluent.DropDownButton>(fixture.Ribbon, submenu);
            var headerTemplate = TextTemplate();
            leaf.Header = new CloneModel("Updated action", command);
            leaf.HeaderTemplate = headerTemplate;
            leaf.IconGlyph = "\uE8A5";
            await settle();
            App.LogAutoTestStartup("QAT MENU header presentation updated");
            Check(Equals(leafCopy.Header, leaf.Header) && ReferenceEquals(leafCopy.HeaderTemplate, headerTemplate)
                  && leafCopy.IconGlyph == leaf.IconGlyph,
                "MenuItem clone header/template/icon metadata became a stale snapshot.");
            Invoke(leafCopy);
            await settle();
            App.LogAutoTestStartup("QAT MENU leaf invoked");
            Check(command.Executions == 1 && clicks == 1 && Equals(command.LastParameter, "leaf"),
                "A MenuItem button copy did not forward Command and Click exactly once.");
            checkCopy.OnKeyTipPressed();
            await settle();
            Check(command.Executions == 2 && clicks == 2 && check.IsChecked == true && checkCopy.IsChecked == true && peer.IsChecked == false,
                "A checked MenuItem copy lost its canonical checked state or duplicated its action.");
            peer.IsChecked = true;
            Check(check.IsChecked == false && checkCopy.IsChecked == false, "Source menu-group selection did not reach its QAT copy.");
            check.IsChecked = true;
            Check(peer.IsChecked == false && checkCopy.IsChecked == true, "The QAT checked-state link broke canonical group exclusivity.");
            Invoke(splitCopy);
            await settle();
            App.LogAutoTestStartup("QAT MENU split primary invoked");
            Check(command.Executions == 3 && clicks == 3 && split.IsChecked == true,
                "A split MenuItem copy did not execute its primary action exactly once.");

            command.Allowed = false;
            command.Notify();
            await settle();
            Check(!leafCopy.IsEnabled && !checkCopy.IsEnabled && !splitCopy.IsPrimaryActionEnabled && splitCopy.IsEnabled,
                "Command availability was lost, or disabled a split copy's independent drop-down.");
            var leafResult = leaf.OnKeyTipPressed();
            Check(!leafResult.PressedElementOpenedPopup && command.Executions == 3 && clicks == 3,
                "A command-disabled leaf KeyTip invoked an action.");
            await OpenAsync(split, settle, () =>
            {
                var result = split.OnKeyTipPressed();
                Check(result.PressedElementOpenedPopup,
                    "The primary command gate blocked a split MenuItem's submenu KeyTip.");
            });
            App.LogAutoTestStartup("QAT MENU source submenu opened");
            AssertRealContent(split, splitChild);
            Check(command.Executions == 3 && clicks == 3,
                "Opening a disabled-primary split submenu invoked its primary action.");
            split.OnKeyTipBack();
            await settle();
            App.LogAutoTestStartup("QAT MENU source submenu closed");
            await OpenAsync(splitCopy, settle);
            App.LogAutoTestStartup("QAT MENU split copy submenu opened");
            AssertRealContent(splitCopy, splitChild);
            splitCopy.IsDropDownOpen = false;
            command.Allowed = true;
            command.Notify();
            await settle();

            await OpenAsync(submenuCopy, settle);
            App.LogAutoTestStartup("QAT MENU regular copy submenu opened");
            AssertRealContent(submenuCopy, editor);
            AssertRealContent(submenuCopy, nested);
            editor.Text = "Edited through quick access";
            Check(ReferenceEquals(submenu.Items[1], editor) && editor.Text == "Edited through quick access",
                "Submenu content was replaced by a text snapshot.");
            new RibbonMenuItemAutomationPeer(nested).Invoke();
            await settle();
            Check(command.Executions == 4 && clicks == 4, "A borrowed submenu command did not invoke the original once.");

            var inserted = new Fluent.MenuItem { Header = "Inserted while open", IsDefinitive = false };
            submenu.Items.Insert(0, inserted);
            await settle();
            AssertRealContent(submenuCopy, inserted);
            submenu.Items.Remove(inserted);
            await settle();
            Check(!IsDescendant(inserted, PopupRoot(submenuCopy)), "A removed submenu child remained in the QAT presentation.");
            fixture.Sources.Children.Remove(submenu);
            await settle();
            Check(!submenu.IsLoaded && submenuCopy.IsDropDownOpen, "Unloading the original menu closed its independent QAT surface.");
            AssertRealContent(submenuCopy, editor);
            submenuCopy.IsDropDownOpen = false;
            await settle();
            await OpenAsync(submenuCopy, settle);
            AssertRealContent(submenuCopy, nested);

            fixture.Sources.Children.Remove(leaf);
            command.Allowed = false;
            command.Notify();
            await settle();
            command.Allowed = true;
            command.Notify();
            await settle();
            Invoke(leafCopy);
            await settle();
            Check(command.Executions == 5 && clicks == 5 && leafCopy.IsEnabled,
                "A visible MenuItem copy stopped working after its original unloaded.");
            var replacementCommand = new CloneCommand();
            leaf.Command = replacementCommand;
            leaf.CommandParameter = "replacement";
            command.Allowed = false;
            command.Notify();
            await settle();
            Check(leafCopy.IsEnabled, "The copy retained an observation of its retired command.");
            Invoke(leafCopy);
            await settle();
            Check(replacementCommand.Executions == 1 && Equals(replacementCommand.LastParameter, "replacement")
                  && command.Executions == 5 && clicks == 6,
                "An unloaded source's replacement command/parameter was not forwarded exactly once.");

            fixture.Ribbon.RemoveFromQuickAccessToolBar(submenu);
            await settle();
            Check(!submenuCopy.IsDropDownOpen && !IsDescendant(editor, PopupRoot(submenuCopy)),
                "QAT removal retained an open borrowed menu or its visual owner.");
            var oldHeader = submenuCopy.Header;
            submenu.Header = "Changed while removed";
            await settle();
            Check(Equals(submenuCopy.Header, oldHeader), "A removed QAT copy retained its source-property subscription.");
            fixture.Ribbon.QuickAccessToolBarItems.Add(submenuCopy);
            await settle();
            Check(Equals(submenuCopy.Header, submenu.Header), "Reinsertion did not reconnect the same copy to current state.");
            await OpenAsync(submenuCopy, settle);
            AssertRealContent(submenuCopy, editor);
        }
        finally
        {
            fixture.Dispose();
            await settle();
        }
    }

    private static async Task VerifyDropDownContent(Panel host, Func<Task> settle, Fluent.DropDownButton source)
    {
        var fixture = new Fixture(host);
        var command = new CloneCommand();
        var native = new Microsoft.UI.Xaml.Controls.Button { Content = "Native command", Command = command };
        var text = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Live content", Width = 180 };
        var content = new StackPanel();
        content.Children.Add(text);
        content.Children.Add(native);
        var menu = new Fluent.MenuItem { Header = "Nested", IsDefinitive = false };
        var child = new Fluent.MenuItem { Header = "Nested command", Command = command, IsDefinitive = false };
        menu.Items.Add(child);
        var gallery = new Fluent.Gallery { Width = 220, Height = 70 };
        var galleryItem = new Fluent.GalleryItem { Content = "Gallery command", Command = command };
        gallery.Items.Add(galleryItem);
        source.Items.Add(content);
        source.Items.Add(menu);
        source.Items.Add(gallery);
        var nativeClicks = 0;
        native.Click += (_, _) => nativeClicks++;
        var primaryClicks = 0;
        if (source is RibbonSplitButton sourceSplit)
        {
            sourceSplit.Command = command;
            sourceSplit.IsDefinitive = false;
            sourceSplit.Click += (_, _) => primaryClicks++;
        }

        fixture.Sources.Children.Add(source);
        fixture.Ribbon.AddToQuickAccessToolBar(source);
        try
        {
            await settle();
            var clone = Copy<Fluent.DropDownButton>(fixture.Ribbon, source);
            for (var iteration = 0; iteration < 2; iteration++)
            {
                await OpenAsync(clone, settle);
                AssertRealContent(clone, content);
                AssertRealContent(clone, gallery);
                Check(PopupRoot(source) is null,
                    "The inactive source exposed the QAT anchor's active native presentation.");
                Invoke(native);
                await settle();
                Check(command.Executions == iteration + 1 && nativeClicks == iteration + 1,
                    "The drop-down copy replaced an arbitrary native control or duplicated its action.");
                text.Text = $"Edit {iteration}";
                Check(ReferenceEquals(source.Items[0], content), "Opening a QAT copy replaced its source's item identity.");
                clone.IsDropDownOpen = false;
                await settle();
                Check(!content.IsLoaded && !IsDescendant(content, PopupRoot(clone)),
                    $"Closing a drop-down copy left the source body attached to that copy. "
                    + $"loaded={content.IsLoaded}, open={clone.IsDropDownOpen}, root={Describe(PopupRoot(clone))}");
            }

            fixture.Sources.Children.Remove(source);
            await settle();
            await OpenAsync(clone, settle);
            AssertRealContent(clone, text);
            var dynamic = new Microsoft.UI.Xaml.Controls.TextBox { Text = "Added while original unloaded", Width = 190 };
            source.Items.Insert(1, dynamic);
            await settle();
            AssertRealContent(clone, dynamic);
            menu.IsDropDownOpen = true;
            await settle();
            Check(menu.DropDownPopup?.IsOpen == true && child.IsLoaded && clone.IsDropDownOpen,
                "A borrowed nested submenu did not retain its live QAT parent.");
            new RibbonMenuItemAutomationPeer(child).Invoke();
            await settle();
            Check(command.Executions == 3, "Nested content did not execute the original command once.");
            menu.IsDropDownOpen = false;

            if (clone is RibbonSplitButton splitClone)
            {
                Invoke(splitClone);
                await settle();
                Check(primaryClicks == 1 && command.Executions == 4,
                    "An unloaded source split's primary command ran zero or multiple times.");
            }

            clone.IsDropDownOpen = false;
            await settle();
            var models = new ObservableCollection<CloneModel> { new("First model", command) };
            source.Items.Clear();
            source.ItemTemplate = MenuTemplate();
            source.ItemsSource = models;
            await OpenAsync(clone, settle);
            var modelItem = Find<Fluent.MenuItem>(PopupRoot(clone), item => Equals(item.Header, "First model"));
            Check(modelItem is not null && modelItem.IsLoaded, "The QAT drop-down did not realize its live ItemsSource template.");
            models[0].Label = "Edited model";
            models.Add(new CloneModel("Second model", command));
            await settle();
            Check(Find<Fluent.MenuItem>(PopupRoot(clone), item => Equals(item.Header, "Edited model")) is not null
                  && Find<Fluent.MenuItem>(PopupRoot(clone), item => Equals(item.Header, "Second model")) is not null,
                "The copy snapshotted source models or their template bindings.");
            models.RemoveAt(0);
            await settle();
            Check(Find<Fluent.MenuItem>(PopupRoot(clone), item => Equals(item.Header, "Edited model")) is null,
                "A removed model remained in the live QAT drop-down.");
            fixture.Ribbon.RemoveFromQuickAccessToolBar(source);
            await settle();
            Check(!clone.IsDropDownOpen, "Removing a QAT drop-down did not release its active presentation.");
        }
        finally
        {
            source.IsDropDownOpen = false;
            menu.IsDropDownOpen = false;
            fixture.Dispose();
            await settle();
        }
    }

    private static async Task VerifyGroup(Panel host, Func<Task> settle)
    {
        var fixture = new Fixture(host);
        var command = new CloneCommand();
        var action = new Fluent.Button { Header = "Group action", Command = command, IsDefinitive = false };
        var group = fixture.Ribbon.Tabs[0].Groups[0];
        group.Header = "Independent group";
        group.Items.Add(action);
        group.IsLauncherVisible = true;
        group.LauncherText = "Group dialog";
        group.LauncherCommand = command;
        var clicks = 0;
        var launcherClicks = 0;
        action.Click += (_, _) => clicks++;
        group.LauncherClick += (_, _) => launcherClicks++;
        fixture.Ribbon.AddToQuickAccessToolBar(group);
        try
        {
            await settle();
            var clone = Copy<RibbonGroupBox>(fixture.Ribbon, group);
            Check(clone.State == RibbonGroupBoxState.QuickAccess && clone.IsInButtonState,
                "A group QAT provider did not create an independent QuickAccess group.");
            Check(ReferenceEquals(clone.Items, group.Items)
                  && ReferenceEquals(clone.GetValue(RibbonGroupBox.ItemsProperty), group.Items),
                "A group copy introduced another authoritative container collection.");
            foreach (var state in new[] { RibbonGroupBoxState.Large, RibbonGroupBoxState.Collapsed })
            {
                group.State = state;
                await settle();
                var originalState = group.State;
                await OpenAsync(clone, settle);
                AssertRealContent(clone, action);
                Check(!group.IsDropDownOpen && group.State == originalState && group.IsSnapped,
                    "The group copy opened or resized its original instead of presenting independently.");
                Invoke(action);
                await settle();
                clone.IsDropDownOpen = false;
                await settle();
                Check(!group.IsSnapped && group.Items.Count == 1 && ReferenceEquals(group.Items[0], action),
                    "Group close failed to restore source state and authored item identity.");
            }

            Check(clicks == 2 && command.Executions == 2, "Group commands were copied, lost, or forwarded more than once.");
            fixture.Ribbon.SelectedTab = fixture.Ribbon.Tabs[1];
            await settle();
            Check(!group.IsLoaded, "The inactive-tab group fixture did not unload its original.");
            await OpenAsync(clone, settle);
            AssertRealContent(clone, action);
            var launcher = clone.LauncherButton;
            Check(launcher is not null && launcher.IsLoaded, "The QAT group did not expose its live launcher.");
            if (launcher is null)
            {
                throw new InvalidOperationException("The group launcher is missing.");
            }
            Invoke(launcher);
            await settle();
            Check(launcherClicks == 1 && command.Executions == 3, "The QAT launcher duplicated or lost Command/LauncherClick.");
            clone.IsDropDownOpen = false;
            await settle();
            var models = new ObservableCollection<CloneModel> { new("Model group item", command) };
            group.Items.Clear();
            group.ItemTemplate = ButtonTemplate();
            group.ItemsSource = models;
            await OpenAsync(clone, settle);
            var second = new CloneModel("Inserted group item", command);
            models.Add(second);
            await settle();
            var container = group.ContainerFromItem(second);
            Check(container is FrameworkElement && ReferenceEquals(container, clone.ContainerFromItem(second)),
                "An off-tab QAT group lost its canonical data-container mapping.");
            var generated = Find<Fluent.Button>(PopupRoot(clone), button => Equals(button.Header, second.Label));
            Check(generated is not null && generated.IsLoaded, "A live QAT group ignored ItemsSource mutation while its original was unloaded.");
            fixture.Ribbon.RemoveFromQuickAccessToolBar(group);
            await settle();
            Check(!clone.IsDropDownOpen && !group.IsSnapped, "Removing a group copy retained its content lease.");
            fixture.Ribbon.QuickAccessToolBarItems.Add(clone);
            await settle();
            await OpenAsync(clone, settle);
            Check(Find<Fluent.Button>(PopupRoot(clone), button => Equals(button.Header, second.Label)) is { IsLoaded: true },
                "Reinserting the same group copy could not reacquire current source content.");
        }
        finally
        {
            fixture.Dispose();
            await settle();
        }
    }

    private static async Task VerifyGallery(Panel host, Func<Task> settle)
    {
        await PortGalleryCloneContractTests.VerifyAsync(host, settle);
        var fixture = new Fixture(host);
        var command = new CloneCommand();
        var models = new ObservableCollection<CloneModel> { new("First gallery item", command), new("Second gallery item", command) };
        var gallery = new InRibbonGallery
        {
            Header = "Live QAT gallery", Width = 250, Height = 80, ItemWidth = 90, ItemHeight = 40,
            ItemsSource = models, ItemTemplate = TextTemplate(),
        };
        var menu = new Fluent.MenuItem { Header = "Gallery menu", Command = command, IsDefinitive = false };
        gallery.MenuItems.Add(menu);
        gallery.SelectedItem = models[0];
        fixture.Sources.Children.Add(gallery);
        fixture.Ribbon.AddToQuickAccessToolBar(gallery);
        try
        {
            await settle();
            var clone = Copy<InRibbonGallery>(fixture.Ribbon, gallery);
            fixture.Sources.Children.Remove(gallery);
            await settle();
            await OpenAsync(clone, settle);
            var second = gallery.Items[1];
            AssertRealContent(clone, second);
            AssertRealContent(clone, menu);
            Check(clone.Items.Count == 2 && ReferenceEquals(clone.Items[1], second),
                "A gallery copy did not borrow its original interactive containers.");
            new GalleryItemWrapperAutomationPeer(RequireType<RibbonGalleryItem>(second)).Select();
            await settle();
            Check(ReferenceEquals(clone.SelectedItem, models[1]) && ReferenceEquals(gallery.SelectedItem, models[1]),
                "The QAT gallery did not keep model selection live in both directions.");
            models.Add(new CloneModel("Third gallery item", command));
            await settle();
            Check(gallery.Items.Count == 3 && clone.Items.Count == 3 && ReferenceEquals(gallery.Items[2], clone.Items[2]),
                "The unloaded original gallery stopped observing its live QAT source.");
            AssertRealContent(clone, gallery.Items[2]);
            gallery.SelectedItem = models[2];
            gallery.ItemWidth = 105;
            await settle();
            Check(ReferenceEquals(clone.SelectedItem, models[2]) && clone.ItemWidth == 105,
                "A gallery clone retained stale selection or layout metadata.");
            var all = new GalleryGroupFilter { Title = "All items" };
            var alternate = new GalleryGroupFilter { Title = "Alternate view" };
            gallery.Filters.Add(all);
            gallery.Filters.Add(alternate);
            await settle();
            Check(clone.Filters.SequenceEqual(gallery.Filters), "A gallery copy snapshotted its filter collection.");
            clone.SelectedFilter = alternate;
            Check(ReferenceEquals(gallery.SelectedFilter, alternate), "An open QAT gallery did not update canonical filter selection.");
            gallery.Filters.Remove(all);
            await settle();
            Check(clone.Filters.Count == 1 && ReferenceEquals(clone.SelectedFilter, alternate),
                "Dynamic filter removal lost the current QAT selection.");
            new RibbonMenuItemAutomationPeer(menu).Invoke();
            await settle();
            Check(command.Executions == 1, "The gallery's borrowed menu did not invoke its real command exactly once.");
            clone.IsDropDownOpen = false;
            await settle();
            Check(!gallery.IsFrozen && !gallery.IsSnapped && !menu.IsLoaded
                  && !IsDescendant(second, PopupRoot(clone)) && ReferenceEquals(gallery.SelectedItem, models[2]),
                "Closing the gallery copy did not restore its original ownership/selection.");
            await OpenAsync(clone, settle);
            AssertRealContent(clone, gallery.Items[2]);
            fixture.Ribbon.RemoveFromQuickAccessToolBar(gallery);
            await settle();
            Check(!clone.IsDropDownOpen && !gallery.IsFrozen, "Removing an open QAT gallery retained its owner or subscriptions.");
        }
        finally
        {
            gallery.IsDropDownOpen = false;
            fixture.Dispose();
            await settle();
        }
    }

    private static async Task VerifyPresentationLeases(Panel host, Func<Task> settle)
    {
        App.LogAutoTestStartup("QAT LEASE constructing fixture");
        var fixture = new Fixture(host);
        var source = fixture.Ribbon.Tabs[0].Groups[0];
        var authored = new Fluent.Button { Header = "Leased authored item" };
        source.Items.Add(authored);
        fixture.Ribbon.AddToQuickAccessToolBar(source);
        try
        {
            await settle();
            var clone = Copy<RibbonGroupBox>(fixture.Ribbon, source);
            fixture.Ribbon.SelectedTab = fixture.Ribbon.Tabs[1];
            await settle();
            var binding = Field(source, "itemsBinding")
                          ?? throw new InvalidOperationException("The real group's container binding is missing.");
            App.LogAutoTestStartup("QAT LEASE source unloaded");
            AssertLeaseState(binding, count: 0, suspended: true);
            await OpenAsync(clone, settle);
            AssertRealContent(clone, authored);
            AssertLeaseState(binding, count: 1, suspended: false);
            App.LogAutoTestStartup("QAT LEASE content acquired");
            var parent = VisualTreeHelper.GetParent(authored);
            var additionalLease = RequireType<IDisposable>(
                binding.GetType().GetMethod("AcquirePresentationLease", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(binding, null));
            try
            {
                AssertLeaseState(binding, count: 2, suspended: false);
                Check(ReferenceEquals(parent, VisualTreeHelper.GetParent(authored)),
                    "Acquiring another observation lease reparented the owner's UI.");
            }
            finally
            {
                additionalLease.Dispose();
                additionalLease.Dispose();
            }
            AssertLeaseState(binding, count: 1, suspended: false);
#if !WINDOWS
            var watcher = Field(binding, "nativeItemsObservation");
            Check(watcher is not null, "The live authored-items QAT view did not start its native indexer watcher.");
            var timer = watcher is null ? null : Field(watcher, "timer");
#endif
            clone.IsDropDownOpen = false;
            await settle();
            AssertLeaseState(binding, count: 0, suspended: true);
#if !WINDOWS
            Check(Field(binding, "nativeItemsObservation") is null
                  && timer?.GetType().GetProperty("IsEnabled")?.GetValue(timer) is false,
                "The final lease retained a polling timer after the source and popup unloaded.");
#endif

            clone.IsDropDownOpen = true;
            clone.IsDropDownOpen = false;
            await settle();
            AssertLeaseState(binding, count: 0, suspended: true);
            Check(!clone.IsDropDownOpen && !source.IsSnapped,
                "Cancelling a pending QAT presentation retained ownership or a lease.");
            App.LogAutoTestStartup("QAT LEASE cancellation verified");

            var models = new LeaseSource();
            models.Add(new CloneModel("Initial leased model", new CloneCommand()));
            source.Items.Clear();
            source.ItemTemplate = ButtonTemplate();
            source.ItemsSource = models;
            await settle();
            var inactiveSubscriptions = models.Subscribers;
            App.LogAutoTestStartup("QAT LEASE preparing expected source failure");
            models.FailEnumeration = true;
            try
            {
                // Exercise the lease transaction directly: deliberately throwing through a
                // native WinUI DP callback can leave a deferred framework failure after catch.
                using var failedLease = RequireType<IDisposable>(
                    binding.GetType().GetMethod("AcquirePresentationLease", BindingFlags.Instance | BindingFlags.NonPublic)
                        ?.Invoke(binding, null));
                throw new InvalidOperationException("The expected presentation-lease acquisition failure was swallowed.");
            }
            catch (TargetInvocationException exception) when (exception.InnerException?.Message.Contains(
                                                                 LeaseSource.Failure, StringComparison.Ordinal) == true)
            {
            }
            finally
            {
                models.FailEnumeration = false;
            }
            AssertLeaseState(binding, count: 0, suspended: true);
            Check(models.Subscribers == inactiveSubscriptions && !clone.IsDropDownOpen && !source.IsSnapped,
                "Failed QAT preparation retained a source callback or effective ownership state.");
            App.LogAutoTestStartup("QAT LEASE failure rollback verified");

            await OpenAsync(clone, settle);
            AssertLeaseState(binding, count: 1, suspended: false);
            var added = new CloneModel("Added to the leased view", new CloneCommand());
            models.Add(added);
            await settle();
            var generated = Find<Fluent.Button>(PopupRoot(clone), item => Equals(item.Header, added.Label));
            Check(!source.IsLoaded && clone.IsLoaded && generated is { IsLoaded: true },
                "Source-unloaded/clone-loaded collection observation did not remain live.");
            fixture.Ribbon.RemoveFromQuickAccessToolBar(source);
            await settle();
            AssertLeaseState(binding, count: 0, suspended: true);
            Check(models.Subscribers == inactiveSubscriptions && Field(binding, "sourceSubscription") is null,
                "Removing the final QAT presentation retained its source collection callback.");
            App.LogAutoTestStartup("QAT LEASE final callback cleanup verified");
        }
        finally
        {
            fixture.Dispose();
            await settle();
        }
    }

    private static void AssertLeaseState(object binding, int count, bool suspended)
    {
        Check(Field(binding, "presentationLeases") is int actual && actual == count
              && Field(binding, "isSuspended") is bool current && current == suspended,
            $"Expected {count} presentation lease(s), suspended={suspended}.");
        if (count == 0 && suspended)
        {
            Check(Field(binding, "sourceSubscription") is null && Field(binding, "nativeItemsObservation") is null,
                "An inactive presentation retained a helper-owned collection callback or polling watcher.");
        }
    }

    private static object? Field(object owner, string name)
    {
        for (var type = owner.GetType(); type is not null; type = type.BaseType)
        {
            if (type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is { } field)
            {
                return field.GetValue(owner);
            }
        }
        return null;
    }

    private static T Copy<T>(Ribbon ribbon, UIElement source) where T : class =>
        RequireType<T>(ribbon.GetQuickAccessElements()[source]);

    private static T RequireType<T>(object? value) where T : class =>
        value as T ?? throw new InvalidOperationException($"Expected {typeof(T).Name}, got {value?.GetType().Name ?? "null"}.");

    private static async Task OpenAsync(IDropDownControl control, Func<Task> settle, Action? open = null)
    {
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Popup? observedPopup = null;
        EventHandler<object> onPopupOpened = (_, _) => opened.TrySetResult();
        EventHandler onOpened = (_, _) =>
        {
            if (control is RibbonDropDownButton)
            {
                opened.TrySetResult();
            }
            else
            {
                ObservePopup();
            }
        };
        control.DropDownOpened += onOpened;
        try
        {
            ObservePopup();
            if (open is null)
            {
                control.IsDropDownOpen = true;
            }
            else
            {
                open();
            }
            await opened.Task.WaitAsync(TimeSpan.FromSeconds(4));
            await settle();
        }
        catch (TimeoutException exception)
        {
            var session = Field(control, "_quickAccessContentSession");
            var lease = session is null ? null : Field(session, "lease");
            throw new InvalidOperationException(
                $"The QAT popup never completed opening. Owner={Describe(control as DependencyObject)}, "
                + $"root={Describe(PopupRoot(control))}, anchor={Describe(Field(control, "_button") as DependencyObject)}, "
                + $"open={Field(control, "_flyoutIsOpen")}, closing={Field(control, "_flyoutIsClosing")}, "
                + $"reopen={Field(control, "_reopenFlyoutAfterClosed")}, leaseMoving={(lease is null ? null : Field(lease, "moving"))}, "
                + $"leasePending={(lease is null ? null : (Field(lease, "pending") as System.Collections.IDictionary)?.Count)}.",
                exception);
        }
        finally
        {
            control.DropDownOpened -= onOpened;
            if (observedPopup is not null)
            {
                observedPopup.Opened -= onPopupOpened;
            }
        }

        void ObservePopup()
        {
            if (control.DropDownPopup is not { } popup)
            {
                return;
            }

            if (!ReferenceEquals(observedPopup, popup))
            {
                if (observedPopup is not null)
                {
                    observedPopup.Opened -= onPopupOpened;
                }
                observedPopup = popup;
                popup.Opened += onPopupOpened;
            }
            if (popup.IsOpen)
            {
                opened.TrySetResult();
            }
        }
    }

    private static void Invoke(FrameworkElement element)
    {
        if (element is RibbonSplitButton split)
        {
            new RibbonSplitButtonAutomationPeer(split).Invoke();
            return;
        }

        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(element);
        if (peer?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider provider)
        {
            throw new InvalidOperationException($"The real {element.GetType().Name} has no Invoke provider.");
        }

        provider.Invoke();
    }

    private static DependencyObject? PopupRoot(IDropDownControl control)
    {
        if (control is RibbonDropDownButton dropdown)
        {
            var accessor = typeof(RibbonDropDownButton).GetProperty(
                "OpenFlyoutContentRoot", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("The active dropdown presentation accessor is missing.");
            return accessor.GetValue(dropdown) as DependencyObject;
        }

        return control.DropDownPopup?.Child;
    }

    private static void AssertRealContent(IDropDownControl control, UIElement item)
    {
        var root = PopupRoot(control);
        Check(control.IsDropDownOpen && root is FrameworkElement { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 }
              && item is FrameworkElement { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 }
              && IsDescendant(item, root),
            $"The QAT {control.GetType().Name} did not realize its actual {item.GetType().Name} content. "
            + $"Open={control.IsDropDownOpen}, root={Describe(root)}, item={Describe(item)}");
    }

    private static string Describe(DependencyObject? value) =>
        value is FrameworkElement element
            ? $"{element.GetType().Name},loaded={element.IsLoaded},{element.ActualWidth}x{element.ActualHeight}"
            : value?.GetType().Name ?? "null";

    private static bool IsDescendant(DependencyObject item, DependencyObject? parent)
    {
        if (parent is null)
        {
            return false;
        }

        for (DependencyObject? current = item; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, parent))
            {
                return true;
            }
        }

        return false;
    }

    private static T? Find<T>(DependencyObject? root, Func<T, bool> match) where T : FrameworkElement
    {
        if (root is T element && match(element))
        {
            return element;
        }

        if (root is not null)
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                if (Find(VisualTreeHelper.GetChild(root, index), match) is { } found)
                {
                    return found;
                }
            }
        }

        return default;
    }

    private static DataTemplate MenuTemplate() => (DataTemplate)XamlReader.Load(
        "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:f='using:Fluent'>" +
        "<f:MenuItem Header='{Binding Label}' Command='{Binding Command}' IsDefinitive='False'/></DataTemplate>");

    private static DataTemplate ButtonTemplate() => (DataTemplate)XamlReader.Load(
        "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:f='using:Fluent'>" +
        "<f:Button Header='{Binding Label}' Command='{Binding Command}' IsDefinitive='False'/></DataTemplate>");

    private static DataTemplate TextTemplate() => (DataTemplate)XamlReader.Load(
        "<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
        "<TextBlock Text='{Binding Label}'/></DataTemplate>");

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class Fixture : IDisposable
    {
        private readonly Panel host;
        private readonly Grid root = new();

        internal Fixture(Panel host)
        {
            this.host = host;
            Ribbon = new Ribbon { ShowQuickAccessToolBarAboveRibbon = false };
            var tab = new RibbonTabItem { Header = "Source" };
            tab.Groups.Add(new RibbonGroupBox { Header = "Source controls" });
            Ribbon.Tabs.Add(tab);
            Ribbon.Tabs.Add(new RibbonTabItem { Header = "Inactive source" });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.Children.Add(Ribbon);
            Grid.SetRow(Sources, 1);
            root.Children.Add(Sources);
            host.Children.Add(root);
        }

        internal Ribbon Ribbon { get; }
        internal StackPanel Sources { get; } = new();

        public void Dispose()
        {
            Ribbon.ClearQuickAccessToolBar();
            host.Children.Remove(root);
        }
    }

    public sealed class CloneModel(string label, ICommand command) : INotifyPropertyChanged
    {
        private string label = label;
        public string Label
        {
            get => label;
            set
            {
                label = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Label)));
            }
        }
        public ICommand Command { get; } = command;
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class LeaseSource : ObservableCollection<CloneModel>, IEnumerable<CloneModel>, IEnumerable
    {
        internal const string Failure = "Expected presentation lease enumeration failure";
        private NotifyCollectionChangedEventHandler? changed;
        internal bool FailEnumeration { get; set; }
        internal int Subscribers => changed?.GetInvocationList().Length ?? 0;
        public override event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add
            {
                changed += value;
                base.CollectionChanged += value;
            }
            remove
            {
                changed -= value;
                base.CollectionChanged -= value;
            }
        }
        IEnumerator<CloneModel> IEnumerable<CloneModel>.GetEnumerator()
        {
            if (FailEnumeration)
            {
                throw new InvalidOperationException(Failure);
            }
            return base.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<CloneModel>)this).GetEnumerator();
    }

    private sealed class CloneCommand : ICommand
    {
        public bool Allowed { get; set; } = true;
        public int Executions { get; private set; }
        public object? LastParameter { get; private set; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Allowed;
        public void Execute(object? parameter)
        {
            Check(Allowed, "A disabled clone command was executed.");
            Executions++;
            LastParameter = parameter;
        }
        public void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
