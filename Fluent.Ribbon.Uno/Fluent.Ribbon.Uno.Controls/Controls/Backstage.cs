namespace Fluent;

/// <summary>
/// Represents the Backstage view that provides a full-page overlay
/// for application-level commands like File, Save, Print, etc.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_MenuPanel, Type = typeof(StackPanel))]
[TemplatePart(Name = PART_ContentPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_BackButton, Type = typeof(Button))]
public partial class Backstage : Control
{
    private const string PART_MenuPanel = "PART_MenuPanel";
    private const string PART_ContentPresenter = "PART_ContentPresenter";
    private const string PART_BackButton = "PART_BackButton";

    private StackPanel? _menuPanel;
    private ContentPresenter? _contentPresenter;
    private Button? _backButton;

    #region Events

    /// <summary>
    /// Occurs when the IsOpen property changes.
    /// </summary>
    public event EventHandler<bool>? IsOpenChanged;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(false, OnIsOpenChanged));

    /// <summary>
    /// Gets or sets whether the backstage view is open.
    /// </summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(Backstage),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of backstage tab items and buttons.
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
            typeof(Backstage),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content of the currently selected backstage tab.
    /// </summary>
    public UIElement? SelectedContent
    {
        get => (UIElement?)GetValue(SelectedContentProperty);
        set => SetValue(SelectedContentProperty, value);
    }

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(Backstage),
            new PropertyMetadata("File"));

    /// <summary>
    /// Gets or sets the title displayed on the backstage button.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="BackstageBackground"/> dependency property.</summary>
    public static readonly DependencyProperty BackstageBackgroundProperty =
        DependencyProperty.Register(
            nameof(BackstageBackground),
            typeof(Brush),
            typeof(Backstage),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the background brush for the backstage menu area.
    /// </summary>
    public Brush? BackstageBackground
    {
        get => (Brush?)GetValue(BackstageBackgroundProperty);
        set => SetValue(BackstageBackgroundProperty, value);
    }

    /// <summary>Identifies the <see cref="CloseOnEsc"/> dependency property.</summary>
    public static readonly DependencyProperty CloseOnEscProperty =
        DependencyProperty.Register(
            nameof(CloseOnEsc),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether pressing Escape closes the backstage.
    /// </summary>
    public bool CloseOnEsc
    {
        get => (bool)GetValue(CloseOnEscProperty);
        set => SetValue(CloseOnEscProperty, value);
    }

    /// <summary>Identifies the <see cref="CanChangeIsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty CanChangeIsOpenProperty =
        DependencyProperty.Register(
            nameof(CanChangeIsOpen),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the <see cref="IsOpen"/> state can be changed.
    /// When set to <c>false</c>, the backstage cannot be opened or closed
    /// programmatically, by the back button, or by the Escape key.
    /// Useful for preventing close during critical operations like saving.
    /// </summary>
    public bool CanChangeIsOpen
    {
        get => (bool)GetValue(CanChangeIsOpenProperty);
        set => SetValue(CanChangeIsOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="HideContextTabsOnOpen"/> dependency property.</summary>
    public static readonly DependencyProperty HideContextTabsOnOpenProperty =
        DependencyProperty.Register(
            nameof(HideContextTabsOnOpen),
            typeof(bool),
            typeof(Backstage),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether contextual tabs are hidden when the backstage is open.
    /// </summary>
    public bool HideContextTabsOnOpen
    {
        get => (bool)GetValue(HideContextTabsOnOpenProperty);
        set => SetValue(HideContextTabsOnOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="IsBackButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsBackButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsBackButtonVisible),
            typeof(bool),
            typeof(Backstage),
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
    /// Initializes a new instance of the <see cref="Backstage"/> class.
    /// </summary>
    public Backstage()
    {
        DefaultStyleKey = typeof(Backstage);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;

        // Handle Escape key
        KeyDown += OnKeyDown;
    }

    #endregion

    #region Keyboard

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape && CloseOnEsc && IsOpen && CanChangeIsOpen)
        {
            IsOpen = false;
            e.Handled = true;
        }
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _menuPanel = GetTemplateChild(PART_MenuPanel) as StackPanel;
        _contentPresenter = GetTemplateChild(PART_ContentPresenter) as ContentPresenter;

        if (_backButton is not null)
        {
            _backButton.Click -= OnBackButtonClick;
        }

        _backButton = GetTemplateChild(PART_BackButton) as Button;

        if (_backButton is not null)
        {
            _backButton.Click += OnBackButtonClick;
        }

        SyncItems();
        UpdateVisualState();
    }

    #endregion

    #region Methods

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
    }

    private void SyncItems()
    {
        if (_menuPanel is null) return;

        _menuPanel.Children.Clear();
        foreach (var item in Items)
        {
            _menuPanel.Children.Add(item);

            // Auto-select first BackstageTabItem
            if (item is BackstageTabItem tabItem)
            {
                tabItem.Click -= OnTabItemClick;
                tabItem.Click += OnTabItemClick;
            }
        }

        // Select the first tab by default
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
        // Deselect all tabs
        foreach (var item in Items.OfType<BackstageTabItem>())
        {
            item.IsSelected = false;
        }

        // Select the clicked tab
        tab.IsSelected = true;
        SelectedContent = tab.Content as UIElement;
    }

    private void OnBackButtonClick(object sender, RoutedEventArgs e)
    {
        if (CanChangeIsOpen)
        {
            IsOpen = false;
        }
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Backstage backstage)
        {
            // If CanChangeIsOpen is false, coerce back to the old value
            if (!backstage.CanChangeIsOpen)
            {
                backstage.SetValue(IsOpenProperty, e.OldValue);
                return;
            }

            backstage.UpdateVisualState();
            backstage.IsOpenChanged?.Invoke(backstage, (bool)e.NewValue);

            if ((bool)e.NewValue)
            {
                // When opening, ensure we stretch to fill the available space
                backstage.StretchToPage();
                backstage.Focus(FocusState.Programmatic);
            }
        }
    }

    private void UpdateVisualState()
    {
        VisualStateManager.GoToState(this, IsOpen ? "Open" : "Closed", true);
    }

    /// <summary>
    /// Walks up the visual tree to find the page/root and stretches to fill it.
    /// </summary>
    private void StretchToPage()
    {
        // Find the root element (Page or Frame)
        FrameworkElement? root = this;
        while (root?.Parent is FrameworkElement parent)
        {
            root = parent;
        }

        if (root is not null && root != this)
        {
            // Ensure we're positioned to cover the full page
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
        }
    }

    #endregion
}
