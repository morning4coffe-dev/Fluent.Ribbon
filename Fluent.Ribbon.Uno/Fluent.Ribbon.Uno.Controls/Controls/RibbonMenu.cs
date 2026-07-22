namespace Fluent;

/// <summary>
/// Represents a simple menu control used inside ribbon dropdowns.
/// Contains a collection of <see cref="RibbonMenuItem"/> items.
/// </summary>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonMenu : MenuBase
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(RibbonMenu),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of menu items.
    /// </summary>
    public new ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    #endregion

    #region Fields

    private StackPanel? _itemsPanel;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonMenu"/> class.
    /// </summary>
    public RibbonMenu()
    {
        DefaultStyleKey = typeof(RibbonMenu);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemsPanel = GetTemplateChild("PART_ItemsPanel") as StackPanel;
        SyncItems();
    }

    #endregion

    #region Methods

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
    }

    private void SyncItems()
    {
        if (_itemsPanel is null) return;

        _itemsPanel.Children.Clear();
        foreach (var item in Items)
        {
            _itemsPanel.Children.Add(item);
        }
    }

    /// <summary>Creates the default ribbon menu-item container.</summary>
    protected override DependencyObject GetContainerForItemOverride()
    {
        return new RibbonMenuItem();
    }

    #endregion
}
