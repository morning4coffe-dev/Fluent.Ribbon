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
            new PropertyMetadata(null, OnHelpTopicChanged));

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

    /// <summary>Identifies the <see cref="HelpLabelVisibility"/> dependency property.</summary>
    public static readonly DependencyProperty HelpLabelVisibilityProperty =
        DependencyProperty.Register(
            nameof(HelpLabelVisibility),
            typeof(Visibility),
            typeof(ScreenTip),
            new PropertyMetadata(Visibility.Visible, OnHelpLabelVisibilityChanged));

    /// <summary>
    /// Gets or sets the visibility of the "Press F1 for help" label. The label is only
    /// shown when this is <see cref="Visibility.Visible"/> and a <see cref="HelpTopic"/> is set.
    /// </summary>
    public Visibility HelpLabelVisibility
    {
        get => (Visibility)GetValue(HelpLabelVisibilityProperty);
        set => SetValue(HelpLabelVisibilityProperty, value);
    }

    /// <summary>Identifies the <see cref="HelpLabelActualVisibility"/> dependency property.</summary>
    public static readonly DependencyProperty HelpLabelActualVisibilityProperty =
        DependencyProperty.Register(
            nameof(HelpLabelActualVisibility),
            typeof(Visibility),
            typeof(ScreenTip),
            new PropertyMetadata(Visibility.Collapsed));

    /// <summary>
    /// Gets the effective visibility of the F1 help label (combines
    /// <see cref="HelpLabelVisibility"/> and whether a <see cref="HelpTopic"/> is present).
    /// </summary>
    public Visibility HelpLabelActualVisibility
    {
        get => (Visibility)GetValue(HelpLabelActualVisibilityProperty);
        private set => SetValue(HelpLabelActualVisibilityProperty, value);
    }

    /// <summary>Identifies the <see cref="HelpLabelText"/> dependency property.</summary>
    public static readonly DependencyProperty HelpLabelTextProperty =
        DependencyProperty.Register(
            nameof(HelpLabelText),
            typeof(string),
            typeof(ScreenTip),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets the localized text shown in the F1 help label.
    /// </summary>
    public string HelpLabelText
    {
        get => (string)GetValue(HelpLabelTextProperty);
        private set => SetValue(HelpLabelTextProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Occurs when the user presses F1 while a ScreenTip with a <see cref="HelpTopic"/> is shown.
    /// </summary>
    public static event EventHandler<ScreenTipHelpEventArgs>? HelpPressed;

    #endregion

    #region Constructor

    private readonly KeyEventHandler _keyDownHandler;
    private UIElement? _keyboardRoot;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenTip"/> class.
    /// </summary>
    public ScreenTip()
    {
        DefaultStyleKey = typeof(ScreenTip);
        HelpLabelText = RibbonLocalization.Current.Localization.ScreenTipF1LabelHeader;

        _keyDownHandler = OnRootKeyDown;
        Loaded += OnScreenTipLoaded;
        Unloaded += OnScreenTipUnloaded;
    }

    #endregion

    #region F1 Help Handling

    private static void OnHelpTopicChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ScreenTip)d).UpdateHelpLabelActualVisibility();
    }

    private static void OnHelpLabelVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ScreenTip)d).UpdateHelpLabelActualVisibility();
    }

    private void UpdateHelpLabelActualVisibility()
    {
        HelpLabelActualVisibility = HelpLabelVisibility == Visibility.Visible && HelpTopic is not null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnScreenTipLoaded(object sender, RoutedEventArgs e)
    {
        if (HelpTopic is null)
        {
            return;
        }

        var root = XamlRoot?.Content as UIElement;
        if (root is null || ReferenceEquals(root, _keyboardRoot))
        {
            return;
        }

        DetachKeyboard();
        _keyboardRoot = root;
        _keyboardRoot.AddHandler(UIElement.KeyDownEvent, _keyDownHandler, handledEventsToo: true);
    }

    private void OnScreenTipUnloaded(object sender, RoutedEventArgs e)
    {
        DetachKeyboard();
    }

    private void DetachKeyboard()
    {
        if (_keyboardRoot is null)
        {
            return;
        }

        _keyboardRoot.RemoveHandler(UIElement.KeyDownEvent, _keyDownHandler);
        _keyboardRoot = null;
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.F1)
        {
            return;
        }

        if (HelpTopic is null)
        {
            return;
        }

        e.Handled = true;
        HelpPressed?.Invoke(null, new ScreenTipHelpEventArgs(HelpTopic));
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

/// <summary>
/// Event args for the <see cref="ScreenTip.HelpPressed"/> event.
/// </summary>
public class ScreenTipHelpEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScreenTipHelpEventArgs"/> class.
    /// </summary>
    /// <param name="helpTopic">The help topic associated with the ScreenTip.</param>
    public ScreenTipHelpEventArgs(object? helpTopic)
    {
        HelpTopic = helpTopic;
    }

    /// <summary>
    /// Gets the help topic associated with the ScreenTip.
    /// </summary>
    public object? HelpTopic { get; }
}
