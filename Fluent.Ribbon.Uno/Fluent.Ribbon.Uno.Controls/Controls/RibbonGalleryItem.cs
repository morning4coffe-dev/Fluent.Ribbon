namespace Fluent;

/// <summary>
/// Represents a single selectable item in a <see cref="RibbonGallery"/>.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public partial class RibbonGalleryItem : ContentControl, IKeyTipedControl
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>
    /// Gets or sets whether this item is selected.
    /// </summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsPressed"/> dependency property.</summary>
    public static readonly DependencyProperty IsPressedProperty =
        DependencyProperty.Register(
            nameof(IsPressed),
            typeof(bool),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets whether this item is currently pressed.
    /// </summary>
    public bool IsPressed
    {
        get => (bool)GetValue(IsPressedProperty);
        private set => SetValue(IsPressedProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="PreviewCommand"/> dependency property.</summary>
    public static readonly DependencyProperty PreviewCommandProperty =
        DependencyProperty.Register(
            nameof(PreviewCommand),
            typeof(ICommand),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command invoked to show a live preview when the pointer
    /// enters this item (e.g. previewing a style/color before it is applied).
    /// </summary>
    public ICommand? PreviewCommand
    {
        get => (ICommand?)GetValue(PreviewCommandProperty);
        set => SetValue(PreviewCommandProperty, value);
    }

    /// <summary>Identifies the <see cref="CancelPreviewCommand"/> dependency property.</summary>
    public static readonly DependencyProperty CancelPreviewCommandProperty =
        DependencyProperty.Register(
            nameof(CancelPreviewCommand),
            typeof(ICommand),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command invoked to cancel a live preview when the pointer
    /// leaves this item.
    /// </summary>
    public ICommand? CancelPreviewCommand
    {
        get => (ICommand?)GetValue(CancelPreviewCommandProperty);
        set => SetValue(CancelPreviewCommandProperty, value);
    }

    /// <summary>Identifies the <see cref="Group"/> dependency property.</summary>
    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.Register(
            nameof(Group),
            typeof(string),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the group name for grouping gallery items.
    /// </summary>
    public string Group
    {
        get => (string)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command to execute when this item is clicked.
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
            typeof(RibbonGalleryItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the item is clicked.
    /// </summary>
    public event RoutedEventHandler? Click;

    #endregion

    #region Constructor

    private bool _isPointerOver;

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGalleryItem"/> class.
    /// </summary>
    public RibbonGalleryItem()
    {
        DefaultStyleKey = typeof(RibbonGalleryItem);
        PointerPressed += OnPointerPressedHandler;
        PointerReleased += OnPointerReleasedHandler;
        PointerEntered += OnPointerEnteredHandler;
        PointerExited += OnPointerExitedHandler;
        PointerCanceled += OnPointerExitedHandler;
        PointerCaptureLost += OnPointerExitedHandler;
    }

    #endregion

    #region Methods

    private void OnPointerPressedHandler(object sender, PointerRoutedEventArgs e)
    {
        IsPressed = true;
        UpdateVisualState();

        IsSelected = true;

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }

        Click?.Invoke(this, new RoutedEventArgs());
    }

    private void OnPointerReleasedHandler(object sender, PointerRoutedEventArgs e)
    {
        IsPressed = false;
        UpdateVisualState();
    }

    private void OnPointerEnteredHandler(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        UpdateVisualState();

        if (PreviewCommand?.CanExecute(CommandParameter) == true)
        {
            PreviewCommand.Execute(CommandParameter);
        }
    }

    private void OnPointerExitedHandler(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = false;
        IsPressed = false;
        UpdateVisualState();

        if (CancelPreviewCommand?.CanExecute(CommandParameter) == true)
        {
            CancelPreviewCommand.Execute(CommandParameter);
        }
    }

    private void UpdateVisualState()
    {
        var state = IsPressed ? "Pressed"
            : IsSelected ? "Selected"
            : _isPointerOver ? "PointerOver"
            : "Normal";
        VisualStateManager.GoToState(this, state, true);
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGalleryItem item)
        {
            item.UpdateVisualState();
        }
    }

    #endregion

    #region IKeyTipedControl

    /// <inheritdoc />
    public void OnKeyTipPressed()
    {
        IsSelected = true;

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }

        Click?.Invoke(this, new RoutedEventArgs());
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
    }

    #endregion
}
