namespace Fluent;

/// <summary>
/// Represents the Start Screen view displayed on application startup.
/// Contains a left pane with recent documents and a right pane with templates/actions.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_LeftPane, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_RightPane, Type = typeof(ContentPresenter))]
public partial class StartScreen : Backstage, IKeyTipedControl
{
    private const string PART_LeftPane = "PART_LeftPane";
    private const string PART_RightPane = "PART_RightPane";
    private WeakReference<UIElement>? _focusBackup;
    private Ribbon? _owningRibbon;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public new static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(StartScreen),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the main content (right pane).
    /// </summary>
    public new object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Identifies the <see cref="LeftPaneContent"/> dependency property.</summary>
    public static readonly DependencyProperty LeftPaneContentProperty =
        DependencyProperty.Register(
            nameof(LeftPaneContent),
            typeof(object),
            typeof(StartScreen),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the left pane content.
    /// </summary>
    public object? LeftPaneContent
    {
        get => GetValue(LeftPaneContentProperty);
        set => SetValue(LeftPaneContentProperty, value);
    }

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public new static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(StartScreen),
            new PropertyMetadata(false, OnIsOpenChanged));

    /// <summary>
    /// Gets or sets whether the start screen is open.
    /// </summary>
    public new bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="Shown"/> dependency property.</summary>
    public static readonly DependencyProperty ShownProperty =
        DependencyProperty.Register(
            nameof(Shown),
            typeof(bool),
            typeof(StartScreen),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether this StartScreen has been shown at least once.
    /// </summary>
    public bool Shown
    {
        get => (bool)GetValue(ShownProperty);
        set => SetValue(ShownProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public new static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(StartScreen),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip used to open the start screen.
    /// </summary>
    public new string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="CloseOnEsc"/> dependency property.</summary>
    public new static readonly DependencyProperty CloseOnEscProperty =
        DependencyProperty.Register(
            nameof(CloseOnEsc),
            typeof(bool),
            typeof(StartScreen),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether Escape closes the start screen.
    /// </summary>
    public new bool CloseOnEsc
    {
        get => (bool)GetValue(CloseOnEscProperty);
        set => SetValue(CloseOnEscProperty, value);
    }

    /// <summary>Identifies the <see cref="LeftPaneWidth"/> dependency property.</summary>
    public static readonly DependencyProperty LeftPaneWidthProperty =
        DependencyProperty.Register(
            nameof(LeftPaneWidth),
            typeof(double),
            typeof(StartScreen),
            new PropertyMetadata(300.0));

    /// <summary>
    /// Gets or sets the width of the left pane.
    /// </summary>
    public double LeftPaneWidth
    {
        get => (double)GetValue(LeftPaneWidthProperty);
        set => SetValue(LeftPaneWidthProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StartScreen"/> class.
    /// </summary>
    public StartScreen()
    {
        DefaultStyleKey = typeof(StartScreen);
        KeyDown += OnStartScreenKeyDown;
        Loaded += OnStartScreenLoaded;
        Unloaded += OnStartScreenUnloaded;
    }

    #endregion

    #region Methods

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Apply the initial visual state now that the template is available. IsOpen may have
        // been set during XAML initialization, before the template was applied, in which case
        // the OnIsOpenChanged GoToState call was a no-op.
        VisualStateManager.GoToState(this, this.IsOpen ? "Open" : "Closed", false);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StartScreen screen)
        {
            var isOpen = (bool)e.NewValue;
            if (isOpen)
            {
                screen.Shown = true;
                screen._focusBackup =
                    FocusRoutingHelper.CaptureFocusedElement(screen, onlyWhenOutsideOwner: true);
            }

            VisualStateManager.GoToState(screen, isOpen ? "Open" : "Closed", true);
            screen.UpdateOwningRibbonState();

            if (isOpen)
            {
                screen.DispatcherQueue?.TryEnqueue(() =>
                {
                    if (screen.IsOpen)
                    {
                        FocusRoutingHelper.FocusFirst(screen);
                    }
                });
            }
            else
            {
                FocusRoutingHelper.RestoreFocus(ref screen._focusBackup);
            }

            if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(screen)
                is Fluent.Automation.Peers.RibbonStartScreenAutomationPeer peer)
            {
                peer.RaiseIsOpenChanged((bool)e.OldValue, isOpen);
            }
        }
    }

    private void OnStartScreenKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (_owningRibbon?.AreAnyKeyTipsVisible == true)
        {
            return;
        }

        if (!args.Handled
            && args.Key == Windows.System.VirtualKey.Escape
            && CloseOnEsc
            && IsOpen)
        {
            IsOpen = false;
            args.Handled = true;
        }
    }

    private void OnStartScreenLoaded(object sender, RoutedEventArgs args)
    {
        if (IsOpen && FocusRoutingHelper.IsEffectivelyVisible(this))
        {
            UpdateOwningRibbonState();
            DispatcherQueue?.TryEnqueue(() =>
            {
                if (IsOpen)
                {
                    FocusRoutingHelper.FocusFirst(this);
                }
            });
        }
    }

    private void OnStartScreenUnloaded(object sender, RoutedEventArgs args)
    {
        _focusBackup = null;
        if (_owningRibbon is not null && IsOpen)
        {
            _owningRibbon.IsBackstageOrStartScreenOpen =
                _owningRibbon.Menu is Backstage { IsOpen: true };
        }

        _owningRibbon = null;
    }

    private void UpdateOwningRibbonState()
    {
        _owningRibbon = FocusRoutingHelper.FindAncestor<Ribbon>(this);
        if (_owningRibbon is null && XamlRoot?.Content is DependencyObject root)
        {
            _owningRibbon = FocusRoutingHelper.FindDescendant<Ribbon>(root);
        }

        if (_owningRibbon is null)
        {
            return;
        }

        // Ribbon.StartScreen is the unambiguous application-level ownership contract.
        // A StartScreen can also be demonstrated inline inside ordinary tab content.
        if (!ReferenceEquals(_owningRibbon.StartScreen, this))
        {
            return;
        }

        if (IsOpen && FocusRoutingHelper.IsEffectivelyVisible(this))
        {
            _owningRibbon.IsBackstageOrStartScreenOpen = true;
        }
        else
        {
            _owningRibbon.IsBackstageOrStartScreenOpen =
                _owningRibbon.Menu is Backstage { IsOpen: true };
        }
    }

    /// <inheritdoc />
    public override KeyTipPressedResult OnKeyTipPressed()
    {
        IsOpen = true;
        return new KeyTipPressedResult(
            pressedElementAquiredFocus: false,
            pressedElementOpenedPopup: true);
    }

    /// <inheritdoc />
    public override void OnKeyTipBack()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Opens the StartScreen if it has not already been shown.
    /// </summary>
    /// <returns><c>true</c> when the StartScreen was opened; otherwise <c>false</c>.</returns>
    protected override bool Show()
    {
        if (Shown)
        {
            return false;
        }

        IsOpen = true;
        return IsOpen;
    }

    /// <summary>
    /// Closes the StartScreen.
    /// </summary>
    protected override void Hide()
    {
        IsOpen = false;
    }

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonStartScreenAutomationPeer(this);

    #endregion
}
