namespace Fluent;

/// <summary>
/// Represents a split button control within a Ribbon that has both a primary action
/// and a dropdown menu.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_Button, Type = typeof(Button))]
[TemplatePart(Name = PART_DropDownButton, Type = typeof(Button))]
public partial class RibbonSplitButton : Control, IRibbonControl, IScalableRibbonControl, ILargeIconProvider, IMediumIconProvider, ISimplifiedRibbonControl, IToggleButton, IDropDownControl
{
    private const string PART_Button = "PART_Button";
    private const string PART_DropDownButton = "PART_DropDownButton";

    private Button? _button;
    private Button? _dropDownButton;
    private Flyout? _flyout;

    #region Events

    /// <summary>
    /// Occurs when the primary button is clicked.
    /// </summary>
    public event RoutedEventHandler? Click;

    /// <summary>
    /// Occurs when the button becomes checked.
    /// </summary>
    public event RoutedEventHandler? Checked;

    /// <summary>
    /// Occurs when the button becomes unchecked.
    /// </summary>
    public event RoutedEventHandler? Unchecked;

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
            typeof(RibbonSplitButton),
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
            typeof(RibbonSplitButton),
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
            typeof(RibbonSplitButton),
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
            typeof(RibbonSplitButton),
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
            typeof(RibbonSplitButton),
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
            typeof(RibbonSplitButton),
            new PropertyMetadata(RibbonControlSize.Large, OnSizeChanged));

    /// <summary>
    /// Gets or sets the size of the button.
    /// </summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of dropdown menu items.
    /// </summary>
    public ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command for the primary button.
    /// </summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command parameter.
    /// </summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonSplitButton),
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
            typeof(RibbonSplitButton),
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
            typeof(RibbonSplitButton),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool?),
            typeof(RibbonSplitButton),
            new PropertyMetadata(false, OnIsCheckedChanged));

    /// <summary>
    /// Gets or sets a value indicating whether the button is checked.
    /// </summary>
    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsCheckable"/> dependency property.</summary>
    public static readonly DependencyProperty IsCheckableProperty =
        DependencyProperty.Register(
            nameof(IsCheckable),
            typeof(bool),
            typeof(RibbonSplitButton),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the button supports toggle/check behavior.
    /// </summary>
    public bool IsCheckable
    {
        get => (bool)GetValue(IsCheckableProperty);
        set => SetValue(IsCheckableProperty, value);
    }

    /// <summary>Identifies the <see cref="GroupName"/> dependency property.</summary>
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(
            nameof(GroupName),
            typeof(string),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the name of the group for mutually exclusive toggle behavior.
    /// </summary>
    public string? GroupName
    {
        get => (string?)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    /// <summary>Identifies the <see cref="DropDownToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty DropDownToolTipProperty =
        DependencyProperty.Register(
            nameof(DropDownToolTip),
            typeof(object),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the tooltip for the dropdown portion of the button.
    /// </summary>
    public object? DropDownToolTip
    {
        get => GetValue(DropDownToolTipProperty);
        set => SetValue(DropDownToolTipProperty, value);
    }

    /// <summary>Identifies the <see cref="IsButtonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsButtonEnabledProperty =
        DependencyProperty.Register(
            nameof(IsButtonEnabled),
            typeof(bool),
            typeof(RibbonSplitButton),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the primary button part is enabled.
    /// </summary>
    public bool IsButtonEnabled
    {
        get => (bool)GetValue(IsButtonEnabledProperty);
        set => SetValue(IsButtonEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="IsDefinitive"/> dependency property.</summary>
    public static readonly DependencyProperty IsDefinitiveProperty =
        DependencyProperty.Register(
            nameof(IsDefinitive),
            typeof(bool),
            typeof(RibbonSplitButton),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether clicking the button is a definitive action (e.g. closes backstage).
    /// </summary>
    public bool IsDefinitive
    {
        get => (bool)GetValue(IsDefinitiveProperty);
        set => SetValue(IsDefinitiveProperty, value);
    }
/// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
public static readonly DependencyProperty IsSimplifiedProperty =
    DependencyProperty.Register(
        nameof(IsSimplified),
        typeof(bool),
        typeof(RibbonSplitButton),
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
        typeof(RibbonSplitButton),
        new PropertyMetadata(null));

/// <summary>
/// Gets or sets the simplified size definition.
/// </summary>
public string? SimplifiedSizeDefinition
{
    get => (string?)GetValue(SimplifiedSizeDefinitionProperty);
    set => SetValue(SimplifiedSizeDefinitionProperty, value);
}

    /// <summary>Identifies the <see cref="HasTriangle"/> dependency property.</summary>
    public static readonly DependencyProperty HasTriangleProperty =
        DependencyProperty.Register(
            nameof(HasTriangle),
            typeof(bool),
            typeof(RibbonSplitButton),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the dropdown triangle/arrow is visible.
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
            typeof(RibbonSplitButton),
            new PropertyMetadata(ContextMenuResizeMode.None));

    /// <summary>
    /// Gets or sets the context menu resize mode for the dropdown.
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
            typeof(RibbonSplitButton),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets the maximum dropdown height.
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
            typeof(RibbonSplitButton),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets the initial dropdown height.
    /// </summary>
    public double DropDownHeight
    {
        get => (double)GetValue(DropDownHeightProperty);
        set => SetValue(DropDownHeightProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonSplitButton"/> class.
    /// </summary>
    public RibbonSplitButton()
    {
        DefaultStyleKey = typeof(RibbonSplitButton);
        Items = new ObservableCollection<UIElement>();
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

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click -= OnDropDownButtonClick;
        }

        _button = GetTemplateChild(PART_Button) as Button;
        _dropDownButton = GetTemplateChild(PART_DropDownButton) as Button;

        if (_button is not null)
        {
            _button.Click += OnButtonClick;
        }

        if (_dropDownButton is not null)
        {
            _dropDownButton.Click += OnDropDownButtonClick;
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
        if (IsCheckable)
        {
            IsChecked = !(IsChecked ?? false);
        }

        Click?.Invoke(this, e);
        
        if (Command?.CanExecute(CommandParameter) == true)
        {
            Command.Execute(CommandParameter);
        }
    }

    private void OnDropDownButtonClick(object sender, RoutedEventArgs e)
    {
        ShowDropDown();
    }

    private void ShowDropDown()
    {
        if (_flyout is null)
        {
            var panel = new StackPanel { MinWidth = 200 };
            foreach (var item in Items)
            {
                panel.Children.Add(item);
            }

            FrameworkElement flyoutContent = panel;

            // Apply height constraints
            if (!double.IsNaN(DropDownHeight))
            {
                var scrollViewer = new ScrollViewer
                {
                    Content = panel,
                    Height = DropDownHeight,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                };
                flyoutContent = scrollViewer;
            }
            else
            {
                if (!double.IsNaN(MaxDropDownHeight))
                {
                    panel.MaxHeight = MaxDropDownHeight;
                }
            }

            _flyout = new Flyout
            {
                Content = flyoutContent,
                Placement = FlyoutPlacementMode.Bottom
            };

            _flyout.Opened += (s, e) =>
            {
                IsDropDownOpen = true;
                DropDownOpened?.Invoke(this, EventArgs.Empty);
            };

            _flyout.Closed += (s, e) =>
            {
                IsDropDownOpen = false;
                DropDownClosed?.Invoke(this, EventArgs.Empty);
            };
        }

        _flyout.ShowAt((FrameworkElement?)_dropDownButton ?? this);
    }

    /// <summary>
    /// Closes the drop-down if it is currently open.
    /// </summary>
    public void CloseDropDown()
    {
        if (_flyout is not null && IsDropDownOpen)
        {
            _flyout.Hide();
        }
    }

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSplitButton button)
        {
            var newValue = (bool?)e.NewValue;
            if (newValue == true)
            {
                button.Checked?.Invoke(button, new RoutedEventArgs());
            }
            else
            {
                button.Unchecked?.Invoke(button, new RoutedEventArgs());
            }

            button.UpdateVisualState();
        }
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSplitButton button)
        {
            button.UpdateVisualState();
        }
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSplitButton button)
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

        if (IsCheckable && IsChecked == true)
        {
            VisualStateManager.GoToState(this, "Checked", true);
        }
        else
        {
            VisualStateManager.GoToState(this, "Unchecked", true);
        }
        
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
        if (Command is not null && Command.CanExecute(CommandParameter))
        {
            Command.Execute(CommandParameter);
        }
        else
        {
            ShowDropDown();
        }
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
}
