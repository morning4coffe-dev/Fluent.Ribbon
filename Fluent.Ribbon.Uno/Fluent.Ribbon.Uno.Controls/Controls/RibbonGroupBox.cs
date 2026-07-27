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

    // Header alignment (WPF SharedSizeGroup emulation). See AlignInputHeaders.
    private bool _headerAlignmentPending;
    private bool _headerLayoutHooked;
    private bool _isAligningHeaders;
    private int _headerAlignmentAttempts;

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
            groupBox.InvalidateHeaderAlignment();
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
        InvalidateHeaderAlignment();
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
        InvalidateHeaderAlignment();
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
        // While the collapsed drop-down is open the items legitimately live in the popup panel.
        // Pulling them back into the in-ribbon panel here would reparent elements out from under
        // an open popup, so sync whichever panel currently owns them.
        var target = _collapsedPopup?.IsOpen == true ? _popupItemsPanel : _itemsPanel;
        if (target is null) return;

        target.Children.Clear();
        foreach (var item in Items)
        {
            DetachFromParent(item);
            target.Children.Add(item);
        }
    }

    /// <summary>
    /// Collapsed groups move their items between the in-ribbon panel and the drop-down panel.
    /// Adding an element that still has a parent throws inside the XAML framework and fail-fasts
    /// the process, so always detach before re-adding.
    /// </summary>
    private static void DetachFromParent(UIElement element)
    {
        // Resolve the host through the visual parent (the elements are hosted directly in a
        // Panel's Children, whose logical Parent reads null on the native WinUI head). Callers
        // must invoke this while that host is still rooted: detaching a realized element from a
        // panel already removed from the visual tree corrupts its native peer, after which
        // re-adding it throws COMException 0x800F1000.
        if (VisualTreeHelper.GetParent(element) is Panel panel)
        {
            panel.Children.Remove(element);
        }
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGroupBox groupBox)
        {
            groupBox.IsCollapsed = (RibbonGroupBoxState)e.NewValue == RibbonGroupBoxState.Collapsed;
            groupBox.UpdateVisualState();
            groupBox.UpdateItemSizes();
            groupBox.InvalidateHeaderAlignment();
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

        // Only one collapsed group may be expanded at a time, matching WPF. Without this the
        // panels stack up and overlap each other and the page body.
        CloseOtherCollapsedPopup(this);
        // Set header text
        if (_popupHeaderText is not null)
        {
            _popupHeaderText.Text = Header?.ToString() ?? string.Empty;
        }

        // Move items from main panel to popup panel at Large size
        _itemsPanel?.Children.Clear();
        _popupItemsPanel.Children.Clear();

        foreach (var item in Items)
        {
            // Set items to Large size for the popup display
            if (item is IScalableRibbonControl scalable)
            {
                scalable.ScaleTo(RibbonControlSize.Large);
            }

            DetachFromParent(item);
            _popupItemsPanel.Children.Add(item);
        }

        // Position the popup below the collapsed button
        if (_collapsedButton is not null)
        {
            try
            {
                var rootContent = XamlRoot?.Content;
                var rootWidth = XamlRoot?.Size.Width ?? 0;
                if (rootContent is not null && rootWidth > 0)
                {
                    var groupPosition = TransformToVisual(rootContent)
                        .TransformPoint(new Windows.Foundation.Point(0, 0));
                    var buttonPosition = _collapsedButton.TransformToVisual(rootContent)
                        .TransformPoint(new Windows.Foundation.Point(0, 0));

                    var popupWidth = 0.0;
                    if (_collapsedPopup.Child is FrameworkElement popupChild)
                    {
                        popupChild.Measure(
                            new Windows.Foundation.Size(
                                rootWidth,
                                double.PositiveInfinity));
                        popupWidth = Math.Min(popupChild.DesiredSize.Width, rootWidth);
                    }

                    var popupLeft = Math.Clamp(
                        buttonPosition.X,
                        0,
                        Math.Max(0, rootWidth - popupWidth));
                    _collapsedPopup.HorizontalOffset = popupLeft - groupPosition.X;
                    _collapsedPopup.VerticalOffset =
                        buttonPosition.Y - groupPosition.Y + _collapsedButton.ActualHeight;
                }
                else
                {
                    var transform = _collapsedButton.TransformToVisual(this);
                    var buttonPos = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
                    _collapsedPopup.HorizontalOffset = buttonPos.X;
                    _collapsedPopup.VerticalOffset = buttonPos.Y + _collapsedButton.ActualHeight;
                }
            }
            catch
            {
                // Fallback: just open at default position
            }
        }

        FlyoutShowHelper.OpenDeferred(_collapsedPopup);
        _openCollapsedGroup = new WeakReference<RibbonGroupBox>(this);
        if (!IsDropDownOpen)
        {
            IsDropDownOpen = true;
        }
    }

    /// <summary>
    /// Tracks the collapsed group whose drop-down is currently open so a newly opened group can
    /// close it. A weak reference keeps this from rooting group boxes for the app's lifetime.
    /// </summary>
    private static WeakReference<RibbonGroupBox>? _openCollapsedGroup;

    private static void CloseOtherCollapsedPopup(RibbonGroupBox opening)
    {
        if (_openCollapsedGroup is null
            || !_openCollapsedGroup.TryGetTarget(out var previous)
            || ReferenceEquals(previous, opening))
        {
            return;
        }

        _openCollapsedGroup = null;
        previous.CollapseForAutomation();
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
        if (_openCollapsedGroup is not null
            && _openCollapsedGroup.TryGetTarget(out var open)
            && ReferenceEquals(open, this))
        {
            _openCollapsedGroup = null;
        }

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
        InvalidateHeaderAlignment();
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
            else if (item is DependencyObject container)
            {
                // Items can also be plain layout panels (StackPanel/Grid) that host ribbon
                // controls. WPF's RibbonGroupBox recurses into such panels and sizes every
                // nested control from its SizeDefinition; mirror that so controls hosted in
                // raw panels are not left in their default (Small) visual state.
                ApplyNestedItemSizes(container);
            }
        }
    }

    /// <summary>
    /// Recursively sizes ribbon controls nested inside a non-scalable layout panel item.
    /// Each nested <see cref="IScalableRibbonControl"/> is scaled from its
    /// <c>SizeDefinition</c> resolved against the current group state, matching the way the
    /// WPF <c>RibbonGroupBox</c> propagates sizes into panel children.
    /// </summary>
    private void ApplyNestedItemSizes(DependencyObject element)
    {
        if (element is not Panel panel)
        {
            return;
        }

        foreach (var child in panel.Children)
        {
            if (child is IScalableRibbonControl scalable)
            {
                var resolved = RibbonProperties.GetSizeDefinition(child).GetSize(State);
                scalable.ScaleTo(resolved);
            }
            else
            {
                ApplyNestedItemSizes(child);
            }
        }
    }

    // WPF aligns the header/label of every input control in a group by placing the header column
    // of Spinner/TextBox/ComboBox into a Grid.IsSharedSizeScope with a shared SharedSizeGroup, so
    // all inputs start at the same x regardless of individual header text length. WinUI / Uno has
    // no Grid.IsSharedSizeScope, so the group emulates it here: it measures the natural width of
    // every input header and imposes the widest one on all of them.
    private const int MaxHeaderAlignmentAttempts = 16;

    /// <summary>
    /// Requests a header-alignment pass (see <see cref="AlignInputHeaders"/>). The pass is deferred
    /// until after the next layout via <see cref="FrameworkElement.LayoutUpdated"/> so it never
    /// mutates element sizes during a measure pass — this codebase has repeatedly hit
    /// <c>COMException 0x800F1000</c> when the visual tree is touched mid-measure.
    /// </summary>
    private void InvalidateHeaderAlignment()
    {
        _headerAlignmentPending = true;
        _headerAlignmentAttempts = 0;

        if (!_headerLayoutHooked)
        {
            LayoutUpdated += OnHeaderAlignmentLayoutUpdated;
            _headerLayoutHooked = true;
        }
    }

    private void OnHeaderAlignmentLayoutUpdated(object? sender, object e)
    {
        if (!_headerAlignmentPending)
        {
            UnhookHeaderAlignmentLayout();
            return;
        }

        _headerAlignmentAttempts++;
        var completed = AlignInputHeaders();

        // Stop once the widths are aligned, or give up after a bounded number of retries (a later
        // items/size/simplified change will re-request the pass). This keeps the LayoutUpdated hook
        // attached only briefly, during convergence.
        if (completed || _headerAlignmentAttempts >= MaxHeaderAlignmentAttempts)
        {
            _headerAlignmentPending = false;
            UnhookHeaderAlignmentLayout();
        }
    }

    private void UnhookHeaderAlignmentLayout()
    {
        if (_headerLayoutHooked)
        {
            LayoutUpdated -= OnHeaderAlignmentLayoutUpdated;
            _headerLayoutHooked = false;
        }
    }

    /// <summary>
    /// Sizes the header of every <see cref="IRibbonHeaderAlignable"/> item in this group to the
    /// widest natural header width, so all inputs start at the same x. This is a no-op for groups
    /// with fewer than two visible header-bearing controls. It only assigns a width when the value
    /// actually changes and only reads/writes element widths (never the visual tree), so it is safe
    /// to run after layout. Returns <see langword="true"/> when every alignable item's template was
    /// resolved (nothing left to retry).
    /// </summary>
    private bool AlignInputHeaders()
    {
        if (_isAligningHeaders)
        {
            return false;
        }

        List<FrameworkElement>? headers = null;
        var allResolved = true;

        foreach (var item in Items)
        {
            if (item is not IRibbonHeaderAlignable alignable)
            {
                continue;
            }

            var header = alignable.HeaderPresenter;
            if (header is null)
            {
                // Template not applied yet; retry after the next layout pass.
                allResolved = false;
                continue;
            }

            if (header.Visibility != Visibility.Visible)
            {
                continue;
            }

            (headers ??= new List<FrameworkElement>()).Add(header);
        }

        // Fewer than two visible headers: release any width previously imposed so a lone control
        // sizes to its own content again.
        if (headers is null || headers.Count < 2)
        {
            ResetImposedHeaderWidths();
            return allResolved;
        }

        _isAligningHeaders = true;
        try
        {
            var max = 0.0;
            foreach (var header in headers)
            {
                // Drop any previously imposed width so the header reports its natural (content) width.
                if (!double.IsNaN(header.Width))
                {
                    header.Width = double.NaN;
                }

                header.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));

                var natural = header.DesiredSize.Width - header.Margin.Left - header.Margin.Right;
                if (natural > max)
                {
                    max = natural;
                }
            }

            if (max > 0)
            {
                foreach (var header in headers)
                {
                    if (header.Width != max)
                    {
                        header.Width = max;
                    }
                }
            }
        }
        finally
        {
            _isAligningHeaders = false;
        }

        return allResolved;
    }

    private void ResetImposedHeaderWidths()
    {
        foreach (var item in Items)
        {
            if (item is IRibbonHeaderAlignable alignable
                && alignable.HeaderPresenter is { } header
                && !double.IsNaN(header.Width))
            {
                header.Width = double.NaN;
            }
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonGroupBoxAutomationPeer(this);
}
