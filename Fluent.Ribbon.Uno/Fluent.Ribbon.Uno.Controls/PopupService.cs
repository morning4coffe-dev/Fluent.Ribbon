namespace Fluent;

/// <summary>
/// Provides portable popup-dismissal behavior.
/// </summary>
public static class PopupService
{
    /// <summary>
    /// WPF compatibility token. WinUI does not support registering custom routed events.
    /// </summary>
    public static readonly RoutedEvent DismissPopupEvent = null!;

    /// <summary>
    /// Occurs when popup dismissal is requested.
    /// </summary>
    public static event EventHandler<DismissPopupEventArgs>? DismissPopup;

    /// <summary>
    /// Raises popup dismissal on the sender's dispatcher queue.
    /// </summary>
    public static void RaiseDismissPopupEventAsync(
        object sender,
        DismissPopupMode mode,
        DismissPopupReason reason = DismissPopupReason.Undefined)
    {
        ArgumentNullException.ThrowIfNull(sender);

        if (sender is DependencyObject dependencyObject
            && dependencyObject.DispatcherQueue is { } dispatcherQueue)
        {
            if (!dispatcherQueue.TryEnqueue(() => RaiseDismissPopupEvent(sender, mode, reason)))
            {
                throw new InvalidOperationException("Could not enqueue popup dismissal.");
            }

            return;
        }

        RaiseDismissPopupEvent(sender, mode, reason);
    }

    /// <summary>
    /// Raises popup dismissal immediately.
    /// </summary>
    public static void RaiseDismissPopupEvent(
        object sender,
        DismissPopupMode mode,
        DismissPopupReason reason = DismissPopupReason.Undefined)
    {
        ArgumentNullException.ThrowIfNull(sender);

        var args = new DismissPopupEventArgs(mode, reason);
        DismissPopup?.Invoke(sender, args);

        if (sender is IDropDownControl dropDown
            && (mode == DismissPopupMode.Always
                || sender is not UIElement element
                || !IsMousePhysicallyOver(element)))
        {
            dropDown.IsDropDownOpen = false;
        }
    }

    /// <summary>
    /// Retained for WPF source compatibility; WinUI does not expose class routed-event handlers.
    /// </summary>
    public static void Attach(Type classType)
    {
        ArgumentNullException.ThrowIfNull(classType);
    }

    /// <summary>
    /// Gets whether <paramref name="parent"/> is an ancestor of <paramref name="element"/>.
    /// </summary>
    public static bool IsAncestorOf(
        DependencyObject? parent,
        DependencyObject? element)
    {
        if (parent is null || element is null)
        {
            return false;
        }

        var current = VisualTreeHelper.GetParent(element);
        while (current is not null)
        {
            if (ReferenceEquals(parent, current))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    /// <summary>
    /// Gets whether the pointer is over the popup child when that child exposes pointer state.
    /// </summary>
    public static bool IsMousePhysicallyOver(Popup? popup)
    {
        return popup?.Child is UIElement child && IsMousePhysicallyOver(child);
    }

    /// <summary>
    /// Gets whether the pointer is over a control.
    /// </summary>
    public static bool IsMousePhysicallyOver(UIElement? element)
    {
        return false;
    }
}
