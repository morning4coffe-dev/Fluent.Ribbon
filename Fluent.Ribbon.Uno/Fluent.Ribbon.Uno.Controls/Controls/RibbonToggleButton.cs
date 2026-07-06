using Fluent.Helpers;

namespace Fluent;

/// <summary>
/// Represents a toggle button control within a Ribbon.
/// </summary>
[TemplatePart(Name = PART_Icon, Type = typeof(Image))]
[TemplatePart(Name = PART_Label, Type = typeof(TextBlock))]
public partial class RibbonToggleButton : ToggleButton, IRibbonControl, IScalableRibbonControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl
{
    private const string PART_Icon = "PART_Icon";
    private const string PART_Label = "PART_Label";

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
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
            typeof(RibbonToggleButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the simplified size definition.
    /// </summary>
    public string? SimplifiedSizeDefinition
    {
        get => (string?)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="GroupName"/> dependency property.</summary>
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(
            nameof(GroupName),
            typeof(string),
            typeof(RibbonToggleButton),
            new PropertyMetadata(null, OnGroupNameChanged));

    /// <summary>
    /// Gets or sets the name of the group that this toggle button belongs to.
    /// Toggle buttons that share a group name behave like radio buttons:
    /// checking one unchecks the others and a checked button cannot be
    /// unchecked by clicking it again.
    /// </summary>
    public string? GroupName
    {
        get => (string?)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    #endregion

    #region Constructor

    private string? _registeredGroupName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonToggleButton"/> class.
    /// </summary>
    public RibbonToggleButton()
    {
        DefaultStyleKey = typeof(RibbonToggleButton);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        Checked += OnCheckedUpdateGroup;
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Ensure initial size state is applied after the template is fully ready.
        VisualStateManager.GoToState(this, "Small", false);
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateVisualState();
            UpdateScreenTip();
        });
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RegisterInGroup();
        UpdateScreenTip();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_registeredGroupName))
        {
            ToggleButtonHelper.Unregister(_registeredGroupName!, this);
            _registeredGroupName = null;
        }
    }

    private void RegisterInGroup()
    {
        if (string.Equals(_registeredGroupName, GroupName, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.IsNullOrEmpty(_registeredGroupName))
        {
            ToggleButtonHelper.Unregister(_registeredGroupName!, this);
        }

        _registeredGroupName = GroupName;

        if (!string.IsNullOrEmpty(_registeredGroupName))
        {
            ToggleButtonHelper.Register(_registeredGroupName!, this);

            // Keep the group consistent if this button starts out checked.
            if (IsChecked == true)
            {
                ToggleButtonHelper.UpdateButtonGroup(_registeredGroupName!, this);
            }
        }
    }

    private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonToggleButton button)
        {
            button.RegisterInGroup();
        }
    }

    private void OnCheckedUpdateGroup(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(GroupName))
        {
            ToggleButtonHelper.UpdateButtonGroup(GroupName!, this);
        }
    }

    /// <inheritdoc />
    protected override void OnToggle()
    {
        // Radio-button-like behavior: a checked member of a group cannot be
        // unchecked by clicking it again. Click/Command have already been
        // raised by ButtonBase.OnClick before OnToggle runs.
        if (!string.IsNullOrEmpty(GroupName)
            && IsChecked == true)
        {
            return;
        }

        base.OnToggle();
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
        if (d is RibbonToggleButton button)
        {
            button.UpdateVisualState();
        }
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonToggleButton button)
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
    public void OnKeyTipPressed()
    {
        if (Microsoft.UI.Xaml.Automation.Peers.AutomationPeer.ListenerExists(Microsoft.UI.Xaml.Automation.Peers.AutomationEvents.InvokePatternOnInvoked))
        {
            var peer = Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this) ?? Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(this);
            var invokeProv = peer.GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke) as Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider;
            invokeProv?.Invoke();
        }
        else
        {
            IsChecked = !IsChecked;
            Command?.Execute(CommandParameter);
        }
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
}
