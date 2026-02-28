namespace Fluent;

/// <summary>
/// Represents an individual item in a <see cref="RibbonStatusBar"/>.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public partial class RibbonStatusBarItem : ContentControl
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(RibbonStatusBarItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the title of the status bar item (used by context menu).
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(RibbonStatusBarItem),
            new PropertyMetadata(true, OnIsCheckedChanged));

    /// <summary>
    /// Gets or sets whether this item is visible in the status bar.
    /// </summary>
    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonStatusBarItem"/> class.
    /// </summary>
    public RibbonStatusBarItem()
    {
        DefaultStyleKey = typeof(RibbonStatusBarItem);
    }

    #endregion

    #region Methods

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonStatusBarItem item)
        {
            item.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    #endregion
}
