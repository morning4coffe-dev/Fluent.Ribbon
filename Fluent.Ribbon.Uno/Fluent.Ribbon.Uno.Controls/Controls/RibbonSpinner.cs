namespace Fluent;

/// <summary>
/// Represents a numeric spinner control within a Ribbon. The spinner allows
/// the user to increment or decrement a numeric value.
/// </summary>
[TemplatePart(Name = PART_TextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PART_UpButton, Type = typeof(Button))]
[TemplatePart(Name = PART_DownButton, Type = typeof(Button))]
public partial class RibbonSpinner : Control, IScalableRibbonControl, IHeaderedControl, IMediumIconProvider
{
    private const string PART_TextBox = "PART_TextBox";
    private const string PART_UpButton = "PART_UpButton";
    private const string PART_DownButton = "PART_DownButton";

    private TextBox? _textBox;
    private Button? _upButton;
    private Button? _downButton;
    private DispatcherTimer? _repeatTimer;
    private bool _isIncrementing;

    #region Events

    /// <summary>
    /// Occurs when the value changes.
    /// </summary>
    public event EventHandler<double>? ValueChanged;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonSpinner),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the spinner.
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
            typeof(RibbonSpinner),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the icon displayed beside the spinner.
    /// </summary>
    public ImageSource? MediumIcon
    {
        get => (ImageSource?)GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>Identifies the <see cref="IconGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty IconGlyphProperty =
        DependencyProperty.Register(
            nameof(IconGlyph),
            typeof(string),
            typeof(RibbonSpinner),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the icon glyph character (Segoe Fluent Icons / MDL2 Assets).
    /// </summary>
    public string IconGlyph
    {
        get => (string)GetValue(IconGlyphProperty);
        set => SetValue(IconGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(RibbonSpinner),
            new PropertyMetadata(0.0, OnValueChanged));

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, CoerceValue(value));
    }

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(RibbonSpinner),
            new PropertyMetadata(0.0));

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(RibbonSpinner),
            new PropertyMetadata(double.MaxValue));

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Identifies the <see cref="Increment"/> dependency property.</summary>
    public static readonly DependencyProperty IncrementProperty =
        DependencyProperty.Register(
            nameof(Increment),
            typeof(double),
            typeof(RibbonSpinner),
            new PropertyMetadata(1.0));

    /// <summary>
    /// Gets or sets the increment step.
    /// </summary>
    public double Increment
    {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>Identifies the <see cref="Format"/> dependency property.</summary>
    public static readonly DependencyProperty FormatProperty =
        DependencyProperty.Register(
            nameof(Format),
            typeof(string),
            typeof(RibbonSpinner),
            new PropertyMetadata("F1", OnFormatChanged));

    /// <summary>
    /// Gets or sets the format string for displaying the value (e.g., "F0", "F2", "N1").
    /// </summary>
    public string Format
    {
        get => (string)GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonSpinner),
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
            typeof(RibbonSpinner),
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
            typeof(RibbonSpinner),
            new PropertyMetadata(60.0));

    /// <summary>
    /// Gets or sets the width of the input area.
    /// </summary>
    public double InputWidth
    {
        get => (double)GetValue(InputWidthProperty);
        set => SetValue(InputWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="Delay"/> dependency property.</summary>
    public static readonly DependencyProperty DelayProperty =
        DependencyProperty.Register(
            nameof(Delay),
            typeof(int),
            typeof(RibbonSpinner),
            new PropertyMetadata(400));

    /// <summary>
    /// Gets or sets the delay in milliseconds before the repeat behavior starts.
    /// </summary>
    public int Delay
    {
        get => (int)GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    /// <summary>Identifies the <see cref="Interval"/> dependency property.</summary>
    public static readonly DependencyProperty IntervalProperty =
        DependencyProperty.Register(
            nameof(Interval),
            typeof(int),
            typeof(RibbonSpinner),
            new PropertyMetadata(80));

    /// <summary>
    /// Gets or sets the interval in milliseconds between repeat increments/decrements.
    /// </summary>
    public int Interval
    {
        get => (int)GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    /// <summary>Identifies the <see cref="SelectAllTextOnFocus"/> dependency property.</summary>
    public static readonly DependencyProperty SelectAllTextOnFocusProperty =
        DependencyProperty.Register(
            nameof(SelectAllTextOnFocus),
            typeof(bool),
            typeof(RibbonSpinner),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether all text is selected when the text box receives focus.
    /// </summary>
    public bool SelectAllTextOnFocus
    {
        get => (bool)GetValue(SelectAllTextOnFocusProperty);
        set => SetValue(SelectAllTextOnFocusProperty, value);
    }

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(RibbonSpinner),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets the current text representation of the value. (Read-only.)
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        private set => SetValue(TextProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonSpinner),
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
    /// Initializes a new instance of the <see cref="RibbonSpinner"/> class.
    /// </summary>
    public RibbonSpinner()
    {
        DefaultStyleKey = typeof(RibbonSpinner);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_upButton is not null)
        {
            _upButton.Click -= OnUpButtonClick;
            _upButton.PointerPressed -= OnUpButtonPointerPressed;
            _upButton.PointerReleased -= OnButtonPointerReleased;
            _upButton.PointerExited -= OnButtonPointerExited;
        }

        if (_downButton is not null)
        {
            _downButton.Click -= OnDownButtonClick;
            _downButton.PointerPressed -= OnDownButtonPointerPressed;
            _downButton.PointerReleased -= OnButtonPointerReleased;
            _downButton.PointerExited -= OnButtonPointerExited;
        }

        _textBox = GetTemplateChild(PART_TextBox) as TextBox;
        _upButton = GetTemplateChild(PART_UpButton) as Button;
        _downButton = GetTemplateChild(PART_DownButton) as Button;

        if (_upButton is not null)
        {
            _upButton.Click += OnUpButtonClick;
            _upButton.PointerPressed += OnUpButtonPointerPressed;
            _upButton.PointerReleased += OnButtonPointerReleased;
            _upButton.PointerExited += OnButtonPointerExited;
        }

        if (_downButton is not null)
        {
            _downButton.Click += OnDownButtonClick;
            _downButton.PointerPressed += OnDownButtonPointerPressed;
            _downButton.PointerReleased += OnButtonPointerReleased;
            _downButton.PointerExited += OnButtonPointerExited;
        }

        if (_textBox is not null)
        {
            _textBox.LostFocus += OnTextBoxLostFocus;
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.GotFocus += OnTextBoxGotFocus;
        }

        UpdateTextBox();
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

    private void OnUpButtonClick(object sender, RoutedEventArgs e)
    {
        Value = CoerceValue(Value + Increment);
    }

    private void OnDownButtonClick(object sender, RoutedEventArgs e)
    {
        Value = CoerceValue(Value - Increment);
    }

    #region Repeat Button Behavior

    private void OnUpButtonPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isIncrementing = true;
        StartRepeatTimer();
    }

    private void OnDownButtonPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isIncrementing = false;
        StartRepeatTimer();
    }

    private void OnButtonPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        StopRepeatTimer();
    }

    private void OnButtonPointerExited(object sender, PointerRoutedEventArgs e)
    {
        StopRepeatTimer();
    }

    private void StartRepeatTimer()
    {
        StopRepeatTimer();
        _repeatTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Delay),
        };
        _repeatTimer.Tick += OnRepeatTimerFirstTick;
        _repeatTimer.Start();
    }

    private void OnRepeatTimerFirstTick(object? sender, object e)
    {
        if (_repeatTimer is null) return;

        // After the initial delay, switch to interval ticks
        _repeatTimer.Stop();
        _repeatTimer.Tick -= OnRepeatTimerFirstTick;
        _repeatTimer.Interval = TimeSpan.FromMilliseconds(Interval);
        _repeatTimer.Tick += OnRepeatTimerTick;
        _repeatTimer.Start();
        DoRepeatAction();
    }

    private void OnRepeatTimerTick(object? sender, object e)
    {
        DoRepeatAction();
    }

    private void DoRepeatAction()
    {
        if (_isIncrementing)
        {
            Value = CoerceValue(Value + Increment);
        }
        else
        {
            Value = CoerceValue(Value - Increment);
        }
    }

    private void StopRepeatTimer()
    {
        if (_repeatTimer is not null)
        {
            _repeatTimer.Stop();
            _repeatTimer = null;
        }
    }

    #endregion

    private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
    {
        if (SelectAllTextOnFocus && _textBox is not null)
        {
            _textBox.SelectAll();
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        ApplyTextBoxValue();
    }

    private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ApplyTextBoxValue();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Up)
        {
            Value = CoerceValue(Value + Increment);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Down)
        {
            Value = CoerceValue(Value - Increment);
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Escape)
        {
            UpdateTextBox(); // Revert text to current value
            e.Handled = true;
        }
    }

    private void ApplyTextBoxValue()
    {
        if (_textBox is not null && double.TryParse(_textBox.Text, out var newValue))
        {
            Value = CoerceValue(newValue);
        }
        else
        {
            UpdateTextBox();
        }
    }

    private double CoerceValue(double value)
    {
        return Math.Max(Minimum, Math.Min(Maximum, value));
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSpinner spinner)
        {
            spinner.UpdateTextBox();
            spinner.Text = spinner.Value.ToString(spinner.Format);
            spinner.ValueChanged?.Invoke(spinner, (double)e.NewValue);
        }
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSpinner spinner)
        {
            spinner.UpdateTextBox();
            spinner.Text = spinner.Value.ToString(spinner.Format);
        }
    }

    private static void OnSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSpinner spinner)
        {
            spinner.UpdateVisualState();
        }
    }

    private void UpdateTextBox()
    {
        if (_textBox is not null)
        {
            _textBox.Text = Value.ToString(Format);
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
}
