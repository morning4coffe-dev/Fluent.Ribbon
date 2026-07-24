namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents a group of controls within a RibbonTab.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_ItemsPanel, Type = typeof(StackPanel))]
[TemplatePart(Name = PART_HeaderPresenter, Type = typeof(ContentControl))]
[TemplatePart(Name = PART_CollapsedButton, Type = typeof(WinUIButton))]
public partial class RibbonGroupBox : HeaderedItemsControl, IHeaderedControl
{
    private static readonly RibbonGroupBoxStateDefinition DefaultSimplifiedStateDefinition =
        new("Large,Middle,Collapsed");

    private const string PART_ItemsPanel = "PART_ItemsPanel";
    private const string PART_HeaderPresenter = "PART_HeaderPresenter";
    private const string PART_CollapsedButton = "PART_CollapsedButton";
    private const string PART_CollapsedPopup = "PART_CollapsedPopup";
    private const string PART_PopupItemsPanel = "PART_PopupItemsPanel";
    private const string PART_PopupHeaderText = "PART_PopupHeaderText";

    private Panel? _itemsPanel;
    private WinUIButton? _collapsedButton;
    private Popup? _collapsedPopup;
    private StackPanel? _popupItemsPanel;
    private TextBlock? _popupHeaderText;

    /// <summary>
    /// Stores the authored (preferred) size of each scalable item, captured before any
    /// group-driven scaling occurs. This lets a group honor per-control sizes
    /// (e.g. a large Paste next to small Cut/Copy) instead of forcing every item
    /// to the group's uniform size.
    /// </summary>
    private readonly Dictionary<UIElement, RibbonControlSize> _preferredSizes = new();

    #region Dependency Properties

    /// <summary>
    /// Identifies whether a header presenter belongs to the collapsed group surface.
    /// </summary>
    public static readonly DependencyProperty IsCollapsedHeaderContentPresenterProperty =
        DependencyProperty.RegisterAttached(
            "IsCollapsedHeaderContentPresenter",
            typeof(bool),
            typeof(RibbonGroupBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Sets whether a header presenter belongs to the collapsed group surface.
    /// </summary>
    public static void SetIsCollapsedHeaderContentPresenter(
        DependencyObject element,
        bool value)
    {
        element.SetValue(IsCollapsedHeaderContentPresenterProperty, value);
    }

    /// <summary>
    /// Gets whether a header presenter belongs to the collapsed group surface.
    /// </summary>
    public static bool GetIsCollapsedHeaderContentPresenter(DependencyObject element)
    {
        return (bool)element.GetValue(IsCollapsedHeaderContentPresenterProperty);
    }

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header content of the group.
    /// </summary>
    public new object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of items in this group.
    /// </summary>
    public new ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="State"/> dependency property.</summary>
    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(
            nameof(State),
            typeof(RibbonGroupBoxState),
            typeof(RibbonGroupBox),
            new PropertyMetadata(RibbonGroupBoxState.Large, OnStateChanged));

    /// <summary>
    /// Gets or sets the current state of the group.
    /// </summary>
    public RibbonGroupBoxState State
    {
        get => (RibbonGroupBoxState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null, OnAnyIconChanged));

    /// <summary>
    /// Gets or sets the icon displayed when the group is collapsed.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="MediumIcon"/> dependency property.</summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null, OnAnyIconChanged));

    /// <summary>
    /// Gets or sets the medium-sized icon for the group.
    /// </summary>
    public object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(object),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null, OnAnyIconChanged));

    /// <summary>
    /// Gets or sets the large-sized icon for the group.
    /// </summary>
    public object? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>Identifies the <see cref="CollapsedIcon"/> dependency property.</summary>
    public static readonly DependencyProperty CollapsedIconProperty =
        DependencyProperty.Register(
            nameof(CollapsedIcon),
            typeof(object),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the icon shown in the collapsed group button, preferring
    /// <see cref="LargeIcon"/>, then <see cref="MediumIcon"/>, then <see cref="Icon"/>.
    /// </summary>
    public object? CollapsedIcon
    {
        get => GetValue(CollapsedIconProperty);
        private set => SetValue(CollapsedIconProperty, value);
    }

    private static void OnAnyIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var groupBox = (RibbonGroupBox)d;
        groupBox.CollapsedIcon = groupBox.LargeIcon ?? groupBox.MediumIcon ?? groupBox.Icon;
    }

    /// <summary>Identifies the <see cref="IsCollapsed"/> dependency property.</summary>
    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(
            nameof(IsCollapsed),
            typeof(bool),
            typeof(RibbonGroupBox),
            new PropertyMetadata(false, OnIsCollapsedChanged));

    /// <summary>
    /// Gets or sets whether the group is collapsed.
    /// </summary>
    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsLauncherVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsLauncherVisibleProperty =
        DependencyProperty.Register(
            nameof(IsLauncherVisible),
            typeof(bool),
            typeof(RibbonGroupBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the dialog launcher button is visible.
    /// </summary>
    public bool IsLauncherVisible
    {
        get => (bool)GetValue(IsLauncherVisibleProperty);
        set => SetValue(IsLauncherVisibleProperty, value);
    }

    /// <summary>Identifies the <see cref="LauncherCommand"/> dependency property.</summary>
    public static readonly DependencyProperty LauncherCommandProperty =
        DependencyProperty.Register(
            nameof(LauncherCommand),
            typeof(ICommand),
            typeof(RibbonGroupBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command for the dialog launcher.
    /// </summary>
    public ICommand? LauncherCommand
    {
        get => (ICommand?)GetValue(LauncherCommandProperty);
        set => SetValue(LauncherCommandProperty, value);
    }

    /// <summary>
    /// Occurs when the dialog launcher button is clicked.
    /// </summary>
    public event RoutedEventHandler? LauncherClick;

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(RibbonGroupBox),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph for the collapsed state display.
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonGroupBox),
            new PropertyMetadata(false, OnIsSimplifiedChanged));

    /// <summary>
    /// Gets or sets whether this group box is in simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    private static void OnIsSimplifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGroupBox groupBox)
        {
            var isSimplified = (bool)e.NewValue;
            groupBox.State = groupBox.GetInitialStateForMode(isSimplified);

            foreach (var item in groupBox.Items)
            {
                if (item is ISimplifiedStateControl simplifiedControl)
                {
                    simplifiedControl.UpdateSimplifiedState(isSimplified);
                }
            }

            groupBox.UpdateVisualState();
            groupBox.UpdateItemSizes();
        }
    }

    /// <summary>Identifies the <see cref="StateDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty StateDefinitionProperty =
        DependencyProperty.Register(
            nameof(StateDefinition),
            typeof(RibbonGroupBoxStateDefinition),
            typeof(RibbonGroupBox),
            new PropertyMetadata(default(RibbonGroupBoxStateDefinition)));

    /// <summary>
    /// Gets or sets the state definition that controls how this group transitions between states.
    /// </summary>
    public RibbonGroupBoxStateDefinition StateDefinition
    {
        get => (RibbonGroupBoxStateDefinition)GetValue(StateDefinitionProperty);
        set => SetValue(StateDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="SimplifiedStateDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SimplifiedStateDefinitionProperty =
        DependencyProperty.Register(
            nameof(SimplifiedStateDefinition),
            typeof(RibbonGroupBoxStateDefinition),
            typeof(RibbonGroupBox),
            new PropertyMetadata(DefaultSimplifiedStateDefinition));

    /// <summary>
    /// Gets or sets the state definition for simplified mode.
    /// </summary>
    public RibbonGroupBoxStateDefinition SimplifiedStateDefinition
    {
        get => (RibbonGroupBoxStateDefinition)GetValue(SimplifiedStateDefinitionProperty);
        set => SetValue(SimplifiedStateDefinitionProperty, value);
    }

    #endregion

    #region Intermediate State (used by RibbonGroupsContainer for reduce/enlarge)

    /// <summary>
    /// Gets or sets the intermediate state used during layout calculations.
    /// This is set by <see cref="RibbonGroupsContainer"/> and applied after measurement.
    /// </summary>
    internal RibbonGroupBoxState StateIntermediate { get; set; } = RibbonGroupBoxState.Large;

    /// <summary>
    /// Gets or sets the intermediate scale used during layout calculations.
    /// Positive values enlarge scalable children; negative values reduce them.
    /// </summary>
    internal int ScaleIntermediate { get; set; }

    internal RibbonGroupBoxStateDefinition GetStateDefinitionForMode(bool isSimplified)
        => isSimplified ? SimplifiedStateDefinition : StateDefinition;

    internal RibbonGroupBoxState GetInitialStateForMode(bool isSimplified)
    {
        var definition = GetStateDefinitionForMode(isSimplified);
        if (isSimplified && definition == DefaultSimplifiedStateDefinition)
        {
            return RibbonGroupBoxState.Medium;
        }

        return definition.States[0];
    }

    /// <summary>
    /// Gets the desired size using the intermediate state, without committing the state change.
    /// </summary>
    internal Windows.Foundation.Size GetDesiredSizeIntermediate()
    {
        var previousState = State;
        State = StateIntermediate;
        Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
        var result = DesiredSize;
        State = previousState;
        return result;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGroupBox"/> class.
    /// </summary>
    public RibbonGroupBox()
    {
        DefaultStyleKey = typeof(RibbonGroupBox);
        Items = new ObservableCollection<UIElement>();
        Items.CollectionChanged += OnItemsCollectionChanged;
        InitializeCompatibility();
        QuickAccessHelper.AttachContextMenu(this);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _itemsPanel = GetTemplateChild(PART_ItemsPanel) as Panel;

        if (_collapsedButton is not null)
        {
            _collapsedButton.Click -= OnCollapsedButtonClick;
        }

        _collapsedButton = GetTemplateChild(PART_CollapsedButton) as WinUIButton;

        if (_collapsedButton is not null)
        {
            _collapsedButton.Click += OnCollapsedButtonClick;
        }

        // Wire up popup parts
        if (_collapsedPopup is not null)
        {
            _collapsedPopup.Closed -= OnCollapsedPopupClosed;
        }

        _collapsedPopup = GetTemplateChild(PART_CollapsedPopup) as Popup;
        _popupItemsPanel = GetTemplateChild(PART_PopupItemsPanel) as StackPanel;
        _popupHeaderText = GetTemplateChild(PART_PopupHeaderText) as TextBlock;

        if (_collapsedPopup is not null)
        {
            _collapsedPopup.Closed += OnCollapsedPopupClosed;
        }
        
        var launcherBtn = GetTemplateChild("LauncherButton") as Button;
        ApplyCompatibilityTemplateParts(launcherBtn);
        if (launcherBtn is not null)
        {
            launcherBtn.Click -= OnLauncherButtonClick;
            launcherBtn.Click += OnLauncherButtonClick;
        }

        SyncItems();
        UpdateVisualState();
    }
    
    private void OnLauncherButtonClick(object sender, RoutedEventArgs e)
    {
        LauncherClick?.Invoke(this, e);
    }

    #endregion

    #region Methods

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnItemsChanged(e);
    }

    /// <summary>Handles item collection changes.</summary>
    protected virtual void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            _preferredSizes.Clear();
        }
        else if (e.OldItems is not null)
        {
            foreach (var old in e.OldItems)
            {
                if (old is UIElement element)
                {
                    _preferredSizes.Remove(element);
                }
            }
        }

        // Capture authored sizes for any newly added scalable items *before* the group
        // applies its own state-based sizing, so per-control sizes are preserved.
        CapturePreferredSizes();
        SyncItems();
        UpdateItemSizes();
    }

    /// <summary>Handles the WPF-compatible primary-pointer hook.</summary>
    protected virtual void OnMouseLeftButtonDown(PointerRoutedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, this) && IsInButtonState)
        {
            IsDropDownOpen = true;
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.Handled)
        {
            OnMouseLeftButtonDown(e);
        }
    }

    /// <summary>
    /// Records the authored size of each scalable item the first time it is seen.
    /// Once captured, an item's preferred size is never overwritten by group scaling.
    /// </summary>
    private void CapturePreferredSizes()
    {
        foreach (var item in Items)
        {
            if (item is IScalableRibbonControl scalable && !_preferredSizes.ContainsKey(item))
            {
                _preferredSizes[item] = scalable.Size;
            }
        }
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

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGroupBox groupBox)
        {
            groupBox.IsCollapsed = (RibbonGroupBoxState)e.NewValue == RibbonGroupBoxState.Collapsed;
            groupBox.UpdateVisualState();
            groupBox.UpdateItemSizes();
        }
    }

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGroupBox groupBox)
        {
            groupBox.UpdateVisualState();
        }
    }

    private void OnCollapsedButtonClick(object sender, RoutedEventArgs e)
        => ExpandForAutomation();

    internal bool IsDropDownOpenForAutomation => _collapsedPopup?.IsOpen == true;

    internal void ExpandForAutomation()
    {
        if (_collapsedPopup is null || _popupItemsPanel is null) return;
        if (_collapsedPopup.IsOpen) return; // Prevent double-click

        // Set header text
        if (_popupHeaderText is not null)
        {
            _popupHeaderText.Text = Header?.ToString() ?? string.Empty;
        }

        // Move items from main panel to popup panel at Large size
        _itemsPanel?.Children.Clear();

        foreach (var item in Items)
        {
            // Set items to Large size for the popup display
            if (item is IScalableRibbonControl scalable)
            {
                scalable.ScaleTo(RibbonControlSize.Large);
            }

            _popupItemsPanel.Children.Add(item);
        }

        // Position the popup below the collapsed button
        if (_collapsedButton is not null)
        {
            try
            {
                var transform = _collapsedButton.TransformToVisual(this);
                var buttonPos = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                _collapsedPopup.HorizontalOffset = buttonPos.X;
                _collapsedPopup.VerticalOffset = buttonPos.Y + _collapsedButton.ActualHeight;
            }
            catch
            {
                // Fallback: just open at default position
            }
        }

        _collapsedPopup.IsOpen = true;
        if (!IsDropDownOpen)
        {
            IsDropDownOpen = true;
        }
    }

    internal void CollapseForAutomation()
    {
        if (_collapsedPopup is not null)
        {
            _collapsedPopup.IsOpen = false;
        }
    }

    private void OnCollapsedPopupClosed(object? sender, object e)
    {
        if (IsDropDownOpen)
        {
            IsDropDownOpen = false;
        }

        if (_popupItemsPanel is null) return;

        // Move items back from popup panel to main panel
        _popupItemsPanel.Children.Clear();

        // Re-sync items back to the main panel and restore sizes
        SyncItems();
        UpdateItemSizes();
    }

    private void UpdateVisualState()
    {
        var stateName = State switch
        {
            RibbonGroupBoxState.Large => "Large",
            RibbonGroupBoxState.Medium => "Medium",
            RibbonGroupBoxState.Small => "Small",
            RibbonGroupBoxState.Collapsed => "Collapsed",
            _ => "Large"
        };

        VisualStateManager.GoToState(this, stateName, true);
        VisualStateManager.GoToState(this, IsSimplified ? "Simplified" : "Classic", true);
    }

    private void UpdateItemSizes()
    {
        // The group state acts as a cap: controls may be their authored size or smaller,
        // but never larger than what the current group state allows.
        var cap = State switch
        {
            RibbonGroupBoxState.Large => RibbonControlSize.Large,
            RibbonGroupBoxState.Medium => RibbonControlSize.Medium,
            RibbonGroupBoxState.Small or RibbonGroupBoxState.Collapsed => RibbonControlSize.Small,
            _ => RibbonControlSize.Large
        };

        foreach (var item in Items)
        {
            if (item is IScalableRibbonControl scalable)
            {
                var preferred = _preferredSizes.TryGetValue(item, out var p) ? p : scalable.Size;

                // RibbonControlSize orders Large(0) < Medium(1) < Small(2), so a larger
                // enum value means a smaller control. Clamp to the group cap by taking
                // whichever is the smaller control (the higher enum value).
                var effective = (RibbonControlSize)System.Math.Max((int)preferred, (int)cap);
                scalable.ScaleTo(effective);
            }
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonGroupBoxAutomationPeer(this);
}
