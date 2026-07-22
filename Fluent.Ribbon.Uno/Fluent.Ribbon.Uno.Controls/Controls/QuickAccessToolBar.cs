namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents the Quick Access Toolbar displayed in the ribbon title bar area.
/// Supports overflow items that move to a dropdown when there isn't enough horizontal space.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_ToolBarPanel, Type = typeof(StackPanel))]
[TemplatePart(Name = PART_OverflowButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_OverflowPanel, Type = typeof(StackPanel))]
[TemplatePart(Name = PART_MenuButton, Type = typeof(WinUIButton))]
public partial class QuickAccessToolBar : Control
{
    private const string PART_ToolBarPanel = "PART_ToolBarPanel";
    private const string PART_OverflowButton = "PART_OverflowButton";
    private const string PART_OverflowPanel = "PART_OverflowPanel";
    private const string PART_MenuButton = "PART_MenuButton";

    private StackPanel? _toolBarPanel;
    private WinUIButton? _overflowButton;
#pragma warning disable CS0169
    private StackPanel? _overflowPanel;
#pragma warning restore CS0169
    private WinUIButton? _menuButton;
    private Flyout? _overflowFlyout;
    private Flyout? _menuFlyout;

    #region Events

    /// <summary>
    /// Occurs when the user requests to show the QAT above or below the ribbon.
    /// </summary>
    public event EventHandler<bool>? ShowAboveRibbonChanged;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(QuickAccessToolBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of quick access toolbar items.
    /// </summary>
    public ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="HasOverflowItems"/> dependency property.</summary>
    public static readonly DependencyProperty HasOverflowItemsProperty =
        DependencyProperty.Register(
            nameof(HasOverflowItems),
            typeof(bool),
            typeof(QuickAccessToolBar),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets whether the toolbar has overflow items.
    /// </summary>
    public bool HasOverflowItems
    {
        get => (bool)GetValue(HasOverflowItemsProperty);
        private set => SetValue(HasOverflowItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="ShowAboveRibbon"/> dependency property.</summary>
    public static readonly DependencyProperty ShowAboveRibbonProperty =
        DependencyProperty.Register(
            nameof(ShowAboveRibbon),
            typeof(bool),
            typeof(QuickAccessToolBar),
            new PropertyMetadata(true, OnShowAboveRibbonChanged));

    /// <summary>
    /// Gets or sets whether the toolbar is shown above the ribbon (in the title bar)
    /// or below the ribbon.
    /// </summary>
    public bool ShowAboveRibbon
    {
        get => (bool)GetValue(ShowAboveRibbonProperty);
        set => SetValue(ShowAboveRibbonProperty, value);
    }

    /// <summary>Identifies the <see cref="CanQuickAccessLocationChanging"/> dependency property.</summary>
    public static readonly DependencyProperty CanQuickAccessLocationChangingProperty =
        DependencyProperty.Register(
            nameof(CanQuickAccessLocationChanging),
            typeof(bool),
            typeof(QuickAccessToolBar),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the user can change the QAT location (above/below ribbon).
    /// </summary>
    public bool CanQuickAccessLocationChanging
    {
        get => (bool)GetValue(CanQuickAccessLocationChangingProperty);
        set => SetValue(CanQuickAccessLocationChangingProperty, value);
    }

    /// <summary>Identifies the <see cref="IsMenuDropDownVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsMenuDropDownVisibleProperty =
        DependencyProperty.Register(
            nameof(IsMenuDropDownVisible),
            typeof(bool),
            typeof(QuickAccessToolBar),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the menu dropdown button is visible.
    /// </summary>
    public bool IsMenuDropDownVisible
    {
        get => (bool)GetValue(IsMenuDropDownVisibleProperty);
        set => SetValue(IsMenuDropDownVisibleProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickAccessToolBar"/> class.
    /// </summary>
    public QuickAccessToolBar()
    {
        DefaultStyleKey = typeof(QuickAccessToolBar);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
        SizeChanged += OnSizeChanged;
        InitializeCompatibility();
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _toolBarPanel = GetTemplateChild(PART_ToolBarPanel) as StackPanel;

        if (_overflowButton is not null)
        {
            _overflowButton.Click -= OnOverflowButtonClick;
        }

        _overflowButton = GetTemplateChild(PART_OverflowButton) as WinUIButton;

        if (_overflowButton is not null)
        {
            _overflowButton.Click += OnOverflowButtonClick;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                _overflowButton,
                RibbonLocalization.Current.Localization.QuickAccessToolBarMoreControlsButtonTooltip);
        }

        if (_menuButton is not null)
        {
            _menuButton.Click -= OnMenuButtonClick;
        }

        _menuButton = GetTemplateChild(PART_MenuButton) as WinUIButton;

        if (_menuButton is not null)
        {
            _menuButton.Click += OnMenuButtonClick;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                _menuButton,
                RibbonLocalization.Current.Localization.QuickAccessToolBarDropDownButtonTooltip);
        }

        SyncItems();
    }

    #endregion

    #region Methods

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncItems();
        OnCompatibilityItemsChanged(e);
    }

    private void SyncItems()
    {
        if (_toolBarPanel is null) return;

        _toolBarPanel.Children.Clear();
        foreach (var item in Items)
        {
            _toolBarPanel.Children.Add(item);
        }

        UpdateOverflow();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateOverflow();
    }

    /// <summary>
    /// Calculates which items overflow and updates the overflow button visibility.
    /// </summary>
    private void UpdateOverflow(double? availableWidthOverride = null)
    {
        if (_toolBarPanel is null || _overflowButton is null) return;

        var availableWidth = availableWidthOverride ?? ActualWidth;
        if (availableWidth <= 0 || double.IsInfinity(availableWidth)) return;

        // Reserve space for the overflow and menu buttons
        var reservedWidth = 0.0;
        if (_overflowButton is not null)
        {
            _overflowButton.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            reservedWidth += Math.Max(
                _overflowButton.DesiredSize.Width,
                _overflowButton.MinWidth);
        }

        if (_menuButton is not null && IsMenuDropDownVisible)
        {
            _menuButton.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            reservedWidth += _menuButton.DesiredSize.Width;
        }

        var usableWidth = availableWidth - reservedWidth;
        var accumulatedWidth = 0.0;
        var hasOverflow = false;

        var measuredChildren = new List<(FrameworkElement Child, double Width)>();
        for (var i = 0; i < _toolBarPanel.Children.Count; i++)
        {
            var child = _toolBarPanel.Children[i] as FrameworkElement;
            if (child is null)
            {
                continue;
            }

            if (_overflowedItems.Remove(child))
            {
                child.Visibility = Visibility.Visible;
            }

            if (child.Visibility != Visibility.Visible)
            {
                continue;
            }

            child.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            measuredChildren.Add((child, child.DesiredSize.Width));
        }

        foreach (var (child, width) in measuredChildren)
        {
            accumulatedWidth += width;

            if (accumulatedWidth > usableWidth)
            {
                child.Visibility = Visibility.Collapsed;
                _overflowedItems.Add(child);
                hasOverflow = true;
            }
        }

        HasOverflowItems = hasOverflow;
        if (_overflowButton is not null)
        {
            _overflowButton.Visibility = hasOverflow ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnOverflowButtonClick(object sender, RoutedEventArgs e)
    {
        if (_overflowFlyout is null)
        {
            _overflowFlyout = new Flyout
            {
                Placement = FlyoutPlacementMode.Bottom
            };
        }

        // Build overflow content with hidden items
        var panel = new StackPanel { MinWidth = 120 };
        if (_toolBarPanel is not null)
        {
            var overflowIndex = 0;
            foreach (var child in _toolBarPanel.Children)
            {
                if (child is FrameworkElement fe && _overflowedItems.Contains(fe))
                {
                    // Create a proxy representation for the overflow item
                    var header = "Item";
                    if (child is RibbonButton rb) header = rb.Header?.ToString() ?? "Button";
                    else if (child is IHeaderedControl hc) header = hc.Header?.ToString() ?? "Item";

                    var menuItem = new WinUIButton
                    {
                        Content = header,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                        BorderThickness = new Thickness(0),
                        Padding = new Thickness(12, 6, 12, 6),
                    };
                    Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
                        menuItem,
                        $"QuickAccessOverflowItem{overflowIndex++}");
                    Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(menuItem, header);

                    // Click restores original item's click
                    var original = fe;
                    menuItem.Click += (s, args) =>
                    {
                        _overflowFlyout?.Hide();
                        InvokeOverflowItem(original);
                    };

                    panel.Children.Add(menuItem);
                }
            }
        }

        _overflowFlyout.Content = panel;
        _overflowFlyout.ShowAt(_overflowButton!);
    }

    private void OnMenuButtonClick(object sender, RoutedEventArgs e)
    {
        if (_menuFlyout is null)
        {
            _menuFlyout = new Flyout
            {
                Placement = FlyoutPlacementMode.Bottom
            };
        }

        var panel = new StackPanel { MinWidth = 200 };
        AddQuickAccessCustomizationItems(panel);

        if (CanQuickAccessLocationChanging)
        {
            var locationLabel = ShowAboveRibbon
                ? RibbonLocalization.Current.Localization.QuickAccessToolBarMenuShowBelow
                : RibbonLocalization.Current.Localization.QuickAccessToolBarMenuShowAbove;

            var locationButton = new WinUIButton
            {
                Content = locationLabel,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(12, 8, 12, 8),
            };
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(
                locationButton,
                "QuickAccessLocationButton");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(
                locationButton,
                locationLabel);

            locationButton.Click += (s, args) =>
            {
                _menuFlyout?.Hide();
                ShowAboveRibbon = !ShowAboveRibbon;
            };

            panel.Children.Add(locationButton);
        }

        _menuFlyout.Content = panel;
        _menuFlyout.ShowAt(_menuButton!);
    }

    private static void InvokeOverflowItem(FrameworkElement original)
    {
        var peer =
            Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
                .FromElement(original)
            ?? Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer
                .CreatePeerForElement(original);
        if (peer is null)
        {
            return;
        }

        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
            is Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invokeProvider)
        {
            invokeProvider.Invoke();
            return;
        }

        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Toggle)
            is Microsoft.UI.Xaml.Automation.Provider.IToggleProvider toggleProvider)
        {
            toggleProvider.Toggle();
            return;
        }

        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.ExpandCollapse)
            is Microsoft.UI.Xaml.Automation.Provider.IExpandCollapseProvider expandCollapseProvider)
        {
            expandCollapseProvider.Expand();
        }
    }

    private static void OnShowAboveRibbonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is QuickAccessToolBar qat)
        {
            qat.ShowAboveRibbonChanged?.Invoke(qat, (bool)e.NewValue);
        }
    }

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonQuickAccessToolBarAutomationPeer(this);

    #endregion
}
