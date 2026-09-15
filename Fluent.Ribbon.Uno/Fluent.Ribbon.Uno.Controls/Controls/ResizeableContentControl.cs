namespace Fluent;

using Fluent.Automation.Peers;
using Fluent.Localization;
using Microsoft.UI.Xaml.Automation;
using Windows.System;

/// <summary>
/// A content control that can be resized by the user using drag handles.
/// Typically used in dropdown popups to allow the user to resize gallery content.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_ResizeBothThumb, Type = typeof(Control))]
[TemplatePart(Name = PART_ResizeVerticalThumb, Type = typeof(Control))]
public partial class ResizeableContentControl : ContentControl
{
    internal const double KeyboardResizeStep = 10;
    internal const double AcceleratedKeyboardResizeStep = 50;

    private const string PART_ResizeBothThumb = "PART_ResizeBothThumb";
    private const string PART_ResizeVerticalThumb = "PART_ResizeVerticalThumb";

    private ResizeHandle? _resizeBothThumb;
    private ResizeHandle? _resizeVerticalThumb;
    private Windows.Foundation.Point _dragStart;
    private Windows.Foundation.Size _sizeAtDragStart;
    private bool _isDragging;
    private bool _isDraggingBoth;
    private uint? _activePointerId;
    private UIElement? _dragCoordinateRoot;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="CanResizeBothDirections"/> dependency property.</summary>
    public static readonly DependencyProperty CanResizeBothDirectionsProperty =
        DependencyProperty.Register(
            nameof(CanResizeBothDirections),
            typeof(bool),
            typeof(ResizeableContentControl),
            new PropertyMetadata(true, OnResizeAvailabilityChanged));

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
            new PropertyMetadata(true, OnResizeAvailabilityChanged));

    /// <summary>
    /// Gets or sets whether the control can be resized vertically.
    /// </summary>
    public bool CanResizeVertical
    {
        get => (bool)GetValue(CanResizeVerticalProperty);
        set => SetValue(CanResizeVerticalProperty, value);
    }

    /// <summary>Identifies the <see cref="ResizeMode"/> dependency property.</summary>
    public static readonly DependencyProperty ResizeModeProperty =
        DependencyProperty.Register(
            nameof(ResizeMode),
            typeof(ContextMenuResizeMode),
            typeof(ResizeableContentControl),
            new PropertyMetadata(ContextMenuResizeMode.Both, OnResizeModeChanged));

    /// <summary>Gets or sets the resize mode.</summary>
    public ContextMenuResizeMode ResizeMode
    {
        get => (ContextMenuResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    /// <summary>Gets whether a pointer is over a resize handle or a resize interaction is active.</summary>
    public bool IsMouseOverResizeThumbs =>
        _isDragging
        || (_resizeBothThumb?.IsPointerOverHandle ?? false)
        || (_resizeVerticalThumb?.IsPointerOverHandle ?? false);

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizeableContentControl"/> class.
    /// </summary>
    public ResizeableContentControl()
    {
        DefaultStyleKey = typeof(ResizeableContentControl);
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedHandleMetadata);
        Unloaded += (_, _) => CancelResize();
        IsEnabledChanged += (_, _) => UpdateHandleAvailability();
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        DetachHandlers();

        _resizeBothThumb = GetTemplateChild(PART_ResizeBothThumb) as ResizeHandle;
        _resizeVerticalThumb = GetTemplateChild(PART_ResizeVerticalThumb) as ResizeHandle;

        ConfigureHandle(_resizeBothThumb, resizesBothDirections: true);
        ConfigureHandle(_resizeVerticalThumb, resizesBothDirections: false);
        AttachHandlers();
        UpdateHandleAvailability();
    }

    private void RefreshLocalizedHandleMetadata()
    {
        ConfigureHandle(_resizeBothThumb, resizesBothDirections: true);
        ConfigureHandle(_resizeVerticalThumb, resizesBothDirections: false);
    }

    #endregion

    #region Resize Handling

    private static void OnResizeModeChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (ResizeableContentControl)d;
        control.UpdateHandleAvailability();
    }

    private static void OnResizeAvailabilityChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        ((ResizeableContentControl)d).UpdateHandleAvailability();
    }

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
        CancelResize();
        if (_resizeBothThumb is not null)
        {
            _resizeBothThumb.PointerPressed -= OnResizeBothPointerPressed;
            _resizeBothThumb.PointerMoved -= OnResizePointerMoved;
            _resizeBothThumb.PointerReleased -= OnResizePointerReleased;
            _resizeBothThumb.PointerCaptureLost -= OnResizePointerCaptureLost;
            _resizeBothThumb.ResizeOwner = null;
        }

        if (_resizeVerticalThumb is not null)
        {
            _resizeVerticalThumb.PointerPressed -= OnResizeVerticalPointerPressed;
            _resizeVerticalThumb.PointerMoved -= OnResizePointerMoved;
            _resizeVerticalThumb.PointerReleased -= OnResizePointerReleased;
            _resizeVerticalThumb.PointerCaptureLost -= OnResizePointerCaptureLost;
            _resizeVerticalThumb.ResizeOwner = null;
        }
    }

    private void ConfigureHandle(
        ResizeHandle? handle,
        bool resizesBothDirections)
    {
        if (handle is null)
        {
            return;
        }

        handle.ResizeOwner = this;
        handle.ResizesBothDirections = resizesBothDirections;

        var localization = RibbonLocalization.Current.Localization;
        AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
            handle,
            resizesBothDirections
                ? localization.ResizeBothHandleName
                : localization.ResizeVerticalHandleName);

        AutomationPeerHelpers.SetValueIfUnsetOrGenerated(
            handle,
            AutomationProperties.HelpTextProperty,
            resizesBothDirections
                ? localization.ResizeBothHandleHelpText
                : localization.ResizeVerticalHandleHelpText);
        handle.AutomationHelpText = AutomationProperties.GetHelpText(handle);
    }

    private void OnResizeBothPointerPressed(
        object sender,
        PointerRoutedEventArgs e)
    {
        StartDrag(sender, e, isBoth: true);
    }

    private void OnResizeVerticalPointerPressed(
        object sender,
        PointerRoutedEventArgs e)
    {
        StartDrag(sender, e, isBoth: false);
    }

    private void StartDrag(
        object sender,
        PointerRoutedEventArgs e,
        bool isBoth)
    {
        if (sender is not UIElement element || !IsResizeHandleAvailable(isBoth))
        {
            return;
        }

        if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse
            && !e.GetCurrentPoint(element).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (_activePointerId.HasValue || !element.CapturePointer(e.Pointer))
        {
            return;
        }

        _isDragging = true;
        _isDraggingBoth = isBoth;
        _activePointerId = e.Pointer.PointerId;
        _dragCoordinateRoot = XamlRoot?.Content ?? this;
        _dragStart = e.GetCurrentPoint(_dragCoordinateRoot).Position;
        _sizeAtDragStart = new Windows.Foundation.Size(
            GetEffectiveWidth(),
            GetEffectiveHeight());
        e.Handled = true;
    }

    private void OnResizePointerMoved(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isDragging || _activePointerId != e.Pointer.PointerId)
        {
            return;
        }

        var current = e.GetCurrentPoint(_dragCoordinateRoot ?? this).Position;
        var horizontalChange = (current.X - _dragStart.X)
                               * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1);
        ResizeTo(
            _sizeAtDragStart.Width + horizontalChange,
            _sizeAtDragStart.Height + current.Y - _dragStart.Y,
            _isDraggingBoth);
        e.Handled = true;
    }

    private void OnResizePointerReleased(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (!_isDragging || _activePointerId != e.Pointer.PointerId)
        {
            return;
        }

        _isDragging = false;
        _activePointerId = null;
        _dragCoordinateRoot = null;
        if (sender is UIElement element)
        {
            element.ReleasePointerCapture(e.Pointer);
        }

        e.Handled = true;
    }

    private void OnResizePointerCaptureLost(
        object sender,
        PointerRoutedEventArgs e)
    {
        if (_activePointerId != e.Pointer.PointerId)
        {
            return;
        }

        _isDragging = false;
        _activePointerId = null;
        _dragCoordinateRoot = null;
    }

    private void CancelResize()
    {
        _isDragging = false;
        _activePointerId = null;
        _dragCoordinateRoot = null;
        _resizeBothThumb?.ReleasePointerCaptures();
        _resizeVerticalThumb?.ReleasePointerCaptures();
    }

    internal bool TryResizeFromKey(
        VirtualKey key,
        bool resizesBothDirections,
        bool shiftDown)
    {
        if (!IsResizeHandleAvailable(resizesBothDirections)
            || !TryGetKeyboardResizeDelta(
                key,
                resizesBothDirections,
                shiftDown,
                out var horizontalChange,
                out var verticalChange))
        {
            return false;
        }

        ResizeBy(
            horizontalChange * (FlowDirection == FlowDirection.RightToLeft ? -1 : 1),
            verticalChange,
            resizesBothDirections);
        return true;
    }

    internal static bool TryGetKeyboardResizeDelta(
        VirtualKey key,
        bool resizesBothDirections,
        bool shiftDown,
        out double horizontalChange,
        out double verticalChange)
    {
        var step = shiftDown
            ? AcceleratedKeyboardResizeStep
            : KeyboardResizeStep;
        horizontalChange = 0;
        verticalChange = 0;

        switch (key)
        {
            case VirtualKey.Up:
                verticalChange = -step;
                return true;
            case VirtualKey.Down:
                verticalChange = step;
                return true;
            case VirtualKey.Left when resizesBothDirections:
                horizontalChange = -step;
                return true;
            case VirtualKey.Right when resizesBothDirections:
                horizontalChange = step;
                return true;
            default:
                return false;
        }
    }

    internal static double ClampResizeDimension(
        double value,
        double minimum,
        double maximum)
        => Math.Max(minimum, Math.Min(maximum, value));

    internal bool IsResizeHandleAvailable(bool resizesBothDirections)
    {
        return IsResizeHandleAvailable(
            ResizeMode,
            CanResizeVertical,
            CanResizeBothDirections,
            IsEnabled,
            resizesBothDirections);
    }

    internal static bool IsResizeHandleAvailable(
        ContextMenuResizeMode resizeMode,
        bool canResizeVertical,
        bool canResizeBothDirections,
        bool isEnabled,
        bool resizesBothDirections)
        => isEnabled
           && (resizesBothDirections
               ? resizeMode == ContextMenuResizeMode.Both
                 && canResizeBothDirections
               : resizeMode == ContextMenuResizeMode.Vertical
                 && canResizeVertical);

    internal void ResizeTo(
        double width,
        double height,
        bool resizeWidth)
    {
        var handle = ResizeMode == ContextMenuResizeMode.Both ? _resizeBothThumb : _resizeVerticalThumb;
        var handleWidth = handle is null ? 0 : Math.Max(handle.ActualWidth, handle.MinWidth);
        var handleHeight = handle is null ? 0 : Math.Max(handle.ActualHeight, handle.MinHeight);
        var minimumWidth = Math.Min(MaxWidth, Math.Max(
            MinWidth, handleWidth + Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right));
        var minimumHeight = Math.Min(MaxHeight, Math.Max(
            MinHeight, handleHeight + Padding.Top + Padding.Bottom + BorderThickness.Top + BorderThickness.Bottom));
        if (resizeWidth)
        {
            Width = ClampResizeDimension(width, minimumWidth, MaxWidth);
        }

        Height = ClampResizeDimension(height, minimumHeight, MaxHeight);
        PopupResizeHelper.RecordResize(this, resizeWidth);
    }

    private void ResizeBy(
        double horizontalChange,
        double verticalChange,
        bool resizeWidth)
    {
        ResizeTo(
            GetEffectiveWidth() + horizontalChange,
            GetEffectiveHeight() + verticalChange,
            resizeWidth);
    }

    private double GetEffectiveWidth()
        => double.IsNaN(Width) ? ActualWidth : Width;

    private double GetEffectiveHeight()
        => double.IsNaN(Height) ? ActualHeight : Height;

    private void UpdateHandleAvailability()
    {
        if (_isDragging && !IsResizeHandleAvailable(_isDraggingBoth))
        {
            CancelResize();
        }

        if (_resizeBothThumb is null && _resizeVerticalThumb is null)
        {
            return;
        }

        var focusedElement = XamlRoot is null
            ? null
            : FocusManager.GetFocusedElement(XamlRoot);
        var focusedHandle = focusedElement as ResizeHandle;
        var showBoth = ResizeMode == ContextMenuResizeMode.Both
                       && CanResizeBothDirections;
        var showVertical = ResizeMode == ContextMenuResizeMode.Vertical
                           && CanResizeVertical;

        if (_resizeBothThumb is not null)
        {
            _resizeBothThumb.Visibility = showBoth
                ? Visibility.Visible
                : Visibility.Collapsed;
            _resizeBothThumb.IsTabStop = showBoth;
        }

        if (_resizeVerticalThumb is not null)
        {
            _resizeVerticalThumb.Visibility = showVertical
                ? Visibility.Visible
                : Visibility.Collapsed;
            _resizeVerticalThumb.IsTabStop = showVertical;
        }

        if (focusedHandle is null
            || focusedHandle.Visibility == Visibility.Visible)
        {
            return;
        }

        var replacement = showBoth
            ? _resizeBothThumb
            : showVertical
                ? _resizeVerticalThumb
                : null;
        if (replacement is not null)
        {
            replacement.Focus(FocusState.Keyboard);
        }
        else
        {
            var focusTarget = XamlRoot?.Content is DependencyObject searchRoot
                ? FocusManager.FindFirstFocusableElement(searchRoot) as Control
                : null;
            focusTarget?.Focus(FocusState.Keyboard);
        }
    }

    #endregion
}
