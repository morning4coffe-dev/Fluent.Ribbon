namespace Fluent;

/// <summary>
/// Represents a single selectable item in a <see cref="RibbonGallery"/>.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public partial class RibbonGalleryItem : ContentControl
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

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGalleryItem"/> class.
    /// </summary>
    public RibbonGalleryItem()
    {
        DefaultStyleKey = typeof(RibbonGalleryItem);
        PointerPressed += OnPointerPressedHandler;
    }

    #endregion

    #region Methods

    private void OnPointerPressedHandler(object sender, PointerRoutedEventArgs e)
    {
        IsSelected = true;

        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }

        Click?.Invoke(this, new RoutedEventArgs());
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGalleryItem item)
        {
            VisualStateManager.GoToState(item, (bool)e.NewValue ? "Selected" : "Normal", true);
        }
    }

    #endregion
}
