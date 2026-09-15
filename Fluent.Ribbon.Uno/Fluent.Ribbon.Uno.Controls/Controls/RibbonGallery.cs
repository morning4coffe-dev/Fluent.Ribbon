using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Controls;

namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents a gallery control that displays a grid of selectable items.
/// Galleries are typically used in ribbon drop-downs for selecting styles,
/// colors, shapes, etc.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonGallery : ListBox
{
    private const string PART_FilterLabel = "PART_FilterLabel";

    #region Dependency Properties

    /// <summary>Identifies whether the gallery contains any items.</summary>
    public static readonly DependencyProperty HasItemsProperty = Fluent.Helpers.ItemsControlBinding.HasItemsProperty;

    /// <summary>Gets whether the gallery contains any items.</summary>
    public bool HasItems => (bool)GetValue(HasItemsProperty);

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(RibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of gallery items.
    /// </summary>
    public new ObservableCollection<UIElement> Items
    {
        get
        {
            itemsBinding?.RefreshUnnotifiedNativeItems();
            return (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        }
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public new static readonly DependencyProperty ItemsSourceProperty =
        ItemsControl.ItemsSourceProperty;

    /// <summary>
    /// Gets or sets the data source for gallery items.
    /// </summary>
    public new IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemTemplate"/> dependency property.</summary>
    public new static readonly DependencyProperty ItemTemplateProperty =
        ItemsControl.ItemTemplateProperty;

    /// <summary>
    /// Gets or sets the data template used to display each item.
    /// </summary>
    public new DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property.</summary>
    public new static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(RibbonGallery),
            new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    public new object? SelectedItem
    {
        get
        {
            itemsBinding?.RefreshUnnotifiedNativeItems();
            return GetValue(SelectedItemProperty);
        }
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public new static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(RibbonGallery),
            new PropertyMetadata(-1, OnSelectedIndexChanged));

    /// <summary>
    /// Gets or sets the index of the currently selected item.
    /// </summary>
    public new int SelectedIndex
    {
        get
        {
            itemsBinding?.RefreshUnnotifiedNativeItems();
            return (int)GetValue(SelectedIndexProperty);
        }
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(RibbonGallery),
            new PropertyMetadata(72.0));

    /// <summary>
    /// Gets or sets the width of each gallery item.
    /// </summary>
    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemHeight"/> dependency property.</summary>
    public static readonly DependencyProperty ItemHeightProperty =
        DependencyProperty.Register(
            nameof(ItemHeight),
            typeof(double),
            typeof(RibbonGallery),
            new PropertyMetadata(56.0));

    /// <summary>
    /// Gets or sets the height of each gallery item.
    /// </summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MaxItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MaxItemsInRow),
            typeof(int),
            typeof(RibbonGallery),
            new PropertyMetadata(0));

    /// <summary>
    /// Gets or sets the maximum number of items per row. 0 means auto.
    /// </summary>
    public int MaxItemsInRow
    {
        get => (int)GetValue(MaxItemsInRowProperty);
        set => SetValue(MaxItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="MinItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MinItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MinItemsInRow),
            typeof(int),
            typeof(RibbonGallery),
            new PropertyMetadata(1));

    /// <summary>
    /// Gets or sets the minimum number of items per row.
    /// </summary>
    public int MinItemsInRow
    {
        get => (int)GetValue(MinItemsInRowProperty);
        set => SetValue(MinItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(RibbonGallery),
            new PropertyMetadata(Orientation.Horizontal));

    /// <summary>
    /// Gets or sets the orientation of the gallery items.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/group label for the gallery.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="GroupBy"/> dependency property.</summary>
    public static readonly DependencyProperty GroupByProperty =
        DependencyProperty.Register(
            nameof(GroupBy),
            typeof(string),
            typeof(RibbonGallery),
            new PropertyMetadata(null, OnGroupingChanged));

    /// <summary>
    /// Gets or sets the property name used to group items.
    /// </summary>
    public string? GroupBy
    {
        get => (string?)GetValue(GroupByProperty);
        set => SetValue(GroupByProperty, value);
    }

    /// <summary>Identifies the <see cref="IsGrouped"/> dependency property.</summary>
    public static readonly DependencyProperty IsGroupedProperty =
        DependencyProperty.Register(
            nameof(IsGrouped),
            typeof(bool),
            typeof(RibbonGallery),
            new PropertyMetadata(false, OnGroupingChanged));

    /// <summary>
    /// Gets or sets whether items are displayed in groups.
    /// </summary>
    public bool IsGrouped
    {
        get => (bool)GetValue(IsGroupedProperty);
        set => SetValue(IsGroupedProperty, value);
    }

    /// <summary>Identifies the <see cref="Selectable"/> dependency property.</summary>
    public static readonly DependencyProperty SelectableProperty =
        DependencyProperty.Register(
            nameof(Selectable),
            typeof(bool),
            typeof(RibbonGallery),
            new PropertyMetadata(true, OnSelectableChanged));

    /// <summary>
    /// Gets or sets whether items in the gallery can be selected.
    /// </summary>
    public bool Selectable
    {
        get => (bool)GetValue(SelectableProperty);
        set => SetValue(SelectableProperty, value);
    }

    /// <summary>Identifies the <see cref="Filters"/> dependency property.</summary>
    public static readonly DependencyProperty FiltersProperty =
        DependencyProperty.Register(
            nameof(Filters),
            typeof(ObservableCollection<GalleryGroupFilter>),
            typeof(RibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of filters for the gallery.
    /// </summary>
    public ObservableCollection<GalleryGroupFilter> Filters
    {
        get => (ObservableCollection<GalleryGroupFilter>)GetValue(FiltersProperty);
        private set => SetValue(FiltersProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedFilter"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedFilterProperty =
        DependencyProperty.Register(
            nameof(SelectedFilter),
            typeof(GalleryGroupFilter),
            typeof(RibbonGallery),
            new PropertyMetadata(null, OnSelectedFilterChanged));

    /// <summary>
    /// Gets or sets the currently active filter.
    /// </summary>
    public GalleryGroupFilter? SelectedFilter
    {
        get => (GalleryGroupFilter?)GetValue(SelectedFilterProperty);
        set => SetValue(SelectedFilterProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedFilterTitle"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedFilterTitleProperty =
        DependencyProperty.Register(
            nameof(SelectedFilterTitle),
            typeof(string),
            typeof(RibbonGallery),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets the title of the currently selected filter.
    /// </summary>
    public string SelectedFilterTitle
    {
        get => (string)GetValue(SelectedFilterTitleProperty);
        private set => SetValue(SelectedFilterTitleProperty, value);
    }

    /// <summary>Identifies the <see cref="HasFilter"/> dependency property.</summary>
    public static readonly DependencyProperty HasFilterProperty =
        DependencyProperty.Register(
            nameof(HasFilter),
            typeof(bool),
            typeof(RibbonGallery),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets whether the gallery has any filters defined.
    /// </summary>
    public bool HasFilter
    {
        get => (bool)GetValue(HasFilterProperty);
        private set => SetValue(HasFilterProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the selected item changes.
    /// </summary>
    public new event SelectionChangedEventHandler? SelectionChanged;

    #endregion

    #region Constructor

    private readonly Dictionary<RibbonGalleryItem, long> _selectionTokens = new();
    private readonly Dictionary<RibbonGalleryItem, long> _groupTokens = new();
    private readonly HashSet<UIElement> _hookedItems = new();
    private readonly Fluent.Helpers.ItemsControlBinding itemsBinding;
    private bool _isSynchronizingSelection;
    private UIElement? selectedContainer;
    private object? selectedValue;
    private UIElement? notifiedSelectionContainer;
    private object? notifiedSelectionValue;
    private int selectionVersion;

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGallery"/> class.
    /// </summary>
    public RibbonGallery()
    {
        DefaultStyleKey = typeof(RibbonGallery);
        IsTabStop = false;
        Items = Fluent.Helpers.ItemsControlBinding.CreateItems(this);
        itemsBinding = new Fluent.Helpers.ItemsControlBinding(
            this, Items,
#if WINDOWS
            IsCanonicalGalleryContainer, CreateCanonicalGalleryContainer,
            PrepareContainerForItemOverride, ClearCanonicalGalleryContainer,
#else
            IsItemItsOwnContainerOverride, GetContainerForItemOverride,
            PrepareContainerForItemOverride, ClearContainerForItemOverride,
#endif
            OnItemsChanged);
#if WINDOWS
        InitializeNativeGallery();
#endif
        Filters = new ObservableCollection<GalleryGroupFilter>();
        Filters.CollectionChanged += OnFiltersChanged;
        Loaded += OnGalleryLoaded;
        Unloaded += OnGalleryUnloaded;
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedTemplateMetadata);
    }

    #endregion

    #region Item Container

    /// <summary>Creates a gallery item container for data items.</summary>
#if WINDOWS
    protected override DependencyObject GetContainerForItemOverride()
    {
        return GetNativeGalleryContainer() ?? new RibbonGalleryItem();
    }
#else
    protected override DependencyObject GetContainerForItemOverride() => new RibbonGalleryItem();
#endif

    /// <summary>Preserves directly authored UI elements as their own containers.</summary>
#if WINDOWS
    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        if (!creatingCanonicalGalleryContainer && itemsBinding is { UsesNativeGenerator: true })
        {
            nativeContainerItem = item is ListBoxItem ? null : item;
            hasNativeContainerItem = item is not ListBoxItem;
            return item is ListBoxItem;
        }
        return item is UIElement;
    }
#else
    protected override bool IsItemItsOwnContainerOverride(object item) => item is UIElement;
#endif

    /// <inheritdoc />
#if WINDOWS
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        if (!ReferenceEquals(element, item) && item is UIElement && element is ListBoxItem nativeContainer)
        {
            nativeContainer.Content = item;
            return;
        }
        Fluent.Helpers.ItemsControlBinding.PrepareContent(this, element, item);
    }
#else
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        => Fluent.Helpers.ItemsControlBinding.PrepareContent(this, element, item);
#endif

    /// <inheritdoc />
#if WINDOWS
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        if (!clearingCanonicalGalleryContainer && itemsBinding is { UsesNativeGenerator: true }
            && itemsBinding.ReleaseNativeContainer(element))
        {
            return;
        }
        Fluent.Helpers.ItemsControlBinding.ClearContent(element, item);
    }
#else
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
        => Fluent.Helpers.ItemsControlBinding.ClearContent(element, item);
#endif

    /// <summary>Returns the live gallery container for a source item.</summary>
    public new DependencyObject? ContainerFromItem(object item) => itemsBinding.ContainerFromItem(item);

    /// <summary>Returns the live gallery container at an item index.</summary>
    public new DependencyObject? ContainerFromIndex(int index) => itemsBinding.ContainerFromIndex(index);

    /// <summary>Returns the source item represented by a live gallery container.</summary>
    public new object? ItemFromContainer(DependencyObject container) => itemsBinding.ItemFromContainer(container);

    /// <summary>Returns the index of a live gallery container.</summary>
    public new int IndexFromContainer(DependencyObject container) => itemsBinding.IndexFromContainer(container);

    internal object? GetSelectionValue(UIElement container) => itemsBinding.ItemFromContainer(container);

    internal UIElement? FindSelectionContainer(object? value)
    {
        if (selectedContainer is not null
            && Items.Contains(selectedContainer)
            && ReferenceEquals(GetSelectionValue(selectedContainer), value))
        {
            return selectedContainer;
        }

        if (value is UIElement element && Items.Contains(element))
        {
            return element;
        }

        return value is null ? null : itemsBinding.ContainerFromItem(value);
    }

    #endregion

    #region Template

    private ScrollViewer? _scrollViewer;
#if !WINDOWS
    private UniformItemsPanel? _itemsPanel;
    private StackPanel? _groupedPanel;
#endif
    private StackPanel? _filterButtons;
    private TextBlock? _filterLabel;
#if !WINDOWS
    private readonly List<(string Group, FrameworkElement? Header, UIElement Panel)> _groupEntries = new();
#endif

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        DetachItemsFromPanels();
        base.OnApplyTemplate();

        _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
#if !WINDOWS
        _itemsPanel = GetTemplateChild("PART_ItemsPanel") as UniformItemsPanel;
        _groupedPanel = GetTemplateChild("PART_GroupedPanel") as StackPanel;
#endif
        _filterButtons = GetTemplateChild("PART_FilterButtons") as StackPanel;
        _filterLabel = GetTemplateChild(PART_FilterLabel) as TextBlock;
#if WINDOWS
        ApplyNativeGalleryTemplate();
#endif
        RefreshLocalizedTemplateMetadata();

        SyncItems();
        SyncFilters();
    }

    private void RefreshLocalizedTemplateMetadata()
    {
        if (_filterLabel is not null)
        {
            Fluent.Automation.Peers.AutomationPeerHelpers.SetValueIfUnsetOrGenerated(
                _filterLabel,
                TextBlock.TextProperty,
                RibbonLocalization.Current.Localization.GalleryFilter);
        }
    }

    private void OnGalleryLoaded(object? sender, RoutedEventArgs e)
    {
        SyncItems();
        UpdateItemTabStops();
    }

    private void OnGalleryUnloaded(object? sender, RoutedEventArgs e)
    {
        DetachItemsFromPanels();
    }

    private void DetachItemsFromPanels()
    {
#if WINDOWS
        // The native generator alone removes its children, including during unload/retemplate.
        DetachNativeGalleryTemplate();
#else
        _itemsPanel?.Children.Clear();
        foreach (var (_, _, panel) in _groupEntries)
        {
            if (panel is Panel itemsPanel)
            {
                itemsPanel.Children.Clear();
            }
        }
#endif
    }

    #endregion

    #region Methods

    /// <summary>Reconciles gallery containers and selection after an item change.</summary>
    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        foreach (var item in _hookedItems.Where(item => !Items.Contains(item)).ToList())
        {
            ResetRemovedItem(item);
            UnhookItem(item);
        }

        foreach (var item in Items)
        {
            HookItem(item);
        }

        var selection = selectedContainer;
        if (selection is not null && !Items.Contains(selection))
        {
            selection = Items.FirstOrDefault(item => ReferenceEquals(GetSelectionValue(item), selectedValue));
        }

        if (selection is null && selectedContainer is null)
        {
            selection = Items
                .OfType<RibbonGalleryItem>()
                .FirstOrDefault(item => item.IsSelected && IsEnabled && item.IsEnabled);
        }

        SetSelection(Selectable ? selection : null);
        SyncItems();
        UpdateItemTabStops();
    }

    private void ResetRemovedItem(UIElement item)
    {
        if (item is not RibbonGalleryItem { IsSelected: true } galleryItem)
        {
            return;
        }

        var wasSynchronizing = _isSynchronizingSelection;
        _isSynchronizingSelection = true;
        try
        {
            galleryItem.IsSelected = false;
        }
        finally
        {
            _isSynchronizingSelection = wasSynchronizing;
        }
    }

    private void HookItem(UIElement item)
    {
        if (item is RibbonGalleryItem galleryItem)
        {
            galleryItem.GalleryOwner = this;
        }

        if (!_hookedItems.Add(item))
        {
            return;
        }

        item.PointerPressed += OnItemPointerPressed;
        if (item is RibbonGalleryItem selectableItem)
        {
            _selectionTokens[selectableItem] = selectableItem.RegisterPropertyChangedCallback(
                RibbonGalleryItem.IsSelectedProperty,
                OnGalleryItemIsSelectedChanged);
            _groupTokens[selectableItem] = selectableItem.RegisterPropertyChangedCallback(
                RibbonGalleryItem.GroupProperty,
                OnGalleryItemGroupChanged);
        }
    }

    private void UnhookItem(UIElement item)
    {
        if (!_hookedItems.Remove(item))
        {
            return;
        }

        item.PointerPressed -= OnItemPointerPressed;
        if (item is RibbonGalleryItem galleryItem)
        {
            if (ReferenceEquals(galleryItem.GalleryOwner, this))
            {
                galleryItem.GalleryOwner = null;
            }

            if (_selectionTokens.Remove(galleryItem, out var token))
            {
                galleryItem.UnregisterPropertyChangedCallback(
                    RibbonGalleryItem.IsSelectedProperty,
                    token);
            }

            if (_groupTokens.Remove(galleryItem, out var groupToken))
            {
                galleryItem.UnregisterPropertyChangedCallback(RibbonGalleryItem.GroupProperty, groupToken);
            }
        }
    }

    private void OnItemPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is UIElement item)
        {
            SelectItem(item);
        }
    }

    private void OnGalleryItemIsSelectedChanged(DependencyObject sender, DependencyProperty property)
    {
        if (itemsBinding.IsUpdating || sender is not RibbonGalleryItem item || !Items.Contains(item))
        {
            return;
        }

        // Only ignore the state being applied by SetSelection. A callback can make a
        // conflicting request; the selection version then retires the interrupted update.
        if (_isSynchronizingSelection && item.IsSelected == ReferenceEquals(selectedContainer, item))
        {
            return;
        }

        if (item.IsSelected)
        {
            if (!SelectItem(item))
            {
                ResetRemovedItem(item);
            }
        }
        else if (ReferenceEquals(selectedContainer, item))
        {
            SetSelection(null);
        }
    }

    private void OnGalleryItemGroupChanged(DependencyObject sender, DependencyProperty property) => SyncItems();

    private void SyncItems()
    {
#if WINDOWS
        SyncNativeGallery();
#else
        if (itemsBinding is null || itemsBinding.IsUpdating || itemsBinding.IsSuspended
            || _groupedPanel is null || _itemsPanel is null)
        {
            return;
        }

        _itemsPanel.ItemWidth = ItemWidth;
        _itemsPanel.ItemHeight = ItemHeight;

        // Detach every item from whichever panel currently hosts it *before* those panels are
        // removed from the tree. Detaching an item from a panel that has already been removed from
        // the visual tree leaves the item's native peer in a state where re-adding it elsewhere
        // throws COMException (0x800F1000) on the WinUI3 head, so the ordering here matters.
        DetachItemsFromPanels();
        foreach (var item in Items)
        {
            DetachFromParent(item);
        }

        _groupEntries.Clear();

        // Now remove the previously added group headers/panels (they are empty at this point;
        // keep the flat items panel).
        for (int i = _groupedPanel.Children.Count - 1; i >= 0; i--)
        {
            if (_groupedPanel.Children[i] != _itemsPanel)
            {
                _groupedPanel.Children.RemoveAt(i);
            }
        }

        if (IsGrouped || !string.IsNullOrEmpty(GroupBy))
        {
            // Hide flat panel, build grouped view
            _itemsPanel.Visibility = Visibility.Collapsed;
            BuildGroupedView();
        }
        else
        {
            // Flat mode: host all items directly in the uniform panel
            _itemsPanel.Visibility = Visibility.Visible;
            foreach (var item in Items)
            {
                _itemsPanel.Children.Add(item);
            }

            ApplyFilter();
        }
#endif
    }

#if !WINDOWS
    // A UIElement can only have a single parent; moving items between the flat and group panels
    // requires first detaching from whichever panel currently owns them. On the native WinUI head the
    // logical Parent reads null for elements hosted directly in a Panel's Children, so the host is
    // resolved through the visual tree as well — otherwise this silently no-ops and the subsequent
    // re-add throws COMException 0x800F1000.
    private static void DetachFromParent(UIElement element)
    {
        if (VisualTreeHelper.GetParent(element) is Panel visualParent)
        {
            visualParent.Children.Remove(element);
        }

        if (element is FrameworkElement { Parent: Panel logicalParent })
        {
            logicalParent.Children.Remove(element);
        }
    }

    private void BuildGroupedView()
    {
        if (_groupedPanel is null)
        {
            return;
        }

        _groupEntries.Clear();

        // Group items by their Group property
        var groups = new Dictionary<string, List<UIElement>>();
        var ungrouped = new List<UIElement>();

        foreach (var item in Items)
        {
            var groupName = GetItemGroup(item);

            if (string.IsNullOrEmpty(groupName))
            {
                ungrouped.Add(item);
            }
            else
            {
                if (!groups.ContainsKey(groupName))
                {
                    groups[groupName] = new List<UIElement>();
                }
                groups[groupName].Add(item);
            }
        }

        int insertIndex = 0;

        // Build every group (headers + item panels). Filter visibility is applied separately by
        // ApplyGroupFilter so that changing the active filter only toggles Visibility and never
        // re-parents the live item elements — repeatedly moving shared UIElements between panels
        // throws COMException (0x800F1000) on the WinUI3 head.
        foreach (var (groupName, items) in groups)
        {
            // Group header
            var header = new TextBlock
            {
                Text = groupName,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 11,
                Margin = new Thickness(4, 8, 4, 4),
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["RibbonSecondaryTextBrush"],
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(header, groupName);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetHeadingLevel(
                header,
                Microsoft.UI.Xaml.Automation.Peers.AutomationHeadingLevel.Level3);
            _groupedPanel.Children.Insert(insertIndex++, header);

            // Items grid (non-virtualizing, hosts the live item elements directly)
            var groupPanel = new UniformItemsPanel
            {
                ItemWidth = ItemWidth,
                ItemHeight = ItemHeight,
            };
            foreach (var item in items)
            {
                DetachFromParent(item);
                groupPanel.Children.Add(item);
            }
            _groupedPanel.Children.Insert(insertIndex++, groupPanel);

            _groupEntries.Add((groupName, header, groupPanel));
        }

        // Add ungrouped items at the end if any
        if (ungrouped.Count > 0)
        {
            var groupPanel = new UniformItemsPanel
            {
                ItemWidth = ItemWidth,
                ItemHeight = ItemHeight,
            };
            foreach (var item in ungrouped)
            {
                DetachFromParent(item);
                groupPanel.Children.Add(item);
            }
            _groupedPanel.Children.Insert(insertIndex, groupPanel);

            _groupEntries.Add((string.Empty, null, groupPanel));
        }

        ApplyGroupFilter();
    }
#endif

    // Toggles the visibility of each built group (header + items panel) to match the active filter,
    // without re-parenting any live item elements.
    private void ApplyGroupFilter()
    {
#if WINDOWS
        ApplyFilter();
        SyncNativeGallery();
#else
        var allowedGroups = GetAllowedGroupNames();

        foreach (var (groupName, header, panel) in _groupEntries)
        {
            // Ungrouped items (empty group name) and the "no filter" case are always shown.
            var visible = allowedGroups is null
                || string.IsNullOrEmpty(groupName)
                || allowedGroups.Contains(groupName);

            var visibility = visible ? Visibility.Visible : Visibility.Collapsed;

            if (header is not null)
            {
                header.Visibility = visibility;
            }

            panel.Visibility = visibility;
        }
#endif
    }

    private void ApplyFilter()
    {
        var allowedGroups = GetAllowedGroupNames();
        foreach (var item in Items)
        {
            var group = GetItemGroup(item);
            item.Visibility = allowedGroups is null
                              || string.IsNullOrEmpty(group)
                              || allowedGroups.Contains(group)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private string GetItemGroup(UIElement item)
    {
        if (item is RibbonGalleryItem { Group.Length: > 0 } galleryItem)
        {
            return galleryItem.Group;
        }

        if (string.IsNullOrEmpty(GroupBy) || item is not FrameworkElement element)
        {
            return string.Empty;
        }

        var source = element.DataContext ?? element;
        return Fluent.Helpers.PropertyValueHelper
                   .GetPublicPropertyValue(source, GroupBy)
                   ?.ToString()
               ?? string.Empty;
    }

#if WINDOWS
    internal UIElement GetNativeGalleryItem(UIElement container)
        => container is ListBoxItem { Content: UIElement item } && !Items.Contains(container)
            ? item
            : container;

    internal string GetNativeGalleryGroup(UIElement item) => GetItemGroup(GetNativeGalleryItem(item));
#endif

    private HashSet<string>? GetAllowedGroupNames()
    {
        if (SelectedFilter is null) return null;

        var groupNames = SelectedFilter.GetGroupNames();
        if (groupNames.Length == 0) return null;

        return new HashSet<string>(groupNames, StringComparer.OrdinalIgnoreCase);
    }

    private void SyncFilters()
    {
        if (_filterButtons is null || Filters.Count == 0) return;

        var buttons = new List<WinUIButton>();
        var targetSize = Fluent.Helpers.TouchTargetGeometry.ResolveCompactTargetSize(this);
        foreach (var filter in Filters)
        {
            var btn = new WinUIButton
            {
                Content = filter.Title,
                Tag = filter,
                Padding = new Thickness(8, 2, 8, 2),
                MinHeight = targetSize,
                FontSize = 11,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
            };
            if (_filterLabel is not null)
            {
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetLabeledBy(
                    btn,
                    _filterLabel);
            }

            btn.Click += OnFilterButtonClick;
            buttons.Add(btn);
        }

        _filterButtons.Children.Clear();
        foreach (var btn in buttons)
        {
            _filterButtons.Children.Add(btn);
        }

        // Highlight the selected filter
        UpdateFilterHighlight();
    }

    private void OnFilterButtonClick(object sender, RoutedEventArgs e)
    {
        if (sender is WinUIButton btn && btn.Tag is GalleryGroupFilter filter)
        {
            SelectedFilter = filter;
        }
    }

    private void UpdateFilterHighlight()
    {
        if (_filterButtons is null)
        {
            return;
        }

        foreach (var child in _filterButtons.Children)
        {
            if (child is not WinUIButton btn)
            {
                continue;
            }

            var isSelected = Equals(btn.Tag, SelectedFilter);
            btn.FontWeight = isSelected
                ? Microsoft.UI.Text.FontWeights.Bold
                : Microsoft.UI.Text.FontWeights.Normal;
        }
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGallery gallery)
        {
            gallery.HandleSelectedItemChanged(e.OldValue, e.NewValue);
        }
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGallery gallery)
        {
            var index = (int)e.NewValue;
            if (gallery._isSynchronizingSelection
                && index == (gallery.selectedContainer is null ? -1 : gallery.Items.IndexOf(gallery.selectedContainer)))
            {
                return;
            }

            var container = gallery.Selectable && index >= 0 && index < gallery.Items.Count
                ? gallery.Items[index]
                : null;
            gallery.SetSelection(container);
        }
    }

    private static void OnSelectableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGallery gallery && !(bool)e.NewValue)
        {
            gallery.SetSelection(null);
        }
    }

    private void HandleSelectedItemChanged(object? oldValue, object? newValue)
    {
        if (_isSynchronizingSelection && ReferenceEquals(newValue, selectedValue))
        {
            return;
        }

        var container = newValue is null ? null : FindSelectionContainer(newValue);
        if (newValue is not null && (!Selectable || container is null))
        {
            // An item that is not in the view cannot replace the current selection.
            SetSelection(selectedContainer);
            return;
        }

        SetSelection(container);
    }

    private void SetSelection(UIElement? container)
    {
        var version = ++selectionVersion;
        var newValue = container is null ? null : GetSelectionValue(container);
        selectedContainer = container;
        selectedValue = newValue;
        var wasSynchronizing = _isSynchronizingSelection;
        _isSynchronizingSelection = true;
        try
        {
            SelectedItem = newValue;
            if (version != selectionVersion)
            {
                return;
            }

            SelectedIndex = container is null ? -1 : Items.IndexOf(container);
            if (version != selectionVersion)
            {
                return;
            }

            foreach (var item in Items.OfType<RibbonGalleryItem>().ToArray())
            {
                item.IsSelected = ReferenceEquals(item, container);
                if (version != selectionVersion)
                {
                    return;
                }
            }
        }
        finally
        {
            _isSynchronizingSelection = wasSynchronizing;
        }

        UpdateItemTabStops();
        if (version != selectionVersion)
        {
            return;
        }

        var oldContainer = notifiedSelectionContainer;
        var oldValue = notifiedSelectionValue;
        if (ReferenceEquals(oldContainer, container) && ReferenceEquals(oldValue, newValue))
        {
            return;
        }

        notifiedSelectionContainer = container;
        notifiedSelectionValue = newValue;
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is Fluent.Automation.Peers.RibbonGalleryAutomationPeer peer)
        {
            peer.RaiseSelectionChanged(oldContainer, container);
        }

        var removed = oldContainer is null ? new List<object>() : new List<object> { oldValue! };
        var added = container is null ? new List<object>() : new List<object> { newValue! };
        OnSelectionChanged(new SelectionChangedEventArgs(removed, added));
    }

    /// <summary>Raises a selection change with source items, not generated containers.</summary>
    protected virtual void OnSelectionChanged(SelectionChangedEventArgs e) => SelectionChanged?.Invoke(this, e);

    internal bool SelectItem(UIElement item)
    {
        if (!Selectable || !IsEnabled || !IsItemEnabled(item) || !Items.Contains(item))
        {
            return false;
        }

        SetSelection(item);
        return ReferenceEquals(selectedContainer, item);
    }

    internal bool RemoveItemFromSelection(UIElement item)
    {
        if (!IsEnabled || !IsItemEnabled(item) || !ReferenceEquals(selectedContainer, item))
        {
            return false;
        }

        SetSelection(null);
        return selectedContainer is null;
    }

    internal IEnumerable<UIElement> GetAutomationItems()
    {
        var allowedGroups = GetAllowedGroupNames();
        return Items.Where(item =>
            Fluent.Automation.Peers.AutomationPeerHelpers.IsEffectivelyVisible(item)
            && (allowedGroups is null
                || string.IsNullOrEmpty(GetItemGroup(item))
                || allowedGroups.Contains(GetItemGroup(item))));
    }

    internal bool HandleGalleryItemKeyDown(UIElement source, KeyRoutedEventArgs e)
        => HandleNavigationKey(source, e);

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (!e.Handled)
        {
            HandleNavigationKey(e.OriginalSource as UIElement, e);
        }

        base.OnKeyDown(e);
    }

    private bool HandleNavigationKey(UIElement? source, KeyRoutedEventArgs e)
    {
        if (!TryGetNavigationDirection(e.Key, out var direction))
        {
            return false;
        }

        if (Orientation == Orientation.Vertical
            && e.Key is Windows.System.VirtualKey.Left or Windows.System.VirtualKey.Right)
        {
            return false;
        }

        var items = GetAutomationItems().Where(IsItemEnabled).ToList();
        if (items.Count == 0)
        {
            return false;
        }

        var current = source is null ? null : FindContainingItem(source, items);
        current ??= selectedContainer;
        var currentIndex = current is null ? 0 : Math.Max(0, items.IndexOf(current));
        var columns = Orientation == Orientation.Vertical
            ? 1
            : GalleryLayoutMath.ComputeColumns(
                ActualWidth,
                ItemWidth,
                items.Count,
                MinItemsInRow,
                MaxItemsInRow,
                Orientation);
        var targetIndex = GalleryNavigationMath.GetTargetIndex(
            currentIndex,
            items.Count,
            columns,
            Orientation,
            direction);
        if (targetIndex < 0)
        {
            return false;
        }

        var target = items[targetIndex];
        SelectItem(target);
        SetRovingFocus(target);
        target.Focus(FocusState.Keyboard);
        target.StartBringIntoView();
        e.Handled = true;
        return true;
    }

    private static UIElement? FindContainingItem(UIElement source, IReadOnlyCollection<UIElement> items)
    {
        DependencyObject? current = source;
        while (current is not null)
        {
            if (current is UIElement element && items.Contains(element))
            {
                return element;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static bool TryGetNavigationDirection(
        Windows.System.VirtualKey key,
        out GalleryNavigationDirection direction)
    {
        direction = key switch
        {
            Windows.System.VirtualKey.Left => GalleryNavigationDirection.Previous,
            Windows.System.VirtualKey.Right => GalleryNavigationDirection.Next,
            Windows.System.VirtualKey.Up => GalleryNavigationDirection.PreviousRow,
            Windows.System.VirtualKey.Down => GalleryNavigationDirection.NextRow,
            Windows.System.VirtualKey.Home => GalleryNavigationDirection.First,
            Windows.System.VirtualKey.End => GalleryNavigationDirection.Last,
            _ => default,
        };
        return key is Windows.System.VirtualKey.Left
            or Windows.System.VirtualKey.Right
            or Windows.System.VirtualKey.Up
            or Windows.System.VirtualKey.Down
            or Windows.System.VirtualKey.Home
            or Windows.System.VirtualKey.End;
    }

    private void UpdateItemTabStops()
    {
        var focusTarget = selectedContainer
                          ?? GetAutomationItems().FirstOrDefault(IsItemEnabled);
        SetRovingFocus(focusTarget);
    }

    private void SetRovingFocus(UIElement? focusTarget)
    {
        foreach (var item in Items.OfType<Control>())
        {
            item.IsTabStop = ReferenceEquals(item, focusTarget);
        }
    }

    private static bool IsItemEnabled(UIElement item)
        => item is not Control control || control.IsEnabled;

    private static void OnGroupingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((RibbonGallery)d).SyncItems();

    private static void OnSelectedFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGallery gallery)
        {
            var filter = (GalleryGroupFilter?)e.NewValue;
            gallery.SelectedFilterTitle = filter?.Title ?? string.Empty;

            // Re-apply the filter. In grouped mode this only toggles group visibility (no rebuild
            // / re-parenting); in flat mode it toggles per-item visibility.
            if (gallery.IsGrouped || !string.IsNullOrEmpty(gallery.GroupBy))
            {
                gallery.ApplyGroupFilter();
            }
            else
            {
                gallery.ApplyFilter();
            }

            gallery.UpdateFilterHighlight();
            gallery.UpdateItemTabStops();
        }
    }

    private void OnFiltersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasFilter = Filters.Count > 0;

        if (SelectedFilter is null && Filters.Count > 0)
        {
            SelectedFilter = Filters[0];
        }
    }

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonGalleryAutomationPeer(this);

    #endregion
}
