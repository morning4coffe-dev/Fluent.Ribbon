using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml.Controls;

namespace Fluent;

/// <summary>
/// Represents a gallery control that displays a grid of selectable items.
/// Galleries are typically used in ribbon drop-downs for selecting styles,
/// colors, shapes, etc.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonGallery : Control
{
    #region Dependency Properties

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
    public ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(RibbonGallery),
            new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>
    /// Gets or sets the data source for gallery items.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(
            nameof(ItemTemplate),
            typeof(DataTemplate),
            typeof(RibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the data template used to display each item.
    /// </summary>
    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(RibbonGallery),
            new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(RibbonGallery),
            new PropertyMetadata(-1, OnSelectedIndexChanged));

    /// <summary>
    /// Gets or sets the index of the currently selected item.
    /// </summary>
    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
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
            new PropertyMetadata(null));

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
            new PropertyMetadata(false));

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
            new PropertyMetadata(true));

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
    public event EventHandler<object?>? SelectionChanged;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGallery"/> class.
    /// </summary>
    public RibbonGallery()
    {
        DefaultStyleKey = typeof(RibbonGallery);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsChanged;
        Filters = new ObservableCollection<GalleryGroupFilter>();
        Filters.CollectionChanged += OnFiltersChanged;
    }

    #endregion

    #region Template

    private ScrollViewer? _scrollViewer;
    private UniformItemsPanel? _itemsPanel;
    private StackPanel? _groupedPanel;
    private StackPanel? _filterButtons;
    private readonly List<(string Group, FrameworkElement? Header, UIElement Panel)> _groupEntries = new();

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
        _itemsPanel = GetTemplateChild("PART_ItemsPanel") as UniformItemsPanel;
        _groupedPanel = GetTemplateChild("PART_GroupedPanel") as StackPanel;
        _filterButtons = GetTemplateChild("PART_FilterButtons") as StackPanel;

        // Re-sync live children on load/unload so items are never orphaned between panels
        // (a UIElement can only live under one parent at a time).
        Loaded -= OnGalleryLoaded;
        Unloaded -= OnGalleryUnloaded;
        Loaded += OnGalleryLoaded;
        Unloaded += OnGalleryUnloaded;

        SyncItems();
        SyncFilters();
    }

    private void OnGalleryLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            SyncItems();
        }
        catch
        {
            // Swallow any exceptions during load to avoid crashing the app at this point.
        }
    }

    private void OnGalleryUnloaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Detach the live children so a subsequent reload can re-parent them cleanly.
            _itemsPanel?.Children.Clear();
        }
        catch
        {
            // Ignore any errors during unload.
        }
    }

    #endregion

    #region Methods

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
    }

    private void SyncItems()
    {
        if (_groupedPanel is null || _itemsPanel is null)
        {
            return;
        }

        _itemsPanel.ItemWidth = ItemWidth;
        _itemsPanel.ItemHeight = ItemHeight;

        // Detach every item from whichever panel currently hosts it *before* those panels are
        // removed from the tree. Detaching an item from a panel that has already been removed from
        // the visual tree leaves the item's native peer in a state where re-adding it elsewhere
        // throws COMException (0x800F1000) on the WinUI3 head, so the ordering here matters.
        _itemsPanel.Children.Clear();
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
    }

    // A UIElement can only have a single parent; moving items between the flat and group panels
    // requires first detaching from whichever panel currently owns them.
    private static void DetachFromParent(UIElement element)
    {
        if (element is FrameworkElement fe && fe.Parent is Panel panel)
        {
            panel.Children.Remove(element);
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
            string groupName = string.Empty;

            if (item is RibbonGalleryItem galleryItem && !string.IsNullOrEmpty(galleryItem.Group))
            {
                groupName = galleryItem.Group;
            }
            else if (!string.IsNullOrEmpty(GroupBy) && item is FrameworkElement fe)
            {
                // Try to get group from data context property
                var dataContext = fe.DataContext;
                if (dataContext is not null)
                {
                    var prop = dataContext.GetType().GetProperty(GroupBy);
                    groupName = prop?.GetValue(dataContext)?.ToString() ?? string.Empty;
                }
            }

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

    // Toggles the visibility of each built group (header + items panel) to match the active filter,
    // without re-parenting any live item elements.
    private void ApplyGroupFilter()
    {
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
    }

    private void ApplyFilter()
    {
        var allowedGroups = GetAllowedGroupNames();
        if (allowedGroups is null)
        {
            // No filter active: show all items
            foreach (var item in Items)
            {
                if (item is UIElement ue)
                {
                    ue.Visibility = Visibility.Visible;
                }
            }
            return;
        }

        // Apply filter: show only items whose Group is in the allowed set
        foreach (var item in Items)
        {
            if (item is RibbonGalleryItem galleryItem)
            {
                galleryItem.Visibility = string.IsNullOrEmpty(galleryItem.Group) ||
                    allowedGroups.Contains(galleryItem.Group)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }
    }

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

        var buttons = new List<Button>();
        foreach (var filter in Filters)
        {
            var btn = new Button
            {
                Content = filter.Title,
                Tag = filter,
                Padding = new Thickness(8, 2, 8, 2),
                MinHeight = 20,
                FontSize = 11,
                Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
            };
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
        if (sender is Button btn && btn.Tag is GalleryGroupFilter filter)
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
            if (child is not Button btn)
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
            if (!gallery.Selectable)
            {
                return;
            }

            gallery.SelectionChanged?.Invoke(gallery, e.NewValue);
        }
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGallery gallery)
        {
            var index = (int)e.NewValue;
            if (index >= 0 && index < gallery.Items.Count)
            {
                gallery.SelectedItem = gallery.Items[index];
            }
        }
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGallery gallery)
        {
            gallery.OnItemsSourceChanged(e.OldValue as IEnumerable, e.NewValue as IEnumerable);
        }
    }

    private void OnItemsSourceChanged(IEnumerable? oldSource, IEnumerable? newSource)
    {
        // Unsubscribe from old collection
        if (oldSource is INotifyCollectionChanged oldNotify)
        {
            oldNotify.CollectionChanged -= OnItemsSourceCollectionChanged;
        }

        // Clear existing items generated from source
        Items.Clear();

        // Subscribe to new collection and generate items
        if (newSource is not null)
        {
            if (newSource is INotifyCollectionChanged newNotify)
            {
                newNotify.CollectionChanged += OnItemsSourceCollectionChanged;
            }

            foreach (var item in newSource)
            {
                Items.Add(CreateItemContainer(item));
            }
        }
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    var insertIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : Items.Count;
                    foreach (var item in e.NewItems)
                    {
                        Items.Insert(insertIndex++, CreateItemContainer(item));
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    for (var i = 0; i < e.OldItems.Count; i++)
                    {
                        if (e.OldStartingIndex >= 0 && e.OldStartingIndex < Items.Count)
                        {
                            Items.RemoveAt(e.OldStartingIndex);
                        }
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                Items.Clear();
                if (ItemsSource is not null)
                {
                    foreach (var item in ItemsSource)
                    {
                        Items.Add(CreateItemContainer(item));
                    }
                }
                break;

            default:
                break;
        }
    }

    private UIElement CreateItemContainer(object item)
    {
        if (item is UIElement element)
        {
            return element;
        }

        if (ItemTemplate is not null)
        {
            var content = new ContentPresenter
            {
                Content = item,
                ContentTemplate = ItemTemplate,
            };
            return content;
        }

        return new TextBlock { Text = item?.ToString() ?? string.Empty };
    }

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

    #endregion
}
