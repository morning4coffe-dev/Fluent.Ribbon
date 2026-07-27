namespace Fluent;

using Microsoft.UI.Dispatching;

/// <summary>
/// Helpers for showing light-dismiss flyouts and popups without racing the pointer input
/// that triggered them.
/// </summary>
/// <remarks>
/// Showing a light-dismiss <see cref="FlyoutBase"/> or opening a light-dismiss
/// <see cref="Popup"/> synchronously from a click / pointer handler can let the same pointer
/// release reach the freshly created light-dismiss overlay and immediately dismiss it, so the
/// flyout never appears (or flashes) on a real mouse click. Deferring the show/open until the
/// current input has finished processing avoids that race while behaving identically for
/// programmatic and automation callers. The dispatcher fallback keeps the synchronous
/// behaviour on targets where a dispatcher queue is not available.
/// </remarks>
internal static class FlyoutShowHelper
{
    /// <summary>
    /// Shows <paramref name="flyout"/> anchored to <paramref name="target"/> after the current
    /// input event has finished processing.
    /// </summary>
    internal static void ShowDeferred(FlyoutBase? flyout, FrameworkElement? target)
    {
        if (flyout is null || target is null)
        {
            return;
        }

        Defer(target, () => ShowIfStillLive(target, () => flyout.ShowAt(target)));
    }

    /// <summary>
    /// Shows <paramref name="flyout"/> anchored to <paramref name="target"/> at the supplied
    /// <paramref name="position"/> after the current input event has finished processing.
    /// </summary>
    internal static void ShowDeferred(MenuFlyout? flyout, FrameworkElement? target, Windows.Foundation.Point position)
    {
        if (flyout is null || target is null)
        {
            return;
        }

        Defer(target, () => ShowIfStillLive(target, () => flyout.ShowAt(target, position)));
    }

    /// <summary>
    /// Opens <paramref name="popup"/> after the current input event has finished processing.
    /// </summary>
    internal static void OpenDeferred(Popup? popup)
    {
        if (popup is null)
        {
            return;
        }

        Defer(popup, () => popup.IsOpen = true);
    }

    private static void Defer(DependencyObject context, Action show)
    {
        DispatcherQueue? queue = context.DispatcherQueue;
        if (queue is null || !queue.TryEnqueue(() => show()))
        {
            show();
        }
    }

    /// <summary>
    /// Because the show is deferred by one dispatcher turn, the anchor can be torn down in the
    /// meantime — switching ribbon tabs unloads it, and an automation or keyboard request can
    /// arrive for a control that is no longer realized. Calling <c>ShowAt</c> against a detached
    /// anchor raises a stowed exception inside the framework, which terminates the process with
    /// 0xC000027B rather than surfacing as a catchable error, so verify the anchor is still live
    /// and swallow any late teardown race.
    /// </summary>
    private static void ShowIfStillLive(FrameworkElement target, Action show)
    {
        if (target.XamlRoot is null || !target.IsLoaded)
        {
            return;
        }

        try
        {
            show();
        }
        catch (Exception)
        {
            // The anchor was torn down between the liveness check and the show.
        }
    }
}
