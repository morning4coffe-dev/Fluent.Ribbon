namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents a button with a dropdown menu in the Ribbon.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_Button, Type = typeof(WinUIButton))]
public partial class RibbonDropDownButton : ItemsControl, IRibbonControl, IScalableRibbonControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl, IDropDownControl
{
    private const string PART_Button = "PART_Button";

    private WinUIButton? _button;
    private Flyout? _flyout;

    /// <summary>Gets the template part used to open the drop-down.</summary>
    protected virtual string DropDownButtonTemplatePartName => PART_Button;

    /// <summary>Gets whether the base class should connect its drop-down template part.</summary>
    protected virtual bool UsesDefaultDropDownButtonTemplateBehavior => true;

    #region Events

    /// <inheritdoc />
    public event EventHandler? DropDownOpened;

    /// <inheritdoc />
    public event EventHandler? DropDownClosed;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonDropDownButton),
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
            typeof(RibbonDropDownButton),
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
            typeof(RibbonDropDownButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>
    /// Gets or sets the medium/small icon (16x16).
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
            typeof(RibbonDropDownButton),
            new PropertyMetadata(null, OnIconChanged));

    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="CurrentIcon"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentIconProperty =
        DependencyProperty.Register(
            nameof(CurrentIcon),
            typeof(ImageSource),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the current icon based on size.
    /// </summary>
    public ImageSource? CurrentIcon
    {
        get => (ImageSource?)GetValue(CurrentIconProperty);
        private set => SetValue(CurrentIconProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonDropDownButton),
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
            typeof(RibbonDropDownButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the dropdown is open.
    /// </summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="MenuHeader"/> dependency property.</summary>
    public static readonly DependencyProperty MenuHeaderProperty =
        DependencyProperty.Register(
            nameof(MenuHeader),
            typeof(string),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the header text displayed at the top of the dropdown menu.
    /// </summary>
    public string MenuHeader
    {
        get => (string)GetValue(MenuHeaderProperty);
        set => SetValue(MenuHeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Gallery"/> dependency property.</summary>
    public static readonly DependencyProperty GalleryProperty =
        DependencyProperty.Register(
            nameof(Gallery),
            typeof(RibbonGallery),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets a gallery to display in the dropdown.
    /// </summary>
    public RibbonGallery? Gallery
    {
        get => (RibbonGallery?)GetValue(GalleryProperty);
        set => SetValue(GalleryProperty, value);
    }

    /// <summary>Identifies the <see cref="HasTriangle"/> dependency property.</summary>
    public static readonly DependencyProperty HasTriangleProperty =
        DependencyProperty.Register(
            nameof(HasTriangle),
            typeof(bool),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the button shows a dropdown triangle/arrow.
    /// </summary>
    public bool HasTriangle
    {
        get => (bool)GetValue(HasTriangleProperty);
        set => SetValue(HasTriangleProperty, value);
    }

    /// <summary>Identifies the <see cref="ResizeMode"/> dependency property.</summary>
    public static readonly DependencyProperty ResizeModeProperty =
        DependencyProperty.Register(
            nameof(ResizeMode),
            typeof(ContextMenuResizeMode),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(ContextMenuResizeMode.None));

    /// <summary>
    /// Gets or sets the context menu resize mode.
    /// </summary>
    public ContextMenuResizeMode ResizeMode
    {
        get => (ContextMenuResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxDropDownHeight"/> dependency property.</summary>
    public static readonly DependencyProperty MaxDropDownHeightProperty =
        DependencyProperty.Register(
            nameof(MaxDropDownHeight),
            typeof(double),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets the maximum drop down height.
    /// </summary>
    public double MaxDropDownHeight
    {
        get => (double)GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="DropDownHeight"/> dependency property.</summary>
    public static readonly DependencyProperty DropDownHeightProperty =
        DependencyProperty.Register(
            nameof(DropDownHeight),
            typeof(double),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets the initial drop down height.
    /// </summary>
    public double DropDownHeight
    {
        get => (double)GetValue(DropDownHeightProperty);
        set => SetValue(DropDownHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="ClosePopupOnMouseDown"/> dependency property.</summary>
    public static readonly DependencyProperty ClosePopupOnMouseDownProperty =
        DependencyProperty.Register(
            nameof(ClosePopupOnMouseDown),
            typeof(bool),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the popup closes automatically on mouse down.
    /// </summary>
    public bool ClosePopupOnMouseDown
    {
        get => (bool)GetValue(ClosePopupOnMouseDownProperty);
        set => SetValue(ClosePopupOnMouseDownProperty, value);
    }

    /// <summary>Identifies the <see cref="ClosePopupOnMouseDownDelay"/> dependency property.</summary>
    public static readonly DependencyProperty ClosePopupOnMouseDownDelayProperty =
        DependencyProperty.Register(
            nameof(ClosePopupOnMouseDownDelay),
            typeof(int),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(150));

    /// <summary>
    /// Gets or sets the delay in milliseconds before auto-closing the popup on mouse down.
    /// </summary>
    public int ClosePopupOnMouseDownDelay
    {
        get => (int)GetValue(ClosePopupOnMouseDownDelayProperty);
        set => SetValue(ClosePopupOnMouseDownDelayProperty, Math.Max(100, value));
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the ribbon is in Simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
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
            typeof(RibbonDropDownButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the simplified size definition.
    /// </summary>
    public string? SimplifiedSizeDefinition
    {
        get => (string?)GetValue(SimplifiedSizeDefinitionProperty);
        set => SetValue(SimplifiedSizeDefinitionProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(RibbonDropDownButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the template for the header content.
    /// </summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonDropDownButton"/> class.
    /// </summary>
    public RibbonDropDownButton()
    {
        DefaultStyleKey = typeof(RibbonDropDownButton);
        Items.VectorChanged += (_, _) => ResetFlyout();
        RegisterPropertyChangedCallback(
            ItemsControl.ItemsSourceProperty,
            static (sender, _) => ((RibbonDropDownButton)sender).ResetFlyout());
        QuickAccessHelper.AttachContextMenu(this);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_button is not null)
        {
            _button.Click -= OnButtonClick;
        }

        _button = UsesDefaultDropDownButtonTemplateBehavior
            ? GetTemplateChild(DropDownButtonTemplatePartName) as WinUIButton
            : null;

        if (_button is not null)
        {
            _button.Click += OnButtonClick;
        }

        UpdateVisualState();
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

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        ShowDropDown();
    }

    private void ShowDropDown()
    {
        if (_flyout is null)
        {
            var panel = new StackPanel { MinWidth = 200 };

            // Optional menu header
            if (!string.IsNullOrEmpty(MenuHeader))
            {
                panel.Children.Add(new TextBlock
                {
                    Text = MenuHeader,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(12, 8, 12, 4),
                });

                panel.Children.Add(new Rectangle
                {
                    Height = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 2, 0, 4),
                });
            }

            // Gallery section
            if (Gallery is not null)
            {
                panel.Children.Add(Gallery);

                panel.Children.Add(new Rectangle
                {
                    Height = 1,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 4, 0, 4),
                });
            }

            var dropDownItems = ItemsSource is System.Collections.IEnumerable source
                ? source.Cast<object>().ToArray()
                : Items.Cast<object>().ToArray();
            foreach (var item in dropDownItems.OfType<IDropDownItemOwner>())
            {
                item.SetDropDownOwner(this);
            }

            var itemsHost = new ItemsControl
            {
                ItemsSource = dropDownItems,
                ItemTemplate = ItemTemplate,
                ItemTemplateSelector = ItemTemplateSelector,
                ItemContainerStyle = ItemContainerStyle,
                ItemsPanel = ItemsPanel,
            };
            panel.Children.Add(itemsHost);

            // Apply height constraints
            if (!double.IsNaN(DropDownHeight))
            {
                var scrollViewer = new ScrollViewer
                {
                    Content = panel,
                    Height = DropDownHeight,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                };
                _flyout = new Flyout
                {
                    Content = scrollViewer,
                    Placement = FlyoutPlacementMode.Bottom,
                };
            }
            else
            {
                if (!double.IsNaN(MaxDropDownHeight))
                {
                    panel.MaxHeight = MaxDropDownHeight;
                }

                _flyout = new Flyout
                {
                    Content = panel,
                    Placement = FlyoutPlacementMode.Bottom,
                };
            }

            _flyout.Opened += (s, e) =>
            {
                IsDropDownOpen = true;
                RaiseDropDownOpened();
            };

            _flyout.Closed += (s, e) =>
            {
                IsDropDownOpen = false;
                RaiseDropDownClosed();
            };
        }

        _flyout.ShowAt((FrameworkElement?)_button ?? this);
    }

    internal void OpenDropDownForAutomation() => ShowDropDown();

    /// <summary>Raises the drop-down-opened notification.</summary>
    protected void RaiseDropDownOpened() =>
        DropDownOpened?.Invoke(this, EventArgs.Empty);

    /// <summary>Raises the drop-down-closed notification.</summary>
    protected void RaiseDropDownClosed() =>
        DropDownClosed?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Closes the drop-down if it is currently open.
    /// </summary>
    public virtual void CloseDropDown()
    {
        if (_flyout is not null && IsDropDownOpen)
        {
            _flyout.Hide();
        }
    }

    private void ResetFlyout()
    {
        _flyout?.Hide();
        _flyout = null;
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonDropDownButton button)
        {
            button.UpdateVisualState();
        }
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonDropDownButton button)
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
    public virtual KeyTipPressedResult OnKeyTipPressed()
    {
        ShowDropDown();
        return new KeyTipPressedResult(true, true);
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
        CloseDropDown();
    }

    #endregion

    #region ISimplifiedStateControl

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonDropDownButtonAutomationPeer(this);
}
