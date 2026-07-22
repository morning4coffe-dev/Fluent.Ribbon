namespace Fluent;

/// <summary>
/// Represents a contextual tab group that visually groups related <see cref="RibbonTabItem"/> instances
/// together with a shared header and color.
/// </summary>
public partial class RibbonContextualTabGroup : Control
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonContextualTabGroup),
            new PropertyMetadata("RibbonContextualTabGroup"));

    /// <summary>
    /// Gets or sets the group header text.
    /// </summary>
    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="TabItemForeground"/> dependency property.</summary>
    public static readonly DependencyProperty TabItemForegroundProperty =
        DependencyProperty.Register(
            nameof(TabItemForeground),
            typeof(Brush),
            typeof(RibbonContextualTabGroup),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the foreground brush for tab items in this group.
    /// </summary>
    public Brush? TabItemForeground
    {
        get => (Brush?)GetValue(TabItemForegroundProperty);
        set => SetValue(TabItemForegroundProperty, value);
    }

    /// <summary>Identifies the <see cref="TabItemSelectedForeground"/> dependency property.</summary>
    public static readonly DependencyProperty TabItemSelectedForegroundProperty =
        DependencyProperty.Register(
            nameof(TabItemSelectedForeground),
            typeof(Brush),
            typeof(RibbonContextualTabGroup),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the foreground brush for the selected tab item in this group.
    /// </summary>
    public Brush? TabItemSelectedForeground
    {
        get => (Brush?)GetValue(TabItemSelectedForegroundProperty);
        set => SetValue(TabItemSelectedForegroundProperty, value);
    }

    /// <summary>Identifies the <see cref="TabItemPointerOverForeground"/> dependency property.</summary>
    public static readonly DependencyProperty TabItemPointerOverForegroundProperty =
        DependencyProperty.Register(
            nameof(TabItemPointerOverForeground),
            typeof(Brush),
            typeof(RibbonContextualTabGroup),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the foreground brush when pointer is over a tab item in this group.
    /// </summary>
    public Brush? TabItemPointerOverForeground
    {
        get => (Brush?)GetValue(TabItemPointerOverForegroundProperty);
        set => SetValue(TabItemPointerOverForegroundProperty, value);
    }

    /// <summary>Identifies the <see cref="TabItemSelectedPointerOverForeground"/> dependency property.</summary>
    public static readonly DependencyProperty TabItemSelectedPointerOverForegroundProperty =
        DependencyProperty.Register(
            nameof(TabItemSelectedPointerOverForeground),
            typeof(Brush),
            typeof(RibbonContextualTabGroup),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the foreground brush when pointer is over a selected tab item in this group.
    /// </summary>
    public Brush? TabItemSelectedPointerOverForeground
    {
        get => (Brush?)GetValue(TabItemSelectedPointerOverForegroundProperty);
        set => SetValue(TabItemSelectedPointerOverForegroundProperty, value);
    }

    /// <summary>Identifies the <see cref="InnerVisibility"/> dependency property.</summary>
    public static readonly DependencyProperty InnerVisibilityProperty =
        DependencyProperty.Register(
            nameof(InnerVisibility),
            typeof(Visibility),
            typeof(RibbonContextualTabGroup),
            new PropertyMetadata(Visibility.Visible));

    /// <summary>
    /// Gets the computed inner visibility of the group
    /// (collapsed when all child tabs are hidden, even if the group itself is visible).
    /// </summary>
    public Visibility InnerVisibility
    {
        get => (Visibility)GetValue(InnerVisibilityProperty);
        private set => SetValue(InnerVisibilityProperty, value);
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets the collection of tab items in this contextual group.
    /// </summary>
    public List<RibbonTabItem> Items { get; } = new();

    /// <summary>
    /// Gets the first visible tab item in this group.
    /// </summary>
    public RibbonTabItem? FirstVisibleItem =>
        Items.FirstOrDefault(item => item.Visibility == Visibility.Visible);

    /// <summary>
    /// Gets the first visible and enabled tab item in this group.
    /// </summary>
    public RibbonTabItem? FirstVisibleAndEnabledItem =>
        Items.FirstOrDefault(item => item.Visibility == Visibility.Visible && item.IsEnabled);

    /// <summary>
    /// Gets the last visible tab item in this group.
    /// </summary>
    public RibbonTabItem? LastVisibleItem =>
        Items.LastOrDefault(item => item.Visibility == Visibility.Visible);

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonContextualTabGroup"/> class.
    /// </summary>
    public RibbonContextualTabGroup()
    {
        DefaultStyleKey = typeof(RibbonContextualTabGroup);
        Visibility = Visibility.Collapsed;

        this.RegisterPropertyChangedCallback(
            VisibilityProperty,
            (s, e) => ((RibbonContextualTabGroup)s).UpdateInnerVisiblityAndGroupBorders());

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    #endregion

    #region Lifecycle

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        foreach (var item in Items)
        {
            AttachTabItem(item);
        }

        UpdateInnerVisiblityAndGroupBorders();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Appends a tab item to this contextual group.
    /// </summary>
    internal void AppendTabItem(RibbonTabItem item)
    {
        if (!Items.Contains(item))
        {
            Items.Add(item);
            AttachTabItem(item);
        }

        UpdateInnerVisiblityAndGroupBorders();
    }

    /// <summary>
    /// Removes a tab item from this contextual group.
    /// </summary>
    internal void RemoveTabItem(RibbonTabItem item)
    {
        DetachTabItem(item);
        Items.Remove(item);
        UpdateInnerVisiblityAndGroupBorders();
    }

    #endregion

    #region Overrides

    /// <inheritdoc />
    protected override void OnTapped(TappedRoutedEventArgs e)
    {
        base.OnTapped(e);

        var firstVisibleItem = FirstVisibleAndEnabledItem;
        if (firstVisibleItem is not null)
        {
            e.Handled = true;
            if (firstVisibleItem.TabControlParent is { IsMinimized: true } tabControl)
            {
                tabControl.IsMinimized = false;
            }

            firstVisibleItem.IsSelected = true;
        }
    }

    /// <summary>Handles the WPF-compatible primary-pointer release hook.</summary>
    protected virtual void OnMouseLeftButtonUp(PointerRoutedEventArgs e)
    {
        var firstVisibleItem = FirstVisibleAndEnabledItem;
        if (firstVisibleItem is null)
        {
            return;
        }

        if (firstVisibleItem.TabControlParent is { IsMinimized: true } tabControl)
        {
            tabControl.IsMinimized = false;
        }

        firstVisibleItem.IsSelected = true;
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!e.Handled)
        {
            OnMouseLeftButtonUp(e);
        }
    }

    #endregion

    #region Private Methods

    private void UpdateInnerVisibility()
    {
        UpdateContextualVisibilityAndBorders();
    }

    #endregion
}
