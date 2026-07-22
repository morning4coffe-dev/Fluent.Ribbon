namespace Fluent.Helpers;

using Windows.Foundation;

/// <summary>Specifies the preferred axis for portable popup placement.</summary>
public enum PopupPrimaryAxis
{
    /// <summary>No preferred axis.</summary>
    None,

    /// <summary>Prefer horizontal adjustment.</summary>
    Horizontal,

    /// <summary>Prefer vertical adjustment.</summary>
    Vertical
}

/// <summary>Describes one portable popup-placement candidate.</summary>
public struct CustomPopupPlacement
{
    /// <summary>Gets or sets the popup origin.</summary>
    public Point Point { get; set; }

    /// <summary>Gets or sets the preferred adjustment axis.</summary>
    public PopupPrimaryAxis PrimaryAxis { get; set; }
}

/// <summary>Computes portable popup-placement candidates.</summary>
public delegate CustomPopupPlacement[] CustomPopupPlacementCallback(
    Size popupSize,
    Size targetSize,
    Point offset);

/// <summary>
/// Provides helper methods for popup and dropdown management.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// Simplified since WinUI uses Flyout/Popup differently from WPF.
/// </remarks>
public static class PopupHelper
{
    /// <summary>Gets the portable simple-placement callback.</summary>
    public static CustomPopupPlacementCallback SimplePlacementCallback =>
        GetSimplePlacement;

    /// <summary>Gets popup placements that avoid covering the target.</summary>
    public static CustomPopupPlacement[] GetSimplePlacement(
        Size popupSize,
        Size targetSize,
        Point offset)
    {
        return
        [
            new()
            {
                Point = new Point(0, 0),
                PrimaryAxis = PopupPrimaryAxis.None
            },
            new()
            {
                Point = new Point(-popupSize.Width, 0),
                PrimaryAxis = PopupPrimaryAxis.Horizontal
            },
            new()
            {
                Point = new Point(0, -popupSize.Height - targetSize.Height),
                PrimaryAxis = PopupPrimaryAxis.Vertical
            },
            new()
            {
                Point = new Point(-popupSize.Width, -popupSize.Height),
                PrimaryAxis = PopupPrimaryAxis.Vertical
            },
            new()
            {
                Point = new Point(targetSize.Width, -popupSize.Height),
                PrimaryAxis = PopupPrimaryAxis.Horizontal
            }
        ];
    }

    /// <summary>
    /// Closes any open flyout on the given element.
    /// </summary>
    /// <param name="element">The element whose flyout should be closed.</param>
    public static void CloseFlyout(FrameworkElement element)
    {
        var flyoutBase = Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.GetAttachedFlyout(element);
        flyoutBase?.Hide();
    }

    /// <summary>
    /// Shows the attached flyout on the given element.
    /// </summary>
    /// <param name="element">The element whose flyout should be shown.</param>
    public static void ShowFlyout(FrameworkElement element)
    {
        Microsoft.UI.Xaml.Controls.Primitives.FlyoutBase.ShowAttachedFlyout(element);
    }
}
