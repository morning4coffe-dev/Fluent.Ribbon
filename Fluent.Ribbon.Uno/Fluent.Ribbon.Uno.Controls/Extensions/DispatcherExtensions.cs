namespace Fluent.Extensions;

/// <summary>
/// Extension methods for dispatcher/UI thread operations.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WPF uses Dispatcher.BeginInvoke; Uno/WinUI uses DispatcherQueue.TryEnqueue.
/// </remarks>
public static class DispatcherExtensions
{
    /// <summary>
    /// Runs the specified action on the UI thread.
    /// If already on the UI thread, runs immediately.
    /// </summary>
    /// <param name="element">The element whose dispatcher to use.</param>
    /// <param name="action">The action to run.</param>
    public static void RunOnUIThread(this DependencyObject element, Action action)
    {
        var dispatcherQueue = element is UIElement uiElement
            ? uiElement.DispatcherQueue
            : Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        if (dispatcherQueue is null)
        {
            action();
            return;
        }

        if (dispatcherQueue.HasThreadAccess)
        {
            action();
        }
        else
        {
            dispatcherQueue.TryEnqueue(() => action());
        }
    }

    /// <summary>
    /// Enqueues an action to run on the UI thread asynchronously (low priority).
    /// </summary>
    /// <param name="element">The element whose dispatcher to use.</param>
    /// <param name="action">The action to enqueue.</param>
    public static void BeginInvokeOnUIThread(this DependencyObject element, Action action)
    {
        var dispatcherQueue = element is UIElement uiElement
            ? uiElement.DispatcherQueue
            : Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        dispatcherQueue?.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => action());
    }
}
