namespace Fluent;

using System.Globalization;
using System.Text;
using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents a numeric spinner control within a Ribbon. The spinner allows
/// the user to increment or decrement a numeric value.
/// </summary>
[TemplatePart(Name = PART_TextBox, Type = typeof(TextBox))]
[TemplatePart(Name = PART_UpButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_DownButton, Type = typeof(WinUIButton))]
public partial class RibbonSpinner : RibbonControl, IScalableRibbonControl, IMediumIconProvider, IRibbonHeaderAlignable
{
    private const string PART_TextBox = "PART_TextBox";
    private const string PART_UpButton = "PART_UpButton";
    private const string PART_DownButton = "PART_DownButton";

    private TextBox? _textBox;
    private WinUIButton? _upButton;
    private WinUIButton? _downButton;
    private FrameworkElement? _headerText;
    private DispatcherTimer? _repeatTimer;
    private bool _isIncrementing;
    private bool _redirectingFocus;
    private bool _suppressNextEditorFocusRedirect;
    private Microsoft.UI.Xaml.FocusState _delegatedFocusState;
    private double _effectiveValue;
    private double _effectiveMinimum;
    private double _effectiveMaximum = double.MaxValue;
    private double _effectiveIncrement = 1.0;
    private bool _isCoercingDependencyValue;

    #region Events

    /// <summary>
    /// Occurs when the value changes.
    /// </summary>
    public event RoutedPropertyChangedEventHandler<double>? ValueChanged;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonSpinner),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the spinner.
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
        get => _effectiveValue;
        set => SetValue(ValueProperty, CoerceValue(value));
    }

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(
            nameof(Minimum),
            typeof(double),
            typeof(RibbonSpinner),
            new PropertyMetadata(0.0, OnMinimumChanged));

    /// <summary>
    /// Gets or sets the minimum value.
    /// </summary>
    public double Minimum
    {
        get => _effectiveMinimum;
        set
        {
            EnsureFinite(value, nameof(Minimum));
            SetValue(MinimumProperty, value);
        }
    }

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(double),
            typeof(RibbonSpinner),
            new PropertyMetadata(double.MaxValue, OnMaximumChanged));

    /// <summary>
    /// Gets or sets the maximum value.
    /// </summary>
    public double Maximum
    {
        get => _effectiveMaximum;
        set
        {
            EnsureFinite(value, nameof(Maximum));
            SetValue(MaximumProperty, value);
        }
    }

    /// <summary>Identifies the <see cref="Increment"/> dependency property.</summary>
    public static readonly DependencyProperty IncrementProperty =
        DependencyProperty.Register(
            nameof(Increment),
            typeof(double),
            typeof(RibbonSpinner),
            new PropertyMetadata(1.0, OnIncrementChanged));

    /// <summary>
    /// Gets or sets the increment step.
    /// </summary>
    public double Increment
    {
        get => _effectiveIncrement;
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
    public new static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonSpinner),
            new PropertyMetadata(RibbonControlSize.Medium, OnSizeChanged));

    /// <summary>
    /// Gets or sets the size of the control.
    /// </summary>
    public new RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public new static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonSpinner),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    public new string KeyTip
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
        GettingFocus += OnRibbonSpinnerGettingFocus;
        GotFocus += OnRibbonSpinnerGotFocus;
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

        if (_textBox is not null)
        {
            _textBox.LostFocus -= OnTextBoxLostFocus;
            _textBox.KeyDown -= OnTextBoxKeyDown;
            _textBox.GotFocus -= OnTextBoxGotFocus;
        }

        _textBox = GetTemplateChild(PART_TextBox) as TextBox;
        _upButton = GetTemplateChild(PART_UpButton) as WinUIButton;
        _downButton = GetTemplateChild(PART_DownButton) as WinUIButton;
        _headerText = GetTemplateChild("HeaderText") as FrameworkElement;

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
            _textBox.IsTabStop = false;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
                _textBox,
                Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            _textBox.LostFocus += OnTextBoxLostFocus;
            _textBox.KeyDown += OnTextBoxKeyDown;
            _textBox.GotFocus += OnTextBoxGotFocus;
        }

        ConfigureImplementationButton(_upButton);
        ConfigureImplementationButton(_downButton);

        UpdateTextBox();
        UpdateVisualState();
        UpdateFocusVisualState(false);
    }

    #endregion

    #region IScalableRibbonControl

    /// <inheritdoc/>
    public void ScaleTo(RibbonControlSize size)
    {
        Size = size;
    }

    #endregion

    /// <inheritdoc />
    FrameworkElement? IRibbonHeaderAlignable.HeaderPresenter => _headerText;

    /// <inheritdoc />
    public override FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new RibbonSpinner
        {
            Header = Header,
            Value = Value,
            Minimum = Minimum,
            Maximum = Maximum,
            Increment = Increment,
            Format = Format,
            Size = RibbonControlSize.Small,
            InputWidth = InputWidth,
            Delay = Delay,
            Interval = Interval,
            SelectAllTextOnFocus = SelectAllTextOnFocus
        };
        BindQuickAccessItem(this, clone);
        return clone;
    }

    #region Methods

    private static void ConfigureImplementationButton(WinUIButton? button)
    {
        if (button is null)
        {
            return;
        }

        button.IsTabStop = false;
        button.AllowFocusOnInteraction = false;
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
            button,
            Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
    }

    private void OnRibbonSpinnerGotFocus(object sender, RoutedEventArgs e)
    {
        if (_suppressNextEditorFocusRedirect)
        {
            _suppressNextEditorFocusRedirect = false;
            return;
        }

        if (_textBox is null || XamlRoot is null)
        {
            return;
        }

        var focused = FocusManager.GetFocusedElement(XamlRoot);
        if (ReferenceEquals(focused, this) && !_redirectingFocus)
        {
            if (FocusState is not Microsoft.UI.Xaml.FocusState.Unfocused)
            {
                _delegatedFocusState = FocusState;
            }

            _redirectingFocus = true;
            try
            {
                var focusState = _delegatedFocusState is Microsoft.UI.Xaml.FocusState.Keyboard
                    or Microsoft.UI.Xaml.FocusState.Pointer
                    ? _delegatedFocusState
                    : Microsoft.UI.Xaml.FocusState.Programmatic;
                FocusEditor(focusState);
            }
            finally
            {
                _redirectingFocus = false;
            }

            UpdateFocusVisualState(false);
            ScheduleFocusVisualUpdate();
        }

    }

    private void OnRibbonSpinnerGettingFocus(UIElement sender, GettingFocusEventArgs e)
    {
        if (ReferenceEquals(e.NewFocusedElement, this)
            || (ReferenceEquals(e.NewFocusedElement, _textBox) && !_redirectingFocus))
        {
            _delegatedFocusState = e.FocusState;
        }
    }

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
        UpdateFocusVisualState(false);
        ScheduleFocusVisualUpdate();
        if (SelectAllTextOnFocus && _textBox is not null)
        {
            _textBox.SelectAll();
        }
    }

    private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        ScheduleFocusVisualUpdate(resetWhenOutside: true);
        ApplyTextBoxValue();
    }

    private void UpdateFocusVisualState(bool useTransitions)
    {
        var focusState = FocusRoutingHelper.ResolveDelegatedFocusState(
            _delegatedFocusState,
            _textBox?.FocusState ?? Microsoft.UI.Xaml.FocusState.Unfocused);
        var stateName = focusState == Microsoft.UI.Xaml.FocusState.Keyboard
            ? "KeyboardFocused"
            : focusState == Microsoft.UI.Xaml.FocusState.Pointer
                ? "PointerFocused"
                : "Unfocused";
        VisualStateManager.GoToState(this, stateName, useTransitions);
    }

    private void ScheduleFocusVisualUpdate(bool resetWhenOutside = false)
    {
        DispatcherQueue.TryEnqueue(
            () =>
            {
                var focused = XamlRoot is null
                    ? null
                    : FocusManager.GetFocusedElement(XamlRoot);
                if (resetWhenOutside
                    && !ReferenceEquals(focused, this)
                    && !ReferenceEquals(focused, _textBox))
                {
                    _delegatedFocusState = Microsoft.UI.Xaml.FocusState.Unfocused;
                }

                UpdateFocusVisualState(true);
            });
    }

    private bool FocusEditor(Microsoft.UI.Xaml.FocusState focusState)
    {
        if (_textBox is null)
        {
            return false;
        }

        var isTabStop = _textBox.IsTabStop;
        try
        {
            _textBox.IsTabStop = true;
            return _textBox.Focus(focusState);
        }
        finally
        {
            _textBox.IsTabStop = isTabStop;
        }
    }

    private void OnTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            ApplyTextBoxValue();
            MoveFocusOffTextBox();
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
            MoveFocusOffTextBox();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Mirrors WPF's Spinner, which moves focus off the text box after Enter/Escape so the commit
    /// is final and the next Tab continues from the spinner.
    /// </summary>
    private void MoveFocusOffTextBox()
    {
        if (IsTabStop)
        {
            _suppressNextEditorFocusRedirect = true;
            if (!Focus(FocusState.Programmatic))
            {
                _suppressNextEditorFocusRedirect = false;
            }
        }
    }

    internal bool FocusEditorForAutomation()
    {
        ApplyTemplate();
        var ownerFocused = Focus(FocusState.Programmatic);
        if (_textBox is null)
        {
            return ownerFocused;
        }

        if (XamlRoot is not null
            && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), _textBox))
        {
            return true;
        }

        return FocusEditor(FocusState.Programmatic);
    }

    private void ApplyTextBoxValue()
    {
        if (_textBox is not null && TryParseValue(_textBox.Text, out var newValue))
        {
            Value = CoerceValue(newValue);
        }

        // Always rewrite the text box from Value: on success this re-applies Format (so "42"
        // becomes "42 px"), and on failure it reverts whatever invalid text the user typed.
        UpdateTextBox();
    }

    /// <summary>
    /// Parses user input leniently, mirroring WPF's <c>SpinnerTextToValueConverter</c>:
    /// everything except digits, decimal/group separators and a leading sign is stripped, then the
    /// remainder must parse as a finite number. Anything else is rejected so the caller reverts.
    /// <see cref="Format"/> decorates the displayed value (for example "0 px"), so the text the
    /// user edits is not a bare number and a plain double.Parse would reject every edit.
    /// </summary>
    private static bool TryParseValue(string? text, out double value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // Keep only the numeric portion, dropping any format decoration around it.
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (char.IsDigit(character)
                || character == '.'
                || character == ','
                || (character == '-' && builder.Length == 0))
            {
                builder.Append(character);
            }
        }

        var stripped = builder.ToString();
        if (stripped.Length == 0)
        {
            return false;
        }

        if (!double.TryParse(stripped, NumberStyles.Any, CultureInfo.CurrentCulture, out value)
            && !double.TryParse(stripped, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            return false;
        }

        // "NaN"/"Infinity" survive TryParse on modern .NET and would otherwise be stored verbatim.
        if (!double.IsFinite(value))
        {
            value = 0;
            return false;
        }

        return true;
    }

    private double CoerceValue(double value)
    {
        if (!double.IsFinite(value))
        {
            return Minimum;
        }

        var min = Minimum;
        var max = Maximum;
        if (max < min)
        {
            (min, max) = (max, min);
        }

        return Math.Max(min, Math.Min(max, value));
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSpinner spinner)
        {
            spinner.ApplyDependencyValue((double)e.NewValue);
        }
    }

    private static void OnMinimumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSpinner spinner)
        {
            var oldMinimum = spinner._effectiveMinimum;
            var newMinimum = (double)e.NewValue;
            EnsureFinite(newMinimum, nameof(Minimum));
            spinner._effectiveMinimum = newMinimum;
            NotifyRangeChanged(
                spinner,
                oldMinimum,
                spinner.Maximum,
                newMinimum,
                spinner.Maximum);
        }
    }

    private static void OnMaximumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonSpinner spinner)
        {
            var oldMaximum = spinner._effectiveMaximum;
            var newMaximum = (double)e.NewValue;
            EnsureFinite(newMaximum, nameof(Maximum));
            spinner._effectiveMaximum = newMaximum;
            NotifyRangeChanged(
                spinner,
                spinner.Minimum,
                oldMaximum,
                spinner.Minimum,
                newMaximum);
        }
    }

    private static void NotifyRangeChanged(
        RibbonSpinner spinner,
        double oldMinimumValue,
        double oldMaximumValue,
        double newMinimumValue,
        double newMaximumValue)
    {
        var oldMinimum = Math.Min(oldMinimumValue, oldMaximumValue);
        var oldMaximum = Math.Max(oldMinimumValue, oldMaximumValue);
        var newMinimum = Math.Min(newMinimumValue, newMaximumValue);
        var newMaximum = Math.Max(newMinimumValue, newMaximumValue);

        spinner.ApplyDependencyValue((double)spinner.GetValue(ValueProperty));

        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(spinner)
            is Fluent.Automation.Peers.RibbonSpinnerAutomationPeer peer)
        {
            peer.RaiseRangeChanged(oldMinimum, newMinimum, oldMaximum, newMaximum);
        }
    }

    private static void OnIncrementChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not RibbonSpinner spinner)
        {
            return;
        }

        var oldIncrement = spinner._effectiveIncrement;
        var newIncrement = (double)e.NewValue;
        spinner._effectiveIncrement = newIncrement;
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(spinner)
            is Fluent.Automation.Peers.RibbonSpinnerAutomationPeer peer)
        {
            peer.RaiseIncrementChanged(oldIncrement, newIncrement);
        }
    }

    private void UpdateEffectiveValue(double newValue)
    {
        var oldValue = _effectiveValue;
        _effectiveValue = newValue;
        UpdateTextBox();
        Text = newValue.ToString(Format);
        if (oldValue.Equals(newValue))
        {
            return;
        }

        ValueChanged?.Invoke(
            this,
            new RoutedPropertyChangedEventArgs<double>(oldValue, newValue));
        if (Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.FromElement(this)
            is Fluent.Automation.Peers.RibbonSpinnerAutomationPeer peer)
        {
            peer.RaiseValueChanged(oldValue, newValue);
        }
    }

    private void ApplyDependencyValue(double requestedValue)
    {
        var coercedValue = CoerceValue(requestedValue);
        if (!_isCoercingDependencyValue
            && !requestedValue.Equals(coercedValue))
        {
            _isCoercingDependencyValue = true;
            try
            {
                SetValue(ValueProperty, coercedValue);
            }
            finally
            {
                _isCoercingDependencyValue = false;
            }

            return;
        }

        UpdateEffectiveValue(coercedValue);
    }

    private static void EnsureFinite(double value, string propertyName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                propertyName,
                value,
                "The value must be finite.");
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

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonSpinnerAutomationPeer(this);
}
