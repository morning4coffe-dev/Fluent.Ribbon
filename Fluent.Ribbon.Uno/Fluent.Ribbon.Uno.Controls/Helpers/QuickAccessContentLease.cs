namespace Fluent;

// A content host travels as one subtree. WinUI can report descendants' Unloaded
// after their parent detached, so a null parent alone is not a reuse barrier.
internal sealed class QuickAccessContentLease(
    FrameworkElement content,
    Action detach,
    Action restore,
    Action borrow,
    Action<bool> completed,
    Action? failed = null)
{
    private readonly Dictionary<FrameworkElement, RoutedEventHandler> pending = new();
    private bool requestedBorrowed;
    private bool borrowed;
    private bool moving;
    private bool detaching;
    private bool completionQueued;
    private bool faulted;

    internal bool IsBorrowed => borrowed && !moving;
    internal bool IsMoving => moving;

    internal bool Acquire()
    {
        Request(borrowed: true);
        return IsBorrowed;
    }

    internal void Release() => Request(borrowed: false);

    private void Request(bool borrowed)
    {
        requestedBorrowed = borrowed;
        if (moving || this.borrowed == borrowed)
        {
            return;
        }

        moving = true;
        Observe(content);
        detaching = true;
        try
        {
            detach();
        }
        catch
        {
            Fail();
            throw;
        }
        finally
        {
            detaching = false;
            if (faulted && pending.Count == 0)
            {
                Complete();
            }
        }

        if (pending.Count == 0)
        {
            Complete();
        }
    }

    private void Observe(DependencyObject element)
    {
        if (element is FrameworkElement { IsLoaded: true } frameworkElement && !pending.ContainsKey(frameworkElement))
        {
            RoutedEventHandler handler = (_, _) => OnUnloaded(frameworkElement);
            pending.Add(frameworkElement, handler);
            frameworkElement.Unloaded += handler;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            Observe(VisualTreeHelper.GetChild(element, index));
        }
    }

    private void OnUnloaded(FrameworkElement element)
    {
        if (pending.Remove(element, out var handler))
        {
            element.Unloaded -= handler;
        }

        if (pending.Count != 0 || detaching || completionQueued)
        {
            return;
        }

        completionQueued = true;
        if (!content.DispatcherQueue.TryEnqueue(Complete))
        {
            completionQueued = false;
            throw new InvalidOperationException("The quick access content transfer could not complete on its UI dispatcher.");
        }
    }

    private void Complete()
    {
        if (!moving || pending.Count != 0)
        {
            return;
        }

        completionQueued = false;
        borrowed = requestedBorrowed;
        try
        {
            if (borrowed)
            {
                borrow();
            }
            else
            {
                restore();
            }
        }
        catch
        {
            var failedBorrow = borrowed;
            moving = false;
            Fail();
            if (failedBorrow)
            {
                Request(borrowed: false);
            }
            else
            {
                completed(false);
            }
            throw;
        }

        moving = false;
        completed(borrowed);
        if (borrowed != requestedBorrowed)
        {
            Request(requestedBorrowed);
        }
    }

    private void Fail()
    {
        requestedBorrowed = false;
        if (!faulted)
        {
            faulted = true;
            failed?.Invoke();
        }
    }
}
