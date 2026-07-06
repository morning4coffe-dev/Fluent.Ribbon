namespace Fluent;

/// <summary>
/// Represents the main Ribbon control which consists of multiple tabs, each containing
/// groups of controls. The Ribbon provides an Office-like user interface.
/// </summary>
[ContentProperty(Name = nameof(Tabs))]
[TemplatePart(Name = PART_TabControl, Type = typeof(RibbonTabControl))]
[TemplatePart(Name = PART_QuickAccessToolBar, Type = typeof(QuickAccessToolBar))]
[TemplatePart(Name = PART_ContextualGroupsPanel, Type = typeof(RibbonContextualGroupsContainer))]
[TemplatePart(Name = PART_BelowRibbonQAT, Type = typeof(QuickAccessToolBar))]
[TemplatePart(Name = PART_ToolBarItemsHost, Type = typeof(Panel))]
[TemplatePart(Name = PART_Title, Type = typeof(FrameworkElement))]
public partial class Ribbon : Control
{
    private const string PART_TabControl = "PART_RibbonTabControl";
    private const string PART_QuickAccessToolBar = "PART_QuickAccessToolBar";
    private const string PART_ContextualGroupsPanel = "PART_ContextualGroupsPanel";
    private const string PART_BelowRibbonQAT = "PART_BelowRibbonQAT";
    private const string PART_ToolBarItemsHost = "PART_ToolBarItemsHost";
    private const string PART_Title = "PART_Title";

    private RibbonTabControl? _tabControl;
    private QuickAccessToolBar? _quickAccessToolBar;
    private QuickAccessToolBar? _belowRibbonQAT;
    private RibbonContextualGroupsContainer? _contextualGroupsPanel;
    private Panel? _toolBarItemsHost;
    private FrameworkElement? _titleText;
    private bool _isUpdatingQatLocation;
    private readonly KeyTipService _keyTipService;
    private readonly HashSet<RibbonTab> _visibilityHookedTabs = new();

    #region Events

    /// <summary>
    /// Occurs when the selected tab changes.
    /// </summary>
    public event EventHandler<RibbonTab?>? SelectedTabChanged;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Tabs"/> dependency property.</summary>
    public static readonly DependencyProperty TabsProperty =
        DependencyProperty.Register(
            nameof(Tabs),
            typeof(ObservableCollection<RibbonTab>),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of ribbon tabs.
    /// </summary>
    public ObservableCollection<RibbonTab> Tabs
    {
        get => (ObservableCollection<RibbonTab>)GetValue(TabsProperty);
        private set => SetValue(TabsProperty, value);
    }

    /// <summary>Identifies the <see cref="QuickAccessItems"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessItemsProperty =
        DependencyProperty.Register(
            nameof(QuickAccessItems),
            typeof(ObservableCollection<UIElement>),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of quick access toolbar items.
    /// </summary>
    public ObservableCollection<UIElement> QuickAccessItems
    {
        get => (ObservableCollection<UIElement>)GetValue(QuickAccessItemsProperty);
        private set => SetValue(QuickAccessItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="ContextualGroups"/> dependency property.</summary>
    public static readonly DependencyProperty ContextualGroupsProperty =
        DependencyProperty.Register(
            nameof(ContextualGroups),
            typeof(ObservableCollection<RibbonContextualTabGroup>),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of contextual tab groups.
    /// </summary>
    public ObservableCollection<RibbonContextualTabGroup> ContextualGroups
    {
        get => (ObservableCollection<RibbonContextualTabGroup>)GetValue(ContextualGroupsProperty);
        private set => SetValue(ContextualGroupsProperty, value);
    }

    /// <summary>Identifies the <see cref="ToolBarItems"/> dependency property.</summary>
    public static readonly DependencyProperty ToolBarItemsProperty =
        DependencyProperty.Register(
            nameof(ToolBarItems),
            typeof(ObservableCollection<UIElement>),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of items displayed on the right side of the tab strip.
    /// </summary>
    public ObservableCollection<UIElement> ToolBarItems
    {
        get => (ObservableCollection<UIElement>)GetValue(ToolBarItemsProperty);
        private set => SetValue(ToolBarItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedTab"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTabProperty =
        DependencyProperty.Register(
            nameof(SelectedTab),
            typeof(RibbonTab),
            typeof(Ribbon),
            new PropertyMetadata(null, OnSelectedTabChanged));

    /// <summary>
    /// Gets or sets the currently selected tab.
    /// </summary>
    public RibbonTab? SelectedTab
    {
        get => (RibbonTab?)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectedTabIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTabIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedTabIndex),
            typeof(int),
            typeof(Ribbon),
            new PropertyMetadata(0, OnSelectedTabIndexChanged));

    /// <summary>
    /// Gets or sets the index of the currently selected tab.
    /// </summary>
    public int SelectedTabIndex
    {
        get => (int)GetValue(SelectedTabIndexProperty);
        set => SetValue(SelectedTabIndexProperty, value);
    }

    /// <summary>Identifies the <see cref="IsMinimized"/> dependency property.</summary>
    public static readonly DependencyProperty IsMinimizedProperty =
        DependencyProperty.Register(
            nameof(IsMinimized),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false, OnIsMinimizedChanged));

    /// <summary>
    /// Gets or sets whether the ribbon is minimized.
    /// </summary>
    public bool IsMinimized
    {
        get => (bool)GetValue(IsMinimizedProperty);
        set => SetValue(IsMinimizedProperty, value);
    }

    /// <summary>Identifies the <see cref="CanMinimize"/> dependency property.</summary>
    public static readonly DependencyProperty CanMinimizeProperty =
        DependencyProperty.Register(
            nameof(CanMinimize),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the ribbon can be minimized by the user.
    /// </summary>
    public bool CanMinimize
    {
        get => (bool)GetValue(CanMinimizeProperty);
        set => SetValue(CanMinimizeProperty, value);
    }

    /// <summary>Identifies the <see cref="IsCollapsed"/> dependency property.</summary>
    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(
            nameof(IsCollapsed),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false, OnIsCollapsedChanged));

    /// <summary>
    /// Gets or sets whether the ribbon is collapsed (hidden when the window is very small).
    /// </summary>
    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsAutomaticCollapseEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsAutomaticCollapseEnabledProperty =
        DependencyProperty.Register(
            nameof(IsAutomaticCollapseEnabled),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the ribbon automatically collapses when the window is very small.
    /// </summary>
    public bool IsAutomaticCollapseEnabled
    {
        get => (bool)GetValue(IsAutomaticCollapseEnabledProperty);
        set => SetValue(IsAutomaticCollapseEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(Ribbon),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the title displayed in the ribbon title bar.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="Menu"/> dependency property.</summary>
    public static readonly DependencyProperty MenuProperty =
        DependencyProperty.Register(
            nameof(Menu),
            typeof(UIElement),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the application menu (backstage).
    /// </summary>
    public UIElement? Menu
    {
        get => (UIElement?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowQuickAccessToolBarAboveRibbon"/> dependency property.</summary>
    public static readonly DependencyProperty ShowQuickAccessToolBarAboveRibbonProperty =
        DependencyProperty.Register(
            nameof(ShowQuickAccessToolBarAboveRibbon),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnShowQATAboveRibbonChanged));

    /// <summary>
    /// Gets or sets whether the Quick Access Toolbar is shown above the ribbon (in title bar)
    /// or below the ribbon.
    /// </summary>
    public bool ShowQuickAccessToolBarAboveRibbon
    {
        get => (bool)GetValue(ShowQuickAccessToolBarAboveRibbonProperty);
        set => SetValue(ShowQuickAccessToolBarAboveRibbonProperty, value);
    }
    /// <summary>Identifies the <see cref="IsQuickAccessToolBarVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsQuickAccessToolBarVisibleProperty =
        DependencyProperty.Register(
            nameof(IsQuickAccessToolBarVisible),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnIsQATVisibleChanged));

    /// <summary>
    /// Gets or sets whether the quick access toolbar is visible.
    /// </summary>
    public bool IsQuickAccessToolBarVisible
    {
        get => (bool)GetValue(IsQuickAccessToolBarVisibleProperty);
        set => SetValue(IsQuickAccessToolBarVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false, OnIsSimplifiedChanged));

    /// <summary>
    /// Gets or sets whether the ribbon is in simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    private static void OnIsSimplifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateSimplifiedState();
        }
    }

    private void UpdateSimplifiedState()
    {
        foreach (var tab in Tabs)
        {
            foreach (var group in tab.Groups)
            {
                group.IsSimplified = IsSimplified;

                // When simplified, force all groups to Medium size so buttons show inline
                if (IsSimplified)
                {
                    group.State = RibbonGroupBoxState.Medium;
                }
                else
                {
                    group.State = RibbonGroupBoxState.Large;
                }
            }
        }

        // Adjust tab content height for simplified ribbon
        if (_tabControl is not null)
        {
            _tabControl.ContentHeight = IsSimplified ? 44 : double.NaN;
        }

        VisualStateManager.GoToState(this, IsSimplified ? "SimplifiedOn" : "SimplifiedOff", true);
    }

    /// <summary>Identifies the <see cref="AreTabHeadersVisible"/> dependency property.</summary>
    public static readonly DependencyProperty AreTabHeadersVisibleProperty =
        DependencyProperty.Register(
            nameof(AreTabHeadersVisible),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether ribbon tab headers are visible.
    /// </summary>
    public bool AreTabHeadersVisible
    {
        get => (bool)GetValue(AreTabHeadersVisibleProperty);
        set => SetValue(AreTabHeadersVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="ContentGapHeight"/> dependency property.</summary>
    public static readonly DependencyProperty ContentGapHeightProperty =
        DependencyProperty.Register(
            nameof(ContentGapHeight),
            typeof(double),
            typeof(Ribbon),
            new PropertyMetadata(1.0));

    /// <summary>
    /// Gets or sets the height of the gap between tabs and content.
    /// </summary>
    public double ContentGapHeight
    {
        get => (double)GetValue(ContentGapHeightProperty);
        set => SetValue(ContentGapHeightProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="Ribbon"/> class.
    /// </summary>
    public Ribbon()
    {
        DefaultStyleKey = typeof(Ribbon);
        Tabs = new ObservableCollection<RibbonTab>();
        QuickAccessItems = new ObservableCollection<UIElement>();
        ContextualGroups = new ObservableCollection<RibbonContextualTabGroup>();
        ToolBarItems = new ObservableCollection<UIElement>();

        Tabs.CollectionChanged += OnTabsCollectionChanged;
        ContextualGroups.CollectionChanged += OnContextualGroupsCollectionChanged;
        QuickAccessItems.CollectionChanged += OnQuickAccessItemsCollectionChanged;
        ToolBarItems.CollectionChanged += OnToolBarItemsCollectionChanged;
        _keyTipService = new KeyTipService(this);
        Loaded += OnRibbonLoaded;
        Unloaded += OnRibbonUnloaded;
        SizeChanged += OnRibbonSizeChanged;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _tabControl = GetTemplateChild(PART_TabControl) as RibbonTabControl;
        _quickAccessToolBar = GetTemplateChild(PART_QuickAccessToolBar) as QuickAccessToolBar;
        _belowRibbonQAT = GetTemplateChild(PART_BelowRibbonQAT) as QuickAccessToolBar;
        _contextualGroupsPanel = GetTemplateChild(PART_ContextualGroupsPanel) as RibbonContextualGroupsContainer;
        _toolBarItemsHost = GetTemplateChild(PART_ToolBarItemsHost) as Panel;
        _titleText = GetTemplateChild(PART_Title) as FrameworkElement;

        HookQuickAccessToolBar(_quickAccessToolBar);
        HookQuickAccessToolBar(_belowRibbonQAT);

        SyncAllTabs();
        SyncContextualGroups();
        SyncToolBarItems();
        UpdateQATPosition();
    }

    private void HookQuickAccessToolBar(QuickAccessToolBar? qat)
    {
        if (qat is null)
        {
            return;
        }

        qat.ShowAboveRibbonChanged -= OnQatShowAboveRibbonChanged;
        qat.ShowAboveRibbonChanged += OnQatShowAboveRibbonChanged;
    }

    private void OnRibbonLoaded(object sender, RoutedEventArgs e)
    {
        // Ensure tabs are synced after the control is fully loaded.
        // XAML children may be added before the template is applied.
        SyncAllTabs();
        SyncContextualGroups();
        LinkContextualTabGroups();
        SyncToolBarItems();
        SyncQuickAccessItems();

        // Re-apply simplified layout now that tabs/groups are populated.
        if (IsSimplified)
        {
            UpdateSimplifiedState();
        }

        // Hook keyboard for Alt/F10 KeyTip navigation (XamlRoot is available now).
        _keyTipService.Initialize();
    }

    private void OnRibbonUnloaded(object sender, RoutedEventArgs e)
    {
        _keyTipService.Teardown();
    }

    private void SyncAllTabs()
    {
        if (_tabControl is null) return;

        // Only sync if out of date
        if (_tabControl.TabItems.Count != Tabs.Count)
        {
            _tabControl.TabItems.Clear();
            foreach (var tab in Tabs)
            {
                _tabControl.TabItems.Add(tab);
            }
        }

        if (_tabControl.TabItems.Count > 0 && _tabControl.SelectedIndex < 0)
        {
            _tabControl.SelectedIndex = 0;
        }

        _tabControl.SelectionChanged -= OnTabControlSelectionChanged;
        _tabControl.SelectionChanged += OnTabControlSelectionChanged;

        HookTabVisibility();
        SyncSelectedTab();
    }

    #endregion

    #region Contextual Groups

    private void OnContextualGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncContextualGroups();
        LinkContextualTabGroups();
        UpdateTitleVisibility();
    }

    private void SyncContextualGroups()
    {
        if (_contextualGroupsPanel is null) return;

        _contextualGroupsPanel.Children.Clear();
        foreach (var group in ContextualGroups)
        {
            _contextualGroupsPanel.Children.Add(group);
        }
    }

    /// <summary>
    /// Links tabs to their contextual tab groups based on ContextualTabGroupName.
    /// Also sets initial tab visibility based on the group's visibility.
    /// </summary>
    private void LinkContextualTabGroups()
    {
        foreach (var tab in Tabs)
        {
            if (!string.IsNullOrEmpty(tab.ContextualTabGroupName))
            {
                var group = ContextualGroups.FirstOrDefault(g => g.Header == tab.ContextualTabGroupName);
                if (group is not null)
                {
                    tab.IsContextual = true;
                    tab.Group = group;

                    // Only add if not already in the group's items
                    if (!group.Items.Contains(tab))
                    {
                        group.AppendTabItem(tab);
                    }

                    // Set initial tab visibility to match the group
                    tab.Visibility = group.Visibility;
                }
            }
        }
    }

    #endregion

    #region Auto-Collapse

    private void OnRibbonSizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Tab positions change with width, so keep contextual-group headers aligned.
        _contextualGroupsPanel?.InvalidateArrange();

        if (IsAutomaticCollapseEnabled)
        {
            // Collapse ribbon only when the available width is very small.
            // Note: we only check width because e.NewSize.Height is the ribbon's
            // own height (~160px), not the window height, so a height check would
            // always trigger collapse.
            IsCollapsed = e.NewSize.Width < 300;
        }
    }

    #endregion

    #region Event Handlers

    private void OnTabsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_tabControl is null) return;

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is not null)
                {
                    foreach (RibbonTab tab in e.NewItems)
                    {
                        _tabControl.TabItems.Add(tab);
                    }
                }
                // Auto-select first tab
                if (_tabControl.TabItems.Count > 0 && _tabControl.SelectedIndex < 0)
                {
                    _tabControl.SelectedIndex = 0;
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is not null)
                {
                    foreach (RibbonTab tab in e.OldItems)
                    {
                        _tabControl.TabItems.Remove(tab);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Reset:
                _tabControl.TabItems.Clear();
                break;
        }

        LinkContextualTabGroups();
        HookTabVisibility();
    }

    private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_tabControl?.SelectedItem is RibbonTab tab)
        {
            SelectedTab = tab;
            SelectedTabIndex = Tabs.IndexOf(tab);
            SelectedTabChanged?.Invoke(this, tab);
        }
    }

    private static void OnSelectedTabChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.SyncSelectedTab();
        }
    }

    private static void OnSelectedTabIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon && e.NewValue is int index)
        {
            if (index >= 0 && index < ribbon.Tabs.Count)
            {
                ribbon.SelectedTab = ribbon.Tabs[index];
            }
        }
    }

    private static void OnIsMinimizedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateMinimizedState();
        }
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            VisualStateManager.GoToState(ribbon, (bool)e.NewValue ? "Collapsed" : "Expanded", true);
        }
    }

    private static void OnShowQATAboveRibbonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateQATPosition();
        }
    }

    private static void OnIsQATVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateQATPosition();
        }
    }

    private void SyncSelectedTab()
    {
        if (_tabControl is not null && SelectedTab is not null)
        {
            _tabControl.SelectedItem = SelectedTab;
        }
    }

    /// <summary>
    /// Subscribes to each tab's <see cref="UIElement.Visibility"/> so the ribbon can react when a
    /// contextual tab is hidden (e.g. its contextual group is toggled off). Registration is tracked
    /// per tab so repeated syncs don't add duplicate callbacks.
    /// </summary>
    private void HookTabVisibility()
    {
        foreach (var tab in Tabs)
        {
            if (_visibilityHookedTabs.Add(tab))
            {
                tab.RegisterPropertyChangedCallback(VisibilityProperty, OnTabVisibilityChanged);
            }
        }
    }

    private void OnTabVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        EnsureSelectedTabVisible();

        // A contextual tab appearing/disappearing shifts the tab strip, so re-align the
        // colored contextual-group headers above their tabs.
        _contextualGroupsPanel?.InvalidateArrange();

        // The centered window title and the contextual-group headers share the title bar,
        // so hide the title while any contextual header is showing to avoid them overlapping.
        UpdateTitleVisibility();
    }

    /// <summary>
    /// Hides the centered window title while any contextual-group header is visible, since both
    /// occupy the title bar and a contextual tab near the center would otherwise overlap the title.
    /// </summary>
    private void UpdateTitleVisibility()
    {
        if (_titleText is null)
        {
            return;
        }

        var anyContextualVisible = false;
        foreach (var group in ContextualGroups)
        {
            if (group.InnerVisibility == Visibility.Visible)
            {
                anyContextualVisible = true;
                break;
            }
        }

        _titleText.Visibility = anyContextualVisible ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Ensures the currently selected tab is visible. When the selected tab has been hidden
    /// (a contextual tab whose group was toggled off), the ribbon falls back to the first visible
    /// tab instead of leaving stale content with no selected header — matching WPF behaviour.
    /// </summary>
    private void EnsureSelectedTabVisible()
    {
        if (_tabControl is null)
        {
            return;
        }

        if (_tabControl.SelectedItem is RibbonTab { Visibility: Visibility.Visible })
        {
            return;
        }

        var fallback = Tabs.FirstOrDefault(t => t.Visibility == Visibility.Visible);
        if (fallback is not null)
        {
            _tabControl.SelectedItem = fallback;
        }
    }

    private void UpdateMinimizedState()
    {
        VisualStateManager.GoToState(this, IsMinimized ? "Minimized" : "Normal", true);
    }

    private void UpdateQATPosition()
    {
        var showAbove = ShowQuickAccessToolBarAboveRibbon;
        var visible = IsQuickAccessToolBarVisible;

        VisualStateManager.GoToState(this,
            visible && showAbove ? "QATAbove" :
            visible && !showAbove ? "QATBelow" :
            "QATHidden", true);

        // Keep both toolbars' own menu state aligned with the ribbon (guarded against re-entrancy).
        _isUpdatingQatLocation = true;
        if (_quickAccessToolBar is not null)
        {
            _quickAccessToolBar.ShowAboveRibbon = showAbove;
        }

        if (_belowRibbonQAT is not null)
        {
            _belowRibbonQAT.ShowAboveRibbon = showAbove;
        }
        _isUpdatingQatLocation = false;

        SyncQuickAccessItems();
    }

    private void OnQatShowAboveRibbonChanged(object? sender, bool showAbove)
    {
        if (_isUpdatingQatLocation)
        {
            return;
        }

        ShowQuickAccessToolBarAboveRibbon = showAbove;
    }

    private void OnQuickAccessItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncQuickAccessItems();
    }

    /// <summary>
    /// Mirrors <see cref="QuickAccessItems"/> into whichever quick access toolbar is currently
    /// active (above or below the ribbon), clearing the other so each element keeps a single parent.
    /// </summary>
    private void SyncQuickAccessItems()
    {
        var above = ShowQuickAccessToolBarAboveRibbon;
        var active = above ? _quickAccessToolBar : _belowRibbonQAT;
        var inactive = above ? _belowRibbonQAT : _quickAccessToolBar;

        inactive?.Items.Clear();

        if (active is null)
        {
            return;
        }

        active.Items.Clear();
        foreach (var item in QuickAccessItems)
        {
            active.Items.Add(item);
        }
    }

    private void OnToolBarItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncToolBarItems();
    }

    /// <summary>
    /// Mirrors <see cref="ToolBarItems"/> into the host panel on the right of the title bar.
    /// </summary>
    private void SyncToolBarItems()
    {
        if (_toolBarItemsHost is null)
        {
            return;
        }

        _toolBarItemsHost.Children.Clear();
        foreach (var item in ToolBarItems)
        {
            _toolBarItemsHost.Children.Add(item);
        }
    }

    /// <summary>
    /// Toggles the minimize state of the ribbon (if CanMinimize is true).
    /// </summary>
    public void ToggleMinimize()
    {
        if (CanMinimize)
        {
            IsMinimized = !IsMinimized;
        }
    }

    #endregion
}
