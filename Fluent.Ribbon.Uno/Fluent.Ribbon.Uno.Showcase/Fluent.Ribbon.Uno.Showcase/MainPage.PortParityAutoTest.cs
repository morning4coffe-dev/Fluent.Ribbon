namespace FluentRibbon.Uno.Showcase;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Fluent;
using FluentRibbon.Uno.Showcase.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

public sealed partial class MainPage
{
    private async Task RunPortParityAutoTestAsync(string requestedPhase)
    {
        autoTestFailed = false;
        AutoLog($"PORT-PARITY BEGIN throughPhase={requestedPhase}");
        try
        {
            Require(
                int.TryParse(requestedPhase, out var throughPhase)
                && throughPhase is >= 1 and <= 4,
                $"Invalid portable parity phase '{requestedPhase}'.");

            var cases = ShowcaseDiagnosticOptions.OrderPortParityCases(
                    GetPortParityCases().Where(test => test.Phase <= throughPhase),
                    test => test.Id,
                    Environment.GetEnvironmentVariable("SHOWCASE_NATIVE_POPUP_EXTERNAL_INPUT") == "1")
                .ToArray();
            var expectedCounts = new[] { 0, 6, 12, 15, 17 };
            Require(
                cases.Length == expectedCounts[throughPhase],
                $"Phase {throughPhase} must cover {expectedCounts[throughPhase]} gap areas, "
                + $"but only {cases.Length} cases are registered.");
            Require(
                cases.Select(test => test.Id).Distinct(StringComparer.Ordinal).Count() == cases.Length,
                "Portable parity case identifiers must be unique.");
            var focusedPhase = ShowcaseDiagnosticOptions.GetFocusedPortParityPhase(
                GetDiagnosticOption("SHOWCASE_PORT_PARITY_ONLY_PHASE", "port-parity-only-phase"), throughPhase);
            if (focusedPhase is { } selectedPhase)
            {
                cases = cases.Where(test => test.Phase == selectedPhase).ToArray();
                Require(cases.Length == expectedCounts[selectedPhase] - expectedCounts[selectedPhase - 1],
                    "The focused parity run did not retain every case in its selected phase.");
                AutoLog($"PORT-PARITY FOCUSED BEGIN phase={selectedPhase} cases={cases.Length}");
            }
            Require(Content is Grid, "The Showcase must have a Grid test host.");

            var pageRoot = (Grid)Content;
            var originalRibbonVisibility = MainRibbon.Visibility;
            var fixture = new PortParityFixture();
            Grid.SetRowSpan(fixture, 3);
            Canvas.SetZIndex(fixture, 100);
            MainRibbon.Visibility = Visibility.Collapsed;
            pageRoot.Children.Add(fixture);
            try
            {
                await SettlePortParityAsync(2, 50);
                foreach (var test in cases)
                {
                    try
                    {
                        await test.Run(fixture);
                        AutoLog($"PORT-PARITY PASS phase={test.Phase} id={test.Id}");
                    }
                    catch (Exception exception)
                    {
                        AutoLog($"PORT-PARITY FAIL phase={test.Phase} id={test.Id}: {exception}");
                    }
                    finally
                    {
                        fixture.Host.Children.Clear();
                        await SettlePortParityAsync(1, 25);
                    }
                }
            }
            finally
            {
                pageRoot.Children.Remove(fixture);
                MainRibbon.Visibility = originalRibbonVisibility;
            }

            AutoLog(focusedPhase is { } completedPhase
                ? $"PORT-PARITY FOCUSED COMPLETE phase={completedPhase} cases={cases.Length}"
                : $"PORT-PARITY COMPLETE throughPhase={throughPhase} cases={cases.Length}");
        }
        catch (Exception exception)
        {
            AutoLog($"PORT-PARITY FATAL: {exception}");
        }

        await FinishAutoTestAsync();
    }

    private async Task SettlePortParityAsync(int cycles = 2, int delayMs = 50)
    {
        for (var cycle = 0; cycle < cycles; cycle++)
        {
            UpdateLayout();
            await Task.Delay(delayMs);
        }
    }

    private IReadOnlyList<PortParityCase> GetPortParityCases() =>
    [
        new(1, "qat-overloads", VerifyPortQatEntryPointsAsync),
        new(1, "qat-tracking", VerifyPortQatBookkeepingAsync),
        new(1, "itemscontrol-inheritance", VerifyPortItemsSourceAsync),
        new(1, "gallery-selection", VerifyPortGallerySelectionAsync),
        new(1, "tab-collection", VerifyPortTabMutationsAsync),
        new(1, "contextual-group", VerifyPortContextualBindingAsync),
        new(2, "gallery-enter", fixture => PortInputContractTests.VerifyGalleryEnterAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(2, "command-enablement", fixture => PortInputContractTests.VerifyCommandAvailabilityAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(2, "wpf-keytip-scopes", fixture => PortNavigationContractTests.VerifyKeyTipScopesAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(2, "minimized-content", fixture => PortNavigationContractTests.VerifyMinimizedContentAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(2, "startscreen-host", fixture => PortNavigationContractTests.VerifyStartScreenHostingAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(2, "spinner-converter", fixture => PortInputContractTests.VerifySpinnerConversionAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(3, "qat-clones", fixture => PortQuickAccessCloneTests.VerifyClonesAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(3, "inert-popup-properties", fixture => PortPopupContractTests.VerifyPopupOptionsAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(3, "menu-presentation", fixture => PortPopupContractTests.VerifyMenuPresentationAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(4, "ribbon-inert-options", fixture => PortRibbonOptionsContractTests.VerifyAsync(
            fixture.Host, () => SettlePortParityAsync())),
        new(4, "editor-header-templates", fixture => PortEditorHeaderContractTests.VerifyAsync(
            fixture.Host, () => SettlePortParityAsync())),
    ];

    private async Task<Ribbon> CreatePortRibbonAsync(
        PortParityFixture fixture,
        params RibbonTabItem[] tabs)
    {
        var ribbon = new Ribbon
        {
            Width = 800,
            AutomaticStateManagement = false,
            IsAutomaticCollapseEnabled = false,
            IsKeyTipHandlingEnabled = false,
        };
        if (tabs.Length == 0)
        {
            var tab = new RibbonTabItem { Header = "Parity" };
            tab.Groups.Add(new RibbonGroupBox { Header = "Actions" });
            tabs = [tab];
        }

        foreach (var tab in tabs)
        {
            ribbon.Tabs.Add(tab);
        }

        fixture.Host.Children.Add(ribbon);
        await SettlePortParityAsync(2, 50);
        Require(ribbon.TabControl is not null, "The ribbon did not realize its tab control.");
        return ribbon;
    }

    private async Task VerifyPortQatEntryPointsAsync(PortParityFixture fixture)
    {
        await PortRibbonContractTests.VerifyQuickAccessOverloadsAsync(
            fixture.Host,
            () => SettlePortParityAsync(2, 50));
        var ribbon = await CreatePortRibbonAsync(fixture);
        var button = new Fluent.Button { Header = "QAT entry point" };
        ribbon.Tabs[0].Groups[0].Items.Add(button);
        UIElement element = button;
        IQuickAccessItemProvider provider = button;

        ribbon.AddToQuickAccessToolBar(button);
        Require(ribbon.IsInQuickAccessToolBar(button), "The uncast Button QAT API failed.");
        Require(ribbon.IsInQuickAccessToolBar(element), "The UIElement QAT query disagrees.");
        Require(ribbon.IsInQuickAccessToolBar(provider), "The provider QAT query disagrees.");
        ribbon.AddToQuickAccessToolBar(element);
        ribbon.AddToQuickAccessToolBar(provider);
        Require(ribbon.QuickAccessToolBarItems.Count == 1, "QAT entry points created duplicate clones.");

        ribbon.RemoveFromQuickAccessToolBar(button);
        Require(!ribbon.IsInQuickAccessToolBar(element), "Button removal did not clear UIElement state.");
        Require(!ribbon.IsInQuickAccessToolBar(provider), "Button removal did not clear provider state.");
        ribbon.AddToQuickAccessToolBar(element);
        ribbon.RemoveFromQuickAccessToolBar(provider);
        Require(ribbon.QuickAccessToolBarItems.Count == 0, "Cross-overload removal left a QAT clone.");

        var standaloneProvider = new PortParityQuickAccessProvider();
        ribbon.AddToQuickAccessToolBar(standaloneProvider);
        ribbon.AddToQuickAccessToolBar(standaloneProvider);
        Require(standaloneProvider.CreationCount == 1, "A non-UI provider was cloned more than once.");
        Require(ribbon.IsInQuickAccessToolBar(standaloneProvider), "A non-UI provider was not tracked.");
        ribbon.RemoveFromQuickAccessToolBar(standaloneProvider);
        Require(ribbon.QuickAccessToolBarItems.Count == 0, "A non-UI provider could not be removed.");
    }

    private async Task VerifyPortQatBookkeepingAsync(PortParityFixture fixture)
    {
        await PortRibbonContractTests.VerifyQuickAccessBookkeepingAsync(
            fixture.Host,
            () => SettlePortParityAsync(2, 50));
        var ribbon = await CreatePortRibbonAsync(fixture);
        var pinned = new RibbonButton { Header = "Pinned", CanAddToQuickAccessToolBar = false };
        var first = new Fluent.Button { Header = "First command" };
        var second = new Fluent.Button { Header = "Second command" };
        ribbon.QuickAccessToolBarItems.Add(pinned);
        ribbon.Tabs[0].Groups[0].Items.Add(first);
        ribbon.Tabs[0].Groups[0].Items.Add(second);
        ribbon.AddToQuickAccessToolBar((IQuickAccessItemProvider)first);
        ribbon.AddToQuickAccessToolBar((UIElement)second);

        var mapping = ribbon.GetQuickAccessElements();
        Require(mapping.Count == 2, "The public QAT map omitted a provider or UIElement addition.");
        Require(mapping.ContainsKey(first) && mapping.ContainsKey(second), "The QAT map lost source identity.");
        Require(
            (bool)first.GetValue(RibbonProperties.IsElementInQuickAccessToolBarProperty)
            && (bool)second.GetValue(RibbonProperties.IsElementInQuickAccessToolBarProperty),
            "QAT membership dependency properties disagree with the public map.");

        ribbon.ClearQuickAccessToolBar();
        Require(ribbon.GetQuickAccessElements().Count == 0, "Clear left tracked QAT source elements.");
        Require(
            ribbon.QuickAccessToolBarItems.Count == 1
            && ReferenceEquals(ribbon.QuickAccessToolBarItems[0], pinned),
            "Clear must remove provider-created clones without removing directly authored QAT items.");
        Require(
            !(bool)first.GetValue(RibbonProperties.IsElementInQuickAccessToolBarProperty)
            && !(bool)second.GetValue(RibbonProperties.IsElementInQuickAccessToolBarProperty),
            "Clear left stale QAT membership properties.");
        ribbon.AddToQuickAccessToolBar(first);
        Require(ribbon.QuickAccessToolBarItems.Count == 2, "A cleared source could not be re-added.");
        ribbon.ClearQuickAccessToolBar();
    }

    private async Task VerifyPortItemsSourceAsync(PortParityFixture fixture)
    {
        await PortDataContractTests.RunAsync(fixture.Host, () => SettlePortParityAsync(2, 50));
        var items = new ObservableCollection<PortParityItem>
        {
            new("Alpha"),
            new("Beta"),
        };
        var menu = new Fluent.MenuItem
        {
            Header = "Data menu",
            ItemsSource = items,
            ItemTemplate = fixture.ItemTemplate,
        };
        var status = new Fluent.StatusBar
        {
            ItemsSource = items,
            ItemTemplate = fixture.ItemTemplate,
        };
        var group = new RibbonGroupBox
        {
            Header = "Data group",
            ItemsSource = items,
            ItemTemplate = fixture.ButtonTemplate,
        };
        var tab = new RibbonTabItem { Header = "Data" };
        tab.Groups.Add(group);
        await CreatePortRibbonAsync(fixture, tab);
        fixture.Host.Children.Add(menu);
        fixture.Host.Children.Add(status);
        try
        {
            menu.IsDropDownOpen = true;
            await SettlePortParityAsync(2, 50);
            AssertPresentation();

            items.Insert(1, new PortParityItem("Inserted"));
            await SettlePortParityAsync(2, 50);
            AssertPresentation();
            items.Move(0, 2);
            await SettlePortParityAsync(2, 50);
            AssertPresentation();
            items[0] = new PortParityItem("Replacement");
            await SettlePortParityAsync(2, 50);
            AssertPresentation();
            items.RemoveAt(1);
            await SettlePortParityAsync(2, 50);
            AssertPresentation();
            items.Clear();
            await SettlePortParityAsync(2, 50);
            Require(menu.Items.Count == 0 && status.Items.Count == 0, "Reset left generated menu/status items.");
            Require(GetGroupHeaders().Length == 0, "Reset left generated ribbon-group controls.");
        }
        finally
        {
            menu.IsDropDownOpen = false;
            await SettlePortParityAsync(1, 25);
        }

        void AssertPresentation()
        {
            var titles = items.Select(item => item.Title).ToArray();
            Require(menu.Items.Count == items.Count, "MenuItem.ItemsSource did not synchronize.");
            Require(status.Items.Count == items.Count, "StatusBar.ItemsSource did not synchronize.");
            Require(
                GetGroupHeaders().SequenceEqual(titles),
                "RibbonGroupBox did not render its current ItemsSource in order through ItemTemplate.");
            Require(
                GetPortTemplateLabels(status).SequenceEqual(titles),
                "StatusBar did not render its current ItemsSource through ItemTemplate.");
            Require(menu.DropDownPopup?.Child is not null, "The bound menu did not create its submenu.");
            Require(
                GetPortTemplateLabels(menu.DropDownPopup!.Child).SequenceEqual(titles),
                "MenuItem did not render its current ItemsSource through ItemTemplate. "
                + $"Expected={string.Join("|", titles)} Actual={string.Join("|", GetPortTemplateLabels(menu.DropDownPopup.Child))}");
        }

        string[] GetGroupHeaders() =>
            EnumeratePortVisuals<RibbonButton>(group)
                .Where(button => Equals(button.Tag, "PortParityButtonTemplate"))
                .Select(button => button.Header?.ToString() ?? string.Empty)
                .ToArray();
    }

    private async Task VerifyPortGallerySelectionAsync(PortParityFixture fixture)
    {
        var first = new PortParityItem("First");
        var second = new PortParityItem("Second");
        var model = new PortParitySelectionModel();
        model.Items.Add(first);
        model.Items.Add(second);
        var gallery = new Fluent.Gallery
        {
            Width = 400,
            Height = 160,
            ItemTemplate = fixture.ItemTemplate,
        };
        gallery.SetBinding(
            RibbonGallery.ItemsSourceProperty,
            new Binding { Source = model, Path = new PropertyPath(nameof(model.Items)) });
        gallery.SetBinding(
            RibbonGallery.SelectedItemProperty,
            new Binding
            {
                Source = model,
                Path = new PropertyPath(nameof(model.Selected)),
                Mode = BindingMode.TwoWay,
            });
        SelectionChangedEventArgs? selection = null;
        gallery.SelectionChanged += (_, args) => selection = args;
        fixture.Host.Children.Add(gallery);
        await SettlePortParityAsync(2, 50);

        model.Selected = first;
        await SettlePortParityAsync(1, 25);
        Require(ReferenceEquals(gallery.SelectedItem, first), "Gallery rejected a bound source model.");
        Require(gallery.SelectedIndex == 0, "Model selection did not update SelectedIndex.");
        Require(
            selection?.AddedItems.Count == 1 && ReferenceEquals(selection.AddedItems[0], first),
            "SelectionChanged must expose source models rather than generated UI containers.");
        gallery.SelectedIndex = 1;
        Require(ReferenceEquals(model.Selected, second), "Gallery selection did not update the two-way model.");
        Require(
            selection?.RemovedItems.Count == 1 && ReferenceEquals(selection.RemovedItems[0], first)
            && selection.AddedItems.Count == 1 && ReferenceEquals(selection.AddedItems[0], second),
            "SelectionChanged supplied incorrect removed/added models.");

        model.Items.Move(1, 0);
        await SettlePortParityAsync(1, 25);
        Require(ReferenceEquals(gallery.SelectedItem, second) && gallery.SelectedIndex == 0,
            "Moving data lost selection identity or its index.");
        model.Items.Insert(0, new PortParityItem("Before selection"));
        await SettlePortParityAsync(1, 25);
        Require(ReferenceEquals(gallery.SelectedItem, second) && gallery.SelectedIndex == 1,
            "Inserting data did not preserve selection identity.");
        model.Items.Remove(second);
        await SettlePortParityAsync(1, 25);
        Require(gallery.SelectedItem is null && model.Selected is null && gallery.SelectedIndex == -1,
            "Removing selected data left a stale model selection.");
        model.Items.Clear();
        await SettlePortParityAsync(1, 25);
        Require(gallery.Items.Count == 0 && gallery.SelectedItem is null,
            "Reset left gallery containers or selection behind.");

        gallery.ClearValue(RibbonGallery.ItemsSourceProperty);
        gallery.ClearValue(RibbonGallery.SelectedItemProperty);
        var authoredItem = new Fluent.GalleryItem { Content = "Authored item" };
        gallery.Items.Add(authoredItem);
        gallery.SelectedItem = authoredItem;
        Require(ReferenceEquals(gallery.SelectedItem, authoredItem) && authoredItem.IsSelected,
            "Data binding repairs broke directly authored GalleryItem selection.");
#if WINDOWS
        await PortNativeGalleryContractTests.VerifyAsync(
            fixture.Host, () => SettlePortParityAsync(2, 50));
#endif
    }

    private async Task VerifyPortTabMutationsAsync(PortParityFixture fixture)
    {
        await PortRibbonContractTests.VerifyTabCollectionAsync(
            fixture.Host,
            () => SettlePortParityAsync(2, 50));
        var first = new RibbonTabItem { Header = "A" };
        var second = new RibbonTabItem { Header = "B" };
        var ribbon = await CreatePortRibbonAsync(fixture, first, second);
        ribbon.SelectedTab = second;
        ribbon.Tabs.Insert(0, new RibbonTabItem { Header = "Inserted" });
        await SettlePortParityAsync(2, 50);
        AssertOrder();
        Require(ReferenceEquals(ribbon.SelectedTab, second) && ribbon.SelectedTabIndex == 2,
            "Inserting a tab changed selected identity or left its index stale.");
        ribbon.Tabs.Move(2, 0);
        await SettlePortParityAsync(2, 50);
        AssertOrder();
        Require(ReferenceEquals(ribbon.SelectedTab, second) && ribbon.SelectedTabIndex == 0,
            "Moving a tab changed selected identity or left its index stale.");
        ribbon.Tabs[1] = new RibbonTabItem { Header = "Replacement" };
        await SettlePortParityAsync(2, 50);
        AssertOrder();
        ribbon.Tabs.Remove(second);
        await SettlePortParityAsync(2, 50);
        AssertOrder();
        Require(ribbon.SelectedTab is not null && ribbon.Tabs.Contains(ribbon.SelectedTab),
            "Removing the selected tab left stale selection.");
        ribbon.Tabs.Clear();
        await SettlePortParityAsync(2, 50);
        AssertOrder();
        Require(ribbon.SelectedTab is null && ribbon.SelectedTabIndex == -1,
            "Clearing tabs must clear both selection properties.");

        void AssertOrder() =>
            Require(
                ribbon.TabControl!.TabItems.Cast<object>().SequenceEqual(ribbon.Tabs.Cast<object>()),
                "The realized tab order differs from Ribbon.Tabs.");
    }

    private async Task VerifyPortContextualBindingAsync(PortParityFixture fixture)
    {
        await PortRibbonContractTests.VerifyContextualGroupsAsync(
            fixture.Host,
            () => SettlePortParityAsync(2, 50));
        var firstGroup = new RibbonContextualTabGroup { Header = "First tools" };
        var secondGroup = new RibbonContextualTabGroup { Header = "Second tools" };
        var namedGroup = new RibbonContextualTabGroup { Header = "Named tools" };
        var model = new PortParityGroupModel { Group = firstGroup };
        var explicitTab = new RibbonTabItem { Header = "Bound tools" };
        explicitTab.SetBinding(
            RibbonTabItem.GroupProperty,
            new Binding { Source = model, Path = new PropertyPath(nameof(model.Group)) });
        var namedTab = new RibbonTabItem
        {
            Header = "Named tools tab",
            ContextualTabGroupName = "Named tools",
        };
        var ribbon = await CreatePortRibbonAsync(fixture, new RibbonTabItem { Header = "Home" });
        ribbon.ContextualGroups.Add(firstGroup);
        ribbon.ContextualGroups.Add(secondGroup);
        ribbon.ContextualGroups.Add(namedGroup);
        ribbon.Tabs.Add(explicitTab);
        ribbon.Tabs.Add(namedTab);
        await SettlePortParityAsync(2, 50);
        Require(ReferenceEquals(explicitTab.Group, firstGroup),
            "Ribbon synchronization overwrote an explicitly bound contextual group.");
        Require(ReferenceEquals(namedTab.Group, namedGroup), "Uno name-based group linking regressed.");
        Require(firstGroup.Items.Contains(explicitTab), "The explicit group did not register its tab.");

        model.Group = secondGroup;
        await SettlePortParityAsync(2, 50);
        Require(ReferenceEquals(explicitTab.Group, secondGroup),
            "The contextual Group binding was replaced by a local value.");
        Require(!firstGroup.Items.Contains(explicitTab) && secondGroup.Items.Contains(explicitTab),
            "Changing the binding left stale contextual-group membership.");
        ribbon.ContextualGroups.Remove(namedGroup);
        await SettlePortParityAsync(2, 50);
        Require(namedTab.Group is null, "Removing a name-resolved group left stale linkage.");
        Require(ReferenceEquals(explicitTab.Group, secondGroup),
            "Removing a different group disturbed the explicit Group binding.");
    }

    private static string[] GetPortTemplateLabels(DependencyObject root) =>
        EnumeratePortVisuals<TextBlock>(root)
            .Where(text => Equals(text.Tag, "PortParityItemTemplate"))
            .Select(text => text.Text)
            .ToArray();

    private static IEnumerable<T> EnumeratePortVisuals<T>(DependencyObject root)
        where T : DependencyObject
    {
        if (root is T match)
        {
            yield return match;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            foreach (var child in EnumeratePortVisuals<T>(VisualTreeHelper.GetChild(root, index)))
            {
                yield return child;
            }
        }
    }

    private sealed record PortParityCase(
        int Phase,
        string Id,
        Func<PortParityFixture, Task> Run);

    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed record PortParityItem(string Title);

    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed class PortParitySelectionModel : INotifyPropertyChanged
    {
        private object? selected;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<PortParityItem> Items { get; } = new();

        public object? Selected
        {
            get => selected;
            set
            {
                if (ReferenceEquals(selected, value))
                {
                    return;
                }

                selected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
            }
        }
    }

    [Microsoft.UI.Xaml.Data.Bindable]
    public sealed class PortParityGroupModel : INotifyPropertyChanged
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

    private sealed class PortParityQuickAccessProvider : IQuickAccessItemProvider
    {
        public bool CanAddToQuickAccessToolBar { get; set; } = true;

        public int CreationCount { get; private set; }

        public FrameworkElement CreateQuickAccessItem()
        {
            CreationCount++;
            return new Microsoft.UI.Xaml.Controls.Button { Content = "Standalone provider" };
        }
    }
}
