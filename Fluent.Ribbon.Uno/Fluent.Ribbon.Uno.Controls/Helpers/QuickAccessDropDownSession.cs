namespace Fluent;

internal sealed record QuickAccessPopupContent(
    FrameworkElement Element,
    Action Detach,
    Action Restore,
    Action<DependencyObject> SetOwner,
    IDisposable? Observation = null);

internal sealed class QuickAccessDropDownSession
{
    private readonly WeakReference<FrameworkElement> source;
    private readonly WeakReference<RibbonDropDownButton> clone;
    private readonly Func<FrameworkElement, RibbonDropDownButton, QuickAccessPopupContent?> getContent;
    private QuickAccessPopupContent? content;
    private QuickAccessContentLease? lease;

    internal QuickAccessDropDownSession(
        FrameworkElement source,
        RibbonDropDownButton clone,
        Func<FrameworkElement, RibbonDropDownButton, QuickAccessPopupContent?> getContent)
    {
        this.source = new(source);
        this.clone = new(clone);
        this.getContent = getContent;
        clone.PrepareQuickAccessContent = Prepare;
        clone.ReleaseQuickAccessContent = Release;
        QuickAccessBindingSession.For(source, clone).Deactivated += Close;
    }

    internal bool IsRestored => lease is null;

    internal bool Prepare()
    {
        if (!source.TryGetTarget(out var owner) || !clone.TryGetTarget(out var copy))
        {
            return false;
        }

        copy.EnsureQuickAccessPopupHost();
        if (lease is null)
        {
            content = getContent(owner, copy);
            if (content is null)
            {
                return false;
            }

            var borrowedContent = content;
            lease = new QuickAccessContentLease(
                borrowedContent.Element,
                () =>
                {
                    copy.DetachQuickAccessPopupContent(borrowedContent.Element);
                    borrowedContent.Detach();
                },
                () =>
                {
                    borrowedContent.Restore();
                    copy.RestoreQuickAccessPopupHost();
                },
                () =>
                {
                    borrowedContent.SetOwner(copy);
                    copy.AttachQuickAccessPopupContent(borrowedContent.Element);
                },
                OnTransferred,
                () =>
                {
                    borrowedContent.Observation?.Dispose();
                    copy.CloseDropDown();
                });
        }

        return lease.Acquire();
    }

    internal void Release() => lease?.Release();

    private void Close()
    {
        Release();
        if (clone.TryGetTarget(out var copy))
        {
            copy.CloseDropDown();
            copy.RetireQuickAccessPopup();
        }
    }

    private void OnTransferred(bool borrowed)
    {
        if (!borrowed)
        {
            content?.Observation?.Dispose();
            content = null;
            lease = null;
        }

        if (borrowed && clone.TryGetTarget(out var copy) && copy.IsDropDownOpen && copy.IsLoaded)
        {
            copy.OpenDropDownForAutomation();
        }
    }
}
