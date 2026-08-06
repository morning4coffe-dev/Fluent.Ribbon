namespace Fluent;

/// <summary>
/// Represents a button in the <see cref="BackstageTabControl"/>.
/// Unlike <see cref="BackstageTabItem"/>, a button does not have content and
/// simply fires a <see cref="Click"/> event.
/// </summary>
public partial class BackstageButton : Control
{
    #region Events

    /// <summary>
    /// Occurs when this button is clicked.
    /// </summary>
#if __ANDROID__ || __IOS__
    public new event RoutedEventHandler? Click;
#else
    public event RoutedEventHandler? Click;
#endif

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(BackstageButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header content.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(BackstageButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(BackstageButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command associated with this button.
    /// </summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(BackstageButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(BackstageButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the key tip string for this button.
    /// </summary>
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <c>IsEnabled</c> dependency property.</summary>
    public static new readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(BackstageButton),
            new PropertyMetadata(true));

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="BackstageButton"/> class.
    /// </summary>
    public BackstageButton()
    {
        DefaultStyleKey = typeof(BackstageButton);
        IsTabStop = true;
        IsEnabledChanged += (_, _) => UpdateVisualState();
    }

    #endregion

    #region Interaction

    private bool _isPointerOver;
    private bool _isPressed;

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState(false);
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        _isPointerOver = true;
        UpdateVisualState();
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerOver = false;
        _isPressed = false;
        UpdateVisualState();
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        _isPressed = true;
        UpdateVisualState();

        InvokeForAutomation();
        e.Handled = true;
    }

    internal void InvokeForAutomation()
    {
        Click?.Invoke(this, new RoutedEventArgs());

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPressed = false;
        UpdateVisualState();
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isPressed = false;
        UpdateVisualState();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled || !IsEnabled)
        {
            return;
        }

        switch (e.Key)
        {
            case Windows.System.VirtualKey.Enter:
                InvokeForAutomation();
                e.Handled = true;
                break;
            case Windows.System.VirtualKey.Space:
                _isPressed = true;
                UpdateVisualState();
                e.Handled = true;
                break;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key != Windows.System.VirtualKey.Space)
        {
            return;
        }

        var shouldInvoke = _isPressed && IsEnabled;
        _isPressed = false;
        UpdateVisualState();
        if (shouldInvoke)
        {
            InvokeForAutomation();
            e.Handled = true;
        }
    }

    private void UpdateVisualState(bool useTransitions = true)
    {
#if WINDOWS
        const string disabledState = "Disabled";
#else
        const string disabledState = "DisabledPortable";
#endif
        string state;

        if (!IsEnabled)
        {
            state = disabledState;
        }
        else if (_isPressed)
        {
            state = "Pressed";
        }
        else if (_isPointerOver)
        {
            state = "PointerOver";
        }
        else
        {
            state = "Normal";
        }

        VisualStateManager.GoToState(this, state, useTransitions);
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonBackstageButtonAutomationPeer(this);
}
