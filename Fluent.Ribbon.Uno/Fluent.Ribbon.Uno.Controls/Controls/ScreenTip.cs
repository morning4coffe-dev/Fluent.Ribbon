namespace Fluent;

/// <summary>
/// Represents an enhanced tooltip (ScreenTip) that displays a title, description,
/// and optional image. ScreenTips provide rich tooltip functionality as seen
/// in Microsoft Office applications.
/// </summary>
[ContentProperty(Name = nameof(Text))]
public partial class ScreenTip : ContentControl
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ScreenTip),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the title of the ScreenTip.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ScreenTip),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the description text of the ScreenTip.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Identifies the <see cref="Image"/> dependency property.</summary>
    public static readonly DependencyProperty ImageProperty =
        DependencyProperty.Register(
            nameof(Image),
            typeof(ImageSource),
            typeof(ScreenTip),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the image shown in the ScreenTip.
    /// </summary>
    public ImageSource? Image
    {
        get => (ImageSource?)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    /// <summary>Identifies the <see cref="DisableReason"/> dependency property.</summary>
    public static readonly DependencyProperty DisableReasonProperty =
        DependencyProperty.Register(
            nameof(DisableReason),
            typeof(string),
            typeof(ScreenTip),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the reason why the control is disabled (shown when applicable).
    /// </summary>
    public string DisableReason
    {
        get => (string)GetValue(DisableReasonProperty);
        set => SetValue(DisableReasonProperty, value);
    }

    /// <summary>Identifies the <see cref="HelpTopic"/> dependency property.</summary>
    public static readonly DependencyProperty HelpTopicProperty =
        DependencyProperty.Register(
            nameof(HelpTopic),
            typeof(object),
            typeof(ScreenTip),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the help topic associated with this ScreenTip.
    /// </summary>
    public object? HelpTopic
    {
        get => GetValue(HelpTopicProperty);
        set => SetValue(HelpTopicProperty, value);
    }

    /// <summary>Identifies the <see cref="IsRibbonAligned"/> dependency property.</summary>
    public static readonly DependencyProperty IsRibbonAlignedProperty =
        DependencyProperty.Register(
            nameof(IsRibbonAligned),
            typeof(bool),
            typeof(ScreenTip),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the ScreenTip is aligned to the ribbon.
    /// </summary>
    public bool IsRibbonAligned
    {
        get => (bool)GetValue(IsRibbonAlignedProperty);
        set => SetValue(IsRibbonAlignedProperty, value);
    }

    /// <summary>Identifies the <see cref="Width"/> dependency property override.</summary>
    public static readonly DependencyProperty MaxWidthOverrideProperty =
        DependencyProperty.Register(
            nameof(MaxWidthOverride),
            typeof(double),
            typeof(ScreenTip),
            new PropertyMetadata(318.0));

    /// <summary>
    /// Gets or sets the maximum width of the ScreenTip.
    /// </summary>
    public double MaxWidthOverride
    {
        get => (double)GetValue(MaxWidthOverrideProperty);
        set => SetValue(MaxWidthOverrideProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenTip"/> class.
    /// </summary>
    public ScreenTip()
    {
        DefaultStyleKey = typeof(ScreenTip);
    }

    #endregion

    #region Static Helper

    /// <summary>
    /// Attaches a ScreenTip to a framework element using its ToolTip property.
    /// </summary>
    /// <param name="element">The element to attach the ScreenTip to.</param>
    /// <param name="title">The ScreenTip title.</param>
    /// <param name="text">The ScreenTip description text.</param>
    /// <param name="image">Optional image source.</param>
    public static void Attach(FrameworkElement element, string title, string text, ImageSource? image = null)
    {
        var screenTip = new ScreenTip
        {
            Title = title,
            Text = text,
            Image = image,
        };

        ToolTipService.SetToolTip(element, screenTip);
    }

    #endregion
}
