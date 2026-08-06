namespace Fluent;

/// <summary>
/// Represents a TextBox control within a Ribbon.
/// </summary>
public partial class RibbonTextBox : TextBox, IHeaderedControl, IScalableRibbonControl, IMediumIconProvider, IQuickAccessItemProvider, IRibbonHeaderAlignable
{
    private FrameworkElement? _headerPresenter;
    private bool _synchronizingSelection;
    private TextBox? _textBox;
    private bool _redirectingFocus;
    private Microsoft.UI.Xaml.FocusState _delegatedFocusState;

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
        GettingFocus += OnRibbonTextBoxGettingFocus;
        GotFocus += OnRibbonTextBoxGotFocus;
        SelectionChanged += OnOwnerSelectionChanged;
        QuickAccessHelper.AttachContextMenu(this);
    }

    #endregion

    #region Template

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (_textBox is not null)
        {
            _textBox.SelectionChanged -= OnEditorSelectionChanged;
            _textBox.GotFocus -= OnEditorGotFocus;
            _textBox.LostFocus -= OnEditorLostFocus;
        }

        base.OnApplyTemplate();

        _headerPresenter = GetTemplateChild("HeaderText") as FrameworkElement;
        _textBox = GetTemplateChild("PART_TextBox") as TextBox;
        if (_textBox is not null)
        {
            _textBox.IsTabStop = false;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
                _textBox,
                Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            _textBox.SelectionChanged += OnEditorSelectionChanged;
            _textBox.GotFocus += OnEditorGotFocus;
            _textBox.LostFocus += OnEditorLostFocus;
            SynchronizeEditorSelection();
        }

        UpdateFocusVisualState(false);
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
            return;
        }

        if (ReferenceEquals(focused, _textBox) && SelectAllTextOnFocus)
        {
            _textBox.SelectAll();
        }
    }

    private void OnRibbonTextBoxGettingFocus(UIElement sender, GettingFocusEventArgs e)
    {
        if (ReferenceEquals(e.NewFocusedElement, this)
            || (ReferenceEquals(e.NewFocusedElement, _textBox) && !_redirectingFocus))
        {
            _delegatedFocusState = e.FocusState;
        }
    }

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_textBox is null || _synchronizingSelection)
        {
            return;
        }

        _synchronizingSelection = true;
        try
        {
            base.SelectionStart = _textBox.SelectionStart;
            base.SelectionLength = _textBox.SelectionLength;
        }
        finally
        {
            _synchronizingSelection = false;
        }
    }

    private void OnEditorGotFocus(object sender, RoutedEventArgs e)
    {
        UpdateFocusVisualState(false);
        ScheduleFocusVisualUpdate();
    }

    private void OnEditorLostFocus(object sender, RoutedEventArgs e)
        => ScheduleFocusVisualUpdate(resetWhenOutside: true);

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

    private void OnOwnerSelectionChanged(object sender, RoutedEventArgs e)
    {
        if (_synchronizingSelection)
        {
            return;
        }

        SynchronizeEditorSelection();
    }

    private void SynchronizeEditorSelection()
    {
        if (_textBox is null || _synchronizingSelection)
        {
            return;
        }

        _synchronizingSelection = true;
        try
        {
            var start = Math.Clamp(base.SelectionStart, 0, _textBox.Text.Length);
            _textBox.Select(start, Math.Min(base.SelectionLength, _textBox.Text.Length - start));
        }
        finally
        {
            _synchronizingSelection = false;
        }
    }

    /// <summary>Selects text in the editable template part.</summary>
    public new void Select(int start, int length)
    {
        base.Select(start, length);
        _textBox?.Select(start, length);
    }

    /// <summary>Selects all text in the editable template part.</summary>
    public new void SelectAll()
    {
        base.SelectAll();
        _textBox?.SelectAll();
    }

    /// <summary>Gets or sets the caret/selection start owned by the editable template part.</summary>
    public new int SelectionStart
    {
        get => _textBox?.SelectionStart ?? base.SelectionStart;
        set
        {
            base.SelectionStart = value;
            if (_textBox is not null)
            {
                _textBox.SelectionStart = value;
            }
        }
    }

    /// <summary>Gets or sets the selection length owned by the editable template part.</summary>
    public new int SelectionLength
    {
        get => _textBox?.SelectionLength ?? base.SelectionLength;
        set
        {
            base.SelectionLength = value;
            if (_textBox is not null)
            {
                _textBox.SelectionLength = value;
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
