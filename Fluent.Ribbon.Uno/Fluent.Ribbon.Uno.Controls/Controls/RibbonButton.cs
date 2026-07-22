namespace Fluent;

/// <summary>
/// Represents a button control within a Ribbon.
/// </summary>
[TemplatePart(Name = PART_Icon, Type = typeof(Image))]
[TemplatePart(Name = PART_Label, Type = typeof(TextBlock))]
public partial class RibbonButton : Microsoft.UI.Xaml.Controls.Button, IRibbonControl, IScalableRibbonControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl, IQuickAccessItemProvider
{
    private const string PART_Icon = "PART_Icon";
    private const string PART_Label = "PART_Label";

    private Image? _iconImage;
    private TextBlock? _labelText;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the button.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(ImageSource),
            typeof(RibbonButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>
    /// Gets or sets the large icon (32x32).
    /// </summary>
    public ImageSource? LargeIcon
    {
        get => (ImageSource?)GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>Identifies the <see cref="MediumIcon"/> dependency property.</summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(ImageSource),
            typeof(RibbonButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>
    /// Gets or sets the medium/small icon (16x16).
    /// </summary>
    public ImageSource? MediumIcon
    {
        get => (ImageSource?)GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonButton),
            new PropertyMetadata(RibbonControlSize.Large, OnSizeChanged));

    /// <summary>
    /// Gets or sets the size of the button.
    /// </summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="ScreenTipTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTitleProperty =
        DependencyProperty.Register(
            nameof(ScreenTipTitle),
            typeof(string),
            typeof(RibbonButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the screen tip title.
    /// </summary>
    public string ScreenTipTitle
    {
        get => (string)GetValue(ScreenTipTitleProperty);
        set => SetValue(ScreenTipTitleProperty, value);
    }

    /// <summary>Identifies the <see cref="ScreenTipText"/> dependency property.</summary>
    public static readonly DependencyProperty ScreenTipTextProperty =
        DependencyProperty.Register(
            nameof(ScreenTipText),
            typeof(string),
            typeof(RibbonButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the screen tip description text.
    /// </summary>
    public string ScreenTipText
    {
        get => (string)GetValue(ScreenTipTextProperty);
        set => SetValue(ScreenTipTextProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(RibbonButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
    /// When set, a FontIcon is displayed instead of the Image icon.
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="CurrentIcon"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentIconProperty =
        DependencyProperty.Register(
            nameof(CurrentIcon),
            typeof(ImageSource),
            typeof(RibbonButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the current icon based on size.
    /// </summary>
    public ImageSource? CurrentIcon
    {
        get => (ImageSource?)GetValue(CurrentIconProperty);
        private set => SetValue(CurrentIconProperty, value);
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(RibbonButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>
    /// Gets or sets the icon for the element.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonButton),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the ribbon is in Simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        private set => SetValue(IsSimplifiedProperty, value);
    }

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty = RibbonProperties.SizeDefinitionProperty;

    /// <summary>
    /// Gets or sets the size definition.
    /// </summary>
    public string? SizeDefinition
    {
        get => (string?)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="SimplifiedSizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SimplifiedSizeDefinitionProperty =
        DependencyProperty.RegisterAttached(
            "SimplifiedSizeDefinition",
            typeof(string),
            typeof(RibbonButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the simplified size definition.
    /// </summary>
    public string? SimplifiedSizeDefinition
    {
        get => (string?)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonButton"/> class.
    /// </summary>
    public RibbonButton()
    {
        DefaultStyleKey = typeof(RibbonButton);
        Loaded += OnLoaded;
        QuickAccessHelper.AttachContextMenu(this);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _iconImage = GetTemplateChild(PART_Icon) as Image;
        _labelText = GetTemplateChild(PART_Label) as TextBlock;

        // Ensure initial size state is applied after the template is fully ready.
        // GoToState("Large") can be a no-op when the template starts in an unnamed state
        // that visually matches Large. Force through an intermediate state first.
        VisualStateManager.GoToState(this, "Small", false);
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateVisualState();
            UpdateScreenTip();
        });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateScreenTip();
    }

    /// <summary>
    /// Auto-generates a ScreenTip ToolTip if ScreenTipTitle or ScreenTipText is set.
    /// </summary>
    private void UpdateScreenTip()
    {
        if (!string.IsNullOrEmpty(ScreenTipTitle) || !string.IsNullOrEmpty(ScreenTipText))
        {
            ScreenTip.Attach(this, ScreenTipTitle, ScreenTipText);
        }
    }

    #endregion

    #region IScalableRibbonControl

    /// <inheritdoc/>
    public void ScaleTo(RibbonControlSize size)
    {
        Size = size;
    }

    #endregion

    #region Methods

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonButton button)
        {
            button.UpdateVisualState();
        }
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonButton button)
        {
            button.UpdateCurrentIcon();
        }
    }

    private void UpdateVisualState()
    {
        var stateName = Size switch
        {
            RibbonControlSize.Large => "Large",
            RibbonControlSize.Medium => "Medium",
            RibbonControlSize.Small => "Small",
            _ => "Large"
        };

        VisualStateManager.GoToState(this, stateName, true);
        UpdateCurrentIcon();
    }

    private void UpdateCurrentIcon()
    {
        var smallIcon = Icon as ImageSource;
        CurrentIcon = Size == RibbonControlSize.Large
            ? LargeIcon ?? MediumIcon ?? smallIcon
            : smallIcon ?? MediumIcon ?? LargeIcon;
    }

    #endregion

    #region IKeyTipedControl

    /// <inheritdoc />
    public KeyTipPressedResult OnKeyTipPressed()
    {
        var peer =
            Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            ?? Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(this);
        if (peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)
            is Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider invokeProvider)
        {
            invokeProvider.Invoke();
        }
        else
        {
            Command?.Execute(CommandParameter);
        }

        return KeyTipPressedResult.Empty;
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
    }

    #endregion

    #region ISimplifiedStateControl

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
    }

    #endregion

    #region IQuickAccessItemProvider

    /// <inheritdoc />
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    /// <inheritdoc />
    public FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new RibbonButton
        {
            Size = RibbonControlSize.Small,
            Header = QuickAccessHelper.ClonePresentationValue(Header),
            Icon = QuickAccessHelper.ClonePresentationValue(Icon),
            LargeIcon = LargeIcon,
            MediumIcon = MediumIcon,
            IconGlyph = IconGlyph,
            ScreenTipTitle = ScreenTipTitle,
            ScreenTipText = ScreenTipText,
            // The copy itself is not addable, so right-clicking it does nothing.
            CanAddToQuickAccessToolBar = false,
        };
        clone.Click += (_, _) => Fluent.Modern.Commands.RibbonInvoker.Invoke(this);

        return clone;
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonButtonAutomationPeer(this);
}
