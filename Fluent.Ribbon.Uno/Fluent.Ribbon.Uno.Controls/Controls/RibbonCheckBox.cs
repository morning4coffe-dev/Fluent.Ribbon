namespace Fluent;

/// <summary>
/// Represents a CheckBox control within a Ribbon.
/// </summary>
[ContentProperty(Name = nameof(Header))]
public partial class RibbonCheckBox : CheckBox, IScalableRibbonControl, IHeaderedControl, IMediumIconProvider
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonCheckBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the checkbox.
    /// </summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="MediumIcon"/> dependency property.</summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(ImageSource),
            typeof(RibbonCheckBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon displayed beside the checkbox.
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
            typeof(RibbonCheckBox),
            new PropertyMetadata(RibbonControlSize.Medium, OnSizeChanged));

    /// <summary>
    /// Gets or sets the size of the control.
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
            typeof(RibbonCheckBox),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string KeyTip
    {
        get => (string)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonCheckBox"/> class.
    /// </summary>
    public RibbonCheckBox()
    {
        DefaultStyleKey = typeof(RibbonCheckBox);
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
        if (d is RibbonCheckBox checkBox)
        {
            checkBox.UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        var stateName = Size switch
        {
            RibbonControlSize.Large => "Large",
            RibbonControlSize.Medium => "Medium",
            RibbonControlSize.Small => "Small",
            _ => "Medium"
        };

        VisualStateManager.GoToState(this, stateName, true);
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonCheckBoxAutomationPeer(this);
}
