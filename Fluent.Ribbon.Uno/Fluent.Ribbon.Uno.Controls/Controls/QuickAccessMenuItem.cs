namespace Fluent;

/// <summary>
/// Represents a menu item for adding/removing items from the quick access toolbar.
/// </summary>
public partial class QuickAccessMenuItem : Control, IHeaderedControl
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(QuickAccessMenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the menu item.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Target"/> dependency property.</summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(UIElement),
            typeof(QuickAccessMenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the target element to add/remove from the quick access toolbar.
    /// </summary>
    public UIElement? Target
    {
        get => (UIElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(QuickAccessMenuItem),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the item is checked (shown in the QAT).
    /// </summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickAccessMenuItem"/> class.
    /// </summary>
    public QuickAccessMenuItem()
    {
        DefaultStyleKey = typeof(QuickAccessMenuItem);
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        IsChecked = !IsChecked;
        e.Handled = true;
    }

    #endregion
}
