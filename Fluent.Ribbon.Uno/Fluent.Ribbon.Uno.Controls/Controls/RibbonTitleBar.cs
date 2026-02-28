namespace Fluent;

/// <summary>
/// Represents the title bar of the ribbon, which contains the Quick Access Toolbar,
/// header, contextual tab group headers, and window commands.
/// </summary>
[TemplatePart(Name = PART_HeaderHolder, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_QuickAccessToolBarHolder, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_ContextualGroupsContainer, Type = typeof(RibbonContextualGroupsContainer))]
public partial class RibbonTitleBar : Control
{
    private const string PART_HeaderHolder = "PART_HeaderHolder";
    private const string PART_QuickAccessToolBarHolder = "PART_QuickAccessToolBarHolder";
    private const string PART_ContextualGroupsContainer = "PART_ContextualGroupsContainer";

    private ContentPresenter? _headerHolder;
    private ContentPresenter? _quickAccessToolBarHolder;
    private RibbonContextualGroupsContainer? _contextualGroupsContainer;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(RibbonTitleBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header / window title text.
    /// </summary>
    public string? Header
    {
        get => (string?)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="QuickAccessToolBar"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessToolBarProperty =
        DependencyProperty.Register(
            nameof(QuickAccessToolBar),
            typeof(UIElement),
            typeof(RibbonTitleBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the quick access toolbar.
    /// </summary>
    public UIElement? QuickAccessToolBar
    {
        get => (UIElement?)GetValue(QuickAccessToolBarProperty);
        set => SetValue(QuickAccessToolBarProperty, value);
    }

    /// <summary>Identifies the <see cref="ContextualGroups"/> dependency property.</summary>
    public static readonly DependencyProperty ContextualGroupsProperty =
        DependencyProperty.Register(
            nameof(ContextualGroups),
            typeof(ObservableCollection<RibbonContextualTabGroup>),
            typeof(RibbonTitleBar),
            new PropertyMetadata(null, OnContextualGroupsChanged));

    /// <summary>
    /// Gets or sets the collection of contextual tab groups displayed in the title bar.
    /// </summary>
    public ObservableCollection<RibbonContextualTabGroup>? ContextualGroups
    {
        get => (ObservableCollection<RibbonContextualTabGroup>?)GetValue(ContextualGroupsProperty);
        set => SetValue(ContextualGroupsProperty, value);
    }

    /// <summary>Identifies the <see cref="IsCollapsed"/> dependency property.</summary>
    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(
            nameof(IsCollapsed),
            typeof(bool),
            typeof(RibbonTitleBar),
            new PropertyMetadata(false, OnIsCollapsedChanged));

    /// <summary>
    /// Gets or sets whether the ribbon is collapsed and only the title bar is visible.
    /// </summary>
    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Identifies the <see cref="HideContextTabs"/> dependency property.</summary>
    public static readonly DependencyProperty HideContextTabsProperty =
        DependencyProperty.Register(
            nameof(HideContextTabs),
            typeof(bool),
            typeof(RibbonTitleBar),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether contextual tab headers should be hidden even if groups exist.
    /// </summary>
    public bool HideContextTabs
    {
        get => (bool)GetValue(HideContextTabsProperty);
        set => SetValue(HideContextTabsProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTitleBar"/> class.
    /// </summary>
    public RibbonTitleBar()
    {
        DefaultStyleKey = typeof(RibbonTitleBar);
        ContextualGroups = new ObservableCollection<RibbonContextualTabGroup>();
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _headerHolder = GetTemplateChild(PART_HeaderHolder) as ContentPresenter;
        _quickAccessToolBarHolder = GetTemplateChild(PART_QuickAccessToolBarHolder) as ContentPresenter;
        _contextualGroupsContainer = GetTemplateChild(PART_ContextualGroupsContainer) as RibbonContextualGroupsContainer;

        UpdateContextualGroups();
        UpdateVisualState(false);
    }

    #endregion

    #region Methods

    private static void OnContextualGroupsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonTitleBar titleBar)
        {
            if (e.OldValue is ObservableCollection<RibbonContextualTabGroup> oldCollection)
            {
                oldCollection.CollectionChanged -= titleBar.OnContextualGroupsCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<RibbonContextualTabGroup> newCollection)
            {
                newCollection.CollectionChanged += titleBar.OnContextualGroupsCollectionChanged;
            }

            titleBar.UpdateContextualGroups();
        }
    }

    private void OnContextualGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateContextualGroups();
    }

    private void UpdateContextualGroups()
    {
        if (_contextualGroupsContainer is null || ContextualGroups is null)
        {
            return;
        }

        _contextualGroupsContainer.Children.Clear();

        foreach (var group in ContextualGroups)
        {
            _contextualGroupsContainer.Children.Add(group);
        }
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonTitleBar titleBar)
        {
            titleBar.UpdateVisualState(true);
        }
    }

    private void UpdateVisualState(bool useTransitions)
    {
        VisualStateManager.GoToState(this, IsCollapsed ? "Collapsed" : "Normal", useTransitions);
    }

    /// <summary>
    /// Forces a layout pass to recalculate the position of contextual group headers.
    /// Call this when the ribbon's tab layout changes.
    /// </summary>
    public void ForceMeasure()
    {
        InvalidateMeasure();
        InvalidateArrange();
        _contextualGroupsContainer?.InvalidateMeasure();
        _contextualGroupsContainer?.InvalidateArrange();
    }

    #endregion
}
