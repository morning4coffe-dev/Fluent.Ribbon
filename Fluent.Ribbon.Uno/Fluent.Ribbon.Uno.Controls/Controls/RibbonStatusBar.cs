namespace Fluent;

/// <summary>
/// Represents a status bar typically displayed at the bottom of a window.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonStatusBar : Control
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(RibbonStatusBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of left-aligned status bar items.
    /// </summary>
    public ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="RightItems"/> dependency property.</summary>
    public static readonly DependencyProperty RightItemsProperty =
        DependencyProperty.Register(
            nameof(RightItems),
            typeof(ObservableCollection<UIElement>),
            typeof(RibbonStatusBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of right-aligned status bar items.
    /// </summary>
    public ObservableCollection<UIElement> RightItems
    {
        get => (ObservableCollection<UIElement>)GetValue(RightItemsProperty);
        private set => SetValue(RightItemsProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonStatusBar"/> class.
    /// </summary>
    public RibbonStatusBar()
    {
        DefaultStyleKey = typeof(RibbonStatusBar);
        Items = new ObservableCollection<UIElement>();
        RightItems = new ObservableCollection<UIElement>();

        Items.CollectionChanged += OnItemsChanged;
        RightItems.CollectionChanged += OnRightItemsChanged;
    }

    #endregion

    #region Template

    private StackPanel? _leftPanel;
    private StackPanel? _rightPanel;

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _leftPanel = GetTemplateChild("PART_LeftPanel") as StackPanel;
        _rightPanel = GetTemplateChild("PART_RightPanel") as StackPanel;

        SyncItems();
        SyncRightItems();
    }

    #endregion

    #region Methods

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
    }

    private void OnRightItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncRightItems();
    }

    private void SyncItems()
    {
        if (_leftPanel is null) return;

        _leftPanel.Children.Clear();
        foreach (var item in Items)
        {
            _leftPanel.Children.Add(item);
        }
    }

    private void SyncRightItems()
    {
        if (_rightPanel is null) return;

        _rightPanel.Children.Clear();
        foreach (var item in RightItems)
        {
            _rightPanel.Children.Add(item);
        }
    }

    #endregion
}
