namespace Fluent;

using System.Collections;
using System.Collections.Specialized;
using Windows.System;

/// <summary>
/// Portable WPF-compatible API surface for <see cref="Ribbon"/>.
/// </summary>
public partial class Ribbon : ILogicalChildSupport
{
    private static readonly Dictionary<int, MenuFlyout> RibbonContextMenus = new();
    private IRibbonStateStorage? _ribbonStateStorage;
    private readonly Dictionary<UIElement, UIElement> _quickAccessElements = new();
    private readonly ObservableCollection<VirtualKey> _keyTipKeys = new(KeyTipService.DefaultKeyTipKeys);
    private readonly ObservableCollection<QuickAccessMenuItem> _quickAccessItems = new();
    private bool _hasLoaded;

    /// <summary>Minimal width at which a ribbon remains visible.</summary>
    public const double MinimalVisibleWidth = 300D;

    /// <summary>Minimal height at which a ribbon remains visible.</summary>
    public const double MinimalVisibleHeight = 250D;

    /// <summary>
    /// Gets the customization items shared with the active quick access toolbar.
    /// Direct edits to that toolbar's QuickAccessItems collection update this collection as well.
    /// </summary>
    public ObservableCollection<QuickAccessMenuItem> QuickAccessItems =>
        _quickAccessItems;

    /// <summary>Gets the ribbon context menu for the current managed thread.</summary>
    public static MenuFlyout RibbonContextMenu
    {
        get
        {
            var threadId = Environment.CurrentManagedThreadId;
            if (RibbonContextMenus.TryGetValue(threadId, out var menu))
            {
                return menu;
            }

            menu = new MenuFlyout();
            RibbonContextMenus.Add(threadId, menu);
            return menu;
        }
    }

    /// <summary>Identifies the <see cref="SelectedTabItem"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTabItemProperty;

    static Ribbon()
    {
        SelectedTabItemProperty = SelectedTabProperty;
        RibbonLocalization.Current.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(RibbonLocalization.Localization)
                or nameof(RibbonLocalization.Culture)
                or null
                or "")
            {
                RefreshCommandLabels();
            }
        };
        RefreshCommandLabels();
    }

    /// <summary>Gets or sets the selected WPF-compatible tab item.</summary>
    public RibbonTabItem? SelectedTabItem
    {
        get => SelectedTab;
        set => SelectedTab = value;
    }

    /// <summary>Gets the first visible tab item.</summary>
    public RibbonTabItem? FirstVisibleItem =>
        Tabs.FirstOrDefault(tab => tab.Visibility == Visibility.Visible);

    /// <summary>Gets the last visible tab item.</summary>
    public RibbonTabItem? LastVisibleItem =>
        Tabs.LastOrDefault(tab => tab.Visibility == Visibility.Visible);

    /// <summary>Handles context-menu opening.</summary>
    protected virtual void OnContextMenuOpening(ContextMenuEventArgs e)
    {
    }

    /// <summary>Handles context-menu closing.</summary>
    protected virtual void OnContextMenuClosing(ContextMenuEventArgs e)
    {
    }

    /// <summary>Occurs when ribbon customization is requested.</summary>
    public event EventHandler? CustomizeTheRibbon;

    /// <summary>Occurs when quick access toolbar customization is requested.</summary>
    public event EventHandler? CustomizeQuickAccessToolbar;

    /// <summary>Occurs when <see cref="IsMinimized"/> changes.</summary>
    public event EventHandler<DependencyPropertyChangedEventArgs>? IsMinimizedChanged;

    /// <summary>Occurs when <see cref="IsCollapsed"/> changes.</summary>
    public event EventHandler<DependencyPropertyChangedEventArgs>? IsCollapsedChanged;

    /// <summary>Identifies the <see cref="IsDefaultContextMenuEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsDefaultContextMenuEnabledProperty =
        DependencyProperty.Register(
            nameof(IsDefaultContextMenuEnabled),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnDefaultContextMenuEnabledChanged));

    /// <summary>Identifies the <see cref="IsBackstageOrStartScreenOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsBackstageOrStartScreenOpenProperty =
        DependencyProperty.Register(
            nameof(IsBackstageOrStartScreenOpen),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="StartScreen"/> dependency property.</summary>
    public static readonly DependencyProperty StartScreenProperty =
        DependencyProperty.Register(
            nameof(StartScreen),
            typeof(StartScreen),
            typeof(Ribbon),
            new PropertyMetadata(null, OnStartScreenChanged));

    /// <summary>Identifies the <see cref="QuickAccessToolBar"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessToolBarProperty =
        DependencyProperty.Register(
            nameof(QuickAccessToolBar),
            typeof(QuickAccessToolBar),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="TabControl"/> dependency property.</summary>
    public static readonly DependencyProperty TabControlProperty =
        DependencyProperty.Register(
            nameof(TabControl),
            typeof(RibbonTabControl),
            typeof(Ribbon),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="TitleBar"/> dependency property.</summary>
    public static readonly DependencyProperty TitleBarProperty =
        DependencyProperty.Register(
            nameof(TitleBar),
            typeof(RibbonTitleBar),
            typeof(Ribbon),
            new PropertyMetadata(null, OnTitleBarChanged));

    /// <summary>Identifies the <see cref="QuickAccessToolBarHeight"/> dependency property.</summary>
    public static readonly DependencyProperty QuickAccessToolBarHeightProperty =
        DependencyProperty.Register(
            nameof(QuickAccessToolBarHeight),
            typeof(double),
            typeof(Ribbon),
            new PropertyMetadata(23D));

    /// <summary>Identifies the <see cref="CanCustomizeQuickAccessToolBar"/> dependency property.</summary>
    public static readonly DependencyProperty CanCustomizeQuickAccessToolBarProperty =
        DependencyProperty.Register(
            nameof(CanCustomizeQuickAccessToolBar),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false, OnQuickAccessCustomizationOptionsChanged));

    /// <summary>Identifies the <see cref="CanCustomizeQuickAccessToolBarItems"/> dependency property.</summary>
    public static readonly DependencyProperty CanCustomizeQuickAccessToolBarItemsProperty =
        DependencyProperty.Register(
            nameof(CanCustomizeQuickAccessToolBarItems),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnQuickAccessCustomizationOptionsChanged));

    /// <summary>Identifies the <see cref="IsQuickAccessToolBarMenuDropDownVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsQuickAccessToolBarMenuDropDownVisibleProperty =
        DependencyProperty.Register(
            nameof(IsQuickAccessToolBarMenuDropDownVisible),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnQuickAccessMenuVisibilityChanged));

    /// <summary>Identifies the <see cref="CanCustomizeRibbon"/> dependency property.</summary>
    public static readonly DependencyProperty CanCustomizeRibbonProperty =
        DependencyProperty.Register(
            nameof(CanCustomizeRibbon),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="CanUseSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty CanUseSimplifiedProperty =
        DependencyProperty.Register(
            nameof(CanUseSimplified),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsDisplayOptionsButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsDisplayOptionsButtonVisibleProperty =
        DependencyProperty.Register(
            nameof(IsDisplayOptionsButtonVisible),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ContentHeight"/> dependency property.</summary>
    public static readonly DependencyProperty ContentHeightProperty =
        DependencyProperty.Register(
            nameof(ContentHeight),
            typeof(double),
            typeof(Ribbon),
            new PropertyMetadata(RibbonTabControl.DefaultContentHeight, OnContentHeightChanged));

    /// <summary>Identifies the <see cref="CanQuickAccessLocationChanging"/> dependency property.</summary>
    public static readonly DependencyProperty CanQuickAccessLocationChangingProperty =
        DependencyProperty.Register(
            nameof(CanQuickAccessLocationChanging),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnCanQuickAccessLocationChangingChanged));

    /// <summary>Identifies the <see cref="IsToolBarVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsToolBarVisibleProperty =
        DependencyProperty.Register(
            nameof(IsToolBarVisible),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnToolBarVisibilityChanged));

    /// <summary>Identifies the <see cref="IsMouseWheelScrollingEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsMouseWheelScrollingEnabledProperty =
        DependencyProperty.Register(
            nameof(IsMouseWheelScrollingEnabled),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsMouseWheelScrollingEnabledEverywhere"/> dependency property.</summary>
    public static readonly DependencyProperty IsMouseWheelScrollingEnabledEverywhereProperty =
        DependencyProperty.Register(
            nameof(IsMouseWheelScrollingEnabledEverywhere),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsKeyTipHandlingEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsKeyTipHandlingEnabledProperty =
        DependencyProperty.Register(
            nameof(IsKeyTipHandlingEnabled),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnIsKeyTipHandlingEnabledChanged));

    /// <summary>Identifies the <see cref="AutomaticStateManagement"/> dependency property.</summary>
    public static readonly DependencyProperty AutomaticStateManagementProperty =
        DependencyProperty.Register(
            nameof(AutomaticStateManagement),
            typeof(bool),
            typeof(Ribbon),
            new PropertyMetadata(true, OnAutomaticStateManagementChanged));

    /// <summary>Gets the command that adds an element to the quick access toolbar.</summary>
    public static readonly XamlUICommand AddToQuickAccessCommand =
        CreateQuickAccessCommand("Add to Quick Access Toolbar", add: true);

    /// <summary>Gets the command that removes an element from the quick access toolbar.</summary>
    public static readonly XamlUICommand RemoveFromQuickAccessCommand =
        CreateQuickAccessCommand("Remove from Quick Access Toolbar", add: false);

    /// <summary>Gets the command that moves the quick access toolbar above the ribbon.</summary>
    public static readonly XamlUICommand ShowQuickAccessAboveCommand =
        CreateRibbonCommand(string.Empty, ribbon => ribbon.ShowQuickAccessToolBarAboveRibbon = true,
            ribbon => ribbon.IsQuickAccessToolBarVisible && ribbon.CanQuickAccessLocationChanging);

    /// <summary>Gets the command that moves the quick access toolbar below the ribbon.</summary>
    public static readonly XamlUICommand ShowQuickAccessBelowCommand =
        CreateRibbonCommand(string.Empty, ribbon => ribbon.ShowQuickAccessToolBarAboveRibbon = false,
            ribbon => ribbon.IsQuickAccessToolBarVisible && ribbon.CanQuickAccessLocationChanging);

    /// <summary>Gets the command that toggles ribbon minimization.</summary>
    public static readonly XamlUICommand ToggleMinimizeTheRibbonCommand =
        CreateRibbonCommand(string.Empty, ribbon => ribbon.ToggleMinimize(), ribbon => ribbon.CanMinimize);

    /// <summary>Gets the command that switches to the classic ribbon.</summary>
    public static readonly XamlUICommand SwitchToTheClassicRibbonCommand =
        CreateRibbonCommand(string.Empty, ribbon => ribbon.IsSimplified = false, ribbon => ribbon.CanUseSimplified);

    /// <summary>Gets the command that switches to the simplified ribbon.</summary>
    public static readonly XamlUICommand SwitchToTheSimplifiedRibbonCommand =
        CreateRibbonCommand(string.Empty, ribbon => ribbon.IsSimplified = true, ribbon => ribbon.CanUseSimplified);

    /// <summary>Gets the command that requests quick access toolbar customization.</summary>
    public static readonly XamlUICommand CustomizeQuickAccessToolbarCommand =
        CreateRibbonCommand(
            string.Empty,
            ribbon => ribbon.CustomizeQuickAccessToolbar?.Invoke(ribbon, EventArgs.Empty),
            ribbon => ribbon.CanCustomizeQuickAccessToolBar);

    /// <summary>Gets the command that requests ribbon customization.</summary>
    public static readonly XamlUICommand CustomizeTheRibbonCommand =
        CreateRibbonCommand(
            string.Empty,
            ribbon => ribbon.CustomizeTheRibbon?.Invoke(ribbon, EventArgs.Empty),
            ribbon => ribbon.CanCustomizeRibbon);

    private static void RefreshCommandLabels()
    {
        var localization = RibbonLocalization.Current.Localization;
        AddToQuickAccessCommand.Label = localization.RibbonContextMenuAddItem;
        RemoveFromQuickAccessCommand.Label = localization.RibbonContextMenuRemoveItem;
        ShowQuickAccessAboveCommand.Label = localization.RibbonContextMenuShowAbove;
        ShowQuickAccessBelowCommand.Label = localization.RibbonContextMenuShowBelow;
        ToggleMinimizeTheRibbonCommand.Label = localization.RibbonContextMenuMinimizeRibbon;
        SwitchToTheClassicRibbonCommand.Label = localization.UseClassicRibbon;
        SwitchToTheSimplifiedRibbonCommand.Label = localization.UseSimplifiedRibbon;
        CustomizeQuickAccessToolbarCommand.Label = localization.RibbonContextMenuCustomizeQuickAccessToolBar;
        CustomizeTheRibbonCommand.Label = localization.RibbonContextMenuCustomizeRibbon;
    }

    /// <summary>Gets or sets whether the default ribbon context menu is enabled.</summary>
    /// <remarks>Explicitly authored context flyouts are not replaced or disabled by this option.</remarks>
    public bool IsDefaultContextMenuEnabled
    {
        get => (bool)GetValue(IsDefaultContextMenuEnabledProperty);
        set => SetValue(IsDefaultContextMenuEnabledProperty, value);
    }

    /// <summary>Gets or sets whether the backstage or start screen is open.</summary>
    public bool IsBackstageOrStartScreenOpen
    {
        get => (bool)GetValue(IsBackstageOrStartScreenOpenProperty);
        set => SetValue(IsBackstageOrStartScreenOpenProperty, value);
    }

    /// <summary>Gets or sets the start screen.</summary>
    public StartScreen? StartScreen
    {
        get => (StartScreen?)GetValue(StartScreenProperty);
        set => SetValue(StartScreenProperty, value);
    }

    /// <summary>Gets the template quick access toolbar.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public QuickAccessToolBar? QuickAccessToolBar
    {
        get => (QuickAccessToolBar?)GetValue(QuickAccessToolBarProperty);
        private set => SetValue(QuickAccessToolBarProperty, value);
    }

    /// <summary>Gets the template tab control.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public RibbonTabControl? TabControl
    {
        get => (RibbonTabControl?)GetValue(TabControlProperty);
        private set => SetValue(TabControlProperty, value);
    }

    /// <summary>Gets or sets the ribbon title bar.</summary>
    public RibbonTitleBar? TitleBar
    {
        get => (RibbonTitleBar?)GetValue(TitleBarProperty);
        set => SetValue(TitleBarProperty, value);
    }

    /// <summary>Gets or sets the quick access title height.</summary>
    public double QuickAccessToolBarHeight
    {
        get => (double)GetValue(QuickAccessToolBarHeightProperty);
        set => SetValue(QuickAccessToolBarHeightProperty, value);
    }

    /// <summary>Gets or sets whether users can request the full quick access toolbar customization UI.</summary>
    public bool CanCustomizeQuickAccessToolBar
    {
        get => (bool)GetValue(CanCustomizeQuickAccessToolBarProperty);
        set => SetValue(CanCustomizeQuickAccessToolBarProperty, value);
    }

    /// <summary>Gets or sets whether users can add or remove quick access items. Programmatic edits remain available.</summary>
    public bool CanCustomizeQuickAccessToolBarItems
    {
        get => (bool)GetValue(CanCustomizeQuickAccessToolBarItemsProperty);
        set => SetValue(CanCustomizeQuickAccessToolBarItemsProperty, value);
    }

    /// <summary>Gets or sets whether the quick access menu button is visible.</summary>
    public bool IsQuickAccessToolBarMenuDropDownVisible
    {
        get => (bool)GetValue(IsQuickAccessToolBarMenuDropDownVisibleProperty);
        set => SetValue(IsQuickAccessToolBarMenuDropDownVisibleProperty, value);
    }

    /// <summary>Gets or sets whether ribbon customization is available.</summary>
    public bool CanCustomizeRibbon
    {
        get => (bool)GetValue(CanCustomizeRibbonProperty);
        set => SetValue(CanCustomizeRibbonProperty, value);
    }

    /// <summary>Gets or sets whether simplified mode can be used.</summary>
    public bool CanUseSimplified
    {
        get => (bool)GetValue(CanUseSimplifiedProperty);
        set => SetValue(CanUseSimplifiedProperty, value);
    }

    /// <summary>Gets or sets whether the display options button is visible.</summary>
    public bool IsDisplayOptionsButtonVisible
    {
        get => (bool)GetValue(IsDisplayOptionsButtonVisibleProperty);
        set => SetValue(IsDisplayOptionsButtonVisibleProperty, value);
    }

    /// <summary>Gets or sets the ribbon content height.</summary>
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        set => SetValue(ContentHeightProperty, value);
    }

    /// <summary>Gets or sets whether users can change the quick access location.</summary>
    public bool CanQuickAccessLocationChanging
    {
        get => (bool)GetValue(CanQuickAccessLocationChangingProperty);
        set => SetValue(CanQuickAccessLocationChangingProperty, value);
    }

    /// <summary>Gets or sets whether toolbar items are visible.</summary>
    public bool IsToolBarVisible
    {
        get => (bool)GetValue(IsToolBarVisibleProperty);
        set => SetValue(IsToolBarVisibleProperty, value);
    }

    /// <summary>Gets or sets whether tab cycling by mouse wheel is enabled.</summary>
    public bool IsMouseWheelScrollingEnabled
    {
        get => (bool)GetValue(IsMouseWheelScrollingEnabledProperty);
        set => SetValue(IsMouseWheelScrollingEnabledProperty, value);
    }

    /// <summary>Gets or sets whether tab cycling is enabled across the ribbon.</summary>
    public bool IsMouseWheelScrollingEnabledEverywhere
    {
        get => (bool)GetValue(IsMouseWheelScrollingEnabledEverywhereProperty);
        set => SetValue(IsMouseWheelScrollingEnabledEverywhereProperty, value);
    }

    /// <summary>Gets whether any key tips are visible.</summary>
    public bool AreAnyKeyTipsVisible => _keyTipService.AreAnyKeyTipsVisible;

    /// <summary>Gets or sets whether key tip handling is enabled.</summary>
    public bool IsKeyTipHandlingEnabled
    {
        get => (bool)GetValue(IsKeyTipHandlingEnabledProperty);
        set => SetValue(IsKeyTipHandlingEnabledProperty, value);
    }

    /// <summary>Gets the keys that activate key tips.</summary>
    public ObservableCollection<VirtualKey> KeyTipKeys => _keyTipKeys;

    /// <summary>Gets or sets whether ribbon state is managed automatically.</summary>
    public bool AutomaticStateManagement
    {
        get => (bool)GetValue(AutomaticStateManagementProperty);
        set => SetValue(AutomaticStateManagementProperty, value);
    }

    /// <summary>Gets the current ribbon state storage.</summary>
    public IRibbonStateStorage RibbonStateStorage =>
        _ribbonStateStorage ??= CreateRibbonStateStorage();

    /// <summary>Gets the currently active quick access element map.</summary>
    protected Dictionary<UIElement, UIElement> QuickAccessElements => _quickAccessElements;

    /// <summary>Creates the ribbon state storage.</summary>
    protected virtual IRibbonStateStorage CreateRibbonStateStorage() => new RibbonStateStorage(this);

    /// <summary>Gets a copy of the active quick access element map.</summary>
    public IDictionary<UIElement, UIElement> GetQuickAccessElements() =>
        _quickAccessElements.ToDictionary(pair => pair.Key, pair => pair.Value);

    /// <summary>Returns whether an element is represented in the quick access toolbar.</summary>
    [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
    public bool IsInQuickAccessToolBar(UIElement? element) =>
        TryGetQuickAccessEntry(element, out _, out _);

    /// <summary>Adds an element to the quick access toolbar when it can create a portable copy.</summary>
    [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
    public void AddToQuickAccessToolBar(UIElement? element) =>
        AddToQuickAccessToolBar(ResolveQuickAccessProvider(element));

    /// <summary>Removes an element from the quick access toolbar.</summary>
    [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
    public void RemoveFromQuickAccessToolBar(UIElement? element) =>
        RemoveQuickAccessEntry(element);

    /// <summary>Clears provider-created copies, preserving directly authored toolbar items.</summary>
    public void ClearQuickAccessToolBar()
    {
        foreach (var (source, _) in EnumerateQuickAccessEntries().ToArray())
        {
            RemoveQuickAccessEntry(source);
        }
    }

    /// <summary>Moves focus to the selected tab when the ribbon itself is focused.</summary>
    protected override void OnGotFocus(RoutedEventArgs args)
    {
        base.OnGotFocus(args);

        // GotFocus bubbles, so this fires for every descendant that takes focus. Redirecting
        // unconditionally would yank focus off the control the user just clicked, which drops
        // its pointer capture and cancels the click. Only forward when the ribbon itself
        // received focus (tabbing into the ribbon, or an explicit Ribbon.Focus() call).
        if (!ReferenceEquals(args.OriginalSource, this))
        {
            return;
        }

        SelectedTab?.Focus(FocusState.Programmatic);
    }

    /// <summary>Enumerates portable logical children.</summary>
    protected IEnumerator LogicalChildren
    {
        get
        {
            var children = new List<object>();
            if (Menu is not null)
            {
                children.Add(Menu);
            }

            if (StartScreen is not null)
            {
                children.Add(StartScreen);
            }

            if (QuickAccessToolBar is not null)
            {
                children.Add(QuickAccessToolBar);
            }

            if (TabControl is not null)
            {
                children.Add(TabControl);
            }

            if (TitleBar is not null)
            {
                children.Add(TitleBar);
            }

            return children.GetEnumerator();
        }
    }

    void ILogicalChildSupport.AddLogicalChild(object child)
    {
    }

    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
    }

    private void InitializeCompatibility()
    {
        VerticalAlignment = VerticalAlignment.Top;
        QuickAccessHelper.AttachContextMenu(this);
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => RefreshQuickAccessOptions());
        Loaded += OnCompatibilityLoaded;
        Unloaded += OnCompatibilityUnloaded;
        _keyTipKeys.CollectionChanged += OnKeyTipKeysCollectionChanged;
        _quickAccessItems.CollectionChanged += OnQuickAccessCustomizationItemsCollectionChanged;
        SyncKeyTipKeys();
    }

    private void OnQuickAccessCustomizationItemsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs args)
    {
        SyncQuickAccessItems();
    }

    private void UpdateCompatibilityTemplateParts()
    {
        TabControl = _tabControl;

        if (_tabControl is not null)
        {
            BindTabControlOptions();
            _tabControl.IsSimplified = IsSimplified;
            _tabControl.IsMinimized = IsMinimized;
            _tabControl.ContentHeight = IsSimplified ? SimplifiedContentHeight : ContentHeight;
        }

        UpdateCompatibilityQatSurface();
        UpdateCompatibilityToolBarSurface();
    }

    private void UpdateCompatibilityQatSurface()
    {
        QuickAccessToolBar = ShowQuickAccessToolBarAboveRibbon ? _quickAccessToolBar : _belowRibbonQAT;

        if (_quickAccessToolBar is not null)
        {
            _quickAccessToolBar.CanQuickAccessLocationChanging = CanQuickAccessLocationChanging;
            _quickAccessToolBar.IsMenuDropDownVisible = IsQuickAccessToolBarMenuDropDownVisible;
        }

        if (_belowRibbonQAT is not null)
        {
            _belowRibbonQAT.CanQuickAccessLocationChanging = CanQuickAccessLocationChanging;
            _belowRibbonQAT.IsMenuDropDownVisible = IsQuickAccessToolBarMenuDropDownVisible;
        }

        if (TitleBar is not null)
        {
            TitleBar.ContextualGroups = ContextualGroups;
            TitleBar.QuickAccessToolBar = ShowQuickAccessToolBarAboveRibbon
                ? _quickAccessToolBar
                : null;
        }
    }

    private void UpdateCompatibilityToolBarSurface()
    {
        if (_toolBarItemsHost is not null)
        {
            _toolBarItemsHost.Visibility = IsToolBarVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void RaiseIsMinimizedChanged(DependencyPropertyChangedEventArgs args) =>
        IsMinimizedChanged?.Invoke(this, args);

    private void RaiseIsCollapsedChanged(DependencyPropertyChangedEventArgs args) =>
        IsCollapsedChanged?.Invoke(this, args);

    private void SaveStateTemporaryIfAvailable()
    {
        if (_ribbonStateStorage is { IsLoading: false })
        {
            _ribbonStateStorage.SaveTemporary();
        }
    }

    private void OnCompatibilityLoaded(object sender, RoutedEventArgs args)
    {
        _hasLoaded = true;
        if (AutomaticStateManagement && !RibbonStateStorage.IsLoaded)
        {
            RibbonStateStorage.Load();
        }
    }

    private void OnCompatibilityUnloaded(object sender, RoutedEventArgs args)
    {
        _hasLoaded = false;
        _ribbonStateStorage?.Save();
    }

    private void OnKeyTipKeysCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) =>
        SyncKeyTipKeys();

    private void SyncKeyTipKeys()
    {
        _keyTipService.KeyTipKeys.Clear();
        foreach (var key in _keyTipKeys)
        {
            _keyTipService.KeyTipKeys.Add(key);
        }
    }

    private static void OnTitleBarChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((Ribbon)sender).UpdateCompatibilityQatSurface();

    private static void OnQuickAccessMenuVisibilityChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var ribbon = (Ribbon)sender;
        ribbon.UpdateCompatibilityQatSurface();
        ribbon.RefreshQuickAccessOptions();
    }

    private static void OnContentHeightChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var ribbon = (Ribbon)sender;
        var height = ribbon.IsSimplified ? SimplifiedContentHeight : (double)args.NewValue;
        foreach (var tab in ribbon.Tabs)
        {
            tab.SetContentHeight(height);
        }

        if (ribbon._tabControl is { } tabControl)
        {
            tabControl.ContentHeight = height;
        }
    }

    private static void OnCanQuickAccessLocationChangingChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var ribbon = (Ribbon)sender;
        ribbon.UpdateCompatibilityQatSurface();
        ribbon.RefreshQuickAccessOptions();
    }

    private static void OnDefaultContextMenuEnabledChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        QuickAccessHelper.CloseDefaultContextMenu((Ribbon)sender);

    private static void OnQuickAccessCustomizationOptionsChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((Ribbon)sender).RefreshQuickAccessOptions();

    private void RefreshQuickAccessOptions()
    {
        QuickAccessHelper.CloseDefaultContextMenu(this);
        _quickAccessToolBar?.CloseCustomizationMenu();
        _belowRibbonQAT?.CloseCustomizationMenu();
        foreach (var toolbar in _quickAccessCustomizationToolBars.ToArray())
        {
            toolbar.CloseCustomizationMenu();
        }

        AddToQuickAccessCommand.NotifyCanExecuteChanged();
        RemoveFromQuickAccessCommand.NotifyCanExecuteChanged();
        ShowQuickAccessAboveCommand.NotifyCanExecuteChanged();
        ShowQuickAccessBelowCommand.NotifyCanExecuteChanged();
        CustomizeQuickAccessToolbarCommand.NotifyCanExecuteChanged();
    }

    private static void OnToolBarVisibilityChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((Ribbon)sender).UpdateCompatibilityToolBarSurface();

    private static void OnIsKeyTipHandlingEnabledChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var ribbon = (Ribbon)sender;
        if ((bool)args.NewValue && ribbon._hasLoaded)
        {
            ribbon._keyTipService.Attach();
        }
        else
        {
            ribbon._keyTipService.Detach();
        }
    }

    private static void OnAutomaticStateManagementChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var ribbon = (Ribbon)sender;
        if ((bool)args.NewValue && ribbon._hasLoaded && !ribbon.RibbonStateStorage.IsLoaded)
        {
            ribbon.RibbonStateStorage.Load();
        }
    }

    private static XamlUICommand CreateRibbonCommand(
        string label,
        Action<Ribbon> execute,
        Func<Ribbon, bool>? canExecute = null)
    {
        var command = new XamlUICommand { Label = label };
        command.ExecuteRequested += (_, args) =>
        {
            if (ResolveRibbon(args.Parameter) is { IsEnabled: true } ribbon
                && (canExecute?.Invoke(ribbon) ?? true))
            {
                execute(ribbon);
            }
        };
        command.CanExecuteRequested += (_, args) =>
        {
            var ribbon = ResolveRibbon(args.Parameter);
            args.CanExecute = ribbon is { IsEnabled: true } && (canExecute?.Invoke(ribbon) ?? true);
        };
        return command;
    }

    private static XamlUICommand CreateQuickAccessCommand(string label, bool add)
    {
        var command = new XamlUICommand { Label = label };
        command.ExecuteRequested += (_, args) =>
        {
            if (args.Parameter is not UIElement element
                || ResolveRibbon(element) is not { } ribbon
                || !ribbon.CanCustomizeQuickAccessItem(element, add))
            {
                return;
            }

            if (add)
            {
                ribbon.AddToQuickAccessToolBar(element);
            }
            else
            {
                ribbon.RemoveFromQuickAccessToolBar(element);
            }
        };
        command.CanExecuteRequested += (_, args) =>
        {
            if (args.Parameter is not UIElement element || ResolveRibbon(element) is not { } ribbon)
            {
                args.CanExecute = false;
                return;
            }

            args.CanExecute = ribbon.CanCustomizeQuickAccessItem(element, add);
        };
        return command;
    }

    internal static XamlUICommand CreateGuardedMenuCommand(string label, Func<bool> canExecute, Action execute)
    {
        var command = new XamlUICommand { Label = label };
        command.CanExecuteRequested += (_, args) => args.CanExecute = canExecute();
        command.ExecuteRequested += (_, _) =>
        {
            // Native/posted menu execution need not be preceded by a fresh CanExecute query.
            if (canExecute())
            {
                execute();
            }
        };
        return command;
    }

    private static Ribbon? ResolveRibbon(object? parameter)
    {
        if (parameter is Ribbon ribbon)
        {
            return ribbon;
        }

        return QuickAccessHelper.FindOwningRibbon(parameter as DependencyObject);
    }
}
