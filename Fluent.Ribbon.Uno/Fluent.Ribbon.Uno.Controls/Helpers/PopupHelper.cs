namespace Fluent.Helpers;

/// <summary>
/// Provides helper methods for popup and dropdown management.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// Simplified since WinUI uses Flyout/Popup differently from WPF.
/// </remarks>
public static class PopupHelper
{
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
