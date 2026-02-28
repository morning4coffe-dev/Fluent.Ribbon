namespace Fluent;

/// <summary>
/// A tab control for the Backstage view. Manages tab selection and
/// content display in a left panel + content area layout.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_ItemsPanel, Type = typeof(StackPanel))]
[TemplatePart(Name = PART_ContentPresenter, Type = typeof(ContentPresenter))]
public partial class BackstageTabControl : Control
{
    private const string PART_ItemsPanel = "PART_ItemsPanel";
    private const string PART_ContentPresenter = "PART_ContentPresenter";

    private StackPanel? _itemsPanel;
    private ContentPresenter? _contentPresenter;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of tab items.
    /// </summary>
    public ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedContent"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedContentProperty =
        DependencyProperty.Register(
            nameof(SelectedContent),
            typeof(UIElement),
            typeof(BackstageTabControl),
            new PropertyMetadata(null, OnSelectedContentChanged));

    /// <summary>
    /// Gets or sets the content of the currently selected tab.
    /// </summary>
    public UIElement? SelectedContent
    {
        get => (UIElement?)GetValue(SelectedContentProperty);
        set => SetValue(SelectedContentProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemsPanelMinWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsPanelMinWidthProperty =
        DependencyProperty.Register(
            nameof(ItemsPanelMinWidth),
            typeof(double),
            typeof(BackstageTabControl),
            new PropertyMetadata(250.0));

    /// <summary>
    /// Gets or sets the minimum width of the items panel.
    /// </summary>
    public double ItemsPanelMinWidth
    {
        get => (double)GetValue(ItemsPanelMinWidthProperty);
        set => SetValue(ItemsPanelMinWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemsPanelBackground"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsPanelBackgroundProperty =
        DependencyProperty.Register(
            nameof(ItemsPanelBackground),
            typeof(Brush),
            typeof(BackstageTabControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the background brush of the items panel.
    /// </summary>
    public Brush? ItemsPanelBackground
    {
        get => (Brush?)GetValue(ItemsPanelBackgroundProperty);
        set => SetValue(ItemsPanelBackgroundProperty, value);
    }

    /// <summary>Identifies the <see cref="IsBackButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsBackButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsBackButtonVisible),
            typeof(bool),
            typeof(BackstageTabControl),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the back button is visible.
    /// </summary>
    public bool IsBackButtonVisible
    {
        get => (bool)GetValue(IsBackButtonVisibleProperty);
        set => SetValue(IsBackButtonVisibleProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="BackstageTabControl"/> class.
    /// </summary>
    public BackstageTabControl()
    {
        DefaultStyleKey = typeof(BackstageTabControl);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemsPanel = GetTemplateChild(PART_ItemsPanel) as StackPanel;
        _contentPresenter = GetTemplateChild(PART_ContentPresenter) as ContentPresenter;

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
        if (_itemsPanel is null)
        {
            return;
        }

        _itemsPanel.Children.Clear();
        foreach (var item in Items)
        {
            _itemsPanel.Children.Add(item);

            if (item is BackstageTabItem tabItem)
            {
                tabItem.Click -= OnTabItemClick;
                tabItem.Click += OnTabItemClick;
            }
        }

        // Select first tab by default
        var firstTab = Items.OfType<BackstageTabItem>().FirstOrDefault();
        if (firstTab is not null)
        {
            SelectTab(firstTab);
        }
    }

    private void OnTabItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is BackstageTabItem tabItem)
        {
            SelectTab(tabItem);
        }
    }

    private void SelectTab(BackstageTabItem tab)
    {
        foreach (var item in Items.OfType<BackstageTabItem>())
        {
            item.IsSelected = false;
        }

        tab.IsSelected = true;
        SelectedContent = tab.Content as UIElement;
    }

    private static void OnSelectedContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BackstageTabControl control && control._contentPresenter is not null)
        {
            control._contentPresenter.Content = e.NewValue;
        }
    }

    #endregion
}
