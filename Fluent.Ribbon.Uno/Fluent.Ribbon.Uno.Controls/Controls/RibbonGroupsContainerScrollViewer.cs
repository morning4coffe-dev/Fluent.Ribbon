namespace Fluent;

/// <summary>
/// Attached behavior that adds horizontal mouse-wheel scrolling to a <see cref="ScrollViewer"/>
/// used to host a <see cref="RibbonGroupsContainer"/>, and prevents scrolling while a dropdown is open.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// Uno heads keep the WPF-shaped <see cref="ScrollViewer"/> base. WinUI uses the
/// composed <see cref="RibbonScrollViewer"/> because its <see cref="ScrollViewer"/> is sealed.
/// The attached behavior remains available for ordinary <see cref="ScrollViewer"/> instances.
/// Attach it in XAML with <c>fluent:RibbonGroupsContainerScrollViewer.EnableHorizontalWheelScrolling="True"</c>.
/// </remarks>
#if WINDOWS
public partial class RibbonGroupsContainerScrollViewer : RibbonScrollViewer
#else
public partial class RibbonGroupsContainerScrollViewer : ScrollViewer
#endif
{
    /// <summary>Initializes a new instance.</summary>
    public RibbonGroupsContainerScrollViewer()
    {
    }

    /// <summary>
    /// Identifies the EnableHorizontalWheelScrolling attached property.
    /// </summary>
    public static readonly DependencyProperty EnableHorizontalWheelScrollingProperty =
        DependencyProperty.RegisterAttached(
            "EnableHorizontalWheelScrolling",
            typeof(bool),
            typeof(RibbonGroupsContainerScrollViewer),
            new PropertyMetadata(false, OnEnableHorizontalWheelScrollingChanged));

    /// <summary>Gets the value of the EnableHorizontalWheelScrolling attached property.</summary>
    public static bool GetEnableHorizontalWheelScrolling(DependencyObject obj) =>
        (bool)obj.GetValue(EnableHorizontalWheelScrollingProperty);

    /// <summary>Sets the value of the EnableHorizontalWheelScrolling attached property.</summary>
    public static void SetEnableHorizontalWheelScrolling(DependencyObject obj, bool value) =>
        obj.SetValue(EnableHorizontalWheelScrollingProperty, value);

    private static void OnEnableHorizontalWheelScrollingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer scrollViewer)
        {
            return;
        }

        if (e.NewValue is true)
        {
            scrollViewer.PointerWheelChanged += OnPointerWheelChanged;
        }
        else
        {
            scrollViewer.PointerWheelChanged -= OnPointerWheelChanged;
        }
    }

    private static void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var properties = e.GetCurrentPoint(scrollViewer).Properties;
        var delta = properties.MouseWheelDelta;

        if (delta == 0)
        {
            return;
        }

        // Check if any dropdown is open — if so, don't scroll
        if (IsAnyDropDownOpen(scrollViewer))
        {
            return;
        }

        var targetOffset = GetHorizontalWheelTarget(
            scrollViewer.HorizontalOffset,
            scrollViewer.ScrollableWidth,
            delta,
            48);
        if (targetOffset.Equals(scrollViewer.HorizontalOffset))
        {
            return;
        }

        if (scrollViewer.ChangeView(targetOffset, null, null))
        {
            e.Handled = true;
        }
    }

    internal static double GetHorizontalWheelTarget(
        double currentOffset,
        double scrollableWidth,
        int delta,
        double step)
    {
        var maximum = Math.Max(0, scrollableWidth);
        var target = currentOffset + (delta > 0 ? -step : step);
        return Math.Clamp(target, 0, maximum);
    }

    /// <summary>Handles WPF-compatible wheel input.</summary>
#if WINDOWS
    protected override void OnMouseWheel(PointerRoutedEventArgs e)
#else
    protected virtual void OnMouseWheel(PointerRoutedEventArgs e)
#endif
    {
#if WINDOWS
        if (e.Handled || IsAnyDropDownOpen(this))
        {
            return;
        }

        base.OnMouseWheel(e);
#else
        OnPointerWheelChanged(this, e);
#endif
    }

#if !WINDOWS
    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!e.Handled)
        {
            OnMouseWheel(e);
        }
    }
#endif

    private static bool IsAnyDropDownOpen(DependencyObject element)
    {
        // Check children for open dropdowns
        foreach (var child in Fluent.Extensions.UIElementExtensions.FindVisualChildren<FrameworkElement>(element))
        {
            if (child is IDropDownControl dropDown && dropDown.IsDropDownOpen)
            {
                return true;
            }
        }

        return false;
    }
}
