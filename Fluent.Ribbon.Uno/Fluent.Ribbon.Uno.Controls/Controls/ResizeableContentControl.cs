namespace Fluent;

/// <summary>
/// A content control that can be resized by the user using drag handles.
/// Typically used in dropdown popups to allow the user to resize gallery content.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// Uses pointer events instead of WPF Thumb/DragDelta.
/// </remarks>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_ResizeBothThumb, Type = typeof(UIElement))]
[TemplatePart(Name = PART_ResizeVerticalThumb, Type = typeof(UIElement))]
public partial class ResizeableContentControl : Control
{
    private const string PART_ResizeBothThumb = "PART_ResizeBothThumb";
    private const string PART_ResizeVerticalThumb = "PART_ResizeVerticalThumb";

    private UIElement? _resizeBothThumb;
    private UIElement? _resizeVerticalThumb;
    private Windows.Foundation.Point _dragStart;
    private Windows.Foundation.Size _sizeAtDragStart;
    private bool _isDragging;
    private bool _isDraggingBoth;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(ResizeableContentControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the content.
    /// </summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Identifies the <see cref="CanResizeBothDirections"/> dependency property.</summary>
    public static readonly DependencyProperty CanResizeBothDirectionsProperty =
        DependencyProperty.Register(
            nameof(CanResizeBothDirections),
            typeof(bool),
            typeof(ResizeableContentControl),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the control can be resized in both horizontal and vertical directions.
    /// </summary>
    public bool CanResizeBothDirections
    {
        get => (bool)GetValue(CanResizeBothDirectionsProperty);
        set => SetValue(CanResizeBothDirectionsProperty, value);
    }

    /// <summary>Identifies the <see cref="CanResizeVertical"/> dependency property.</summary>
    public static readonly DependencyProperty CanResizeVerticalProperty =
        DependencyProperty.Register(
            nameof(CanResizeVertical),
            typeof(bool),
            typeof(ResizeableContentControl),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the control can be resized vertically.
    /// </summary>
    public bool CanResizeVertical
    {
        get => (bool)GetValue(CanResizeVerticalProperty);
        set => SetValue(CanResizeVerticalProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizeableContentControl"/> class.
    /// </summary>
    public ResizeableContentControl()
    {
        DefaultStyleKey = typeof(ResizeableContentControl);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachHandlers();

        _resizeBothThumb = GetTemplateChild(PART_ResizeBothThumb) as UIElement;
        _resizeVerticalThumb = GetTemplateChild(PART_ResizeVerticalThumb) as UIElement;

        AttachHandlers();
    }

    #endregion

    #region Resize Handling

    private void AttachHandlers()
    {
        if (_resizeBothThumb is not null)
        {
            _resizeBothThumb.PointerPressed += OnResizeBothPointerPressed;
            _resizeBothThumb.PointerMoved += OnResizePointerMoved;
            _resizeBothThumb.PointerReleased += OnResizePointerReleased;
            _resizeBothThumb.PointerCaptureLost += OnResizePointerCaptureLost;
        }

        if (_resizeVerticalThumb is not null)
        {
            _resizeVerticalThumb.PointerPressed += OnResizeVerticalPointerPressed;
            _resizeVerticalThumb.PointerMoved += OnResizePointerMoved;
            _resizeVerticalThumb.PointerReleased += OnResizePointerReleased;
            _resizeVerticalThumb.PointerCaptureLost += OnResizePointerCaptureLost;
        }
    }

    private void DetachHandlers()
    {
        if (_resizeBothThumb is not null)
        {
            _resizeBothThumb.PointerPressed -= OnResizeBothPointerPressed;
            _resizeBothThumb.PointerMoved -= OnResizePointerMoved;
            _resizeBothThumb.PointerReleased -= OnResizePointerReleased;
            _resizeBothThumb.PointerCaptureLost -= OnResizePointerCaptureLost;
        }

        if (_resizeVerticalThumb is not null)
        {
            _resizeVerticalThumb.PointerPressed -= OnResizeVerticalPointerPressed;
            _resizeVerticalThumb.PointerMoved -= OnResizePointerMoved;
            _resizeVerticalThumb.PointerReleased -= OnResizePointerReleased;
            _resizeVerticalThumb.PointerCaptureLost -= OnResizePointerCaptureLost;
        }
    }

    private void OnResizeBothPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        StartDrag(sender, e, isBoth: true);
    }

    private void OnResizeVerticalPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        StartDrag(sender, e, isBoth: false);
    }

    private void StartDrag(object sender, PointerRoutedEventArgs e, bool isBoth)
    {
        if (sender is UIElement element)
        {
            _isDragging = true;
            _isDraggingBoth = isBoth;
            _dragStart = e.GetCurrentPoint(this).Position;
            _sizeAtDragStart = new Windows.Foundation.Size(ActualWidth, ActualHeight);
            element.CapturePointer(e.Pointer);
            e.Handled = true;
        }
    }

    private void OnResizePointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var current = e.GetCurrentPoint(this).Position;
        var deltaX = current.X - _dragStart.X;
        var deltaY = current.Y - _dragStart.Y;

        if (_isDraggingBoth)
        {
            var newWidth = Math.Max(MinWidth, _sizeAtDragStart.Width + deltaX);
            if (MaxWidth > 0)
            {
                newWidth = Math.Min(newWidth, MaxWidth);
            }

            Width = newWidth;
        }

        var newHeight = Math.Max(MinHeight, _sizeAtDragStart.Height + deltaY);
        if (MaxHeight > 0 && !double.IsPositiveInfinity(MaxHeight))
        {
            newHeight = Math.Min(newHeight, MaxHeight);
        }

        Height = newHeight;

        e.Handled = true;
    }

    private void OnResizePointerReleased(object sender, PointerRoutedEventArgs e)
    {
        StopDrag(sender, e);
    }

    private void OnResizePointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        _isDragging = false;
    }

    private void StopDrag(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;

        if (sender is UIElement element)
        {
            element.ReleasePointerCapture(e.Pointer);
        }

        e.Handled = true;
    }

    #endregion
}
