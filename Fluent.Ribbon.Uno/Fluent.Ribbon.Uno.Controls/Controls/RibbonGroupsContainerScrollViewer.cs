namespace Fluent;

/// <summary>
/// Attached behavior that adds horizontal mouse-wheel scrolling to a <see cref="ScrollViewer"/>
/// used to host a <see cref="RibbonGroupsContainer"/>, and prevents scrolling while a dropdown is open.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// The WPF version subclasses <c>ScrollViewer</c> and overrides <c>OnMouseWheel</c>; because
/// <see cref="ScrollViewer"/> is sealed under WinUI 3, this is implemented as an attached behavior
/// so it works identically on the Skia and WinUI heads.
/// Attach it in XAML with <c>fluent:RibbonGroupsContainerScrollViewer.EnableHorizontalWheelScrolling="True"</c>.
/// </remarks>
public static class RibbonGroupsContainerScrollViewer
{
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

        // Scroll horizontally
        if (delta > 0)
        {
            scrollViewer.ChangeView(Math.Max(0, scrollViewer.HorizontalOffset - 48), null, null);
        }
        else
        {
            scrollViewer.ChangeView(scrollViewer.HorizontalOffset + 48, null, null);
        }

        e.Handled = true;
    }

    private static bool IsAnyDropDownOpen(ScrollViewer scrollViewer)
    {
        // Check children for open dropdowns
        foreach (var child in Fluent.Extensions.UIElementExtensions.FindVisualChildren<FrameworkElement>(scrollViewer))
        {
            if (child is IDropDownControl dropDown && dropDown.IsDropDownOpen)
            {
                return true;
            }
        }

        return false;
    }
}
