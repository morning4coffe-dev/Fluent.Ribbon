namespace Fluent.Helpers;

/// <summary>
/// Helper for positioning and managing dropdown popups.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WPF version uses Win32 window positioning; Uno version uses Popup/Flyout system.
/// </remarks>
public static class DropDownHelper
{
    /// <summary>Coerces a maximum drop-down height to the portable work area.</summary>
    public static object? CoerceMaxDropDownHeight(
        DependencyObject d,
        object? baseValue)
    {
        return baseValue is double value
            ? GetMaxDropDownHeight(d, value)
            : baseValue;
    }

    /// <summary>Gets the requested height or one third of the current XAML root.</summary>
    public static double GetMaxDropDownHeight(
        DependencyObject d,
        double baseValue)
    {
        if (double.IsNaN(baseValue) is false)
        {
            return baseValue;
        }

        return d is FrameworkElement { XamlRoot: { } xamlRoot }
            ? Math.Floor(xamlRoot.Size.Height / 3D)
            : double.NaN;
    }

    /// <summary>
    /// Opens a <see cref="Popup"/> below the specified placement target.
    /// </summary>
    /// <param name="popup">The popup to open.</param>
    /// <param name="placementTarget">The element to position relative to.</param>
    public static void OpenBelow(Popup popup, FrameworkElement placementTarget)
    {
        if (popup is null || placementTarget is null)
        {
            return;
        }

        var transform = placementTarget.TransformToVisual(null);
        var point = transform.TransformPoint(new Windows.Foundation.Point(0, placementTarget.ActualHeight));

        popup.HorizontalOffset = point.X;
        popup.VerticalOffset = point.Y;
        FlyoutShowHelper.OpenDeferred(popup);
    }

    /// <summary>
    /// Opens a <see cref="Popup"/> above the specified placement target.
    /// </summary>
    /// <param name="popup">The popup to open.</param>
    /// <param name="placementTarget">The element to position relative to.</param>
    /// <param name="popupHeight">The height of the popup content.</param>
    public static void OpenAbove(Popup popup, FrameworkElement placementTarget, double popupHeight)
    {
        if (popup is null || placementTarget is null)
        {
            return;
        }

        var transform = placementTarget.TransformToVisual(null);
        var point = transform.TransformPoint(new Windows.Foundation.Point(0, -popupHeight));

        popup.HorizontalOffset = point.X;
        popup.VerticalOffset = point.Y;
        FlyoutShowHelper.OpenDeferred(popup);
    }

    /// <summary>
    /// Closes the specified popup.
    /// </summary>
    public static void Close(Popup? popup)
    {
        if (popup is not null)
        {
            popup.IsOpen = false;
        }
    }

    /// <summary>
    /// Checks whether the popup needs to be flipped (opened upward)
    /// based on available screen space.
    /// </summary>
    /// <param name="placementTarget">The target element.</param>
    /// <param name="desiredPopupHeight">The desired height of the popup.</param>
    /// <returns><c>true</c> if the popup should be displayed above the target.</returns>
    public static bool ShouldOpenUpward(FrameworkElement placementTarget, double desiredPopupHeight)
    {
        if (placementTarget is null)
        {
            return false;
        }

        try
        {
            var transform = placementTarget.TransformToVisual(null);
            var point = transform.TransformPoint(new Windows.Foundation.Point(0, placementTarget.ActualHeight));

            var xamlRoot = placementTarget.XamlRoot;
            if (xamlRoot is null)
            {
                return false;
            }

            var windowHeight = xamlRoot.Size.Height;
            return point.Y + desiredPopupHeight > windowHeight;
        }
        catch
        {
            return false;
        }
    }
}
