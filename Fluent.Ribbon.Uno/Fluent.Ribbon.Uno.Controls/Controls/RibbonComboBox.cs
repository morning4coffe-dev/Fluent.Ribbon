namespace Fluent;

/// <summary>
/// Represents a ComboBox control within a Ribbon.
/// </summary>
public partial class RibbonComboBox : ComboBox, IHeaderedControl, IScalableRibbonControl, IMediumIconProvider, IDropDownControl
{
    #region Events

    /// <inheritdoc />
    public new event EventHandler? DropDownOpened;

    /// <inheritdoc />
    public new event EventHandler? DropDownClosed;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the combo box.
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
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon displayed beside the combo box.
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
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon for the combo box.
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
            typeof(RibbonComboBox),
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
            typeof(RibbonComboBox),
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
            typeof(RibbonComboBox),
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
            typeof(RibbonComboBox),
            new PropertyMetadata(100.0));

    /// <summary>
    /// Gets or sets the width of the input area.
    /// </summary>
    public double InputWidth
    {
        get => (double)GetValue(InputWidthProperty);
        set => SetValue(InputWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="ResizeMode"/> dependency property.</summary>
    public static readonly DependencyProperty ResizeModeProperty =
        DependencyProperty.Register(
            nameof(ResizeMode),
            typeof(ContextMenuResizeMode),
            typeof(RibbonComboBox),
            new PropertyMetadata(ContextMenuResizeMode.None));

    /// <summary>
    /// Gets or sets the context menu resize mode.
    /// </summary>
    public ContextMenuResizeMode ResizeMode
    {
        get => (ContextMenuResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    /// <summary>Identifies the <see cref="DropDownHeight"/> dependency property.</summary>
    public static readonly DependencyProperty DropDownHeightProperty =
        DependencyProperty.Register(
            nameof(DropDownHeight),
            typeof(double),
            typeof(RibbonComboBox),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets the initial drop down height.
    /// </summary>
    public double DropDownHeight
    {
        get => (double)GetValue(DropDownHeightProperty);
        set => SetValue(DropDownHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static new readonly DependencyProperty IsDropDownOpenProperty =
        Microsoft.UI.Xaml.Controls.ComboBox.IsDropDownOpenProperty;

    /// <summary>
    /// Gets or sets a value indicating whether the drop down is currently open.
    /// </summary>
    public new bool IsDropDownOpen
    {
        get => base.IsDropDownOpen;
        set => base.IsDropDownOpen = value;
    }

    /// <summary>Identifies the <see cref="TopPopupContent"/> dependency property.</summary>
    public static readonly DependencyProperty TopPopupContentProperty =
        DependencyProperty.Register(
            nameof(TopPopupContent),
            typeof(object),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets content to show on the top side of the Popup.
    /// </summary>
    public object? TopPopupContent
    {
        get => GetValue(TopPopupContentProperty);
        set => SetValue(TopPopupContentProperty, value);
    }

    /// <summary>Identifies the <see cref="TopPopupContentTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty TopPopupContentTemplateProperty =
        DependencyProperty.Register(
            nameof(TopPopupContentTemplate),
            typeof(DataTemplate),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the data template for top popup content.
    /// </summary>
    public DataTemplate? TopPopupContentTemplate
    {
        get => (DataTemplate?)GetValue(TopPopupContentTemplateProperty);
        set => SetValue(TopPopupContentTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="Menu"/> dependency property.</summary>
    public static readonly DependencyProperty MenuProperty =
        DependencyProperty.Register(
            nameof(Menu),
            typeof(UIElement),
            typeof(RibbonComboBox),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets a menu to show at the bottom of the combo box drop down.
    /// </summary>
    public UIElement? Menu
    {
        get => (UIElement?)GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonComboBox),
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
    /// Initializes a new instance of the <see cref="RibbonComboBox"/> class.
    /// </summary>
    public RibbonComboBox()
    {
        DefaultStyleKey = typeof(RibbonComboBox);
        base.DropDownOpened += OnNativeDropDownOpened;
        base.DropDownClosed += OnNativeDropDownClosed;
        DropDownOpened += (_, _) => { };
        DropDownClosed += (_, _) => { };
    }

    #endregion

    #region Template

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState();
    }

    #endregion

    #region IScalableRibbonControl

    /// <inheritdoc />
    public void ScaleTo(RibbonControlSize size)
    {
        Size = size;
    }

    #endregion

    #region Methods

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonComboBox comboBox)
        {
            comboBox.UpdateVisualState();
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

    private void OnNativeDropDownOpened(object? sender, object e)
    {
        DropDownOpened?.Invoke(this, EventArgs.Empty);
    }

    private void OnNativeDropDownClosed(object? sender, object e)
    {
        DropDownClosed?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonComboBoxAutomationPeer(this);
}
