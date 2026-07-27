namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents a scrolling helper for ribbon tabs and groups.
/// Wraps a ScrollViewer with left/right scroll buttons for horizontal scrolling.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// Uno heads keep the WPF-shaped <see cref="ScrollViewer"/> base. WinUI uses a
/// <see cref="Control"/> wrapper because its <see cref="ScrollViewer"/> is sealed.
/// </remarks>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_ScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = PART_LeftButton, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_RightButton, Type = typeof(WinUIButton))]
#if WINDOWS
public partial class RibbonScrollViewer : Control
#else
public partial class RibbonScrollViewer : ScrollViewer
#endif
{
    private const string PART_ScrollViewer = "PART_ScrollViewer";
    private const string PART_LeftButton = "PART_LeftButton";
    private const string PART_RightButton = "PART_RightButton";

    private ScrollViewer? _scrollViewer;
    private WinUIButton? _leftButton;
    private WinUIButton? _rightButton;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
#if WINDOWS
    public static readonly DependencyProperty ContentProperty =
#else
    public new static readonly DependencyProperty ContentProperty =
#endif
        DependencyProperty.Register(
            nameof(Content),
            typeof(UIElement),
            typeof(RibbonScrollViewer),
            new PropertyMetadata(null, OnContentChanged));

    /// <summary>
    /// Gets or sets the scrollable content.
    /// </summary>
#if WINDOWS
    public UIElement? Content
#else
    public new UIElement? Content
#endif
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

        if (_scrollViewer is not null)
        {
            _scrollViewer.ViewChanged -= OnScrollViewerViewChanged;
            _scrollViewer.SizeChanged -= OnScrollViewerSizeChanged;
            _scrollViewer.LayoutUpdated -= OnScrollViewerLayoutUpdated;
        }

        _scrollViewer = GetTemplateChild(PART_ScrollViewer) as ScrollViewer;
        _leftButton = GetTemplateChild(PART_LeftButton) as WinUIButton;
        _rightButton = GetTemplateChild(PART_RightButton) as WinUIButton;

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
            _scrollViewer.SizeChanged += OnScrollViewerSizeChanged;
            _scrollViewer.LayoutUpdated += OnScrollViewerLayoutUpdated;
        }

        ApplyContent();
        UpdateButtonVisibility();
    }

    #endregion

    #region Methods

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonScrollViewer viewer)
        {
            viewer.ApplyContent();
        }
    }

    // The shadowing Content property is not reliably picked up by the template binding on every
    // head, so the hosted scroll viewer is populated explicitly.
    private void ApplyContent()
    {
        if (_scrollViewer is not null
            && !ReferenceEquals(_scrollViewer.Content, Content))
        {
            _scrollViewer.Content = Content;
        }
    }

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

    private void OnScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateButtonVisibility();
    }

    // ScrollableWidth only becomes accurate after layout, and no view change is raised when the
    // content grows or the viewport shrinks, so the affordances are refreshed on every layout pass.
    private void OnScrollViewerLayoutUpdated(object? sender, object e)
    {
        UpdateButtonVisibility();
    }

    private void UpdateButtonVisibility()
    {
        if (_scrollViewer is null)
        {
            return;
        }

        // Assign only on change: this runs from LayoutUpdated, and an unconditional write would
        // invalidate layout again and spin.
        if (_leftButton is not null)
        {
            var leftVisibility = _scrollViewer.HorizontalOffset > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (_leftButton.Visibility != leftVisibility)
            {
                _leftButton.Visibility = leftVisibility;
            }
        }

        if (_rightButton is not null)
        {
            var maxOffset = _scrollViewer.ScrollableWidth;
            var rightVisibility = _scrollViewer.HorizontalOffset < maxOffset - 1
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (_rightButton.Visibility != rightVisibility)
            {
                _rightButton.Visibility = rightVisibility;
            }
        }
    }

    /// <summary>Performs a portable hit test.</summary>
    protected virtual HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        return new HitTestResult(this);
    }

    /// <summary>Handles WPF-compatible wheel input.</summary>
    protected virtual void OnMouseWheel(PointerRoutedEventArgs e)
    {
#if WINDOWS
        if (_scrollViewer is null)
        {
            return;
        }

        var scrollViewer = _scrollViewer;
#else
        var scrollViewer = this;
#endif
        var delta = e.GetCurrentPoint(scrollViewer).Properties.MouseWheelDelta;
        if (delta == 0)
        {
            return;
        }

        scrollViewer.ChangeView(
            Math.Max(0, scrollViewer.HorizontalOffset + (delta > 0 ? -ScrollStep : ScrollStep)),
            null,
            null);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!e.Handled)
        {
            OnMouseWheel(e);
        }
    }

    #endregion
}
