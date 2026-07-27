namespace Fluent;

/// <summary>
/// Represents a TextBox control within a Ribbon.
/// </summary>
public partial class RibbonTextBox : TextBox, IHeaderedControl, IScalableRibbonControl, IMediumIconProvider, IQuickAccessItemProvider, IRibbonHeaderAlignable
{
    private FrameworkElement? _headerPresenter;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonTextBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the text box.
    /// </summary>
    public new object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="MediumIcon"/> dependency property.</summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(ImageSource),
            typeof(RibbonTextBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon displayed beside the text box.
    /// </summary>
    public ImageSource? MediumIcon
    {
        get => (ImageSource?)GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(RibbonTextBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon for the text box.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(RibbonTextBox),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonTextBox),
            new PropertyMetadata(RibbonControlSize.Medium, OnSizeChanged));

    /// <summary>
    /// Gets or sets the size of the control within the ribbon.
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
            typeof(RibbonTextBox),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string KeyTip
    {
        get => (string)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="InputWidth"/> dependency property.</summary>
    public static readonly DependencyProperty InputWidthProperty =
        DependencyProperty.Register(
            nameof(InputWidth),
            typeof(double),
            typeof(RibbonTextBox),
            new PropertyMetadata(150.0));

    /// <summary>
    /// Gets or sets the width of the input area.
    /// </summary>
    public double InputWidth
    {
        get => (double)GetValue(InputWidthProperty);
        set => SetValue(InputWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectAllTextOnFocus"/> dependency property.</summary>
    public static readonly DependencyProperty SelectAllTextOnFocusProperty =
        DependencyProperty.Register(
            nameof(SelectAllTextOnFocus),
            typeof(bool),
            typeof(RibbonTextBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether all text is selected when the text box receives focus.
    /// </summary>
    public bool SelectAllTextOnFocus
    {
        get => (bool)GetValue(SelectAllTextOnFocusProperty);
        set => SetValue(SelectAllTextOnFocusProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonTextBox),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the ribbon is in Simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTextBox"/> class.
    /// </summary>
    public RibbonTextBox()
    {
        DefaultStyleKey = typeof(RibbonTextBox);
        GotFocus += OnRibbonTextBoxGotFocus;
        QuickAccessHelper.AttachContextMenu(this);
    }

    #endregion

    #region Template

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _headerPresenter = GetTemplateChild("HeaderText") as FrameworkElement;
    }

    /// <inheritdoc />
    FrameworkElement? IRibbonHeaderAlignable.HeaderPresenter => _headerPresenter;

    #endregion

    #region IScalableRibbonControl

    /// <inheritdoc />
    public void ScaleTo(RibbonControlSize size)
    {
        Size = size;
    }

    #endregion

    #region Methods

    private void OnRibbonTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (SelectAllTextOnFocus)
        {
            SelectAll();
        }
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonTextBox textBox)
        {
            textBox.UpdateVisualState();
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

    #region IQuickAccessItemProvider

    /// <inheritdoc />
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    /// <inheritdoc />
    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new RibbonTextBox
        {
            Header = QuickAccessHelper.ClonePresentationValue(Header),
            MediumIcon = MediumIcon,
            IconGlyph = IconGlyph,
            Size = RibbonControlSize.Small,
            InputWidth = Math.Min(InputWidth, 120d),
            SelectAllTextOnFocus = SelectAllTextOnFocus,
            CanAddToQuickAccessToolBar = false,
        };

        RibbonControl.BindQuickAccessItem(this, clone);
        BindOneWay(IsReadOnlyProperty);
        BindOneWay(MaxLengthProperty);
        BindTwoWay(TextProperty);
        return clone;

        void BindOneWay(DependencyProperty property) =>
            RibbonControl.Synchronize(this, property, clone, property);

        void BindTwoWay(DependencyProperty property)
        {
            RibbonControl.Synchronize(this, property, clone, property);
            RibbonControl.Synchronize(clone, property, this, property);
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonTextBoxAutomationPeer(this);
}
