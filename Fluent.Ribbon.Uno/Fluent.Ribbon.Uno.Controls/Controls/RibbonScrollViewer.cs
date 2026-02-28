namespace Fluent;

/// <summary>
/// Represents a scrolling helper for ribbon tabs and groups.
/// Wraps a ScrollViewer with left/right scroll buttons for horizontal scrolling.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// In WPF this subclassed ScrollViewer with custom HitTest; here it's a wrapper Control.
/// </remarks>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_ScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = PART_LeftButton, Type = typeof(Button))]
[TemplatePart(Name = PART_RightButton, Type = typeof(Button))]
public partial class RibbonScrollViewer : Control
{
    private const string PART_ScrollViewer = "PART_ScrollViewer";
    private const string PART_LeftButton = "PART_LeftButton";
    private const string PART_RightButton = "PART_RightButton";

    private ScrollViewer? _scrollViewer;
    private Button? _leftButton;
    private Button? _rightButton;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(UIElement),
            typeof(RibbonScrollViewer),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the scrollable content.
    /// </summary>
    public UIElement? Content
    {
        get => (UIElement?)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Identifies the <see cref="ScrollStep"/> dependency property.</summary>
    public static readonly DependencyProperty ScrollStepProperty =
        DependencyProperty.Register(
            nameof(ScrollStep),
            typeof(double),
            typeof(RibbonScrollViewer),
            new PropertyMetadata(48.0));

    /// <summary>
    /// Gets or sets the scroll step amount in pixels.
    /// </summary>
    public double ScrollStep
    {
        get => (double)GetValue(ScrollStepProperty);
        set => SetValue(ScrollStepProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonScrollViewer"/> class.
    /// </summary>
    public RibbonScrollViewer()
    {
        DefaultStyleKey = typeof(RibbonScrollViewer);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_leftButton is not null)
        {
            _leftButton.Click -= OnLeftButtonClick;
        }

        if (_rightButton is not null)
        {
            _rightButton.Click -= OnRightButtonClick;
        }

        _scrollViewer = GetTemplateChild(PART_ScrollViewer) as ScrollViewer;
        _leftButton = GetTemplateChild(PART_LeftButton) as Button;
        _rightButton = GetTemplateChild(PART_RightButton) as Button;

        if (_leftButton is not null)
        {
            _leftButton.Click += OnLeftButtonClick;
        }

        if (_rightButton is not null)
        {
            _rightButton.Click += OnRightButtonClick;
        }

        if (_scrollViewer is not null)
        {
            _scrollViewer.ViewChanged += OnScrollViewerViewChanged;
        }

        UpdateButtonVisibility();
    }

    #endregion

    #region Methods

    private void OnLeftButtonClick(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ChangeView(
                Math.Max(0, _scrollViewer.HorizontalOffset - ScrollStep),
                null,
                null);
        }
    }

    private void OnRightButtonClick(object sender, RoutedEventArgs e)
    {
        if (_scrollViewer is not null)
        {
            _scrollViewer.ChangeView(
                _scrollViewer.HorizontalOffset + ScrollStep,
                null,
                null);
        }
    }

    private void OnScrollViewerViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        if (_scrollViewer is null)
        {
            return;
        }

        if (_leftButton is not null)
        {
            _leftButton.Visibility = _scrollViewer.HorizontalOffset > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (_rightButton is not null)
        {
            var maxOffset = _scrollViewer.ScrollableWidth;
            _rightButton.Visibility = _scrollViewer.HorizontalOffset < maxOffset - 1
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    #endregion
}
