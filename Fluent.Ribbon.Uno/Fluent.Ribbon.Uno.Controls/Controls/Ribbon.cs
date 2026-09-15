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
[TemplatePart(Name = PART_TitleBarHost, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PART_TitleBarDragRegion, Type = typeof(FrameworkElement))]
public partial class Ribbon : Control
{
    private const double SimplifiedContentHeight = 52;

    private const string PART_TabControl = "PART_RibbonTabControl";
    private const string PART_QuickAccessToolBar = "PART_QuickAccessToolBar";
    private const string PART_ContextualGroupsPanel = "PART_ContextualGroupsPanel";
    private const string PART_BelowRibbonQAT = "PART_BelowRibbonQAT";
    private const string PART_ToolBarItemsHost = "PART_ToolBarItemsHost";
    private const string PART_Title = "PART_Title";
    private const string PART_TitleBarHost = "PART_TitleBarHost";
    private const string PART_TitleBarDragRegion = "PART_TitleBarDragRegion";

    private RibbonTabControl? _tabControl;
    private QuickAccessToolBar? _quickAccessToolBar;
    private QuickAccessToolBar? _belowRibbonQAT;
    private RibbonContextualGroupsContainer? _contextualGroupsPanel;
    private Panel? _toolBarItemsHost;
    private FrameworkElement? _titleText;
    private readonly Dictionary<DependencyProperty, Binding> _tabOptionBindings = new();
    private bool _isUpdatingQatLocation;
    private readonly KeyTipService _keyTipService;
    private readonly Dictionary<RibbonTabItem, (long Visibility, long IsEnabled, long GroupName, long Group)>
        _tabPropertyCallbacks = new();
    private readonly Dictionary<RibbonContextualTabGroup, (long InnerVisibility, long Header)>
        _contextualGroupPropertyCallbacks = new();
    private bool _isSynchronizingTabs;
    private bool _tabSynchronizationPending;
    private bool _isSynchronizingSelection;
    private bool _isLinkingContextualGroups;
    private RibbonTabItem? _lastSelectedTab;
    private (RibbonTabItem? Item, int? Index)? _pendingSelection;

    // UIElement providers live exclusively in the WPF-compatible QuickAccessElements map.
    // This extension stores only providers which cannot be represented by that API's key type.
    private readonly Dictionary<IQuickAccessItemProvider, FrameworkElement> _nonVisualQuickAccessItems = new();
    private readonly Dictionary<QuickAccessMenuItem, QuickAccessMenuItemSubscription>
        _quickAccessMenuItemSubscriptions = new();
    private readonly HashSet<QuickAccessToolBar> _quickAccessCustomizationToolBars = new();
    private bool _isSynchronizingQuickAccessMenuItems;
    private bool _quickAccessMenuItemSynchronizationPending;
    private bool _isUpdatingQuickAccessMenuChecks;
    private bool _isSynchronizingQuickAccessItems;
    private bool _quickAccessSynchronizationPending;

    #region Events

    /// <summary>
    /// Occurs when the selected tab changes.
    /// </summary>
    public event SelectionChangedEventHandler? SelectedTabChanged;

    #endregion

    /// <summary>
    /// Gets the template host that can be registered as a native window title bar.
    /// </summary>
    public FrameworkElement? TitleBarHost { get; private set; }

    /// <summary>
    /// Gets the non-interactive region that can be registered for native title-bar dragging.
    /// </summary>
    public FrameworkElement? TitleBarDragRegion { get; private set; }

    internal Backstage? ActiveBackstage { get; set; }

    internal bool IsKeyTipModeActive => _keyTipService.IsActive;

    internal void RegisterKeyTipInputRoot(FrameworkElement root) => _keyTipService.RegisterInputRoot(root);

    internal void UnregisterKeyTipInputRoot(FrameworkElement root) => _keyTipService.UnregisterInputRoot(root);

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Tabs"/> dependency property.</summary>
    public static readonly DependencyProperty TabsProperty =
        DependencyProperty.Register(
            nameof(Tabs),
            typeof(ObservableCollection<RibbonTabItem>),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of ribbon tabs.
    /// </summary>
    public ObservableCollection<RibbonTabItem> Tabs
    {
        get => (ObservableCollection<RibbonTabItem>)GetValue(TabsProperty);
        private set => SetValue(TabsProperty, value);
    }

    /// <summary>Identifies the <see cref="QuickAccessToolBarItems"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessToolBarItemsProperty =
        DependencyProperty.Register(
            nameof(QuickAccessToolBarItems),
            typeof(ObservableCollection<UIElement>),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of quick access toolbar items.
    /// </summary>
    public ObservableCollection<UIElement> QuickAccessToolBarItems
    {
        get => (ObservableCollection<UIElement>)GetValue(QuickAccessToolBarItemsProperty);
        private set => SetValue(QuickAccessToolBarItemsProperty, value);
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
            typeof(RibbonTabItem),
            typeof(Ribbon),
            new PropertyMetadata(null, OnSelectedTabChanged));

    /// <summary>
    /// Gets or sets the currently selected tab.
    /// </summary>
    /// <remarks>
    /// A requested tab not yet in Tabs is retained until population resolves it.
    /// Clearing Tabs cancels an unresolved request.
    /// </remarks>
    public RibbonTabItem? SelectedTab
    {
        get => (RibbonTabItem?)GetValue(SelectedTabProperty);
        set
        {
            if (ReferenceEquals(SelectedTab, value))
            {
                RequestTabSelection(value, null);
            }
            else
            {
                SetValue(SelectedTabProperty, value);
            }
        }
    }

    /// <summary>Identifies the <see cref="SelectedTabIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTabIndexProperty =
        DependencyProperty.Register(
            nameof(SelectedTabIndex),
            typeof(int),
            typeof(Ribbon),
            new PropertyMetadata(-1, OnSelectedTabIndexChanged));

    /// <summary>
    /// Gets or sets the index of the currently selected tab.
    /// </summary>
    /// <remarks>A nonnegative index can be requested before the corresponding tab is populated.</remarks>
    public int SelectedTabIndex
    {
        get => (int)GetValue(SelectedTabIndexProperty);
        set
        {
            if (SelectedTabIndex == value)
            {
                RequestTabSelection(null, value);
            }
            else
            {
                SetValue(SelectedTabIndexProperty, value);
            }
        }
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
            typeof(FrameworkElement),
            typeof(Ribbon),
            new PropertyMetadata(null, OnMenuChanged));

    /// <summary>
    /// Gets or sets the application menu (backstage).
    /// </summary>
    public FrameworkElement? Menu
    {
        get => (FrameworkElement?)GetValue(MenuProperty);
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
            ribbon.SaveStateTemporaryIfAvailable();
        }
    }

    private void UpdateSimplifiedState()
    {
        foreach (var tab in Tabs)
        {
            tab.UpdateSimplifiedState(IsSimplified);
            tab.SetContentHeight(IsSimplified ? SimplifiedContentHeight : ContentHeight);
        }

        // Adjust tab content height for simplified ribbon
        if (_tabControl is not null)
        {
            _tabControl.IsSimplified = IsSimplified;
            _tabControl.ContentHeight = IsSimplified ? SimplifiedContentHeight : ContentHeight;
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
    /// <remarks>Hides only the header strip; selected tab content and tab selection remain available.</remarks>
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
        Tabs = new ObservableCollection<RibbonTabItem>();
        QuickAccessToolBarItems = new ObservableCollection<UIElement>();
        ContextualGroups = new ObservableCollection<RibbonContextualTabGroup>();
        ToolBarItems = new ObservableCollection<UIElement>();

        Tabs.CollectionChanged += OnTabsCollectionChanged;
        ContextualGroups.CollectionChanged += OnContextualGroupsCollectionChanged;
        QuickAccessToolBarItems.CollectionChanged += OnQuickAccessItemsCollectionChanged;
        ToolBarItems.CollectionChanged += OnToolBarItemsCollectionChanged;
        _keyTipService = new KeyTipService(this);
        Loaded += OnRibbonLoaded;
        Unloaded += OnRibbonUnloaded;
        SizeChanged += OnRibbonSizeChanged;
        InitializeCompatibility();
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedAutomationName);
    }

    private void RefreshLocalizedAutomationName()
    {
        Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
            this,
            RibbonLocalization.Current.Localization.RibbonName);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        if (_tabControl is not null)
        {
            ReleaseTabOptionBindings();
            _tabControl.SelectionChanged -= OnTabControlSelectionChanged;
            _tabControl.RequestBackstageClose -= OnRequestBackstageClose;
            _tabControl.TabItems.Clear();
        }

        if (_contextualGroupsPanel is not null)
        {
            _contextualGroupsPanel.Children.Clear();
        }

        UnhookQuickAccessToolBar(_quickAccessToolBar);
        UnhookQuickAccessToolBar(_belowRibbonQAT);
        base.OnApplyTemplate();

        _tabControl = GetTemplateChild(PART_TabControl) as RibbonTabControl;
        _quickAccessToolBar = GetTemplateChild(PART_QuickAccessToolBar) as QuickAccessToolBar;
        _belowRibbonQAT = GetTemplateChild(PART_BelowRibbonQAT) as QuickAccessToolBar;
        _contextualGroupsPanel = GetTemplateChild(PART_ContextualGroupsPanel) as RibbonContextualGroupsContainer;
        _toolBarItemsHost = GetTemplateChild(PART_ToolBarItemsHost) as Panel;
        _titleText = GetTemplateChild(PART_Title) as FrameworkElement;
        TitleBarHost = GetTemplateChild(PART_TitleBarHost) as FrameworkElement;
        TitleBarDragRegion = GetTemplateChild(PART_TitleBarDragRegion) as FrameworkElement;
#if !WINDOWS
        if (_contextualGroupsPanel is not null)
        {
            // Keep contextual headers out of the right-side title-bar action column.
            Grid.SetColumnSpan(_contextualGroupsPanel, 3);
        }
#endif
        ImportQuickAccessCustomizationItems();
        UpdateCompatibilityTemplateParts();
        UpdateMenu();

        HookQuickAccessToolBar(_quickAccessToolBar);
        HookQuickAccessToolBar(_belowRibbonQAT);

        if (_tabControl is not null)
        {
            _tabControl.SelectionChanged += OnTabControlSelectionChanged;
            _tabControl.RequestBackstageClose += OnRequestBackstageClose;
        }

        SyncAllTabs();
        SyncContextualGroups();
        SyncToolBarItems();
        UpdateQATPosition();
        SyncQuickAccessItems();
        UpdateStartScreenPresentation();
    }

    private void BindTabControlOptions()
    {
        if (_tabControl is not { } tabControl)
        {
            return;
        }

        BindOption(RibbonTabControl.AreTabHeadersVisibleProperty, nameof(AreTabHeadersVisible));
        BindOption(RibbonTabControl.IsDisplayOptionsButtonVisibleProperty, nameof(IsDisplayOptionsButtonVisible));
        BindOption(RibbonTabControl.CanMinimizeProperty, nameof(CanMinimize));
        BindOption(RibbonTabControl.CanUseSimplifiedProperty, nameof(CanUseSimplified));

        void BindOption(DependencyProperty property, string sourceProperty)
        {
            // Custom templates can supply a local value or even an unresolved binding.
            // Only install the ribbon's defaults where the part has no authored value.
            if (tabControl.ReadLocalValue(property) != DependencyProperty.UnsetValue
                || tabControl.GetBindingExpression(property) is not null)
            {
                return;
            }

            var binding = new Binding
            {
                Source = this,
                Path = new PropertyPath(sourceProperty),
                Mode = BindingMode.OneWay,
            };
            _tabOptionBindings[property] = binding;
            tabControl.SetBinding(property, binding);
        }
    }

    private void ReleaseTabOptionBindings()
    {
        foreach (var (property, binding) in _tabOptionBindings)
        {
            if (_tabControl is { } tabControl
                && ReferenceEquals(tabControl.GetBindingExpression(property)?.ParentBinding, binding))
            {
                tabControl.ClearValue(property);
            }
        }

        _tabOptionBindings.Clear();
    }

    private void HookQuickAccessToolBar(QuickAccessToolBar? qat)
    {
        if (qat is null)
        {
            return;
        }

        qat.ShowAboveRibbonChanged -= OnQatShowAboveRibbonChanged;
        qat.ShowAboveRibbonChanged += OnQatShowAboveRibbonChanged;
        qat.ItemsChanged -= OnQuickAccessToolBarItemsChanged;
        qat.ItemsChanged += OnQuickAccessToolBarItemsChanged;
    }

    private void UnhookQuickAccessToolBar(QuickAccessToolBar? qat)
    {
        if (qat is null)
        {
            return;
        }

        qat.ShowAboveRibbonChanged -= OnQatShowAboveRibbonChanged;
        qat.ItemsChanged -= OnQuickAccessToolBarItemsChanged;
        var wasSynchronizing = _isSynchronizingQuickAccessItems;
        _isSynchronizingQuickAccessItems = true;
        try
        {
            qat.Items.Clear();
            qat.QuickAccessItems.Clear();
            qat.DetachCustomizationRibbon();
        }
        finally
        {
            _isSynchronizingQuickAccessItems = wasSynchronizing;
        }
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
        UpdateStartScreenPresentation();

        // A contextual group authored visible in XAML links its tabs here; make sure the title
        // reflects that immediately so it doesn't overlap the header on first render.
        UpdateTitleVisibility();

        // Re-apply simplified layout now that tabs/groups are populated.
        if (IsSimplified)
        {
            UpdateSimplifiedState();
        }

        // Re-apply minimized state so the tab control picks it up once the template is available.
        if (IsMinimized)
        {
            UpdateMinimizedState();
        }

        // Hook keyboard for Alt/F10 KeyTip navigation (XamlRoot is available now).
        if (IsKeyTipHandlingEnabled)
        {
            _keyTipService.Initialize();
        }
    }

    private void OnRibbonUnloaded(object sender, RoutedEventArgs e)
    {
        QuickAccessHelper.CloseDefaultContextMenu(this);
        if (_tabControl is not null)
        {
            _tabControl.IsDropDownOpen = false;
        }

        CloseStartScreenPresentation();
        _keyTipService.Teardown();
    }

    private void OnRequestBackstageClose(object? sender, EventArgs args)
    {
        ActiveBackstage?.SetIsOpen(false);
        StartScreen?.SetIsOpen(false);
    }

    private void SyncAllTabs(NotifyCollectionChangedEventArgs? change = null)
    {
        if (_isSynchronizingTabs)
        {
            _tabSynchronizationPending = true;
            return;
        }

        do
        {
            _tabSynchronizationPending = false;
            _isSynchronizingTabs = true;
            try
            {
                var selectedTab = SelectedTab;
                if (_tabControl is { IsDropDownOpen: true }
                    && (change?.Action == NotifyCollectionChangedAction.Reset
                        || (selectedTab is not null && !Tabs.Contains(selectedTab))))
                {
                    _tabControl.IsDropDownOpen = false;
                }

                HookTabVisibility();
                LinkContextualTabGroups();

                if (_tabControl is not null)
                {
                    var items = _tabControl.TabItems;
                    for (var index = items.Count - 1; index >= 0; index--)
                    {
                        if (items[index] is not RibbonTabItem tab || !Tabs.Contains(tab))
                        {
                            items.RemoveAt(index);
                        }
                    }

                    for (var index = 0; index < Tabs.Count; index++)
                    {
                        var tab = Tabs[index];
                        tab.SetContentHeight(_tabControl.ContentHeight);
                        if (index < items.Count && ReferenceEquals(items[index], tab))
                        {
                            continue;
                        }

                        var previousIndex = items.IndexOf(tab);
                        if (previousIndex >= 0)
                        {
                            if (ReferenceEquals(tab, selectedTab))
                            {
                                // Move surrounding headers rather than detach a surviving selected tab.
                                for (var move = index; move < previousIndex; move++)
                                {
                                    var preceding = items[index];
                                    items.RemoveAt(index);
                                    items.Insert(previousIndex, preceding);
                                }

                                continue;
                            }

                            items.RemoveAt(previousIndex);
                        }

                        items.Insert(index, tab);
                    }

                    while (items.Count > Tabs.Count)
                    {
                        items.RemoveAt(items.Count - 1);
                    }
                }

                _tabControl?.NotifyTabItemsChanged(
                    change ?? new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                SynchronizeSelectedTab(SelectedTab, selectFallback: true);
            }
            finally
            {
                _isSynchronizingTabs = false;
            }
        }
        while (_tabSynchronizationPending);
    }

    #endregion

    #region Contextual Groups

    private void OnContextualGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncContextualGroups();
    }

    private void SyncContextualGroups()
    {
        foreach (var (group, callbacks) in _contextualGroupPropertyCallbacks.ToArray())
        {
            if (ContextualGroups.Contains(group))
            {
                continue;
            }

            group.UnregisterPropertyChangedCallback(
                RibbonContextualTabGroup.InnerVisibilityProperty, callbacks.InnerVisibility);
            group.UnregisterPropertyChangedCallback(
                RibbonContextualTabGroup.HeaderProperty, callbacks.Header);
            _contextualGroupPropertyCallbacks.Remove(group);
            _contextualGroupsPanel?.ForgetGroupPosition(group);
            group.SynchronizeTabItems([]);
        }

        _contextualGroupsPanel?.Children.Clear();
        foreach (var group in ContextualGroups)
        {
            _contextualGroupsPanel?.Children.Add(group);
            if (!_contextualGroupPropertyCallbacks.ContainsKey(group))
            {
                _contextualGroupPropertyCallbacks.Add(group, (
                    group.RegisterPropertyChangedCallback(
                        RibbonContextualTabGroup.InnerVisibilityProperty,
                        OnContextualGroupInnerVisibilityChanged),
                    group.RegisterPropertyChangedCallback(
                        RibbonContextualTabGroup.HeaderProperty,
                        OnContextualGroupHeaderChanged)));
            }
        }

        LinkContextualTabGroups();
    }

    private void OnContextualGroupHeaderChanged(DependencyObject sender, DependencyProperty property) =>
        LinkContextualTabGroups();

    private void OnContextualGroupInnerVisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        // A contextual header appearing or disappearing changes whether the centered title would
        // overlap it, and shifts the header row, so refresh the title and re-align the headers.
        UpdateTitleVisibility();
        _contextualGroupsPanel?.InvalidateArrange();
    }

    /// <summary>
    /// Resolves Uno name links without replacing authored Group values or bindings.
    /// Group membership follows the current ribbon tab order, not assignment order.
    /// </summary>
    private void LinkContextualTabGroups()
    {
        if (_isLinkingContextualGroups)
        {
            return;
        }

        _isLinkingContextualGroups = true;
        try
        {
            foreach (var tab in Tabs)
            {
                tab.UpdateContextualGroupLink();
            }

            foreach (var group in ContextualGroups)
            {
                group.SynchronizeTabItems(
                    Tabs.Where(tab => ReferenceEquals(tab.ActiveContextualGroup, group)).ToArray());
            }
        }
        finally
        {
            _isLinkingContextualGroups = false;
        }

        EnsureSelectedTabVisible();
        UpdateTitleVisibility();
        _contextualGroupsPanel?.InvalidateArrange();
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
        if (e.Action == NotifyCollectionChangedAction.Reset
            || (_pendingSelection?.Item is { } requestedTab && e.OldItems?.Contains(requestedTab) == true))
        {
            _pendingSelection = null;
        }

        SyncAllTabs(e);
    }

    private void OnTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isSynchronizingTabs && !_isSynchronizingSelection)
        {
            _pendingSelection = null;
            SynchronizeSelectedTab(_tabControl?.SelectedItem as RibbonTabItem, selectFallback: true);
        }
    }

    private static void OnSelectedTabChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Ribbon)d).RequestTabSelection((RibbonTabItem?)e.NewValue, null);
    }

    private static void OnMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateMenu();
        }
    }

    private static void OnSelectedTabIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Ribbon)d).RequestTabSelection(null, (int)e.NewValue);
    }

    private static void OnIsMinimizedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateMinimizedState();
            ribbon.RaiseIsMinimizedChanged(e);
            ribbon.SaveStateTemporaryIfAvailable();
            if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(ribbon)
                is Fluent.Automation.Peers.RibbonAutomationPeer peer)
            {
                peer.RaiseIsMinimizedChanged((bool)e.OldValue, (bool)e.NewValue);
            }
        }
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            VisualStateManager.GoToState(ribbon, (bool)e.NewValue ? "Collapsed" : "Expanded", true);
            ribbon.RaiseIsCollapsedChanged(e);
        }
    }

    private static void OnShowQATAboveRibbonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateQATPosition();
            ribbon.UpdateCompatibilityQatSurface();
            ribbon.SaveStateTemporaryIfAvailable();
        }
    }

    private static void OnIsQATVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Ribbon ribbon)
        {
            ribbon.UpdateQATPosition();
            ribbon.RefreshQuickAccessOptions();
        }
    }

    private void RequestTabSelection(RibbonTabItem? tab, int? index)
    {
        if (_isSynchronizingSelection)
        {
            return;
        }

        _pendingSelection = (tab, index);
        if (_isSynchronizingTabs)
        {
            _tabSynchronizationPending = true;
            return;
        }

        SynchronizeSelectedTab(tab, selectFallback: tab is not null);
    }

    private void SynchronizeSelectedTab(RibbonTabItem? preferredTab, bool selectFallback)
    {
        if (_isSynchronizingSelection)
        {
            return;
        }

        var preserveRequestedItem = false;
        var preserveRequestedIndex = false;
        // A provisional fallback must not overwrite the requested value or update its binding source.
        if (_pendingSelection is { } request)
        {
            if (request.Index is { } requestedIndex)
            {
                preserveRequestedIndex = requestedIndex >= 0 && requestedIndex >= Tabs.Count;
                preferredTab = requestedIndex >= 0 && requestedIndex < Tabs.Count
                    ? Tabs[requestedIndex]
                    : null;
            }
            else
            {
                preserveRequestedItem = request.Item is not null && !Tabs.Contains(request.Item);
                preferredTab = preserveRequestedItem ? null : request.Item;
            }

            selectFallback = preferredTab is not null;
            if (!preserveRequestedItem && !preserveRequestedIndex)
            {
                _pendingSelection = null;
            }
        }

        var selectedTab = preferredTab is { Visibility: Visibility.Visible, IsEnabled: true }
                          && Tabs.Contains(preferredTab)
            ? preferredTab
            : selectFallback
                ? Tabs.FirstOrDefault(tab => tab.Visibility == Visibility.Visible && tab.IsEnabled)
                : null;
        var oldTab = _lastSelectedTab;
        var index = selectedTab is null ? -1 : Tabs.IndexOf(selectedTab);

        _isSynchronizingSelection = true;
        try
        {
            if (!ReferenceEquals(oldTab, selectedTab))
            {
                _tabControl?.PrepareSelectionPresentation();
            }

            if (oldTab is not null && !ReferenceEquals(oldTab, selectedTab))
            {
                oldTab.IsSelected = false;
            }

            if (_tabControl is not null)
            {
                _tabControl.SelectedItem = selectedTab;
                _tabControl.SelectedIndex = index;
                _tabControl.RefreshSelectedContent();
            }

            foreach (var tab in Tabs)
            {
                tab.IsSelected = ReferenceEquals(tab, selectedTab);
            }

            if (!preserveRequestedItem && !ReferenceEquals(SelectedTab, selectedTab))
            {
                SelectedTab = selectedTab;
            }

            if (!preserveRequestedIndex && SelectedTabIndex != index)
            {
                SelectedTabIndex = index;
            }

            _lastSelectedTab = selectedTab;
        }
        finally
        {
            _isSynchronizingSelection = false;
        }

        if (!ReferenceEquals(oldTab, selectedTab))
        {
            SelectedTabChanged?.Invoke(
                this,
                new SelectionChangedEventArgs(
                    oldTab is null ? Array.Empty<object>() : [oldTab],
                    selectedTab is null ? Array.Empty<object>() : [selectedTab]));
        }
    }

    /// <summary>
    /// Observes tab eligibility and contextual linkage only while a tab belongs to this ribbon.
    /// </summary>
    private void HookTabVisibility()
    {
        foreach (var (tab, callbacks) in _tabPropertyCallbacks.ToArray())
        {
            if (Tabs.Contains(tab))
            {
                continue;
            }

            tab.UnregisterPropertyChangedCallback(VisibilityProperty, callbacks.Visibility);
            tab.UnregisterPropertyChangedCallback(IsEnabledProperty, callbacks.IsEnabled);
            tab.UnregisterPropertyChangedCallback(RibbonTabItem.ContextualTabGroupNameProperty, callbacks.GroupName);
            tab.UnregisterPropertyChangedCallback(RibbonTabItem.GroupProperty, callbacks.Group);
            _tabPropertyCallbacks.Remove(tab);
            tab.DetachFromRibbon(this);
            tab.IsSelected = false;
        }

        foreach (var tab in Tabs)
        {
            if (!_tabPropertyCallbacks.ContainsKey(tab))
            {
                tab.AttachToRibbon(this);
                tab.UpdateSimplifiedState(IsSimplified);
                tab.SetContentHeight(IsSimplified ? SimplifiedContentHeight : ContentHeight);
                _tabPropertyCallbacks.Add(tab, (
                    tab.RegisterPropertyChangedCallback(VisibilityProperty, OnTabVisibilityChanged),
                    tab.RegisterPropertyChangedCallback(IsEnabledProperty, OnTabVisibilityChanged),
                    tab.RegisterPropertyChangedCallback(
                        RibbonTabItem.ContextualTabGroupNameProperty, OnTabContextualGroupChanged),
                    tab.RegisterPropertyChangedCallback(RibbonTabItem.GroupProperty, OnTabContextualGroupChanged)));
            }
        }
    }

    private void OnTabContextualGroupChanged(DependencyObject sender, DependencyProperty property) =>
        LinkContextualTabGroups();

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
    /// Keeps selection on a visible, enabled member, or clears it if none remain.
    /// </summary>
    private void EnsureSelectedTabVisible()
    {
        if (_isSynchronizingTabs || _isLinkingContextualGroups)
        {
            return;
        }

        SynchronizeSelectedTab(SelectedTab, selectFallback: true);
    }

    private void UpdateMinimizedState()
    {
        foreach (var tab in Tabs)
        {
            tab.SetContentHeight(IsSimplified ? SimplifiedContentHeight : ContentHeight);
        }

        // Keep the tab control in sync with the ribbon's minimized state. The template binding on
        // RibbonTabControl.IsMinimized does not reliably propagate here, so push it explicitly (the
        // same way UpdateSimplifiedState pushes IsSimplified). Without this the tab control still
        // reports IsMinimized=false and immediately force-closes the transient tab drop-down that a
        // KeyTip / header click opens while minimized.
        if (_tabControl is not null)
        {
            _tabControl.IsMinimized = IsMinimized;

            _tabControl.ContentHeight = IsSimplified ? SimplifiedContentHeight : ContentHeight;
        }

        VisualStateManager.GoToState(this, IsMinimized ? "Minimized" : "Normal", true);
    }

    private void UpdateMenu()
    {
        if (_tabControl is not null)
        {
            _tabControl.TabStripHeader = Menu;
        }
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
        SynchronizeQuickAccessRegistrations();
        SyncQuickAccessItems();
    }

    private void OnQuickAccessToolBarItemsChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        var active = ShowQuickAccessToolBarAboveRibbon ? _quickAccessToolBar : _belowRibbonQAT;
        if (_isSynchronizingQuickAccessItems || active is null || !ReferenceEquals(sender, active))
        {
            return;
        }

        var items = active.Items.ToArray();
        _isSynchronizingQuickAccessItems = true;
        try
        {
            SynchronizeQuickAccessSource(items, QuickAccessToolBarItems);
        }
        finally
        {
            _isSynchronizingQuickAccessItems = false;
        }

        SyncQuickAccessItems();
    }

    private static void SynchronizeQuickAccessSource<T>(
        IReadOnlyList<T> items,
        ObservableCollection<T> target)
        where T : class
    {
        for (var index = target.Count - 1; index >= 0; index--)
        {
            if (!items.Contains(target[index]))
            {
                target.RemoveAt(index);
            }
        }

        for (var index = 0; index < items.Count; index++)
        {
            if (index < target.Count && ReferenceEquals(target[index], items[index]))
            {
                continue;
            }

            var previousIndex = target.IndexOf(items[index]);
            if (previousIndex >= 0)
            {
                target.Move(previousIndex, index);
            }
            else
            {
                target.Insert(index, items[index]);
            }
        }
    }

    private void ImportQuickAccessCustomizationItems()
    {
        if (_isSynchronizingQuickAccessItems)
        {
            return;
        }

        var items = (_quickAccessToolBar?.QuickAccessItems ?? Enumerable.Empty<QuickAccessMenuItem>())
            .Concat(_belowRibbonQAT?.QuickAccessItems ?? Enumerable.Empty<QuickAccessMenuItem>())
            .Distinct()
            .ToArray();
        _isSynchronizingQuickAccessItems = true;
        try
        {
            foreach (var item in items)
            {
                if (!QuickAccessItems.Contains(item))
                {
                    QuickAccessItems.Add(item);
                }
            }
        }
        finally
        {
            _isSynchronizingQuickAccessItems = false;
        }
    }

    internal void OnQuickAccessCustomizationItemsChanged(QuickAccessToolBar toolbar)
    {
        _quickAccessCustomizationToolBars.Add(toolbar);
        if (_isSynchronizingQuickAccessItems || !IsTemplateQuickAccessToolBar(toolbar))
        {
            SynchronizeQuickAccessMenuItemSubscriptions();
            return;
        }

        var active = ShowQuickAccessToolBarAboveRibbon ? _quickAccessToolBar : _belowRibbonQAT;
        if (ReferenceEquals(toolbar, active))
        {
            _isSynchronizingQuickAccessItems = true;
            try
            {
                SynchronizeQuickAccessSource(toolbar.QuickAccessItems.ToArray(), QuickAccessItems);
            }
            finally
            {
                _isSynchronizingQuickAccessItems = false;
            }
        }
        else
        {
            ImportQuickAccessCustomizationItems();
        }

        SyncQuickAccessItems();
    }

    /// <summary>
    /// Mirrors <see cref="QuickAccessToolBarItems"/> into whichever quick access toolbar is currently
    /// active (above or below the ribbon), clearing the other so each element keeps a single parent.
    /// </summary>
    private void SyncQuickAccessItems()
    {
        if (_isSynchronizingQuickAccessItems)
        {
            _quickAccessSynchronizationPending = true;
            return;
        }

        do
        {
            _quickAccessSynchronizationPending = false;
            _isSynchronizingQuickAccessItems = true;
            try
            {
                var above = ShowQuickAccessToolBarAboveRibbon;
                var active = above ? _quickAccessToolBar : _belowRibbonQAT;
                var inactive = above ? _belowRibbonQAT : _quickAccessToolBar;

                inactive?.Items.Clear();
                SynchronizeQuickAccessCustomizationItems(inactive, []);
                SynchronizeQuickAccessCustomizationItems(active, QuickAccessItems);
                SynchronizeQuickAccessMenuItemSubscriptions();

                if (active is not null
                    && (active.Items.Count != QuickAccessToolBarItems.Count
                        || active.Items.Where((item, index) =>
                            !ReferenceEquals(item, QuickAccessToolBarItems[index])).Any()))
                {
                    active.Items.Clear();
                    foreach (var item in QuickAccessToolBarItems)
                    {
                        active.Items.Add(item);
                    }
                }
            }
            finally
            {
                _isSynchronizingQuickAccessItems = false;
            }
        }
        while (_quickAccessSynchronizationPending);
    }

    private static void SynchronizeQuickAccessCustomizationItems(
        QuickAccessToolBar? toolbar,
        IReadOnlyList<QuickAccessMenuItem> items)
    {
        if (toolbar is null)
        {
            return;
        }

        var targetItems = toolbar.QuickAccessItems;
        if (targetItems.Count == items.Count
            && targetItems.Select((item, index) => ReferenceEquals(item, items[index])).All(value => value))
        {
            return;
        }

        targetItems.Clear();
        foreach (var item in items)
        {
            targetItems.Add(item);
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

    #region Quick Access Toolbar integration

    /// <summary>
    /// Returns whether the given control currently has an item in the Quick Access Toolbar.
    /// </summary>
    public bool IsInQuickAccessToolBar(IQuickAccessItemProvider? provider) =>
        TryGetQuickAccessEntry(provider, out _, out _);

    /// <summary>
    /// Resolves calls using a concrete provider type without requiring a UIElement or interface cast.
    /// </summary>
    public bool IsInQuickAccessToolBar<T>(T provider)
        where T : IQuickAccessItemProvider =>
        IsInQuickAccessToolBar((IQuickAccessItemProvider?)provider);

    /// <summary>
    /// Adds a compact copy of the given control to the Quick Access Toolbar. Does nothing
    /// if the control is already present or cannot provide a quick access item.
    /// </summary>
    public void AddToQuickAccessToolBar(IQuickAccessItemProvider? provider)
    {
        provider = ResolveQuickAccessProvider(provider);
        if (provider is null || TryGetQuickAccessEntry(provider, out _, out _))
        {
            return;
        }

        var item = provider.CreateQuickAccessItem();
        if (item is null)
        {
            return;
        }

        QuickAccessHelper.AssociateQuickAccessItem(this, provider, item);
        if (!QuickAccessToolBarItems.Contains(item))
        {
            QuickAccessToolBarItems.Add(item);
        }
        else
        {
            SynchronizeQuickAccessRegistrations();
        }
    }

    /// <summary>
    /// Adds a concrete provider using the same registration as the UIElement and interface APIs.
    /// </summary>
    public void AddToQuickAccessToolBar<T>(T provider)
        where T : IQuickAccessItemProvider =>
        AddToQuickAccessToolBar((IQuickAccessItemProvider?)provider);

    /// <summary>
    /// Removes the Quick Access Toolbar item previously created for the given control.
    /// </summary>
    public void RemoveFromQuickAccessToolBar(IQuickAccessItemProvider? provider) =>
        RemoveQuickAccessEntry(provider);

    /// <summary>
    /// Removes a concrete provider or its active toolbar copy without requiring a cast.
    /// </summary>
    public void RemoveFromQuickAccessToolBar<T>(T provider)
        where T : IQuickAccessItemProvider =>
        RemoveFromQuickAccessToolBar((IQuickAccessItemProvider?)provider);

    private IQuickAccessItemProvider? ResolveQuickAccessProvider(object? element) =>
        QuickAccessHelper.TryGetQuickAccessProvider(this, element, out var provider)
            ? provider
            : element as IQuickAccessItemProvider;

    internal bool CanCustomizeQuickAccessItem(UIElement element, bool add)
    {
        var provider = ResolveQuickAccessProvider(element);
        return IsEnabled
               && IsQuickAccessToolBarVisible
               && CanCustomizeQuickAccessToolBarItems
               && FocusRoutingHelper.IsEffectivelyEnabled(element)
               && (provider is not DependencyObject source || FocusRoutingHelper.IsEffectivelyEnabled(source))
               && (add
                   ? provider is { CanAddToQuickAccessToolBar: true } && !IsInQuickAccessToolBar(provider)
                   : IsInQuickAccessToolBar(element));
    }

    private bool TryGetQuickAccessEntry(object? element, out object source, out UIElement item)
    {
        if (element is not null)
        {
            foreach (var entry in EnumerateQuickAccessEntries())
            {
                if (Equals(entry.Source, element) || ReferenceEquals(entry.Item, element))
                {
                    source = entry.Source;
                    item = entry.Item;
                    return true;
                }
            }
        }

        source = null!;
        item = null!;
        return false;
    }

    private IEnumerable<(object Source, UIElement Item)> EnumerateQuickAccessEntries()
    {
        foreach (var entry in _quickAccessElements)
        {
            yield return (entry.Key, entry.Value);
        }

        foreach (var entry in _nonVisualQuickAccessItems)
        {
            yield return (entry.Key, entry.Value);
        }
    }

    internal bool OwnsQuickAccessItem(UIElement item) =>
        EnumerateQuickAccessEntries().Any(entry => ReferenceEquals(entry.Item, item));

    private void RemoveQuickAccessEntry(object? element)
    {
        if (!TryGetQuickAccessEntry(element, out var source, out var item))
        {
            return;
        }

        if (!QuickAccessToolBarItems.Remove(item))
        {
            UnregisterQuickAccessEntry(source, item);
            UpdateQuickAccessMenuChecks();
        }
    }

    private void UnregisterQuickAccessEntry(object source, UIElement item)
    {
        if (source is UIElement element)
        {
            _quickAccessElements.Remove(element);
        }
        else if (source is IQuickAccessItemProvider provider)
        {
            _nonVisualQuickAccessItems.Remove(provider);
        }

        if (source is DependencyObject dependencyObject)
        {
            RibbonProperties.SetIsElementInQuickAccessToolBar(dependencyObject, false);
        }

        RibbonProperties.SetIsElementInQuickAccessToolBar(item, false);
    }

    private void SynchronizeQuickAccessRegistrations()
    {
        foreach (var (source, item) in EnumerateQuickAccessEntries().ToArray())
        {
            if (!QuickAccessToolBarItems.Contains(item))
            {
                UnregisterQuickAccessEntry(source, item);
            }
        }

        foreach (var item in QuickAccessToolBarItems.OfType<FrameworkElement>())
        {
            if (!QuickAccessHelper.TryGetQuickAccessProvider(this, item, out var provider))
            {
                continue;
            }

            if (TryGetQuickAccessEntry(provider, out _, out var registeredItem))
            {
                if (!ReferenceEquals(registeredItem, item))
                {
                    throw new InvalidOperationException(
                        "A quick access provider cannot have more than one active toolbar copy.");
                }
            }
            else if (provider is UIElement element)
            {
                _quickAccessElements.Add(element, item);
            }
            else
            {
                _nonVisualQuickAccessItems.Add(provider, item);
            }

            if (provider is DependencyObject dependencyObject)
            {
                RibbonProperties.SetIsElementInQuickAccessToolBar(dependencyObject, true);
            }

            RibbonProperties.SetIsElementInQuickAccessToolBar(item, true);
        }

        UpdateQuickAccessMenuChecks();
    }

    internal void RegisterQuickAccessCustomizationToolBar(QuickAccessToolBar toolbar)
    {
        _quickAccessCustomizationToolBars.Add(toolbar);
        if (!_isSynchronizingQuickAccessItems && IsTemplateQuickAccessToolBar(toolbar))
        {
            ImportQuickAccessCustomizationItems();
            SyncQuickAccessItems();
        }
        else
        {
            SynchronizeQuickAccessMenuItemSubscriptions();
        }
    }

    internal void UnregisterQuickAccessCustomizationToolBar(QuickAccessToolBar toolbar)
    {
        _quickAccessCustomizationToolBars.Remove(toolbar);
        SynchronizeQuickAccessMenuItemSubscriptions();
    }

    private void SynchronizeQuickAccessMenuItemSubscriptions()
    {
        if (_isSynchronizingQuickAccessMenuItems)
        {
            _quickAccessMenuItemSynchronizationPending = true;
            return;
        }

        do
        {
            _quickAccessMenuItemSynchronizationPending = false;
            _isSynchronizingQuickAccessMenuItems = true;
            try
            {
                var items = EnumerateQuickAccessCustomizationItems().ToHashSet();
                foreach (var (item, subscription) in _quickAccessMenuItemSubscriptions.ToArray())
                {
                    if (items.Contains(item))
                    {
                        continue;
                    }

                    item.UnregisterPropertyChangedCallback(QuickAccessMenuItem.IsCheckedProperty, subscription.CheckedToken);
                    item.UnregisterPropertyChangedCallback(QuickAccessMenuItem.TargetProperty, subscription.TargetToken);
                    item.ReleaseCustomizationRibbon(this);
                    _quickAccessMenuItemSubscriptions.Remove(item);
                    if (!HasCheckedQuickAccessMenuItem(subscription.Provider))
                    {
                        RemoveQuickAccessEntry(subscription.Provider);
                    }
                }

                foreach (var item in items)
                {
                    if (_quickAccessMenuItemSubscriptions.ContainsKey(item)
                        || !EnumerateQuickAccessCustomizationItems().Contains(item))
                    {
                        continue;
                    }

                    var subscription = new QuickAccessMenuItemSubscription(
                        item.RegisterPropertyChangedCallback(
                            QuickAccessMenuItem.IsCheckedProperty, OnQuickAccessMenuItemCheckedChanged),
                        item.RegisterPropertyChangedCallback(
                            QuickAccessMenuItem.TargetProperty, OnQuickAccessMenuItemTargetChanged));
                    _quickAccessMenuItemSubscriptions.Add(item, subscription);
                    item.SetCustomizationRibbon(this);
                    SynchronizeQuickAccessMenuItem(item, subscription, initialize: true);
                }
            }
            finally
            {
                _isSynchronizingQuickAccessMenuItems = false;
            }
        }
        while (_quickAccessMenuItemSynchronizationPending);
    }

    private bool IsTemplateQuickAccessToolBar(QuickAccessToolBar toolbar) =>
        ReferenceEquals(toolbar, _quickAccessToolBar) || ReferenceEquals(toolbar, _belowRibbonQAT);

    // Template collections are views of QuickAccessItems; standalone toolbars own their own menus.
    private IEnumerable<QuickAccessMenuItem> EnumerateQuickAccessCustomizationItems() =>
        QuickAccessItems.Concat(
            _quickAccessCustomizationToolBars
                .Where(toolbar => !IsTemplateQuickAccessToolBar(toolbar))
                .SelectMany(toolbar => toolbar.QuickAccessItems));

    private bool HasCheckedQuickAccessMenuItem(IQuickAccessItemProvider? provider) =>
        provider is not null && EnumerateQuickAccessCustomizationItems().Any(
            item => item.IsChecked && Equals(ResolveQuickAccessProvider(item.Target), provider));

    private void OnQuickAccessMenuItemCheckedChanged(DependencyObject sender, DependencyProperty property)
    {
        if (!_isUpdatingQuickAccessMenuChecks
            && sender is QuickAccessMenuItem item
            && _quickAccessMenuItemSubscriptions.TryGetValue(item, out var subscription))
        {
            SynchronizeQuickAccessMenuItem(item, subscription, initialize: false);
        }
    }

    private void OnQuickAccessMenuItemTargetChanged(DependencyObject sender, DependencyProperty property)
    {
        if (sender is QuickAccessMenuItem item
            && _quickAccessMenuItemSubscriptions.TryGetValue(item, out var subscription))
        {
            SynchronizeQuickAccessMenuItem(item, subscription, initialize: true);
        }
    }

    private void SynchronizeQuickAccessMenuItem(
        QuickAccessMenuItem item,
        QuickAccessMenuItemSubscription subscription,
        bool initialize)
    {
        var provider = ResolveQuickAccessProvider(item.Target);
        var wasChecked = item.IsChecked;
        var previousProvider = subscription.Provider;
        subscription.Provider = provider;

        // A load-time null target preserves checked intent; clearing a resolved target cancels it.
        // An explicit check while targetless can start a new request.
        if (item.Target is null)
        {
            subscription.HasPendingCheck = wasChecked && (!initialize || !subscription.HasResolvedTarget);
        }
        else
        {
            subscription.HasResolvedTarget = true;
            subscription.HasPendingCheck = false;
        }

        if (previousProvider is not null
            && !Equals(previousProvider, provider)
            && !HasCheckedQuickAccessMenuItem(previousProvider))
        {
            RemoveQuickAccessEntry(previousProvider);
        }

        if (provider is not null)
        {
            if (wasChecked || (initialize && IsInQuickAccessToolBar(provider)))
            {
                AddToQuickAccessToolBar(provider);
            }
            else
            {
                RemoveFromQuickAccessToolBar(provider);
            }
        }

        UpdateQuickAccessMenuChecks();
    }

    private void UpdateQuickAccessMenuChecks()
    {
        if (_isUpdatingQuickAccessMenuChecks)
        {
            return;
        }

        _isUpdatingQuickAccessMenuChecks = true;
        try
        {
            foreach (var (item, subscription) in _quickAccessMenuItemSubscriptions.ToArray())
            {
                var isChecked = subscription.HasPendingCheck || IsInQuickAccessToolBar(subscription.Provider);
                if (item.IsChecked != isChecked)
                {
                    item.IsChecked = isChecked;
                }
            }
        }
        finally
        {
            _isUpdatingQuickAccessMenuChecks = false;
        }
    }

    private sealed class QuickAccessMenuItemSubscription(long checkedToken, long targetToken)
    {
        public long CheckedToken { get; } = checkedToken;
        public long TargetToken { get; } = targetToken;
        public IQuickAccessItemProvider? Provider { get; set; }
        public bool HasResolvedTarget { get; set; }
        public bool HasPendingCheck { get; set; }
    }

    #endregion

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
