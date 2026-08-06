namespace Fluent;

using System.Collections;
using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents a gallery that is displayed inline within a RibbonGroupBox,
/// showing a subset of items directly in the ribbon with an expand button
/// to show all items in a popup.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_GalleryPanel, Type = typeof(UniformItemsPanel))]
[TemplatePart(Name = PART_ExpandButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_UpButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_DownButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_ScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = PART_CollapsedButton, Type = typeof(WinUIButton))]
#if WINDOWS
public partial class InRibbonGallery : ListBox, IScalableRibbonControl, IHeaderedControl
#else
public partial class InRibbonGallery : Selector, IScalableRibbonControl, IHeaderedControl
#endif
{
    private UniformItemsPanel? _galleryPanel;
    private WinUIButton? _expandButton;
    private WinUIButton? _upButton;
    private WinUIButton? _downButton;
    private WinUIButton? _collapsedButton;
    private ScrollViewer? _scrollViewer;
    private Popup? _popup;
    private ScrollViewer? _popupScroller;
    private StackPanel? _popupPanel;
    private bool _isPopupOpen;
    private bool _suppressPopupRebuild;
    private bool _isChangingIsCollapsedInternally;
    private bool _isCollapsedExplicitlySet;
    private bool _isRebuildingItemsSource;
    private int _currentItemsInRow = -1;
    private INotifyCollectionChanged? _subscribedItemsSource;
    private readonly Dictionary<UIElement, object?> _sourceItemByContainer = new();
#pragma warning disable CS0169
    private int _scrollOffset;
#pragma warning restore CS0169

    private const string PART_GalleryPanel = "PART_GalleryPanel";
    private const string PART_ExpandButton = "PART_ExpandButton";
    private const string PART_UpButton = "PART_UpButton";
    private const string PART_DownButton = "PART_DownButton";
    private const string PART_ScrollViewer = "PART_ScrollViewer";
    private const string PART_CollapsedButton = "CollapsedContent";

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the gallery header.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of gallery items.
    /// </summary>
    public new ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public new static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(InRibbonGallery),
            new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>
    /// Gets or sets the data source used to populate gallery items.
    /// </summary>
    public new IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(InRibbonGallery),
            new PropertyMetadata(double.NaN, OnGalleryLayoutPropertyChanged));

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
            typeof(InRibbonGallery),
            new PropertyMetadata(double.NaN, OnGalleryLayoutPropertyChanged));

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
            typeof(InRibbonGallery),
            new PropertyMetadata(8, OnMaxItemsInRowChanged));

    /// <summary>
    /// Gets or sets the maximum number of items in a row for inline display.
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
            typeof(InRibbonGallery),
            new PropertyMetadata(1, OnMinItemsInRowChanged));

    /// <summary>
    /// Gets or sets the minimum number of items in a row.
    /// </summary>
    public int MinItemsInRow
    {
        get => (int)GetValue(MinItemsInRowProperty);
        set => SetValue(MinItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxDropDownItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MaxDropDownItemsInRowProperty =
        MaxItemsInDropDownRowProperty;

    /// <summary>
    /// Gets or sets the maximum number of items in a row for dropdown display.
    /// </summary>
    public int MaxDropDownItemsInRow
    {
        get => (int)GetValue(MaxDropDownItemsInRowProperty);
        set => SetValue(MaxDropDownItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedItem"/> dependency property.</summary>
    public new static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(InRibbonGallery),
            new PropertyMetadata(null, OnSelectedItemChanged));

    /// <summary>
    /// Gets or sets the currently selected item.
    /// </summary>
    public new object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public new static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedIndex),
            typeof(int),
            typeof(InRibbonGallery),
            new PropertyMetadata(-1, OnSelectedIndexChanged));

    /// <summary>
    /// Gets or sets the index of the selected item.
    /// </summary>
    public new int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(false, OnIsDropDownOpenChanged));

    /// <summary>
    /// Gets or sets whether the gallery dropdown popup is open.
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty = RibbonProperties.SizeProperty;

    /// <summary>
    /// Gets or sets the ribbon control size.
    /// </summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(InRibbonGallery),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip.
    /// </summary>
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(object),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the large icon for collapsed display.
    /// </summary>
    public object? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(InRibbonGallery),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph for collapsed display.
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="MenuItems"/> dependency property.</summary>
    public static readonly DependencyProperty MenuItemsProperty =
        DependencyProperty.Register(
            nameof(MenuItems),
            typeof(ObservableCollection<UIElement>),
            typeof(InRibbonGallery),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the menu items shown below the gallery in the popup.
    /// </summary>
    public ObservableCollection<UIElement> MenuItems
    {
        get => (ObservableCollection<UIElement>)GetValue(MenuItemsProperty);
        private set => SetValue(MenuItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="Selectable"/> dependency property.</summary>
    public static readonly DependencyProperty SelectableProperty =
        DependencyProperty.Register(
            nameof(Selectable),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(true, OnSelectableChanged));

    /// <summary>
    /// Gets or sets whether items in the gallery can be selected.
    /// </summary>
    public bool Selectable
    {
        get => (bool)GetValue(SelectableProperty);
        set => SetValue(SelectableProperty, value);
    }

    /// <summary>Identifies the <see cref="CanCollapseToButton"/> dependency property.</summary>
    public static readonly DependencyProperty CanCollapseToButtonProperty =
        DependencyProperty.Register(
            nameof(CanCollapseToButton),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether this gallery can collapse to a button when scaled down.
    /// </summary>
    public bool CanCollapseToButton
    {
        get => (bool)GetValue(CanCollapseToButtonProperty);
        set => SetValue(CanCollapseToButtonProperty, value);
    }

    /// <summary>Identifies the <see cref="IsCollapsed"/> dependency property.</summary>
    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(
            nameof(IsCollapsed),
            typeof(bool),
            typeof(InRibbonGallery),
            new PropertyMetadata(false, OnIsCollapsedChanged));

    /// <summary>
    /// Gets or sets whether the gallery is collapsed to a button.
    /// </summary>
    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set
        {
            if (!_isChangingIsCollapsedInternally)
            {
                _isCollapsedExplicitlySet = true;
            }

            SetValue(IsCollapsedProperty, value);
        }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InRibbonGallery"/> class.
    /// </summary>
    public InRibbonGallery()
    {
        DefaultStyleKey = typeof(InRibbonGallery);
        IsTabStop = false;
        Items = new ObservableCollection<UIElement>();
        MenuItems = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
        Loaded += OnGalleryLoaded;
        Unloaded += OnGalleryUnloaded;
        QuickAccessHelper.AttachContextMenu(this);
        InitializeCompatibility();
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedTemplateMetadata);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _galleryPanel = GetTemplateChild(PART_GalleryPanel) as UniformItemsPanel;

        if (_expandButton is not null)
        {
            _expandButton.Click -= OnExpandButtonClick;
        }

        _expandButton = GetTemplateChild(PART_ExpandButton) as WinUIButton;
        if (_expandButton is not null)
        {
            _expandButton.Click += OnExpandButtonClick;
            Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
                _expandButton,
                RibbonLocalization.Current.Localization.OpenGalleryOptions);
        }

        if (_upButton is not null)
        {
            _upButton.Click -= OnUpButtonClick;
        }

        _upButton = GetTemplateChild(PART_UpButton) as WinUIButton;
        if (_upButton is not null)
        {
            _upButton.Click += OnUpButtonClick;
            Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
                _upButton,
                RibbonLocalization.Current.Localization.ScrollGalleryUp);
        }

        if (_downButton is not null)
        {
            _downButton.Click -= OnDownButtonClick;
        }

        _downButton = GetTemplateChild(PART_DownButton) as WinUIButton;
        if (_downButton is not null)
        {
            _downButton.Click += OnDownButtonClick;
            Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
                _downButton,
                RibbonLocalization.Current.Localization.ScrollGalleryDown);
        }

        if (_collapsedButton is not null)
        {
            _collapsedButton.Click -= OnExpandButtonClick;
        }

        _collapsedButton = GetTemplateChild(PART_CollapsedButton) as WinUIButton;
        if (_collapsedButton is not null)
        {
            _collapsedButton.Click += OnExpandButtonClick;
            Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
                _collapsedButton,
                RibbonLocalization.Current.Localization.OpenGallery);
        }

#if !WINDOWS
        if (_scrollViewer is not null)
        {
            _scrollViewer.SizeChanged -= OnInlineViewportSizeChanged;
        }
#endif

        _scrollViewer = GetTemplateChild(PART_ScrollViewer) as ScrollViewer;
#if !WINDOWS
        if (_scrollViewer is not null)
        {
            _scrollViewer.SizeChanged += OnInlineViewportSizeChanged;
        }
#endif

        RefreshLocalizedTemplateMetadata();
        SetupGalleryPanel();
        UpdateVisualState();
        UpdateCompatibilityTemplate();

        if (IsDropDownOpen)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (IsDropDownOpen)
                {
                    ShowPopup();
                }
            });
        }
    }

    #endregion

    private void RefreshLocalizedTemplateMetadata()
    {
        var localization = RibbonLocalization.Current.Localization;
        SetLocalizedAction(_expandButton, localization.OpenGalleryOptions);
        SetLocalizedAction(_upButton, localization.ScrollGalleryUp);
        SetLocalizedAction(_downButton, localization.ScrollGalleryDown);
        SetLocalizedAction(_collapsedButton, localization.OpenGallery);
    }

    private static void SetLocalizedAction(DependencyObject? action, string name)
    {
        if (action is null)
        {
            return;
        }

        Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(action, name);
        Fluent.Automation.Peers.AutomationPeerHelpers.SetToolTipIfUnsetOrGenerated(action, name);
    }

    #region IScalableRibbonControl

    /// <inheritdoc/>
    public void ScaleTo(RibbonControlSize size)
    {
        var sizeDefinition = IsSimplified ? SimplifiedSizeDefinition : SizeDefinition;
        var resolvedSize = sizeDefinition.GetSize(size);
        var previous = Size;
        Size = resolvedSize;

        if (previous != resolvedSize)
        {
            Scaled?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Reduces the gallery by decreasing MaxItemsInRow.
    /// </summary>
    public void Reduce()
    {
        var currentItemsInRow = GetCurrentItemsInRow();
        var reducedItemsInRow = GalleryLayoutMath.ReduceItemsInRow(
            currentItemsInRow,
            MinItemsInRow);
        if (reducedItemsInRow != currentItemsInRow)
        {
            _currentItemsInRow = reducedItemsInRow;
            UpdateGalleryLayout();
            Scaled?.Invoke(this, EventArgs.Empty);
        }
        else if (CanAutomaticallyChangeIsCollapsed() && !IsCollapsed)
        {
            SetIsCollapsedInternally(true);
            Scaled?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Enlarges the gallery by increasing MaxItemsInRow.
    /// </summary>
    public void Enlarge()
    {
        if (CanAutomaticallyChangeIsCollapsed()
            && IsCollapsed
            && Size == RibbonControlSize.Large)
        {
            SetIsCollapsedInternally(false);
            Scaled?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            var currentItemsInRow = GetCurrentItemsInRow();
            var enlargedItemsInRow = GalleryLayoutMath.EnlargeItemsInRow(
                currentItemsInRow,
                MaxItemsInRow);
            if (enlargedItemsInRow != currentItemsInRow)
            {
                _currentItemsInRow = enlargedItemsInRow;
                UpdateGalleryLayout();
                Scaled?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    #endregion

    #region Methods

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnCompatibilityItemsChanged(e);
        PruneSourceItemMappings();
        SyncInlineChildren();
        if (!_isRebuildingItemsSource)
        {
            SyncActiveQuickAccessClone();
        }
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InRibbonGallery gallery)
        {
            gallery.ReplaceItemsSource(
                e.OldValue as IEnumerable,
                e.NewValue as IEnumerable);
        }
    }

    private void ReplaceItemsSource(IEnumerable? oldSource, IEnumerable? newSource)
    {
        UnsubscribeItemsSource();
        RebuildItemsFromSource();

        if (newSource is INotifyCollectionChanged newNotify && IsLoaded)
        {
            SubscribeItemsSource(newNotify);
        }
    }

    private void OnGalleryLoaded(object sender, RoutedEventArgs e)
    {
        if (ItemsSource is INotifyCollectionChanged notify
            && !ReferenceEquals(_subscribedItemsSource, notify))
        {
            RebuildItemsFromSource();
            SubscribeItemsSource(notify);
        }
    }

    private void OnGalleryUnloaded(object sender, RoutedEventArgs e)
    {
        IsDropDownOpen = false;
        UnsubscribeItemsSource();
    }

    private void SubscribeItemsSource(INotifyCollectionChanged source)
    {
        if (ReferenceEquals(_subscribedItemsSource, source))
        {
            return;
        }

        UnsubscribeItemsSource();
        source.CollectionChanged += OnItemsSourceCollectionChanged;
        _subscribedItemsSource = source;
    }

    private void UnsubscribeItemsSource()
    {
        if (_subscribedItemsSource is null)
        {
            return;
        }

        _subscribedItemsSource.CollectionChanged -= OnItemsSourceCollectionChanged;
        _subscribedItemsSource = null;
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                var insertIndex = e.NewStartingIndex >= 0 ? e.NewStartingIndex : Items.Count;
                foreach (var item in e.NewItems)
                {
                    Items.Insert(insertIndex++, CreateItemContainer(item));
                }

                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                for (var index = 0; index < e.OldItems.Count; index++)
                {
                    if (e.OldStartingIndex >= 0 && e.OldStartingIndex < Items.Count)
                    {
                        Items.RemoveAt(e.OldStartingIndex);
                    }
                }

                break;
            case NotifyCollectionChangedAction.Replace:
                if (e.NewItems is not null
                    && e.NewStartingIndex >= 0
                    && e.NewStartingIndex + e.NewItems.Count <= Items.Count)
                {
                    for (var index = 0; index < e.NewItems.Count; index++)
                    {
                        Items[e.NewStartingIndex + index] =
                            CreateItemContainer(e.NewItems[index]);
                    }
                }
                else
                {
                    RebuildItemsFromSource();
                }

                break;
            case NotifyCollectionChangedAction.Move:
                if (e.OldItems?.Count == 1
                    && e.OldStartingIndex >= 0
                    && e.NewStartingIndex >= 0
                    && e.OldStartingIndex < Items.Count
                    && e.NewStartingIndex < Items.Count)
                {
                    Items.Move(e.OldStartingIndex, e.NewStartingIndex);
                }
                else
                {
                    RebuildItemsFromSource();
                }

                break;
            default:
                RebuildItemsFromSource();

                break;
        }

    }

    internal void RebuildItemsFromSource()
    {
        var selectedValue = SelectedItem;
        _isRebuildingItemsSource = true;
        try
        {
            Items.Clear();
            if (ItemsSource is not null)
            {
                foreach (var item in ItemsSource)
                {
                    Items.Add(CreateItemContainer(item));
                }
            }
        }
        finally
        {
            _isRebuildingItemsSource = false;
        }

        if (selectedValue is not null
            && FindSelectionContainer(selectedValue) is null)
        {
            SelectedItem = null;
        }
        else
        {
            RefreshSelectionContainerState();
        }

        SyncActiveQuickAccessClone();
    }

    private UIElement CreateItemContainer(object? item)
    {
        if (item is UIElement element)
        {
            _sourceItemByContainer.Remove(element);
            return element;
        }

        var container = new RibbonGalleryItem
        {
            Content = item,
            ContentTemplate = ItemTemplate,
            DataContext = item,
        };
        _sourceItemByContainer[container] = item;
        return container;
    }

    private void PruneSourceItemMappings()
    {
        foreach (var container in _sourceItemByContainer.Keys
                     .Where(container => !Items.Contains(container))
                     .ToArray())
        {
            _sourceItemByContainer.Remove(container);
        }
    }

    internal object? GetSelectionValue(UIElement container)
        => _sourceItemByContainer.TryGetValue(container, out var sourceItem)
            ? sourceItem
            : container;

    internal UIElement? FindSelectionContainer(
        object? selectionValue,
        bool requireCurrentItem = true)
    {
        if (selectionValue is UIElement element
            && (!requireCurrentItem || Items.Contains(element)))
        {
            return element;
        }

        foreach (var (container, sourceItem) in _sourceItemByContainer)
        {
            if ((!requireCurrentItem || Items.Contains(container))
                && ReferenceEquals(sourceItem, selectionValue))
            {
                return container;
            }
        }

        foreach (var (container, sourceItem) in _sourceItemByContainer)
        {
            if ((!requireCurrentItem || Items.Contains(container))
                && Equals(sourceItem, selectionValue))
            {
                return container;
            }
        }

        return null;
    }

    internal void CopySourceItemMappingsFrom(
        InRibbonGallery source,
        IEnumerable<UIElement> containers)
    {
        foreach (var container in containers)
        {
            if (source._sourceItemByContainer.TryGetValue(container, out var sourceItem))
            {
                _sourceItemByContainer[container] = sourceItem;
            }
        }
    }

    // Hosts the live gallery UIElements directly as panel children (non-virtualizing). While the
    // popup is open the items live in the popup panel, so inline syncing is skipped until it closes.
    // It is also skipped while _suppressPopupRebuild is set: that flag brackets the bulk item
    // transfers between a Quick Access clone and its owner, during which the shared elements must
    // stay parented in the owner's panel and must not be reparented here (reparenting a realized
    // element out of an unrooted panel corrupts its native peer -> COMException 0x800F1000).
    //
    // NOTE: this reconcile is deliberately SYNCHRONOUS. RibbonGroupItemsPanel force-measures this
    // gallery, so OnApplyTemplate runs inside a live measure pass and the Children.Add below can
    // throw COMException 0x800F1000 when an item's native peer was corrupted by an earlier detach
    // from an unrooted panel. Synchronously that throw is caught by the layout system, which retries
    // the measure once the element is re-rooted, so the gallery self-heals and populates. Deferring
    // the mutation to the dispatcher (as RibbonToolBarControlGroup does) does NOT help here: the
    // Add itself throws regardless of timing, and on the dispatcher the throw is uncaught, escalating
    // the benign first-chance to a fatal stowed 0xC000027B. Keep it synchronous.
    //
    // An idempotent guard (InlineHostMatchesItems) skips the reparent whenever the panel already
    // holds the current items in order, so the caught first-chance is limited to a genuine first
    // population / membership change instead of firing on every redundant re-apply or re-measure.
    private void SyncInlineChildren()
    {
        if (_galleryPanel is null)
        {
            return;
        }

        ConfigurePanel(
            _galleryPanel,
            MinItemsInRow,
            GetCurrentItemsInRow());

        if (_isPopupOpen || _suppressPopupRebuild)
        {
            return;
        }

        // Idempotent fast-path: when the panel already hosts exactly the current items in order there
        // is nothing to reparent, so skip the Clear()+re-add entirely. This avoids the throwing
        // reparent (COMException 0x800F1000) on every redundant re-apply/re-measure - the panel keeps
        // the already-realized children and only the filter visibility is refreshed below.
        if (!InlineHostMatchesItems())
        {
            _galleryPanel.Children.Clear();
            foreach (var item in Items)
            {
                DetachFromParent(item);
                _galleryPanel.Children.Add(item);
            }
        }

        ApplyCurrentFilter();
    }

    // True when _galleryPanel already holds exactly the current Items in the same order, so the
    // inline reconcile can skip the reparent. Read-only reference comparison - never mutates the
    // tree, so it cannot itself trigger the native reparent hazard.
    private bool InlineHostMatchesItems()
    {
        if (_galleryPanel is null)
        {
            return false;
        }

        var children = _galleryPanel.Children;
        if (children.Count != Items.Count)
        {
            return false;
        }

        for (var i = 0; i < children.Count; i++)
        {
            if (!ReferenceEquals(children[i], Items[i]))
            {
                return false;
            }
        }

        return true;
    }

    // Keeps the old method name as a thin wrapper so callers (OnApplyTemplate) stay unchanged.
    private void SetupGalleryPanel() => SyncInlineChildren();

    private void UpdateGalleryLayout()
    {
        if (_galleryPanel is not null)
        {
            ConfigurePanel(
                _galleryPanel,
                MinItemsInRow,
                GetCurrentItemsInRow());
        }
    }

    private void ConfigurePanel(UniformItemsPanel panel, int minItemsInRow, int maxItemsInRow)
    {
        panel.ItemWidth = ItemWidth;
        panel.ItemHeight = GetEffectiveItemHeight(panel);
        panel.MinColumns = GalleryLayoutMath.NormalizeCount(minItemsInRow);
        panel.MaxColumns = GalleryLayoutMath.NormalizeCount(maxItemsInRow);
        panel.Orientation = Orientation;
    }

    private double GetEffectiveItemHeight(UniformItemsPanel panel)
    {
#if WINDOWS
        return ItemHeight;
#else
        if (!ReferenceEquals(panel, _galleryPanel)
            || _isPopupOpen
            || !IsSimplified
            || double.IsNaN(ItemHeight)
            || _scrollViewer is not { ActualHeight: > 0 } scrollViewer)
        {
            return ItemHeight;
        }

        return CompactRibbonLayoutMath.FitInlineGalleryItemHeight(
            ItemHeight,
            scrollViewer.ActualHeight);
#endif
    }

#if !WINDOWS
    private void OnInlineViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsSimplified && !_isPopupOpen && !IsCollapsed)
        {
            UpdateGalleryLayout();
        }
    }
#endif

    private int GetCurrentItemsInRow()
    {
        if (_currentItemsInRow < 0)
        {
            _currentItemsInRow = GalleryLayoutMath.NormalizeCount(MaxItemsInRow);
        }

        return _currentItemsInRow;
    }

    private void ResetCurrentItemsInRow()
    {
        _currentItemsInRow = GalleryLayoutMath.NormalizeCount(MaxItemsInRow);
    }

    private void ClampCurrentItemsInRow()
    {
        _currentItemsInRow = GalleryLayoutMath.ClampCurrentItemsInRow(
            GetCurrentItemsInRow(),
            MinItemsInRow,
            MaxItemsInRow);
    }

    private bool CanAutomaticallyChangeIsCollapsed()
    {
        if (_isCollapsedExplicitlySet
            && ReadLocalValue(IsCollapsedProperty) == DependencyProperty.UnsetValue)
        {
            _isCollapsedExplicitlySet = false;
        }

        return CanCollapseToButton && !_isCollapsedExplicitlySet;
    }

    private void SetIsCollapsedInternally(bool value)
    {
        _isChangingIsCollapsedInternally = true;
        try
        {
            SetValue(IsCollapsedProperty, value);
        }
        finally
        {
            _isChangingIsCollapsedInternally = false;
        }
    }

    private static void OnMaxItemsInRowChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (InRibbonGallery)sender;
        gallery.ResetCurrentItemsInRow();
        OnGalleryLayoutPropertyChanged(sender, args);
    }

    private static void OnMinItemsInRowChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var gallery = (InRibbonGallery)sender;
        gallery.ClampCurrentItemsInRow();
        OnGalleryLayoutPropertyChanged(sender, args);
    }

    // A UIElement can only have a single parent; moving items between the inline and popup panels
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

    private void OnExpandButtonClick(object sender, RoutedEventArgs e)
    {
        ShowPopup();
    }

    private void OnUpButtonClick(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ChangeView(
                null,
                Math.Max(0, _scrollViewer.VerticalOffset - GetScrollStep()),
                null);
        }
    }

    private void OnDownButtonClick(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ChangeView(
                null,
                _scrollViewer.VerticalOffset + GetScrollStep(),
                null);
        }
    }

    private double GetScrollStep()
    {
        if (!double.IsNaN(ItemHeight) && !double.IsInfinity(ItemHeight) && ItemHeight > 0)
        {
            return ItemHeight;
        }

        if (_galleryPanel is not null)
        {
            foreach (var child in _galleryPanel.Children)
            {
                if (child.Visibility == Visibility.Visible && child.DesiredSize.Height > 0)
                {
                    return child.DesiredSize.Height;
                }
            }
        }

        return 16D;
    }

    private void ShowPopup()
    {
        if (_galleryPanel is null || _scrollViewer is null)
        {
            return;
        }

        EnsurePopup();

        if (_isPopupOpen && _popup?.IsOpen == true)
        {
            return;
        }

        _isPopupOpen = true;

        // Move the whole live gallery panel (a single container we own) from the inline
        // ScrollViewer into a persistent Popup, rather than churning each shared user
        // UIElement in and out of a Flyout. A Flyout tears its content subtree down on close,
        // corrupting the items' native peers on the WinUI3 head so that re-adding them throws
        // COMException (0x800F1000). Moving one container into a Popup (whose child stays alive)
        // avoids that entirely and behaves the same on the Skia head.
        //
        // PreparePopupContent performs the actual re-home. It must run while _galleryPanel is
        // still rooted in the inline ScrollViewer so that, for the grouped popup, the shared item
        // elements are detached from a live panel (detaching them from an already-unrooted panel
        // is exactly what corrupts their native peers). Do NOT null out _scrollViewer.Content here.
        PreparePopupContent();
        RebuildPopupSupplementalContent();
        ApplyDropDownDimensions();

        IsDropDownOpen = true;

        // Anchor the popup just below the gallery. Offsets are relative to the XamlRoot content,
        // matching the approach used by KeyTipService and working on both heads.
        if (_popup is not null)
        {
            _popup.XamlRoot = this.XamlRoot;

            if (this.XamlRoot?.Content is UIElement root)
            {
                try
                {
                    var point = this.TransformToVisual(root)
                        .TransformPoint(new Windows.Foundation.Point(0, this.ActualHeight));
                    _popup.HorizontalOffset = point.X;
                    _popup.VerticalOffset = point.Y;
                }
                catch
                {
                    // Keep the default placement if the transform cannot be computed yet.
                }
            }

            FlyoutShowHelper.OpenDeferred(_popup);
        }

    }

    internal void ExpandForAutomation()
    {
        if (_galleryPanel is not null && _scrollViewer is not null)
        {
            ShowPopup();
        }
        else
        {
            IsDropDownOpen = true;
        }
    }

    internal void CollapseForAutomation()
    {
        if (_popup is not null)
        {
            _popup.IsOpen = false;
        }
        else
        {
            IsDropDownOpen = false;
            _isPopupOpen = false;
        }
    }

    private void EnsurePopup()
    {
        if (_popup is not null)
        {
            return;
        }

        _popupScroller = new ScrollViewer
        {
            MaxHeight = 300,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        };

        _popupPanel = new StackPanel();
        _popupPanel.Children.Add(_popupScroller);

        _popupBorder = new Border
        {
            Background = GetBrush("RibbonBackgroundBrush", Microsoft.UI.Colors.White),
            BorderBrush = GetBrush("RibbonBorderBrush", Microsoft.UI.Colors.Gray),
            BorderThickness = new Thickness(1),
            Child = _popupPanel,
        };

        _popupResizeHost = new ResizeableContentControl
        {
            Content = _popupBorder,
        };

        _popup = new Popup
        {
            IsLightDismissEnabled = true,
            Child = _popupResizeHost,
        };
        _popup.Closed += OnPopupClosed;
    }

    private static Microsoft.UI.Xaml.Media.Brush GetBrush(string resourceKey, Windows.UI.Color fallback)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out var value)
            && value is Microsoft.UI.Xaml.Media.Brush brush)
        {
            return brush;
        }

        return new Microsoft.UI.Xaml.Media.SolidColorBrush(fallback);
    }

    private void OnPopupClosed(object? sender, object e)
    {
        if (IsDropDownOpen)
        {
            IsDropDownOpen = false;
        }
        else
        {
            CloseDropDownCore();
        }
    }

    private void UpdateVisualState()
    {
#if WINDOWS
        var displayState = CompactRibbonLayoutMath.ResolveInRibbonGalleryDisplayState(
            IsCollapsed,
            isSimplified: false,
            Size);
#else
        var displayState = CompactRibbonLayoutMath.ResolveInRibbonGalleryDisplayState(
            IsCollapsed,
            IsSimplified,
            Size);
#endif
        VisualStateManager.GoToState(this, displayState, true);
#if !WINDOWS
        VisualStateManager.GoToState(this, IsSimplified ? "Simplified" : "Classic", true);
#endif
    }

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InRibbonGallery gallery)
        {
            gallery.HandleSelectedItemChanged(e.OldValue, e.NewValue);
        }
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InRibbonGallery gallery)
        {
            gallery.HandleSelectedIndexChanged((int)e.NewValue);
        }
    }

    private static void OnSelectableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InRibbonGallery gallery && !(bool)e.NewValue)
        {
            gallery.SelectedItem = null;
        }
    }

    internal bool SelectItem(UIElement item)
    {
        if (!Selectable || !IsEnabled || !IsItemEnabled(item) || !Items.Contains(item))
        {
            return false;
        }

        SelectedItem = GetSelectionValue(item);
        return ReferenceEquals(FindSelectionContainer(SelectedItem), item);
    }

    internal bool RemoveItemFromSelection(UIElement item)
    {
        if (!IsEnabled
            || !IsItemEnabled(item)
            || !ReferenceEquals(FindSelectionContainer(SelectedItem), item))
        {
            return false;
        }

        SelectedItem = null;
        return SelectedItem is null;
    }

    internal IEnumerable<UIElement> GetAutomationItems()
    {
        if (IsCollapsed && !IsDropDownOpen)
        {
            return [];
        }

        return Items.Where(
            Fluent.Automation.Peers.AutomationPeerHelpers.IsEffectivelyVisible);
    }

    internal bool HandleGalleryItemKeyDown(UIElement source, KeyRoutedEventArgs e)
        => HandleNavigationKey(source, e);

    internal bool HandleNavigationKey(UIElement? source, KeyRoutedEventArgs e)
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
        current ??= FindSelectionContainer(SelectedItem);
        var currentIndex = current is null ? 0 : Math.Max(0, items.IndexOf(current));
        var columns = Orientation == Orientation.Vertical
            ? 1
            : Math.Max(
                1,
                IsDropDownOpen
                    ? GalleryLayoutMath.NormalizeCount(MaxItemsInDropDownRow)
                    : GetCurrentItemsInRow());
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

    internal void UpdateItemTabStops()
    {
        var focusTarget = FindSelectionContainer(SelectedItem)
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

    private static bool IsItemEnabled(UIElement item)
        => item is not Control control || control.IsEnabled;

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

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is InRibbonGallery gallery)
        {
            if (!gallery._isChangingIsCollapsedInternally)
            {
                gallery._isCollapsedExplicitlySet =
                    gallery.ReadLocalValue(IsCollapsedProperty) != DependencyProperty.UnsetValue;
            }

            gallery.UpdateVisualState();
        }
    }

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonInRibbonGalleryAutomationPeer(this);

    #endregion
}

internal static class CompactRibbonLayoutMath
{
    internal const double CompactGalleryItemHeightThreshold = 48;

    internal static double FitInlineGalleryItemHeight(
        double configuredHeight,
        double viewportHeight)
        => viewportHeight > 0 && !double.IsNaN(configuredHeight)
            ? Math.Min(configuredHeight, viewportHeight)
            : configuredHeight;

    internal static bool UsesCompactGalleryItemPadding(double actualHeight)
        => actualHeight is > 0 and <= CompactGalleryItemHeightThreshold;

    internal static string ResolveInRibbonGalleryDisplayState(
        bool isCollapsed,
        bool isSimplified,
        RibbonControlSize size)
        => !isCollapsed
            ? "Inline"
            : isSimplified
                ? "CollapsedToButtonSimplified"
                // A Small collapsed gallery (e.g. the Quick Access Toolbar clone) uses a compact
                // icon-only button; larger classic galleries keep the tall icon/header button.
                : size == RibbonControlSize.Small
                    ? "CollapsedToButtonCompact"
                    : "CollapsedToButton";
}
