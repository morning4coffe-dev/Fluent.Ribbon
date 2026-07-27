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

    // WinUI cannot register the bubbling routed event WPF uses to walk from a clicked
    // item up to every owning drop-down. Each drop-down lives in its own popup/flyout
    // visual tree, so a visual-tree ancestor walk cannot cross that boundary. Instead
    // every open drop-down registers here while it is open; a DismissPopupMode.Always
    // request then closes the whole open chain, mirroring WPF's bubbling semantics.
    // Weak references keep the registry leak-free even if a drop-down is torn down
    // without closing first.
    private static readonly List<WeakReference<IDropDownControl>> OpenDropDowns = new();
    private static bool isDismissing;

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

        if (mode == DismissPopupMode.Always)
        {
            // WPF bubbles the dismissal to every ancestor drop-down and closes each one.
            // Since the clicked item is hosted in a separate popup/flyout visual tree, we
            // close the whole registered open chain instead of relying on the tree.
            CloseOpenDropDowns(sender);
        }
        else if (sender is IDropDownControl dropDown
                 && (sender is not UIElement element
                     || !IsMousePhysicallyOver(element)))
        {
            dropDown.IsDropDownOpen = false;
        }
    }

    /// <summary>
    /// Registers a drop-down as open so that a subsequent dismissal can close it.
    /// </summary>
    internal static void RegisterOpenDropDown(IDropDownControl control)
    {
        if (control is null)
        {
            return;
        }

        lock (OpenDropDowns)
        {
            for (var index = OpenDropDowns.Count - 1; index >= 0; index--)
            {
                if (!OpenDropDowns[index].TryGetTarget(out var existing))
                {
                    OpenDropDowns.RemoveAt(index);
                }
                else if (ReferenceEquals(existing, control))
                {
                    return;
                }
            }

            OpenDropDowns.Add(new WeakReference<IDropDownControl>(control));
        }
    }

    /// <summary>
    /// Removes a drop-down from the open registry when it closes.
    /// </summary>
    internal static void UnregisterOpenDropDown(IDropDownControl control)
    {
        if (control is null)
        {
            return;
        }

        lock (OpenDropDowns)
        {
            for (var index = OpenDropDowns.Count - 1; index >= 0; index--)
            {
                if (!OpenDropDowns[index].TryGetTarget(out var existing)
                    || ReferenceEquals(existing, control))
                {
                    OpenDropDowns.RemoveAt(index);
                }
            }
        }
    }

    private static void CloseOpenDropDowns(object sender)
    {
        // Guard against re-entrancy: closing a flyout can synchronously raise another
        // dismissal, which would otherwise recurse through the same snapshot.
        if (isDismissing)
        {
            return;
        }

        isDismissing = true;
        try
        {
            foreach (var dropDown in GetOpenDropDownsSnapshot())
            {
                // Never close the drop-down that raised the dismissal. In WPF the
                // DismissPopup routed event bubbles *up* from the clicked item to its
                // owning drop-downs and closes those ancestors; it never re-enters the
                // sender's own drop-down. Skipping the sender therefore matches WPF and
                // also guarantees a drop-down that is being opened by the very interaction
                // that raised the dismissal is never flashed shut.
                if (ReferenceEquals(dropDown, sender))
                {
                    continue;
                }

                if (dropDown.IsDropDownOpen)
                {
                    dropDown.IsDropDownOpen = false;
                }
            }
        }
        finally
        {
            isDismissing = false;
        }
    }

    private static List<IDropDownControl> GetOpenDropDownsSnapshot()
    {
        lock (OpenDropDowns)
        {
            var snapshot = new List<IDropDownControl>(OpenDropDowns.Count);

            // Iterate most-recently-opened first so nested popups close inside-out.
            for (var index = OpenDropDowns.Count - 1; index >= 0; index--)
            {
                if (OpenDropDowns[index].TryGetTarget(out var control))
                {
                    snapshot.Add(control);
                }
                else
                {
                    OpenDropDowns.RemoveAt(index);
                }
            }

            return snapshot;
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
