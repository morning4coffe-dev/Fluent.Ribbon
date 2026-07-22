namespace Fluent;

/// <summary>
/// Represents an individual item in a <see cref="RibbonStatusBar"/>.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public partial class StatusBarItem : StatusBarItemBase
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(StatusBarItem),
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
            typeof(StatusBarItem),
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
    /// Initializes a new instance of the <see cref="StatusBarItem"/> class.
    /// </summary>
    public StatusBarItem()
    {
        DefaultStyleKey = typeof(StatusBarItem);
        InitializeCompatibility();
    }

    #endregion

    #region Methods

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBarItem item)
        {
            item.OnCheckedStateChanged();
        }
    }

    #endregion
}

/// <summary>Unpublished convenience name retained for existing Uno markup.</summary>
public partial class RibbonStatusBarItem : StatusBarItem
{
}
