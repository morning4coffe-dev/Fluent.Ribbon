namespace Fluent;

/// <summary>
/// Represents a menu item in the <see cref="RibbonStatusBar"/>.
/// Acts as a toggleable item for showing/hiding status bar elements.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
public partial class StatusBarMenuItem : InteractiveMenuItemBase
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(StatusBarMenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header text.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(StatusBarMenuItem),
            new PropertyMetadata(true, OnIsCheckedChanged));

    /// <summary>
    /// Gets or sets whether the status bar item is visible.
    /// </summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Identifies the <see cref="StatusBarItem"/> dependency property.</summary>
    public static readonly DependencyProperty StatusBarItemProperty =
        DependencyProperty.Register(
            nameof(StatusBarItem),
            typeof(UIElement),
            typeof(StatusBarMenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the linked status bar item whose visibility is controlled.
    /// </summary>
    public UIElement? StatusBarItem
    {
        get => (UIElement?)GetValue(StatusBarItemProperty);
        set => SetValue(StatusBarItemProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusBarMenuItem"/> class.
    /// </summary>
    public StatusBarMenuItem()
    {
        DefaultStyleKey = typeof(StatusBarMenuItem);
    }

    #endregion

    #region Methods

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBarMenuItem menuItem && menuItem.StatusBarItem is not null)
        {
            menuItem.StatusBarItem.Visibility = (bool)e.NewValue
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    /// <inheritdoc/>
    protected override void OnInvoke() => IsChecked = !IsChecked;

    #endregion
}
