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
    public event RoutedEventHandler? Click;

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

    /// <summary>Identifies the <see cref="IsEnabled"/> dependency property.</summary>
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
    }

    #endregion

    #region Interaction

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        Click?.Invoke(this, new RoutedEventArgs());

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }

        e.Handled = true;
    }

    #endregion
}
