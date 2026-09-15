namespace Fluent;

/// <summary>
/// Represents a status bar typically displayed at the bottom of a window.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonStatusBar : StatusBarBase
{
    private readonly Fluent.Helpers.ItemsControlBinding itemsBinding;

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
    public new ObservableCollection<UIElement> Items
    {
        get
        {
            itemsBinding?.RefreshUnnotifiedNativeItems();
            return (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        }
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
        Items = Fluent.Helpers.ItemsControlBinding.CreateItems(this);
        RightItems = new ObservableCollection<UIElement>();

        itemsBinding = new Fluent.Helpers.ItemsControlBinding(
            this, Items,
            IsItemItsOwnContainerOverride, GetContainerForItemOverride,
            PrepareContainerForItemOverride, ClearContainerForItemOverride,
            OnBoundItemsChanged);
        RightItems.CollectionChanged += OnRightItemsChanged;
        Loaded += (_, _) =>
        {
            SyncItems();
            SyncRightItems();
        };
        Unloaded += (_, _) =>
        {
            _leftPanel?.Children.Clear();
            _rightPanel?.Children.Clear();
        };
        InitializeCompatibility();
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedMetadata);
    }

    private void RefreshLocalizedMetadata()
    {
        Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
            this,
            RibbonLocalization.Current.Localization.StatusBarName);
        RebuildCustomizationMenu();
    }

    #endregion

    #region Template

    private StackPanel? _leftPanel;
    private StackPanel? _rightPanel;

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        _leftPanel?.Children.Clear();
        _rightPanel?.Children.Clear();
        base.OnApplyTemplate();

        _leftPanel = GetTemplateChild("PART_LeftPanel") as StackPanel;
        _rightPanel = GetTemplateChild("PART_RightPanel") as StackPanel;

        SyncItems();
        SyncRightItems();
    }

    #endregion

    #region Methods

    private void OnBoundItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
        OnItemsChanged(e);
    }

    private void OnRightItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncRightItems();
        OnRightItemsChangedCompatibility(e);
    }

    private void SyncItems()
    {
        if (_leftPanel is null) return;

        itemsBinding.SynchronizePanel(_leftPanel);
    }

    private void SyncRightItems()
    {
        if (_rightPanel is null) return;

        _rightPanel.Children.Clear();
        foreach (var item in RightItems)
        {
            Fluent.Helpers.ItemsControlHelper.DetachFromParent(item);
            _rightPanel.Children.Add(item);
        }
    }

    /// <summary>Returns the live status container for a source item.</summary>
    public new DependencyObject? ContainerFromItem(object item) => itemsBinding.ContainerFromItem(item);

    /// <summary>Returns the live status container at an item index.</summary>
    public new DependencyObject? ContainerFromIndex(int index) => itemsBinding.ContainerFromIndex(index);

    /// <summary>Returns the source item represented by a live status container.</summary>
    public new object? ItemFromContainer(DependencyObject container) => itemsBinding.ItemFromContainer(container);

    /// <summary>Returns the index of a live status container.</summary>
    public new int IndexFromContainer(DependencyObject container) => itemsBinding.IndexFromContainer(container);

    #endregion
}
