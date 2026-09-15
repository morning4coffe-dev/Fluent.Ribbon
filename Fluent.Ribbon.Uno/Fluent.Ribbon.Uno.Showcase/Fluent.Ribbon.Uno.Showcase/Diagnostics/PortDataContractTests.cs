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
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

/// <summary>Native-runtime data regressions; the caller supplies a rooted host and its UI settling routine.</summary>
internal static partial class PortDataContractTests
{
    internal static async Task RunAsync(Panel host, Func<Task> settle)
    {
        VerifyDetachedSources();
        VerifyRepeatedSourceItems();
        VerifyRepeatedNativeItems();
        VerifyReentrantSelection();
        await VerifyReentrantContainerSelection(host, settle);
        var first = new DataItem("First");
        var second = new DataItem("Second");
        var third = new DataItem("Third");
        var model = new DataModel();
        model.Items.ResetWith([first, second, third]);
        var fixture = new PortDataBindingFixture { DataContext = model };
        await VerifyIdleNativeReplacement(host, fixture.PrimaryTemplate, settle);
        await VerifyGroupTemplateCache(host, fixture, settle);
        host.Children.Add(fixture);
        try
        {
            await settle();
            fixture.Menu.IsDropDownOpen = true;
            await settle();
            AssertItems(fixture, model.Items);
            AssertTemplateLabels(fixture.Status, "PortDataPrimary", model.Items);
            AssertTemplateLabels(fixture.Gallery, "PortDataPrimary", model.Items);
            Require(fixture.Menu.DropDownPopup?.Child is not null, "Bound menu did not open.");
            AssertTemplateLabels(fixture.Menu.DropDownPopup!.Child, "PortDataPrimary", model.Items);
            Require(
                Descendants<RibbonButton>(fixture.Group).Where(button => Equals(button.Tag, "PortDataGroup"))
                    .Select(button => button.Header?.ToString()).SequenceEqual(model.Items.Select(item => item.Title)),
                "Group ItemTemplate did not realize its bound ribbon controls.");

            var changes = new List<SelectionChangedEventArgs>();
            fixture.Gallery.SelectionChanged += RecordSelection;
            void RecordSelection(object sender, SelectionChangedEventArgs args) => changes.Add(args);

            var selectedContainer = (RibbonGalleryItem)fixture.Gallery.ContainerFromItem(first)!;
            Require(selectedContainer is GalleryItem, "Gallery bypassed the facade's container factory.");
            model.Selected = first;
            await settle();
            AssertSelection(fixture.Gallery, first, model.Items.IndexOf(first));
            AssertChange(changes.Single(), null, first);

            fixture.InlineGallery.SelectedItem = first;
            Require(ReferenceEquals(fixture.InlineGallery.SelectedItem, first),
                "The existing InRibbonGallery source-model contract regressed.");

            var menuContainer = fixture.Menu.ContainerFromItem(first);
#if WINDOWS
            Require(ReferenceEquals(((ItemsControl)fixture.Menu).ContainerFromItem(first), menuContainer)
                    && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(menuContainer!), fixture.Menu),
                "The menu's canonical data container is not presented by its native source generator.");
#endif
            var statusContainer = fixture.Status.ContainerFromItem(first);
            var groupContainer = fixture.Group.ContainerFromItem(first);
            model.Items.Move(0, 2);
            model.Items.Insert(0, new DataItem("Inserted"));
            model.Items[1] = new DataItem("Replaced unselected");
            model.Items.InsertRange(1, [new DataItem("Range A"), new DataItem("Range B")]);
            model.Items.MoveRange(1, 2, 3);
            model.Items.RemoveRange(3, 2);
            await settle();
            AssertItems(fixture, model.Items);
            AssertSelection(fixture.Gallery, first, model.Items.IndexOf(first));
            Require(changes.Count == 1, "Unselected collection edits emitted spurious selection events.");

            model.Items.ResetWith([third, first, second]);
            await settle();
            AssertItems(fixture, model.Items);
            AssertSelection(fixture.Gallery, first, 1);
            Require(ReferenceEquals(fixture.Gallery.ContainerFromItem(first), selectedContainer),
                "Reset recreated a retained gallery container.");
            Require(ReferenceEquals(fixture.Menu.ContainerFromItem(first), menuContainer)
                    && ReferenceEquals(fixture.Status.ContainerFromItem(first), statusContainer)
                    && ReferenceEquals(fixture.Group.ContainerFromItem(first), groupContainer),
                "A source move/reset recreated retained menu, status, or group containers.");
            Require(changes.Count == 1, "Reset lost and reselected the same source model.");

            var replacement = new DataItem("Replacement");
            model.Items[1] = replacement;
            await settle();
            AssertSelection(fixture.Gallery, null, -1);
            Require(model.Selected is null && !selectedContainer.IsSelected,
                "Removing selected source data did not clear the two-way model/container state.");
            AssertChange(changes.Last(), first, null);
            Require(fixture.Gallery.ItemFromContainer(selectedContainer) is null,
                "A removed container retained its source mapping.");
            var removedPeer = new GalleryItemWrapperAutomationPeer(selectedContainer);
            Require(removedPeer.GetPattern(PatternInterface.SelectionItem) is null,
                "A removed gallery container retained its selection owner.");

            fixture.Gallery.SelectedIndex = 1;
            AssertSelection(fixture.Gallery, replacement, 1);
            Require(ReferenceEquals(model.Selected, replacement), "SelectedIndex did not update the two-way model.");
            AssertChange(changes.Last(), null, replacement);
            var changeCount = changes.Count;
            fixture.Gallery.SelectedItem = new DataItem("Not in the source");
            AssertSelection(fixture.Gallery, replacement, 1);
            Require(changes.Count == changeCount, "Invalid selection changed the committed selection.");

            var replacementContainer = (RibbonGalleryItem)fixture.Gallery.ContainerFromItem(replacement)!;
            var selectionPeer = new GalleryItemWrapperAutomationPeer(replacementContainer);
            Require(selectionPeer.IsSelected, "Automation compared the model to its container.");
            var galleryPeer = new RibbonGalleryAutomationPeer(fixture.Gallery);
            Require(((ISelectionProvider)galleryPeer).GetSelection().Length == 1,
                "Gallery automation omitted a selected data-model container.");
            selectionPeer.RemoveFromSelection();
            AssertSelection(fixture.Gallery, null, -1);
            selectionPeer.Select();
            AssertSelection(fixture.Gallery, replacement, 1);
            replacementContainer.IsSelected = false;
            AssertSelection(fixture.Gallery, null, -1);
            replacementContainer.IsSelected = true;
            AssertSelection(fixture.Gallery, replacement, 1);

            fixture.Gallery.Selectable = false;
            fixture.Gallery.SelectedIndex = 0;
            AssertSelection(fixture.Gallery, null, -1);
            fixture.Gallery.Selectable = true;

            await VerifyTemplatesAndStyles(fixture, model, settle);
            await VerifyItemsPanels(fixture, model, settle);
            VerifyStatusCustomization(fixture, model);

            fixture.Gallery.SelectedItem = third;
            var retained = fixture.Gallery.ContainerFromItem(third);
            fixture.Menu.IsDropDownOpen = false;
            host.Children.Remove(fixture);
            await settle();
            model.Items.ResetWith([replacement, third, new DataItem("While unloaded")]);
            host.Children.Add(fixture);
            await settle();
            AssertItems(fixture, model.Items);
            AssertSelection(fixture.Gallery, third, 1);
            Require(ReferenceEquals(fixture.Gallery.ContainerFromItem(third), retained),
                "Reload discarded the retained selected container.");

            var oldSource = model.Items;
            var newModel = new DataModel();
            newModel.Items.Add(new DataItem("New source"));
            fixture.DataContext = newModel;
            await settle();
            oldSource.Add(new DataItem("Stale source"));
            await settle();
            AssertItems(fixture, newModel.Items);
            Require(fixture.Gallery.Items.Count == 1, "The old source was still subscribed after replacement.");

            newModel.Items.Clear();
            await settle();
            AssertItems(fixture, newModel.Items);
            Require(!fixture.Menu.HasSubItems && !fixture.Menu.HasItems
                    && !fixture.Status.HasItems && !fixture.Group.HasItems && !fixture.Gallery.HasItems,
                "HasItems/HasSubItems did not follow reset.");

            fixture.Gallery.ClearValue(ItemsControl.ItemsSourceProperty);
            fixture.Gallery.ClearValue(RibbonGallery.SelectedItemProperty);
            var authored = new GalleryItem { Content = "Authored", DataContext = new DataItem("Not the item") };
            fixture.Gallery.Items.Add(authored);
            fixture.Gallery.SelectedIndex = 0;
            AssertSelection(fixture.Gallery, authored, 0);
            AssertChange(changes.Last(), null, authored);
            var authoredButton = new Microsoft.UI.Xaml.Controls.Button { Content = "Authored UIElement" };
            fixture.Gallery.Items.Add(authoredButton);
            fixture.Gallery.SelectedItem = authoredButton;
            AssertSelection(fixture.Gallery, authoredButton, 1);
            Require(!authored.IsSelected, "Selecting an authored UIElement broke single selection.");
            AssertChange(changes.Last(), authored, authoredButton);
        }
        finally
        {
            fixture.Menu.IsDropDownOpen = false;
            host.Children.Remove(fixture);
            await settle();
        }
    }

    private static void VerifyDetachedSources()
    {
        var first = new DataItem("Detached");
        var items = new ObservableCollection<DataItem> { first };
        var menu = new HookedMenuItem { ItemsSource = items };
        var status = new StatusBar { ItemsSource = items };
        var group = new RibbonGroupBox { ItemsSource = items };
        var gallery = new Gallery { ItemsSource = items };
        gallery.SelectedItem = first;
        Require(menu.Items.Count == 1 && status.Items.Count == 1 && group.Items.Count == 1 && gallery.Items.Count == 1,
            "ItemsSource required a first Loaded event before creating containers.");
        AssertSelection(gallery, first, 0);
        items.Add(new DataItem("Before first load"));
        Require(menu.Items.Count == 2 && status.Items.Count == 2 && group.Items.Count == 2 && gallery.Items.Count == 2,
            "Source changes before first load were ignored.");
        Require(menu.Created >= 2 && menu.Prepared.Contains(first)
                && menu.LastChange == NotifyCollectionChangedAction.Add
                && Equals(((FrameworkElement)menu.Items[0]).Tag, "Custom menu container"),
            "The item source bypassed overridden menu container/collection hooks.");

        var rejected = false;
        try
        {
            gallery.Items.Clear();
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }
        Require(rejected && gallery.Items.Count == 2 && items.Count == 2,
            "The generated Items view accepted a divergent direct edit.");

        menu.ItemsSource = null;
        status.ItemsSource = null;
        group.ItemsSource = null;
        gallery.ItemsSource = null;
        Require(menu.Cleared.Contains(first), "Removing source data bypassed ClearContainerForItemOverride.");

        var nativeItem = new DataItem("Native Items");
        ((ItemsControl)menu).Items.Add(nativeItem);
        Require(menu.Items.Count == 1 && ReferenceEquals(menu.ItemFromContainer(menu.Items[0]), nativeItem),
            "Inherited native Items edits were not presented.");
        ((ItemsControl)menu).Items.Clear();

        var authoredButton = new RibbonButton { Header = "Direct source visual" };
        group.ItemsSource = new[] { authoredButton };
        Require(ReferenceEquals(group.Items.Single(), authoredButton)
                && ReferenceEquals(((ItemsControl)group).Items.Single(), authoredButton),
            "RibbonGroupBox diverged from native Items or wrapped an authored ribbon control.");
        group.ItemsSource = null;
        gallery.ItemsSource = new[] { authoredButton };
        gallery.SelectedIndex = 0;
        Require(ReferenceEquals(gallery.Items.Single(), authoredButton)
                && ReferenceEquals(gallery.SelectedItem, authoredButton),
            "Gallery wrapped or changed the identity of an authored source UIElement.");
        gallery.ItemsSource = null;
    }

    private static async Task VerifyTemplatesAndStyles(
        PortDataBindingFixture fixture, DataModel model, Func<Task> settle)
    {
        var item = model.Items[0];
        var galleryContainer = (RibbonGalleryItem)fixture.Gallery.ContainerFromItem(item)!;
        fixture.Menu.ItemTemplate = fixture.AlternateTemplate;
        fixture.Status.ItemTemplate = fixture.AlternateTemplate;
        fixture.Gallery.ItemTemplate = fixture.AlternateTemplate;
        await settle();
        Require(ReferenceEquals(fixture.Gallery.ContainerFromItem(item), galleryContainer),
            "Changing ItemTemplate recreated a gallery container.");
        AssertTemplateLabels(fixture.Status, "PortDataAlternate", model.Items);
        AssertTemplateLabels(fixture.Gallery, "PortDataAlternate", model.Items);
        AssertTemplateLabels(fixture.Menu.DropDownPopup!.Child, "PortDataAlternate", model.Items);

        var selector = new ItemTemplateSelector(fixture.PrimaryTemplate);
        fixture.Menu.ItemTemplate = null;
        fixture.Status.ItemTemplate = null;
        fixture.Group.ItemTemplate = null;
        fixture.Gallery.ItemTemplate = null;
        fixture.Menu.ItemTemplateSelector = selector;
        fixture.Status.ItemTemplateSelector = selector;
        fixture.Group.ItemTemplateSelector = selector;
        fixture.Gallery.ItemTemplateSelector = selector;
        await settle();
        Require(selector.Calls > 0, "ItemTemplateSelector was not invoked with data/containers.");
        AssertTemplateLabels(fixture.Status, "PortDataPrimary", model.Items);
        AssertTemplateLabels(fixture.Group, "PortDataPrimary", model.Items);
        AssertTemplateLabels(fixture.Gallery, "PortDataPrimary", model.Items);

        var galleryStyle = new Style { TargetType = typeof(GalleryItem) };
        galleryStyle.Setters.Add(new Setter(FrameworkElement.TagProperty, "Selected container style"));
        var styleSelector = new ItemStyleSelector(galleryStyle);
        fixture.Gallery.ItemContainerStyleSelector = styleSelector;
        Require(ReferenceEquals(galleryContainer.Style, galleryStyle) && styleSelector.Calls > 0,
            "ItemContainerStyleSelector did not style the actual generated GalleryItem.");
        fixture.Gallery.ItemContainerStyleSelector = null;
        Require(galleryContainer.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue,
            "Clearing ItemContainerStyleSelector left an adapter-owned local style.");

        fixture.Menu.ItemTemplateSelector = null;
        fixture.Status.ItemTemplateSelector = null;
        fixture.Group.ItemTemplateSelector = null;
        fixture.Gallery.ItemTemplateSelector = null;
        fixture.Menu.DisplayMemberPath = nameof(DataItem.Title);
        fixture.Status.DisplayMemberPath = nameof(DataItem.Title);
        fixture.Group.DisplayMemberPath = nameof(DataItem.Title);
        fixture.Gallery.DisplayMemberPath = nameof(DataItem.Title);
        item.Title = "Updated bound title";
        await settle();
        Require(Equals(((Fluent.MenuItem)fixture.Menu.ContainerFromItem(item)!).Header, item.Title)
                && Equals(((StatusBarItem)fixture.Status.ContainerFromItem(item)!).Content, item.Title)
                && Equals(((ContentControl)fixture.Group.ContainerFromItem(item)!).Content, item.Title)
                && Equals(galleryContainer.Content, item.Title),
            "DisplayMemberPath did not observe source property changes.");
    }

    private static void VerifyRepeatedSourceItems()
    {
        var repeated = new DataItem("Repeated model");
        var items = new ObservableCollection<DataItem> { repeated, repeated, new("Other") };
        var gallery = new Gallery { ItemsSource = items, SelectedIndex = 1 };
        var selected = gallery.Items[1];
        var changes = 0;
        gallery.SelectionChanged += (_, _) => changes++;
        items.Move(1, 0);
        Require(ReferenceEquals(gallery.Items[0], selected) && gallery.SelectedIndex == 0,
            "Moving a repeated source model lost the selected occurrence's container.");
        items.Insert(0, repeated);
        Require(ReferenceEquals(gallery.Items[1], selected) && gallery.SelectedIndex == 1,
            "Inserting a repeated source model stole an existing container.");
        items.RemoveAt(0);
        Require(ReferenceEquals(gallery.Items[0], selected) && gallery.SelectedIndex == 0 && changes == 0,
            "Unselected repeated-item edits changed the selected occurrence.");
        gallery.ItemsSource = null;
    }

    private static void VerifyRepeatedNativeItems()
    {
        var repeated = new DataItem("Repeated native item");
        var other = new DataItem("Other native item");
        var gallery = new Gallery();
        var items = ((ItemsControl)gallery).Items;
        items.Add(repeated);
        items.Add(repeated);
        gallery.SelectedIndex = 1;
        var selected = (RibbonGalleryItem)gallery.Items[1];
        var firstOccurrence = gallery.Items[0];
        var changes = new List<SelectionChangedEventArgs>();
        gallery.SelectionChanged += (_, args) => changes.Add(args);

        items.RemoveAt(0);
        AssertRetainedSelection(0);
        Require(gallery.ItemFromContainer(firstOccurrence) is null,
            "Native removal retained the wrong occurrence's mapping.");

        items.Insert(0, repeated);
        AssertRetainedSelection(1);
        var inserted = gallery.Items[0];
        Require(!ReferenceEquals(inserted, selected), "Native insertion reused the selected occurrence's container.");

        items[0] = other;
        AssertRetainedSelection(1);
        Require(ReferenceEquals(gallery.ItemFromContainer(gallery.Items[0]), other),
            "Native replacement did not update the changed occurrence.");
        items[0] = repeated;
        AssertRetainedSelection(1);
        items[1] = repeated;
        AssertRetainedSelection(1);
        Require(ReferenceEquals(gallery.ItemFromContainer(selected), repeated),
            "An unchanged native occurrence lost its model mapping.");

        items.Clear();
        AssertSelection(gallery, null, -1);
        Require(gallery.Items.Count == 0 && !selected.IsSelected,
            "Native reset retained containers or selected flags.");
        Require(changes.Count == 1, "Native reset emitted duplicate or spurious selection events.");
        AssertChange(changes[0], repeated, null);
        Require(gallery.ItemFromContainer(selected) is null, "Native reset retained the selected occurrence's mapping.");
        items.Add(repeated);
        items.Add(repeated);
        AssertSelection(gallery, null, -1);
        Require(changes.Count == 1, "Refilling native Items after reset restored a retired selection.");
        items.Clear();

        void AssertRetainedSelection(int index)
        {
            AssertSelection(gallery, repeated, index);
            Require(ReferenceEquals(gallery.Items[index], selected)
                    && ReferenceEquals(gallery.ContainerFromIndex(index), selected)
                    && gallery.IndexFromContainer(selected) == index
                    && changes.Count == 0,
                "A native vector notification replaced the selected occurrence or emitted a spurious selection event.");
        }
    }

    private static async Task VerifyIdleNativeReplacement(Panel host, DataTemplate template, Func<Task> settle)
    {
        var selected = new DataItem("Idle selected original");
        var unselected = new DataItem("Idle unselected original");
        var selectedReplacement = new DataItem("Idle selected replacement");
        var unselectedReplacement = new DataItem("Idle unselected replacement");
        var model = new DataModel();
        var gallery = new Gallery { ItemTemplate = template, Width = 400, Height = 100 };
        var nativeItems = ((ItemsControl)gallery).Items;
        nativeItems.Add(selected);
        nativeItems.Add(unselected);
        gallery.SetBinding(RibbonGallery.SelectedItemProperty, new Binding
        {
            Source = model,
            Path = new PropertyPath(nameof(DataModel.Selected)),
            Mode = BindingMode.TwoWay,
        });
        host.Children.Add(gallery);
        try
        {
            await settle();
            gallery.SelectedIndex = 0;
            await settle();
            var oldSelectedContainer = (RibbonGalleryItem)gallery.Items[0];
            var oldUnselectedContainer = (RibbonGalleryItem)gallery.Items[1];
            var panel = VisualTreeHelper.GetParent(oldSelectedContainer) as Panel
                        ?? throw new InvalidOperationException("The idle gallery did not realize its items panel.");
            var oldSelectedText = Descendants<TextBlock>(oldSelectedContainer)
                .Single(text => Equals(text.Tag, "PortDataPrimary"));
            var oldUnselectedText = Descendants<TextBlock>(oldUnselectedContainer)
                .Single(text => Equals(text.Tag, "PortDataPrimary"));
            var changes = new List<SelectionChangedEventArgs>();
            gallery.SelectionChanged += (_, args) => changes.Add(args);
            Require(oldSelectedContainer.IsSelected && ReferenceEquals(model.Selected, selected),
                "The idle gallery did not establish its initial two-way selection.");
            App.LogAutoTestStartup(
                $"PORT-DATA IDLE before texts={oldSelectedText.Text}|{oldUnselectedText.Text} "
                + $"oldSelected={oldSelectedContainer.IsSelected} model={model.Selected} events={changes.Count}");
            await Task.Delay(500);

            nativeItems[0] = selectedReplacement;
            nativeItems[1] = unselectedReplacement;

            // Nothing below this point queries the gallery's Fluent item/selection APIs
            // or forces layout. Inspect only the model, recorder, and retained visual tree.
            await Task.Delay(1200);
            await Task.Yield();
            var renderedText = Descendants<TextBlock>(panel)
                .Where(text => Equals(text.Tag, "PortDataPrimary")).ToArray();
            var labels = renderedText.Select(text => text.Text).ToArray();
            App.LogAutoTestStartup(
                $"PORT-DATA IDLE after texts={string.Join("|", labels)} "
                + $"oldSelected={oldSelectedContainer.IsSelected} "
                + $"model={model.Selected?.ToString() ?? "<null>"} events={changes.Count}");
            Require(labels.SequenceEqual(new[] { selectedReplacement.Title, unselectedReplacement.Title }),
                "Idle native replacement did not refresh visible content without a Fluent query or forced layout.");
            Require(!renderedText.Contains(oldSelectedText) && !renderedText.Contains(oldUnselectedText)
                    && !panel.Children.Contains(oldSelectedContainer) && !panel.Children.Contains(oldUnselectedContainer),
                "Idle native replacement left retired containers or template visuals mounted.");
            Require(!oldSelectedContainer.IsSelected && model.Selected is null && changes.Count == 1,
                "Idle native replacement did not clear the retired selection, two-way model, and event state.");
            AssertChange(changes[0], selected, null);
#if !WINDOWS
            var binding = typeof(RibbonGallery).GetField("itemsBinding", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(gallery)!;
            var observationField = binding.GetType().GetField(
                "nativeItemsObservation", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var observation = observationField.GetValue(binding)
                              ?? throw new InvalidOperationException("The loaded native-Items watcher was not active.");
            var timer = GetTimer(observation);
            Require(timer.IsEnabled, "The loaded native-Items watcher was stopped.");

            host.Children.Remove(gallery);
            await Task.Delay(200);
            Require(observationField.GetValue(binding) is null && !timer.IsEnabled,
                "Unloading the native-Items owner did not detach and stop its watcher.");
            host.Children.Add(gallery);
            await Task.Delay(250);
            var reloadedObservation = observationField.GetValue(binding)
                                      ?? throw new InvalidOperationException("Reload did not restart the native-Items watcher.");
            var reloadedTimer = GetTimer(reloadedObservation);
            var observableSource = new ObservableCollection<DataItem> { new("Observable source") };
            ((ItemsControl)gallery).ItemsSource = observableSource;
            Require(observationField.GetValue(binding) is null && !reloadedTimer.IsEnabled,
                "Assigning ItemsSource did not detach and stop the native-Items watcher.");
            observableSource.Add(new DataItem("Observable insertion"));
            await Task.Delay(250);
            Require(Descendants<TextBlock>(panel).Where(text => Equals(text.Tag, "PortDataPrimary"))
                    .Select(text => text.Text).SequenceEqual(observableSource.Select(item => item.Title)),
                "Stopping the native watcher broke the observable ItemsSource notification path.");
            App.LogAutoTestStartup("PORT-DATA IDLE lifecycle unload=stopped reload=active ItemsSource=stopped notifications=live");

            static DispatcherTimer GetTimer(object observation)
                => (DispatcherTimer)observation.GetType()
                    .GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(observation)!;
#endif
        }
        finally
        {
            ((ItemsControl)gallery).ItemsSource = null;
            nativeItems.Clear();
            host.Children.Remove(gallery);
            await settle();
        }
    }

    private static void VerifyReentrantSelection()
    {
        var first = new DataItem("Removed during selection");
        var items = new ObservableCollection<DataItem> { first, new("Retained") };
        var gallery = new Gallery { ItemsSource = items };
        var container = (RibbonGalleryItem)gallery.Items[0];
        var changes = 0;
        gallery.SelectionChanged += (_, _) => changes++;
        var token = container.RegisterPropertyChangedCallback(RibbonGalleryItem.IsSelectedProperty, (_, _) =>
        {
            if (container.IsSelected)
            {
                items.Remove(first);
            }
        });
        try
        {
            gallery.SelectedIndex = 0;
            AssertSelection(gallery, null, -1);
            Require(changes == 0, "A reentrant source removal emitted a stale added-item notification.");
        }
        finally
        {
            container.UnregisterPropertyChangedCallback(RibbonGalleryItem.IsSelectedProperty, token);
            gallery.ItemsSource = null;
        }
    }

    private static async Task VerifyReentrantContainerSelection(Panel host, Func<Task> settle)
    {
        var first = new DataItem("First reentrant item");
        var second = new DataItem("Second reentrant item");
        var model = new DataModel();
        model.Items.ResetWith([first, second]);
        var gallery = new Gallery { ItemsSource = model.Items, Width = 400, Height = 100 };
        gallery.SetBinding(RibbonGallery.SelectedItemProperty, new Binding
        {
            Source = model,
            Path = new PropertyPath(nameof(DataModel.Selected)),
            Mode = BindingMode.TwoWay,
        });
        host.Children.Add(gallery);
        var firstContainer = (RibbonGalleryItem)gallery.Items[0];
        var secondContainer = (RibbonGalleryItem)gallery.Items[1];
        var reselectFirst = true;
        var token = secondContainer.RegisterPropertyChangedCallback(RibbonGalleryItem.IsSelectedProperty, (_, _) =>
        {
            if (reselectFirst && secondContainer.IsSelected)
            {
                firstContainer.IsSelected = true;
            }
        });
        try
        {
            await settle();
            model.Selected = first;
            await settle();
            var changes = new List<SelectionChangedEventArgs>();
            gallery.SelectionChanged += (_, args) => changes.Add(args);
            var firstPeer = new GalleryItemWrapperAutomationPeer(firstContainer);
            var secondPeer = new GalleryItemWrapperAutomationPeer(secondContainer);
            var galleryPeer = (ISelectionProvider)new RibbonGalleryAutomationPeer(gallery);

            for (var attempt = 0; attempt < 2; attempt++)
            {
                gallery.SelectedIndex = 1;
                AssertSelection(gallery, first, 0);
                Require(firstContainer.IsSelected && !secondContainer.IsSelected
                        && ReferenceEquals(model.Selected, first)
                        && firstPeer.IsSelected && !secondPeer.IsSelected
                        && galleryPeer.GetSelection().Length == 1
                        && changes.Count == 0,
                    "A conflicting reentrant selection left model, flags, events, or UIA inconsistent.");
            }

            reselectFirst = false;
            gallery.SelectedIndex = 1;
            AssertSelection(gallery, second, 1);
            Require(ReferenceEquals(model.Selected, second)
                    && !firstPeer.IsSelected && secondPeer.IsSelected
                    && galleryPeer.GetSelection().Length == 1 && changes.Count == 1,
                "The gallery could not select normally after draining the reentrant request.");
            AssertChange(changes[0], first, second);
        }
        finally
        {
            secondContainer.UnregisterPropertyChangedCallback(RibbonGalleryItem.IsSelectedProperty, token);
            gallery.ItemsSource = null;
            host.Children.Remove(gallery);
            await settle();
        }
    }

    private static async Task VerifyGroupTemplateCache(
        Panel host, PortDataBindingFixture templates, Func<Task> settle)
    {
        var retained = new RibbonButton { Header = "Retained authored control", Size = RibbonControlSize.Medium };
        var source = new ObservableCollection<object> { new DataItem("Templated control"), retained };
        var buttonTemplate = templates.Group.ItemTemplate;
        var group = new RibbonGroupBox
        {
            Header = "Template cache",
            ItemsSource = source,
            ItemTemplate = buttonTemplate,
            Width = 500,
            Height = 100,
        };
        var field = typeof(RibbonGroupBox).GetField("_preferredSizes", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new InvalidOperationException("The group preferred-size cache was not found.");
        var cache = (IDictionary<UIElement, RibbonControlSize>)field.GetValue(group)!;
        host.Children.Add(group);
        try
        {
            await settle();
            var generated = GetGeneratedButtons();
            Require(generated.Length == 1 && cache.ContainsKey(generated[0])
                    && cache.TryGetValue(retained, out var authoredSize) && authoredSize == RibbonControlSize.Medium,
                "The rooted group did not capture its generated and authored controls' preferred sizes.");
            group.State = RibbonGroupBoxState.Small;
            await settle();

            for (var iteration = 0; iteration < 3; iteration++)
            {
                var retired = generated;
                group.ItemTemplate = templates.PrimaryTemplate;
                await settle();
                Require(retired.All(button => !cache.ContainsKey(button)),
                    "ItemTemplate replacement retained retired ribbon controls in the preferred-size cache.");
                Require(cache.TryGetValue(retained, out authoredSize) && authoredSize == RibbonControlSize.Medium,
                    "Pruning retired descendants overwrote a retained control's authored size.");

                group.ItemTemplate = buttonTemplate;
                await settle();
                generated = GetGeneratedButtons();
                Require(generated.Length == 1 && cache.ContainsKey(generated[0]),
                    "The replacement button template was not realized and tracked.");
            }

            group.State = RibbonGroupBoxState.Large;
            await settle();
            Require(retained.Size == RibbonControlSize.Medium,
                "A retained visual lost its authored size across template changes.");
            source.Clear();
            await settle();
            Require(group.Items.Count == 0 && cache.Count == 0,
                "Clearing the source retained current or retired controls in the preferred-size cache.");
        }
        finally
        {
            source.Clear();
            host.Children.Remove(group);
            await settle();
        }

        RibbonButton[] GetGeneratedButtons()
            => Descendants<RibbonButton>(group).Where(button => Equals(button.Tag, "PortDataGroup")).ToArray();
    }

    private static async Task VerifyItemsPanels(PortDataBindingFixture fixture, DataModel model, Func<Task> settle)
    {
        fixture.Menu.ItemsPanel = fixture.CustomPanel;
        fixture.Status.ItemsPanel = fixture.CustomPanel;
        fixture.Group.ItemsPanel = fixture.CustomPanel;
        await settle();
        Require(Descendants<StackPanel>(fixture.Menu.DropDownPopup!.Child).Any(IsCustomPanel)
                && Descendants<StackPanel>(fixture.Status).Any(IsCustomPanel)
                && Descendants<StackPanel>(fixture.Group).Any(IsCustomPanel),
            "The inherited ItemsPanel template was ignored.");
        model.Items.Add(new DataItem("Custom panel insertion"));
        await settle();
        AssertItems(fixture, model.Items);
        fixture.Menu.ClearValue(ItemsControl.ItemsPanelProperty);
        fixture.Status.ClearValue(ItemsControl.ItemsPanelProperty);
        fixture.Group.ClearValue(ItemsControl.ItemsPanelProperty);
        await settle();
        AssertItems(fixture, model.Items);

        static bool IsCustomPanel(StackPanel panel) => Equals(panel.Tag, "PortDataItemsPanel");
    }

    private static void VerifyStatusCustomization(PortDataBindingFixture fixture, DataModel model)
    {
        var statusItem = (StatusBarItem)fixture.Status.ContainerFromItem(model.Items[0])!;
        var rightItem = new StatusBarItem { Content = "Right" };
        fixture.Status.RightItems.Add(rightItem);
        statusItem.IsChecked = false;
        Require(statusItem.Visibility == Visibility.Collapsed, "A generated status item was not customizable.");
        Require(fixture.Status.RightItems.Single() == rightItem, "ItemsSource altered authored right-side status items.");
        var flyout = (Flyout)fixture.Status.ContextFlyout;
        Require(Descendants<StatusBarMenuItem>((DependencyObject)flyout.Content)
                .Any(menu => ReferenceEquals(menu.StatusBarItem, statusItem)),
            "The customization menu omitted a generated status container.");
        statusItem.IsChecked = true;
    }

    private static void AssertItems(PortDataBindingFixture fixture, IReadOnlyList<DataItem> items)
    {
        Require(fixture.Menu.Items.Count == items.Count && fixture.Status.Items.Count == items.Count
                && fixture.Group.Items.Count == items.Count && fixture.Gallery.Items.Count == items.Count,
            "The source and presented item counts differ.");
        foreach (var control in new ItemsControl[] { fixture.Menu, fixture.Status, fixture.Group, fixture.Gallery })
        {
            Require(control.Items.Cast<object>().SequenceEqual(items.Cast<object>()),
                $"{control.GetType().Name} native Items diverged from its source.");
        }

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            Require(ReferenceEquals(fixture.Menu.ContainerFromItem(item), fixture.Menu.Items[index])
                    && ReferenceEquals(fixture.Status.ContainerFromItem(item), fixture.Status.Items[index])
                    && ReferenceEquals(fixture.Group.ContainerFromItem(item), fixture.Group.Items[index])
                    && ReferenceEquals(fixture.Gallery.ContainerFromItem(item), fixture.Gallery.Items[index]),
                "ContainerFromItem did not preserve source order/identity.");
            Require(fixture.Menu.IndexFromContainer(fixture.Menu.Items[index]) == index
                    && fixture.Status.IndexFromContainer(fixture.Status.Items[index]) == index
                    && fixture.Group.IndexFromContainer(fixture.Group.Items[index]) == index
                    && fixture.Gallery.IndexFromContainer(fixture.Gallery.Items[index]) == index,
                "IndexFromContainer did not follow a collection mutation.");
            Require(ReferenceEquals(fixture.Gallery.ItemFromContainer(fixture.Gallery.Items[index]), item),
                "ItemFromContainer returned a container instead of the source model.");
        }
    }

    private static void AssertSelection(RibbonGallery gallery, object? item, int index)
    {
        Require(ReferenceEquals(gallery.SelectedItem, item) && gallery.SelectedIndex == index,
            "SelectedItem and SelectedIndex disagree with source identity.");
        Require(gallery.Items.OfType<RibbonGalleryItem>().Count(container => container.IsSelected)
                == (index >= 0 && gallery.Items[index] is RibbonGalleryItem ? 1 : 0),
            "Gallery container selection is not single/coherent.");
    }

    private static void AssertChange(SelectionChangedEventArgs args, object? removed, object? added)
    {
        Require(args.RemovedItems.Count == (removed is null ? 0 : 1)
                && (removed is null || ReferenceEquals(args.RemovedItems[0], removed))
                && args.AddedItems.Count == (added is null ? 0 : 1)
                && (added is null || ReferenceEquals(args.AddedItems[0], added)),
            "SelectionChanged supplied inconsistent source-item payloads.");
    }

    private static void AssertTemplateLabels(DependencyObject root, string tag, IEnumerable<DataItem> items)
    {
        Require(Descendants<TextBlock>(root).Where(text => Equals(text.Tag, tag)).Select(text => text.Text)
                .SequenceEqual(items.Select(item => item.Title)),
            $"The {tag} template did not display the current source in order.");
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match)
        {
            yield return match;
        }
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(root, index)))
            {
                yield return child;
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class ItemTemplateSelector(DataTemplate template) : DataTemplateSelector
    {
        internal int Calls { get; private set; }
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            Require(item is DataItem && container is FrameworkElement, "ItemTemplateSelector received the wrong identities.");
            Calls++;
            return template;
        }
    }

    private sealed partial class HookedMenuItem : Fluent.MenuItem
    {
        internal int Created { get; private set; }
        internal List<object> Prepared { get; } = [];
        internal List<object> Cleared { get; } = [];
        internal NotifyCollectionChangedAction LastChange { get; private set; }

        protected override DependencyObject GetContainerForItemOverride()
        {
            Created++;
            return new Fluent.MenuItem { Tag = "Custom menu container" };
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);
            Prepared.Add(item);
        }

        protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        {
            base.ClearContainerForItemOverride(element, item);
            Cleared.Add(item);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs args)
        {
            base.OnItemsChanged(args);
            LastChange = args.Action;
        }
    }

    private sealed class ItemStyleSelector(Style style) : StyleSelector
    {
        internal int Calls { get; private set; }
        protected override Style SelectStyleCore(object item, DependencyObject container)
        {
            Require(item is DataItem && container is GalleryItem, "ItemContainerStyleSelector received the wrong identities.");
            Calls++;
            return style;
        }
    }

    private sealed class DataItem(string title) : INotifyPropertyChanged
    {
        private string currentTitle = title;
        public string Title
        {
            get => currentTitle;
            set
            {
                currentTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        public override string ToString() => Title;
    }

    private sealed class DataModel : INotifyPropertyChanged
    {
        private object? selected;
        public ResettableCollection<DataItem> Items { get; } = new();
        public object? Selected
        {
            get => selected;
            set
            {
                selected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selected)));
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class ResettableCollection<T> : ObservableCollection<T>
    {
        internal void ResetWith(IEnumerable<T> items)
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
            Changed(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        internal void InsertRange(int index, IReadOnlyList<T> items)
        {
            for (var offset = 0; offset < items.Count; offset++)
            {
                Items.Insert(index + offset, items[offset]);
            }
            Changed(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (IList)items.ToArray(), index));
        }

        internal void RemoveRange(int index, int count)
        {
            var removed = this.Skip(index).Take(count).ToArray();
            for (var offset = 0; offset < count; offset++)
            {
                Items.RemoveAt(index);
            }
            Changed(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, (IList)removed, index));
        }

        internal void MoveRange(int oldIndex, int count, int newIndex)
        {
            var moved = this.Skip(oldIndex).Take(count).ToArray();
            for (var offset = 0; offset < count; offset++)
            {
                Items.RemoveAt(oldIndex);
            }
            for (var offset = 0; offset < count; offset++)
            {
                Items.Insert(newIndex + offset, moved[offset]);
            }
            Changed(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, (IList)moved, newIndex, oldIndex));
        }

        private void Changed(NotifyCollectionChangedEventArgs args)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(args);
        }
    }
}
