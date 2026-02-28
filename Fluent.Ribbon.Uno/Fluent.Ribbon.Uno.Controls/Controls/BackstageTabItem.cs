namespace Fluent;

/// <summary>
/// Represents a tab item within the Backstage view.
/// </summary>
[ContentProperty(Name = nameof(Content))]
public partial class BackstageTabItem : Button
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(BackstageTabItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header text of the tab item.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSelected"/> dependency property.</summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(BackstageTabItem),
            new PropertyMetadata(false, OnIsSelectedChanged));

    /// <summary>
    /// Gets or sets whether this tab is selected.
    /// </summary>
    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(ImageSource),
            typeof(BackstageTabItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon for the tab item.
    /// </summary>
    public ImageSource? Icon
    {
        get => (ImageSource?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(BackstageTabItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string KeyTip
    {
        get => (string)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(BackstageTabItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="BackstageTabItem"/> class.
    /// </summary>
    public BackstageTabItem()
    {
        DefaultStyleKey = typeof(BackstageTabItem);
    }

    #endregion

    #region Methods

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BackstageTabItem tabItem)
        {
            VisualStateManager.GoToState(tabItem, (bool)e.NewValue ? "Selected" : "Unselected", true);
        }
    }

    #endregion
}
