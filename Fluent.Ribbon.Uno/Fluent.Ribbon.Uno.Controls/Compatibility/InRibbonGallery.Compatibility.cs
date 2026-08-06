namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

using System.Collections;
using Fluent.Extensibility;
using Microsoft.UI.Xaml.Automation;

/// <summary>
/// Portable WPF-compatible API and behavior for <see cref="InRibbonGallery"/>.
/// </summary>
/// <remarks>
/// The Uno control intentionally remains derived from <see cref="Control"/>. Changing it to
/// Selector would break the existing live-UIElement collection and XAML templates.
/// </remarks>
public partial class InRibbonGallery :
    IDropDownControl,
    IRibbonControl,
    IQuickAccessItemProvider,
    IRibbonSizeChangedSink,
    ILargeIconProvider,
    IMediumIconProvider,
    ISimplifiedRibbonControl,
    IKeyTipedControl,
    ILogicalChildSupport
{
    private ObservableCollection<GalleryGroupFilter>? _filters;
    private readonly Dictionary<RibbonGalleryItem, long> _selectionTokens = new();
    private readonly HashSet<UIElement> _hookedItems = new();
    private readonly List<(string Group, FrameworkElement? Header, UIElement Panel)> _popupGroupEntries = new();
    private StackPanel? _popupFilterBar;
    private ResizeableContentControl? _popupResizeHost;
    private Border? _popupBorder;
    private RibbonGalleryItem? _previewedItem;
    private bool _isSynchronizingSelection;
    private bool _isSnapped;
    private bool _isFrozen;
    private InRibbonGallery? _quickAccessClone;
    private InRibbonGallery? _quickAccessOwner;
    private List<UIElement>? _quickAccessTransferredItems;
    private UniformItemsPanel? _borrowedOwnerPanel;
    private InRibbonGallery? _activeQuickAccessClone;

    /// <summary>Identifies the WPF-compatible size-definition property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(InRibbonGallery),
            new PropertyMetadata(
                new RibbonControlSizeDefinition(
                    RibbonControlSize.Large,
                    RibbonControlSize.Middle,
                    RibbonControlSize.Small)));

    /// <summary>Identifies the WPF-compatible simplified size-definition property.</summary>
    public static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedSizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(InRibbonGallery),
            new PropertyMetadata(
                new RibbonControlSizeDefinition(
                    RibbonControlSize.Large,
                    RibbonControlSize.Middle,
                    RibbonControlSize.Small)));

    /// <summary>Identifies the header-template property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>Identifies the header-template-selector property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>Identifies the WPF-compatible icon property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>Identifies the WPF-compatible medium-icon property.</summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>Identifies the minimum dropdown row-size property.</summary>
    public static readonly DependencyProperty MinItemsInDropDownRowProperty =
        DependencyProperty.Register(
            nameof(MinItemsInDropDownRow),
            typeof(int),
            typeof(InRibbonGallery),
            new PropertyMetadata(1, OnGalleryLayoutPropertyChanged));

    /// <summary>Identifies the maximum dropdown row-size property.</summary>
    public static readonly DependencyProperty MaxItemsInDropDownRowProperty =
        DependencyProperty.Register(
            nameof(MaxItemsInDropDownRow),
            typeof(int),
            typeof(InRibbonGallery),
            new PropertyMetadata(0, OnGalleryLayoutPropertyChanged));

    /// <summary>Identifies the property-name grouping property.</summary>
    public static readonly DependencyProperty GroupByProperty =
        DependencyProperty.Register(
            nameof(GroupBy),
            typeof(string),
            typeof(InRibbonGallery),
            new PropertyMetadata(null, OnGroupingPropertyChanged));

    /// <summary>Identifies the callback-based grouping property.</summary>
    public static readonly DependencyProperty GroupByAdvancedProperty =
        DependencyProperty.Register(
            nameof(GroupByAdvanced),
            typeof(Func<object, string>),
            typeof(InRibbonGallery),
            new PropertyMetadata(null, OnGroupingPropertyChanged));

    /// <summary>Identifies the gallery orientation property.</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(InRibbonGallery),
            new PropertyMetadata(Orientation.Horizontal, OnGalleryLayoutPropertyChanged));

    /// <summary>Identifies the selected filter property.</summary>
    public static readonly DependencyProperty SelectedFilterProperty =
        DependencyProperty.Register(
            nameof(SelectedFilter),
            typeof(GalleryGroupFilter),
            typeof(InRibbonGallery),
            new PropertyMetadata(null, OnSelectedFilterChanged));

    /// <summary>Identifies the selected filter title property.</summary>
    public static readonly DependencyProperty SelectedFilterTitleProperty =
        DependencyProperty.Register(
            nameof(SelectedFilterTitle),
            typeof(string),
            typeof(InRibbonGallery),
            new PropertyMetadata(string.Empty));

    /// <summary>Identifies the selected filter group metadata property.</summary>
    public static readonly DependencyProperty SelectedFilterGroupsProperty =
        DependencyProperty.Register(
            nameof(SelectedFilterGroups),
            typeof(string),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>Identifies the has-filter property.</summary>
    public static readonly DependencyProperty HasFilterProperty =
        DependencyProperty.Register(
            nameof(HasFilter),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(false));

    /// <summary>Identifies the resize mode property.</summary>
    public static readonly DependencyProperty ResizeModeProperty =
        DependencyProperty.Register(
            nameof(ResizeMode),
            typeof(ContextMenuResizeMode),
            typeof(InRibbonGallery),
            new PropertyMetadata(ContextMenuResizeMode.None, OnDropDownDimensionChanged));

    /// <summary>Identifies the popup menu property.</summary>
    public static readonly DependencyProperty MenuProperty =
        DependencyProperty.Register(
            nameof(Menu),
            typeof(RibbonMenu),
            typeof(InRibbonGallery),
            new PropertyMetadata(null, OnPopupSupplementalContentChanged));

    /// <summary>Identifies the maximum dropdown height property.</summary>
    public static readonly DependencyProperty MaxDropDownHeightProperty =
        DependencyProperty.Register(
            nameof(MaxDropDownHeight),
            typeof(double),
            typeof(InRibbonGallery),
            new PropertyMetadata(double.PositiveInfinity, OnDropDownDimensionChanged));

    /// <summary>Identifies the maximum dropdown width property.</summary>
    public static readonly DependencyProperty MaxDropDownWidthProperty =
        DependencyProperty.Register(
            nameof(MaxDropDownWidth),
            typeof(double),
            typeof(InRibbonGallery),
            new PropertyMetadata(double.PositiveInfinity, OnDropDownDimensionChanged));

    /// <summary>Identifies the initial dropdown height property.</summary>
    public static readonly DependencyProperty DropDownHeightProperty =
        DependencyProperty.Register(
            nameof(DropDownHeight),
            typeof(double),
            typeof(InRibbonGallery),
            new PropertyMetadata(double.NaN, OnDropDownDimensionChanged));

    /// <summary>Identifies the initial dropdown width property.</summary>
    public static readonly DependencyProperty DropDownWidthProperty =
        DependencyProperty.Register(
            nameof(DropDownWidth),
            typeof(double),
            typeof(InRibbonGallery),
            new PropertyMetadata(double.NaN, OnDropDownDimensionChanged));

    /// <summary>Identifies the inline gallery container-height property.</summary>
    public static readonly DependencyProperty GalleryPanelContainerHeightProperty =
        DependencyProperty.Register(
            nameof(GalleryPanelContainerHeight),
            typeof(double),
            typeof(InRibbonGallery),
            new PropertyMetadata(68D));

    /// <summary>Identifies the simplified-state property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(false));

    /// <summary>Identifies the expand-button content property.</summary>
    public static readonly DependencyProperty ExpandButtonContentProperty =
        DependencyProperty.Register(
            nameof(ExpandButtonContent),
            typeof(object),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>Identifies the expand-button content-template property.</summary>
    public static readonly DependencyProperty ExpandButtonContentTemplateProperty =
        DependencyProperty.Register(
            nameof(ExpandButtonContentTemplate),
            typeof(DataTemplate),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>Identifies the quick-access availability property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <summary>Occurs when the control scale changes.</summary>
    public event EventHandler? Scaled;

    /// <summary>Occurs when the dropdown opens.</summary>
    public event EventHandler? DropDownOpened;

    /// <summary>Occurs when the dropdown closes.</summary>
    public event EventHandler? DropDownClosed;

    /// <summary>Occurs when the selected item changes.</summary>
    public new event SelectionChangedEventHandler? SelectionChanged;

    /// <summary>Gets or sets the WPF-compatible size definition.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Gets or sets the WPF-compatible simplified size definition.</summary>
    public RibbonControlSizeDefinition SimplifiedSizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Gets or sets the header template.</summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Gets or sets the header template selector.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Gets or sets the WPF-compatible icon value.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the WPF-compatible medium icon value.</summary>
    public object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Gets or sets the minimum number of dropdown items per row.</summary>
    public int MinItemsInDropDownRow
    {
        get => (int)GetValue(MinItemsInDropDownRowProperty);
        set => SetValue(MinItemsInDropDownRowProperty, value);
    }

    /// <summary>Gets or sets the maximum number of dropdown items per row.</summary>
    public int MaxItemsInDropDownRow
    {
        get => (int)GetValue(MaxItemsInDropDownRowProperty);
        set => SetValue(MaxItemsInDropDownRowProperty, value);
    }

    /// <summary>Gets or sets the property used to group items.</summary>
    public string? GroupBy
    {
        get => (string?)GetValue(GroupByProperty);
        set => SetValue(GroupByProperty, value);
    }

    /// <summary>Gets or sets the callback used to group items.</summary>
    public Func<object, string>? GroupByAdvanced
    {
        get => (Func<object, string>?)GetValue(GroupByAdvancedProperty);
        set => SetValue(GroupByAdvancedProperty, value);
    }

    /// <summary>Gets or sets the gallery orientation.</summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Gets the available group filters.</summary>
    public ObservableCollection<GalleryGroupFilter> Filters
    {
        get
        {
            if (_filters is null)
            {
                _filters = new ObservableCollection<GalleryGroupFilter>();
                _filters.CollectionChanged += OnFiltersCollectionChanged;
            }

            return _filters;
        }
    }

    /// <summary>Gets or sets the active group filter.</summary>
    public GalleryGroupFilter? SelectedFilter
    {
        get => (GalleryGroupFilter?)GetValue(SelectedFilterProperty);
        set => SetValue(SelectedFilterProperty, value);
    }

    /// <summary>Gets the active filter title.</summary>
    public string? SelectedFilterTitle
    {
        get => (string?)GetValue(SelectedFilterTitleProperty);
        private set => SetValue(SelectedFilterTitleProperty, value);
    }

    /// <summary>Gets the active filter group metadata.</summary>
    public string? SelectedFilterGroups
    {
        get => (string?)GetValue(SelectedFilterGroupsProperty);
        private set => SetValue(SelectedFilterGroupsProperty, value);
    }

    /// <summary>Gets whether filters are available.</summary>
    public bool HasFilter
    {
        get => (bool)GetValue(HasFilterProperty);
        private set => SetValue(HasFilterProperty, value);
    }

    /// <summary>Gets the active dropdown popup.</summary>
    public Popup? DropDownPopup => _popup;

    /// <summary>Gets or sets whether a context menu is open.</summary>
    public bool IsContextMenuOpened { get; set; }

    /// <summary>Gets or sets the popup resize mode.</summary>
    public ContextMenuResizeMode ResizeMode
    {
        get => (ContextMenuResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    /// <summary>Gets or sets the menu displayed below popup items.</summary>
    public RibbonMenu? Menu
    {
        get => (RibbonMenu?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>Gets or sets the maximum dropdown height.</summary>
    public double MaxDropDownHeight
    {
        get => (double)GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    /// <summary>Gets or sets the maximum dropdown width.</summary>
    public double MaxDropDownWidth
    {
        get => (double)GetValue(MaxDropDownWidthProperty);
        set => SetValue(MaxDropDownWidthProperty, value);
    }

    /// <summary>Gets or sets the initial dropdown height.</summary>
    public double DropDownHeight
    {
        get => (double)GetValue(DropDownHeightProperty);
        set => SetValue(DropDownHeightProperty, value);
    }

    /// <summary>Gets or sets the initial dropdown width.</summary>
    public double DropDownWidth
    {
        get => (double)GetValue(DropDownWidthProperty);
        set => SetValue(DropDownWidthProperty, value);
    }

    /// <summary>Gets or sets the inline gallery container height.</summary>
    public double GalleryPanelContainerHeight
    {
        get => (double)GetValue(GalleryPanelContainerHeightProperty);
        set => SetValue(GalleryPanelContainerHeightProperty, value);
    }

    /// <summary>Gets whether simplified mode is active.</summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        private set => SetValue(IsSimplifiedProperty, value);
    }

    /// <summary>Gets or sets custom expand-button content.</summary>
    public object? ExpandButtonContent
    {
        get => GetValue(ExpandButtonContentProperty);
        set => SetValue(ExpandButtonContentProperty, value);
    }

    /// <summary>Gets or sets the custom expand-button content template.</summary>
    public DataTemplate? ExpandButtonContentTemplate
    {
        get => (DataTemplate?)GetValue(ExpandButtonContentTemplateProperty);
        set => SetValue(ExpandButtonContentTemplateProperty, value);
    }

    /// <summary>Gets whether the inline presentation is snapped while the popup is active.</summary>
    public bool IsSnapped
    {
        get => _isSnapped;
        private set => _isSnapped = value;
    }

    /// <summary>Gets whether the original gallery is frozen by its quick-access clone.</summary>
    public bool IsFrozen
    {
        get => _isFrozen;
        private set => _isFrozen = value;
    }

    /// <summary>Gets or sets whether this gallery may be added to quick access.</summary>
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    /// <summary>Creates a compact gallery clone for the quick access toolbar.</summary>
    public virtual FrameworkElement CreateQuickAccessItem()
    {
        var clone = new InRibbonGallery
        {
            Header = null,
            HeaderTemplate = HeaderTemplate,
            HeaderTemplateSelector = HeaderTemplateSelector,
            Icon = Icon,
            LargeIcon = LargeIcon,
            MediumIcon = MediumIcon,
            IconGlyph = IconGlyph,
            ItemWidth = ItemWidth,
            ItemHeight = ItemHeight,
            MinItemsInDropDownRow = MinItemsInDropDownRow,
            MaxItemsInDropDownRow = MaxItemsInDropDownRow,
            MaxDropDownHeight = MaxDropDownHeight,
            MaxDropDownWidth = MaxDropDownWidth,
            DropDownHeight = DropDownHeight,
            DropDownWidth = DropDownWidth,
            ResizeMode = ResizeMode,
            GroupBy = GroupBy,
            GroupByAdvanced = GroupByAdvanced,
            Orientation = Orientation,
            Selectable = Selectable,
            Menu = Menu,
            CanCollapseToButton = true,
            IsCollapsed = true,
            Size = RibbonControlSize.Small,
            MinHeight = 24,
            FontSize = 16,
            Padding = new Thickness(4, 2, 4, 2),
            _quickAccessOwner = this,
        };
        AutomationProperties.SetName(
            clone,
            Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(Header) is { Length: > 0 } header
                ? header
                : RibbonLocalization.Current.Localization.GalleryName);

        foreach (var filter in Filters)
        {
            clone.Filters.Add(filter);
        }

        clone.SelectedFilter = SelectedFilter;
        clone.DropDownOpened += OnQuickAccessCloneOpened;
        clone.DropDownClosed += OnQuickAccessCloneClosed;
        _quickAccessClone = clone;
        return clone;
    }

    /// <summary>Resets the gallery to its configured maximum inline scale.</summary>
    public void ResetScale()
    {
        var changed = false;
        if (CanAutomaticallyChangeIsCollapsed()
            && Size == RibbonControlSize.Large
            && IsCollapsed)
        {
            SetIsCollapsedInternally(false);
            changed = true;
        }

        if (GetCurrentItemsInRow() != GalleryLayoutMath.NormalizeCount(MaxItemsInRow))
        {
            ResetCurrentItemsInRow();
            changed = true;
        }

        UpdateGalleryLayout();
        if (changed)
        {
            Scaled?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>Scrolls the requested item into view.</summary>
#if WINDOWS
    public new void ScrollIntoView(object item)
#else
    public void ScrollIntoView(object item)
#endif
    {
        if (item is FrameworkElement element)
        {
            element.StartBringIntoView();
            return;
        }

        if (item is not null
            && Items.FirstOrDefault(candidate => candidate is FrameworkElement element
                                                 && ReferenceEquals(element.DataContext, item)) is FrameworkElement container)
        {
            container.StartBringIntoView();
        }
    }

    /// <inheritdoc />
    public void OnSizePropertyChanged(RibbonControlSize previous, RibbonControlSize current)
    {
        if (CanAutomaticallyChangeIsCollapsed())
        {
            SetIsCollapsedInternally(current != RibbonControlSize.Large);
        }

        // The collapsed-button presentation depends on Size (Small uses the compact
        // horizontal icon+chevron button), so refresh the visual state when Size changes.
        UpdateVisualState();

        UpdateGalleryLayout();
    }

    /// <inheritdoc />
    public KeyTipPressedResult OnKeyTipPressed()
    {
        IsDropDownOpen = true;
        return new KeyTipPressedResult(false, true);
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
        IsDropDownOpen = false;
    }

    /// <summary>Creates a gallery item container for data items.</summary>
    protected override DependencyObject GetContainerForItemOverride() => new RibbonGalleryItem();

    /// <summary>Returns whether an item is already a gallery item container.</summary>
    protected override bool IsItemItsOwnContainerOverride(object item) => item is RibbonGalleryItem;

    /// <summary>Notifies derived controls when the live item collection changes.</summary>
    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs args)
    {
    }

    /// <summary>Notifies derived controls and subscribers when selection changes.</summary>
    protected virtual void OnSelectionChanged(SelectionChangedEventArgs args) =>
        SelectionChanged?.Invoke(this, args);

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs args)
    {
        if (!args.Handled && HandleNavigationKey(args.OriginalSource as UIElement, args))
        {
            return;
        }

        if (args.Key == Windows.System.VirtualKey.Escape && IsDropDownOpen)
        {
            IsDropDownOpen = false;
            args.Handled = true;
            return;
        }

        base.OnKeyDown(args);
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyRoutedEventArgs args)
    {
        if (args.Key == Windows.System.VirtualKey.F4)
        {
            IsDropDownOpen = !IsDropDownOpen;
            args.Handled = true;
        }
        else if (args.Key == Windows.System.VirtualKey.Escape && IsDropDownOpen)
        {
            IsDropDownOpen = false;
            args.Handled = true;
        }

        base.OnKeyUp(args);
    }

    void ISimplifiedStateControl.UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
        Size = (isSimplified ? SimplifiedSizeDefinition : SizeDefinition).GetSize(Size);
#if !WINDOWS
        UpdateVisualState();
        UpdateGalleryLayout();
#endif
    }

    void ILogicalChildSupport.AddLogicalChild(object child)
    {
    }

    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
    }

    /// <summary>Enumerates portable logical children.</summary>
    protected IEnumerator LogicalChildren
    {
        get
        {
            var children = new List<object>();
            foreach (var value in new[] { Icon, MediumIcon, LargeIcon, Header, ExpandButtonContent, Menu })
            {
                if (value is not null)
                {
                    children.Add(value);
                }
            }

            return children.GetEnumerator();
        }
    }

    private void InitializeCompatibility()
    {
        ExpandButtonContent = new FontIcon
        {
            Glyph = "\uE72A",
            FontSize = 8,
            Foreground = GetBrush("RibbonIconBrush", Microsoft.UI.Colors.Black),
        };
    }

    private void UpdateCompatibilityTemplate()
    {
        ApplyDropDownDimensions();
        ApplyCurrentFilter();
        HookAllItems();
    }

    private void OnCompatibilityItemsChanged(NotifyCollectionChangedEventArgs args)
    {
        foreach (var item in _hookedItems.Where(item => !Items.Contains(item)).ToList())
        {
            ResetRemovedItem(item);
            UnhookItem(item);
        }

        if (args.OldItems is not null)
        {
            foreach (var item in args.OldItems.OfType<UIElement>())
            {
                if (!Items.Contains(item))
                {
                    ResetRemovedItem(item);
                    UnhookItem(item);
                }
            }
        }

        if (args.NewItems is not null)
        {
            foreach (var item in args.NewItems.OfType<UIElement>())
            {
                HookItem(item);
            }
        }

        if (!_isRebuildingItemsSource)
        {
            if (SelectedItem is not null)
            {
                var selected = FindSelectionContainer(SelectedItem);
                if (selected is not null)
                {
                    var selectedIndex = Items.IndexOf(selected);
                    if (SelectedIndex != selectedIndex)
                    {
                        SelectedIndex = selectedIndex;
                    }
                }
                else
                {
                    SelectedItem = null;
                }
            }
            else
            {
                var preselectedItem = Items
                    .OfType<RibbonGalleryItem>()
                    .FirstOrDefault(item => item.IsSelected);
                if (preselectedItem is not null && !SelectItem(preselectedItem))
                {
                    ResetRemovedItem(preselectedItem);
                }
            }

            foreach (var item in Items
                         .OfType<RibbonGalleryItem>()
                         .Where(item => item.IsSelected
                                        && !ReferenceEquals(item, FindSelectionContainer(SelectedItem))))
            {
                ResetRemovedItem(item);
            }
        }

        OnItemsChanged(args);
        Scaled?.Invoke(this, EventArgs.Empty);
        ApplyCurrentFilter();

        if (_isPopupOpen && !_suppressPopupRebuild)
        {
            PreparePopupContent();
        }
    }

    private void RefreshSelectionContainerState()
    {
        var selectedContainer = FindSelectionContainer(SelectedItem);
        var selectedIndex = selectedContainer is null
            ? -1
            : Items.IndexOf(selectedContainer);
        if (SelectedIndex != selectedIndex)
        {
            SelectedIndex = selectedIndex;
        }

        _isSynchronizingSelection = true;
        try
        {
            foreach (var item in Items.OfType<RibbonGalleryItem>())
            {
                item.IsSelected = ReferenceEquals(item, selectedContainer);
            }
        }
        finally
        {
            _isSynchronizingSelection = false;
        }

        UpdateItemTabStops();
    }

    private void ResetRemovedItem(UIElement item)
    {
        if (item is not RibbonGalleryItem { IsSelected: true } galleryItem)
        {
            return;
        }

        _isSynchronizingSelection = true;
        try
        {
            galleryItem.IsSelected = false;
        }
        finally
        {
            _isSynchronizingSelection = false;
        }
    }

    private void HookAllItems()
    {
        foreach (var item in Items)
        {
            HookItem(item);
        }
    }

    private void HookItem(UIElement item)
    {
        if (item is RibbonGalleryItem ownerItem)
        {
            ownerItem.GalleryOwner = this;
        }

        if (!_hookedItems.Add(item))
        {
            return;
        }

        item.PointerPressed += OnItemPointerPressed;
        item.PointerEntered += OnItemPointerEntered;
        item.PointerExited += OnItemPointerExited;

        if (item is RibbonGalleryItem galleryItem)
        {
            _selectionTokens[galleryItem] = galleryItem.RegisterPropertyChangedCallback(
                RibbonGalleryItem.IsSelectedProperty,
                OnGalleryItemIsSelectedChanged);
        }
    }

    private void UnhookItem(UIElement item)
    {
        if (!_hookedItems.Remove(item))
        {
            return;
        }

        item.PointerPressed -= OnItemPointerPressed;
        item.PointerEntered -= OnItemPointerEntered;
        item.PointerExited -= OnItemPointerExited;

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
        }
    }

    private void OnItemPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (sender is not UIElement item)
        {
            return;
        }

        if (SelectItem(item) && IsDropDownOpen)
        {
            IsDropDownOpen = false;
        }
    }

    private void OnItemPointerEntered(object sender, PointerRoutedEventArgs args)
    {
        if (sender is RibbonGalleryItem galleryItem)
        {
            _previewedItem = galleryItem;
        }
    }

    private void OnItemPointerExited(object sender, PointerRoutedEventArgs args)
    {
        if (ReferenceEquals(sender, _previewedItem))
        {
            _previewedItem = null;
        }
    }

    private void OnGalleryItemIsSelectedChanged(DependencyObject sender, DependencyProperty property)
    {
        if (_isSynchronizingSelection || sender is not RibbonGalleryItem item)
        {
            return;
        }

        if (item.IsSelected)
        {
            if (!SelectItem(item))
            {
                _isSynchronizingSelection = true;
                item.IsSelected = false;
                _isSynchronizingSelection = false;
            }
        }
        else if (ReferenceEquals(FindSelectionContainer(SelectedItem), item))
        {
            SelectedItem = null;
        }
    }

    private void HandleSelectedItemChanged(object? oldValue, object? newValue)
    {
        var selectedContainer = newValue is null
            ? null
            : FindSelectionContainer(newValue);
        if (newValue is not null
            && (!Selectable || selectedContainer is null))
        {
            SelectedItem = null;
            return;
        }

        var newIndex = selectedContainer is null ? -1 : Items.IndexOf(selectedContainer);
        if (SelectedIndex != newIndex)
        {
            SelectedIndex = newIndex;
        }

        _isSynchronizingSelection = true;
        try
        {
            foreach (var item in Items.OfType<RibbonGalleryItem>())
            {
                item.IsSelected = ReferenceEquals(item, selectedContainer);
            }
        }
        finally
        {
            _isSynchronizingSelection = false;
        }

        UpdateItemTabStops();
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is Fluent.Automation.Peers.RibbonInRibbonGalleryAutomationPeer peer)
        {
            peer.RaiseSelectionChanged(
                FindSelectionContainer(oldValue, requireCurrentItem: false),
                selectedContainer);
        }

        var removed = oldValue is null ? Array.Empty<object>() : new[] { oldValue };
        var added = newValue is null ? Array.Empty<object>() : new[] { newValue };
        OnSelectionChanged(new SelectionChangedEventArgs(removed, added));
    }

    private void HandleSelectedIndexChanged(int index)
    {
        var container = index >= 0 && index < Items.Count ? Items[index] : null;
        var item = container is null ? null : GetSelectionValue(container);
        if (!ReferenceEquals(SelectedItem, item) && !Equals(SelectedItem, item))
        {
            SelectedItem = item;
        }
    }

    private static void OnIsDropDownOpenChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (InRibbonGallery)sender;
        if ((bool)args.NewValue)
        {
            if (!gallery._isPopupOpen)
            {
                gallery.ShowPopup();
            }

            gallery.IsSnapped = true;
            PopupService.RegisterOpenDropDown(gallery);
            gallery.DropDownOpened?.Invoke(gallery, EventArgs.Empty);
        }
        else
        {
            PopupService.UnregisterOpenDropDown(gallery);

            if (gallery._popup?.IsOpen == true)
            {
                gallery._popup.IsOpen = false;
            }

            gallery.CloseDropDownCore();
            gallery.DropDownClosed?.Invoke(gallery, EventArgs.Empty);
        }

        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(gallery)
            is Fluent.Automation.Peers.RibbonInRibbonGalleryAutomationPeer peer)
        {
            peer.RaiseIsDropDownOpenChanged(
                (bool)args.OldValue,
                (bool)args.NewValue);
        }
    }

    private void CloseDropDownCore()
    {
        // A QAT clone that borrowed its owner's gallery panel returns it to the owner in
        // OnQuickAccessCloneClosed (which runs immediately after this). Do not run the normal inline
        // move-back / reconcile here: the shared items live in the borrowed panel, and reconciling
        // them into this clone's own panel would reparent realized elements out of an unrooted panel
        // and corrupt their native peers (COMException 0x800F1000).
        if (_borrowedOwnerPanel is not null)
        {
            _isPopupOpen = false;
            CancelPreview();
            IsSnapped = IsFrozen;
            return;
        }

        if (!_isPopupOpen && _popupScroller?.Content is null)
        {
            CancelPreview();
            IsSnapped = IsFrozen;
            return;
        }

        _isPopupOpen = false;
        CancelPreview();

        // Move the single gallery panel (children intact) back into the inline scroller. As on open,
        // the shared items are never detached/re-added across the inline<->popup boundary — only the
        // one container moves — so no native peer is invalidated on the WinUI3 head. Grouped galleries
        // follow the same flat path (see PreparePopupContent).
        if (_popupScroller is not null)
        {
            _popupScroller.Content = null;
        }

        if (_galleryPanel is not null && _scrollViewer is not null)
        {
            _galleryPanel.ConfigureGrouping(null);
            if (!ReferenceEquals(_scrollViewer.Content, _galleryPanel))
            {
                DetachFromParent(_galleryPanel);
                _scrollViewer.Content = _galleryPanel;
            }

            // Now rooted inline again: restore inline column sizing and reconcile any items that were
            // added/removed while the popup was open. Both are peer-safe here because the panel is
            // rooted and every item either already lives in it or arrives parent-less.
            ConfigurePanel(_galleryPanel, MinItemsInRow, GetCurrentItemsInRow());
            ReconcileFlatPanelChildren();
            UpdateGalleryLayout();
            ApplyCurrentFilter();
        }

        IsSnapped = IsFrozen;
    }

    private void PreparePopupContent()
    {
        if (_popupScroller is null || _galleryPanel is null)
        {
            return;
        }

        // Match the authoritative WPF InRibbonGallery: a single gallery panel is moved wholesale
        // between the inline host (_scrollViewer) and the popup host (_popupScroller). The shared
        // item UIElements are NEVER detached/re-added across the inline<->popup boundary. Detaching a
        // realized element from a panel that has been removed from the visual tree corrupts its native
        // peer on the WinUI3 head, so re-hosting it throws COMException (0x800F1000) — the exact hazard
        // documented on RibbonGallery.SyncItems. Moving only the one container keeps every item's peer
        // intact on both heads.
        //
        ConfigurePanel(
            _galleryPanel,
            MinItemsInDropDownRow,
            MaxItemsInDropDownRow);
        _galleryPanel.ItemWidth = ItemWidth;
        _galleryPanel.ItemHeight = ItemHeight;

        if (!ReferenceEquals(_popupScroller.Content, _galleryPanel))
        {
            if (_scrollViewer is not null && ReferenceEquals(_scrollViewer.Content, _galleryPanel))
            {
                _scrollViewer.Content = null;
            }

            DetachFromParent(_galleryPanel);
            _popupScroller.Content = _galleryPanel;
        }

        // Ensure the panel hosts exactly the current Items. On a normal open every item is already a
        // child (no-op). It only ever *adds* elements — e.g. a Quick Access clone whose panel was never
        // rendered inline, whose items arrive parent-less — which is peer-safe (only detaching a
        // realized child from an unrooted panel corrupts peers; adding a parent-less child never does).
        ReconcileFlatPanelChildren();
        _galleryPanel.ConfigureGrouping(
            !string.IsNullOrWhiteSpace(GroupBy) || GroupByAdvanced is not null
                ? GetItemGroup
                : null);
        ApplyCurrentFilter();
    }

    // Ensures the flat (non-grouped) gallery panel hosts exactly the current Items, appending only
    // elements that are not already children and removing stale ones. Existing realized children are
    // left in place so their native peers are never invalidated on the WinUI3 head.
    private void ReconcileFlatPanelChildren()
    {
        if (_galleryPanel is null)
        {
            return;
        }

        for (var i = _galleryPanel.Children.Count - 1; i >= 0; i--)
        {
            if (!Items.Contains(_galleryPanel.Children[i]))
            {
                _galleryPanel.Children.RemoveAt(i);
            }
        }

        foreach (var item in Items)
        {
            if (_galleryPanel.Children.Contains(item))
            {
                continue;
            }

            DetachFromParent(item);
            _galleryPanel.Children.Add(item);
        }
    }

    private void RebuildPopupSupplementalContent()
    {
        if (_popupPanel is null || _popupScroller is null)
        {
            return;
        }

        _popupPanel.Children.Clear();

        if (Filters.Count > 0)
        {
            _popupFilterBar ??= new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Margin = new Thickness(4),
            };
            _popupFilterBar.Children.Clear();

            for (var index = 0; index < Filters.Count; index++)
            {
                var filter = Filters[index];
                var button = new WinUIButton
                {
                    Content = filter.Title,
                    Tag = filter,
                    MinHeight = 24,
                    Padding = new Thickness(8, 2, 8, 2),
                    FontWeight = ReferenceEquals(filter, SelectedFilter)
                        ? Microsoft.UI.Text.FontWeights.Bold
                        : Microsoft.UI.Text.FontWeights.Normal,
                };
                AutomationProperties.SetAutomationId(button, $"InRibbonGalleryFilter_{index}");
                button.Click += OnFilterButtonClick;
                _popupFilterBar.Children.Add(button);
            }

            _popupPanel.Children.Add(_popupFilterBar);
        }

        _popupPanel.Children.Add(_popupScroller);

        if (Menu is not null)
        {
            DetachFromParent(Menu);
            _popupPanel.Children.Add(Menu);
        }

        foreach (var menuItem in MenuItems)
        {
            DetachFromParent(menuItem);
            _popupPanel.Children.Add(menuItem);
        }
    }

    private void ApplyDropDownDimensions()
    {
        if (_popupResizeHost is null)
        {
            return;
        }

        _popupResizeHost.Width = DropDownWidth;
        _popupResizeHost.Height = DropDownHeight;
        var itemWidth = !double.IsNaN(ItemWidth) && !double.IsInfinity(ItemWidth) && ItemWidth > 0D
            ? ItemWidth
            : 0D;
        _popupResizeHost.MinWidth = Math.Max(0D, MinItemsInDropDownRow) * itemWidth;
        _popupResizeHost.MaxWidth = NormalizeMaximum(MaxDropDownWidth);
        _popupResizeHost.MaxHeight = NormalizeMaximum(MaxDropDownHeight);
        _popupResizeHost.ResizeMode = ResizeMode;
        _popupResizeHost.CanResizeBothDirections = ResizeMode == ContextMenuResizeMode.Both;
        _popupResizeHost.CanResizeVertical = ResizeMode is ContextMenuResizeMode.Vertical or ContextMenuResizeMode.Both;
    }

    private static double NormalizeMaximum(double value) =>
        double.IsNaN(value) || value <= 0D ? double.PositiveInfinity : value;

    private void ApplyCurrentFilter()
    {
        var allowed = GetAllowedGroups();
        foreach (var item in Items)
        {
            var group = GetItemGroup(item);
            item.Visibility = allowed is null
                              || string.IsNullOrEmpty(group)
                              || allowed.Contains(group)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        ApplyPopupGroupFilter();
        UpdateItemTabStops();
    }

    private void ApplyPopupGroupFilter()
    {
        var allowed = GetAllowedGroups();
        foreach (var (group, header, panel) in _popupGroupEntries)
        {
            var visibility = allowed is null
                             || string.IsNullOrEmpty(group)
                             || allowed.Contains(group)
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (header is not null)
            {
                header.Visibility = visibility;
            }

            panel.Visibility = visibility;
        }
    }

    private HashSet<string>? GetAllowedGroups()
    {
        var groups = SelectedFilter?.GetGroupNames();
        return groups is { Length: > 0 }
            ? new HashSet<string>(groups, StringComparer.OrdinalIgnoreCase)
            : null;
    }

    private string GetItemGroup(UIElement item)
    {
        var source = item is FrameworkElement element && element.DataContext is not null
            ? element.DataContext
            : item;

        if (GroupByAdvanced is not null)
        {
            return GroupByAdvanced(source) ?? string.Empty;
        }

        if (item is RibbonGalleryItem galleryItem && !string.IsNullOrEmpty(galleryItem.Group))
        {
            return galleryItem.Group;
        }

        if (string.IsNullOrWhiteSpace(GroupBy))
        {
            return string.Empty;
        }

        return Fluent.Helpers.PropertyValueHelper
                   .GetPublicPropertyValue(source, GroupBy)
                   ?.ToString()
               ?? string.Empty;
    }

    private void OnFiltersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        HasFilter = Filters.Count > 0;
        if (Filters.Count == 0)
        {
            SelectedFilter = null;
        }
        else if (SelectedFilter is null || !Filters.Contains(SelectedFilter))
        {
            SelectedFilter = Filters[0];
        }

        if (_isPopupOpen)
        {
            RebuildPopupSupplementalContent();
        }
    }

    private static void OnSelectedFilterChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (InRibbonGallery)sender;
        var filter = (GalleryGroupFilter?)args.NewValue;
        gallery.SelectedFilterTitle = filter?.Title ?? string.Empty;
        gallery.SelectedFilterGroups = filter?.Groups;
        gallery.ApplyCurrentFilter();

        if (gallery._isPopupOpen)
        {
            gallery.RebuildPopupSupplementalContent();
        }
    }

    private void OnFilterButtonClick(object sender, RoutedEventArgs args)
    {
        if (sender is WinUIButton { Tag: GalleryGroupFilter filter })
        {
            SelectedFilter = filter;
        }
    }

    private static void OnGalleryLayoutPropertyChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (InRibbonGallery)sender;
        gallery.UpdateGalleryLayout();
        gallery.ApplyDropDownDimensions();
        if (gallery._isPopupOpen)
        {
            gallery.PreparePopupContent();
        }
    }

    private static void OnGroupingPropertyChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (InRibbonGallery)sender;
        gallery.ApplyCurrentFilter();
        if (gallery._isPopupOpen)
        {
            gallery.PreparePopupContent();
        }
    }

    private static void OnDropDownDimensionChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((InRibbonGallery)sender).ApplyDropDownDimensions();

    private static void OnPopupSupplementalContentChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (InRibbonGallery)sender;
        if (gallery._isPopupOpen)
        {
            gallery.RebuildPopupSupplementalContent();
        }
    }

    private void CancelPreview()
    {
        if (_previewedItem?.CancelPreviewCommand?.CanExecute(_previewedItem.CommandParameter) == true)
        {
            _previewedItem.CancelPreviewCommand.Execute(_previewedItem.CommandParameter);
        }

        _previewedItem = null;
    }

    private void PreviewItemForAutomation(RibbonGalleryItem item)
    {
        _previewedItem = item;
        if (item.PreviewCommand?.CanExecute(item.CommandParameter) == true)
        {
            item.PreviewCommand.Execute(item.CommandParameter);
        }
    }

    private void CancelPreviewForAutomation() => CancelPreview();

    private void OnQuickAccessCloneOpened(object? sender, EventArgs args)
    {
        if (sender is not InRibbonGallery clone || clone._quickAccessOwner is not { } owner)
        {
            return;
        }

        owner.IsFrozen = true;
        owner.IsSnapped = true;
        owner._activeQuickAccessClone = clone;
        owner._quickAccessTransferredItems = owner.Items.ToList();

        // Mirror the owner's items into the clone's *logical* Items collection so the clone's
        // selection, filtering and public Items surface behave like the owner — WITHOUT reparenting
        // the item ELEMENTS. The elements stay children of the owner's _galleryPanel, which is
        // borrowed wholesale into the clone's popup below. Suppressing popup-rebuild + inline sync
        // guarantees that adding to Items never tries to re-home the shared elements into the clone's
        // own panel; re-homing them out of the owner's (usually unrooted, off-tab) panel is exactly
        // what corrupted native peers and threw COMException 0x800F1000. Only the event hooks move,
        // which is peer-safe because it makes no visual-tree change.
        clone._suppressPopupRebuild = true;
        try
        {
            foreach (var item in owner._quickAccessTransferredItems)
            {
                owner.UnhookItem(item);
                clone.Items.Add(item);
            }
        }
        finally
        {
            clone._suppressPopupRebuild = false;
        }

        clone.CopySourceItemMappingsFrom(owner, owner._quickAccessTransferredItems);
        clone.SelectedItem = owner.SelectedItem;
        clone.SelectedFilter = owner.SelectedFilter;
        clone.RebuildPopupSupplementalContent();

        // Borrow the owner's whole gallery panel (all realized items intact) into the clone's popup
        // host, then apply the clone's current filter to the now-shared items.
        clone.BorrowOwnerGalleryPanel(owner);
        clone.ApplyCurrentFilter();
    }

    private void SyncActiveQuickAccessClone()
    {
        if (_activeQuickAccessClone is not { } clone)
        {
            return;
        }

        var selected = clone.SelectedItem;
        clone._isRebuildingItemsSource = true;
        clone._suppressPopupRebuild = true;
        try
        {
            foreach (var item in clone.Items.ToList())
            {
                clone.UnhookItem(item);
                clone.Items.Remove(item);
            }

            clone._sourceItemByContainer.Clear();
            foreach (var item in Items)
            {
                UnhookItem(item);
                clone.Items.Add(item);
            }

            clone.CopySourceItemMappingsFrom(this, Items);
        }
        finally
        {
            clone._suppressPopupRebuild = false;
            clone._isRebuildingItemsSource = false;
        }

        var restoredSelection = clone.FindSelectionContainer(selected) is not null
            ? selected
            : FindSelectionContainer(SelectedItem) is not null
                ? SelectedItem
                : null;
        if (!ReferenceEquals(clone.SelectedItem, restoredSelection)
            && !Equals(clone.SelectedItem, restoredSelection))
        {
            clone.SelectedItem = restoredSelection;
        }
        else
        {
            clone.RefreshSelectionContainerState();
        }
        clone.RebuildPopupSupplementalContent();
        clone.ApplyCurrentFilter();
    }

    // Moves the owner's whole gallery panel (with all realized item children intact) into this
    // clone's popup host. Only the single container is reparented — never the individual items — so
    // no native peer is invalidated even when the owner's tab is not selected and its panel is
    // unrooted. Mirrors the inline<->popup container move performed by PreparePopupContent.
    private void BorrowOwnerGalleryPanel(InRibbonGallery owner)
    {
        if (_popupScroller is null || owner._galleryPanel is not { } panel)
        {
            return;
        }

        if (owner._scrollViewer is not null && ReferenceEquals(owner._scrollViewer.Content, panel))
        {
            owner._scrollViewer.Content = null;
        }

        DetachFromParent(panel);
        ConfigurePanel(panel, MinItemsInDropDownRow, MaxItemsInDropDownRow);
        panel.ConfigureGrouping(
            !string.IsNullOrWhiteSpace(GroupBy) || GroupByAdvanced is not null
                ? GetItemGroup
                : null);
        _popupScroller.Content = panel;
        _borrowedOwnerPanel = panel;
    }

    // Returns a previously borrowed owner panel to the owner's inline scroller, again moving only the
    // single container so the shared items' native peers stay intact regardless of rooted state.
    private void ReturnOwnerGalleryPanel(InRibbonGallery owner)
    {
        if (_borrowedOwnerPanel is not { } panel)
        {
            return;
        }

        _borrowedOwnerPanel = null;

        if (_popupScroller is not null && ReferenceEquals(_popupScroller.Content, panel))
        {
            _popupScroller.Content = null;
        }

        DetachFromParent(panel);
        panel.ConfigureGrouping(null);
        owner.ConfigurePanel(panel, owner.MinItemsInRow, owner.GetCurrentItemsInRow());
        if (owner._scrollViewer is not null)
        {
            owner._scrollViewer.Content = panel;
        }
    }

    private void OnQuickAccessCloneClosed(object? sender, EventArgs args)
    {
        if (sender is not InRibbonGallery clone || clone._quickAccessOwner is not { } owner)
        {
            return;
        }

        var selected = clone.SelectedItem;
        owner._activeQuickAccessClone = null;

        // Hand the borrowed panel (with the shared items still parented in it) back to the owner as a
        // single container BEFORE clearing the clone's logical Items — never reparent the elements.
        clone.ReturnOwnerGalleryPanel(owner);

        clone._isRebuildingItemsSource = true;
        clone._suppressPopupRebuild = true;
        try
        {
            foreach (var item in clone.Items.ToList())
            {
                clone.UnhookItem(item);
                clone.Items.Remove(item);
                owner.HookItem(item);
            }
        }
        finally
        {
            clone._suppressPopupRebuild = false;
            clone._isRebuildingItemsSource = false;
        }

        clone._sourceItemByContainer.Clear();
        owner.SelectedItem = owner.FindSelectionContainer(selected) is not null
            ? selected
            : null;
        owner.SelectedFilter = clone.SelectedFilter;
        owner.IsFrozen = false;
        owner.IsSnapped = false;
        owner._quickAccessTransferredItems = null;

        // The owner's panel already holds every item (it only ever moved as a whole container), so a
        // full inline resync is unnecessary and unsafe (it would reparent items); just restore filter
        // visibility on the shared items.
        owner.ApplyCurrentFilter();
    }
}
