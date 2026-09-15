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
    private bool _isSynchronizingContent;
    private bool? _originalTitleBarCollapsed;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public new static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(StartScreen),
            new PropertyMetadata(null, OnStartScreenContentChanged));

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
            new PropertyMetadata(null, OnLeftPaneContentChanged));

    /// <summary>
    /// Gets or sets the left pane content.
    /// </summary>
    public object? LeftPaneContent
    {
        get => GetValue(LeftPaneContentProperty);
        set => SetValue(LeftPaneContentProperty, value);
    }

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public new static readonly DependencyProperty IsOpenProperty = Backstage.IsOpenProperty;

    /// <summary>
    /// Gets or sets whether the start screen is open.
    /// </summary>
    public new bool IsOpen
    {
        get => base.IsOpen;
        set => base.IsOpen = value;
    }

    /// <summary>Identifies the <see cref="Shown"/> dependency property.</summary>
    public static readonly DependencyProperty ShownProperty =
        DependencyProperty.Register(
            nameof(Shown),
            typeof(bool),
            typeof(StartScreen),
            new PropertyMetadata(false, OnShownChanged));

    /// <summary>
    /// Gets or sets whether this StartScreen has been shown at least once.
    /// </summary>
    public bool Shown
    {
        get => (bool)GetValue(ShownProperty);
        set => SetValue(ShownProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public new static readonly DependencyProperty KeyTipProperty = RibbonControl.KeyTipProperty;

    /// <summary>
    /// Gets or sets the key tip used to open the start screen.
    /// </summary>
    public new string? KeyTip
    {
        get => base.KeyTip;
        set => base.KeyTip = value;
    }

    /// <summary>Identifies the <see cref="CloseOnEsc"/> dependency property.</summary>
    public new static readonly DependencyProperty CloseOnEscProperty = Backstage.CloseOnEscProperty;

    /// <summary>
    /// Gets or sets whether Escape closes the start screen.
    /// </summary>
    public new bool CloseOnEsc
    {
        get => base.CloseOnEsc;
        set => base.CloseOnEsc = value;
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
        Loaded += OnStartScreenLoaded;
        Unloaded += OnStartScreenUnloaded;
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => UpdateTitleBar());
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

    /// <inheritdoc />
    protected override bool HasDisplayContent => Content is not null || LeftPaneContent is not null;

    /// <inheritdoc />
    protected override DependencyObject FocusScope => this;

    /// <inheritdoc />
    protected override bool AffectsParentRibbon => PresentationOwner is not null;

    private static void OnStartScreenContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var screen = (StartScreen)sender;
        if (screen._isSynchronizingContent)
        {
            return;
        }

        screen._isSynchronizingContent = true;
        try
        {
            ((Backstage)screen).Content = args.NewValue is UIElement element
                ? element
                : args.NewValue is null ? null : new ContentPresenter { Content = args.NewValue };
        }
        finally
        {
            screen._isSynchronizingContent = false;
        }

        screen.RefreshRequestedOpenState();
    }

    /// <inheritdoc />
    protected override void OnBackstageContentChanged(UIElement? content)
    {
        if (!_isSynchronizingContent && !ReferenceEquals(Content, content))
        {
            _isSynchronizingContent = true;
            try
            {
                Content = content;
            }
            finally
            {
                _isSynchronizingContent = false;
            }
        }

        base.OnBackstageContentChanged(content);
    }

    private static void OnLeftPaneContentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((StartScreen)sender).RefreshRequestedOpenState();

    private static void OnShownChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (!(bool)args.NewValue)
        {
            ((StartScreen)sender).RefreshRequestedOpenState();
        }
    }

    private void OnStartScreenLoaded(object sender, RoutedEventArgs args)
    {
        UpdateTitleBar();
        if (IsOpen)
        {
            DispatcherQueue?.TryEnqueue(FocusPresentation);
        }
    }

    private void OnStartScreenUnloaded(object sender, RoutedEventArgs args) => RestoreTitleBar();

    private void UpdateTitleBar()
    {
        if (PresentationOwner?.TitleBar is not { } titleBar || !IsOpen)
        {
            return;
        }

        _originalTitleBarCollapsed ??= titleBar.IsCollapsed;
        titleBar.IsCollapsed = Visibility == Visibility.Visible;
    }

    private void RestoreTitleBar()
    {
        if (_originalTitleBarCollapsed is { } value && PresentationOwner?.TitleBar is { } titleBar)
        {
            titleBar.IsCollapsed = value;
        }

        _originalTitleBarCollapsed = null;
    }

    /// <inheritdoc />
    public override KeyTipPressedResult OnKeyTipPressed()
    {
        return base.OnKeyTipPressed();
    }

    /// <inheritdoc />
    public override void OnKeyTipBack()
    {
        base.OnKeyTipBack();
    }

    /// <summary>
    /// Opens the StartScreen if it has not already been shown.
    /// </summary>
    /// <returns><c>true</c> when the StartScreen was opened; otherwise <c>false</c>.</returns>
    protected override bool Show()
    {
        var isInline = PresentationOwner is null && VisualTreeHelper.GetParent(this) is not null;
        if (Shown && !isInline)
        {
            return false;
        }

        if (!base.Show())
        {
            return false;
        }

        Shown = true;
        UpdateTitleBar();
        return true;
    }

    /// <summary>
    /// Closes the StartScreen.
    /// </summary>
    protected override void Hide()
    {
        base.Hide();
        RestoreTitleBar();
    }

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonStartScreenAutomationPeer(this);

    #endregion
}
