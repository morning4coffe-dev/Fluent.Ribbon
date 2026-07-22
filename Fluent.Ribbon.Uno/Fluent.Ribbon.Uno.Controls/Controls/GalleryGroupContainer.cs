namespace Fluent;

/// <summary>
/// A container for a group of gallery items with an optional header.
/// Used inside <see cref="RibbonGallery"/> to visually group items.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// Uses the portable WPF-compatible headered-items base with manual item layout.
/// </remarks>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_ItemsPanel, Type = typeof(Panel))]
[TemplatePart(Name = PART_Header, Type = typeof(ContentPresenter))]
public partial class GalleryGroupContainer : HeaderedItemsControl
{
    private const string PART_ItemsPanel = "PART_ItemsPanel";
    private const string PART_Header = "PART_Header";

    private Panel? _itemsPanel;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(GalleryGroupContainer),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the group header.
    /// </summary>
    public new object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="IsHeadered"/> dependency property.</summary>
    public static readonly DependencyProperty IsHeaderedProperty =
        DependencyProperty.Register(
            nameof(IsHeadered),
            typeof(bool),
            typeof(GalleryGroupContainer),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the header is displayed.
    /// </summary>
    public bool IsHeadered
    {
        get => (bool)GetValue(IsHeaderedProperty);
        set => SetValue(IsHeaderedProperty, value);
    }

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(GalleryGroupContainer),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the collection of items in this group.
    /// </summary>
    public new ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(GalleryGroupContainer),
            new PropertyMetadata(Orientation.Horizontal));

    /// <summary>
    /// Gets or sets the orientation of items within this group.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(GalleryGroupContainer),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets the uniform width for items.
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
            typeof(GalleryGroupContainer),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets the uniform height for items.
    /// </summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="MinItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MinItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MinItemsInRow),
            typeof(int),
            typeof(GalleryGroupContainer),
            new PropertyMetadata(0));

    /// <summary>
    /// Gets or sets the minimum number of items per row.
    /// </summary>
    public int MinItemsInRow
    {
        get => (int)GetValue(MinItemsInRowProperty);
        set => SetValue(MinItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MaxItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MaxItemsInRow),
            typeof(int),
            typeof(GalleryGroupContainer),
            new PropertyMetadata(int.MaxValue));

    /// <summary>
    /// Gets or sets the maximum number of items per row.
    /// </summary>
    public int MaxItemsInRow
    {
        get => (int)GetValue(MaxItemsInRowProperty);
        set => SetValue(MaxItemsInRowProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryGroupContainer"/> class.
    /// </summary>
    public GalleryGroupContainer()
    {
        DefaultStyleKey = typeof(GalleryGroupContainer);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemsPanel = GetTemplateChild(PART_ItemsPanel) as Panel;
        SyncItems();
    }

    #endregion

    #region Methods

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
    }

    /// <summary>Handles a change of the items-panel template.</summary>
    protected virtual void OnItemsPanelChanged(
        ItemsPanelTemplate? oldItemsPanel,
        ItemsPanelTemplate? newItemsPanel)
    {
        SyncItems();
    }

    private void SyncItems()
    {
        if (_itemsPanel is null)
        {
            return;
        }

        _itemsPanel.Children.Clear();

        foreach (var item in Items)
        {
            _itemsPanel.Children.Add(item);
        }
    }

    #endregion
}
