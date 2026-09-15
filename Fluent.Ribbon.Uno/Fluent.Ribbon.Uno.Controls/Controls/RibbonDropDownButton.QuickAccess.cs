namespace Fluent;

public partial class RibbonDropDownButton
{
    private object? _unborrowedPopupContent;
    private WeakReference<RibbonDropDownButton>? _quickAccessBorrower;
    private WeakReference<RibbonDropDownButton>? _pendingQuickAccessBorrower;
    private QuickAccessDropDownSession? _quickAccessContentSession;

    internal Func<bool>? PrepareQuickAccessContent { get; set; }
    internal Action? ReleaseQuickAccessContent { get; set; }
    internal DependencyObject? QuickAccessItemsOwnerOverride { get; private set; }

    internal void EnsureQuickAccessPopupHost()
    {
#if WINDOWS
        EnsureNativePopupTemplate(PopupAnchor);
#else
        _flyout ??= CreateDropDownFlyout();
#endif
    }

    internal void AttachQuickAccessPopupContent(FrameworkElement content)
    {
        EnsureQuickAccessPopupHost();
        if (_popupResizeHost is null)
        {
            throw new InvalidOperationException("The quick access drop-down has no content host.");
        }

        _unborrowedPopupContent ??= _popupResizeHost.Content;
        _popupResizeHost.Content = content;
    }

    internal void DetachQuickAccessPopupContent(FrameworkElement content)
    {
        if (_popupResizeHost is not null && ReferenceEquals(_popupResizeHost.Content, content))
        {
            _popupResizeHost.Content = null;
        }
    }

    internal void RestoreQuickAccessPopupHost()
    {
        if (_popupResizeHost is not null && _unborrowedPopupContent is not null)
        {
            _popupResizeHost.Content = _unborrowedPopupContent;
            _unborrowedPopupContent = null;
        }
    }

    internal void RetireQuickAccessPopup()
    {
        // The template's ItemsPresenter belongs to this control's native generator.
        // Unloading an anchor does not create or retire another generator/presenter.
        CancelMouseDownClose();
        ObservePopupViewport(null);
#if WINDOWS
        if (_nativePopup?.IsOpen != true)
#else
        if (_flyout?.IsOpen != true)
#endif
        {
            CompleteSharedPopupClose();
        }
    }

    internal void BindQuickAccessContent(
        FrameworkElement source,
        Func<FrameworkElement, RibbonDropDownButton, QuickAccessPopupContent?> getContent)
    {
#if WINDOWS
        if (source is MenuItem menu)
        {
            BindNativeMenuPopup(menu);
            return;
        }
#endif
        _quickAccessContentSession = new QuickAccessDropDownSession(source, this, getContent);
    }

    internal QuickAccessPopupContent? GetQuickAccessPopupContent(RibbonDropDownButton clone)
    {
        if (_quickAccessBorrower is { } reference && reference.TryGetTarget(out var current))
        {
            _pendingQuickAccessBorrower = new(clone);
            current.CloseDropDown();
            if (_quickAccessBorrower is not null)
            {
                return null;
            }
        }

        EnsureQuickAccessPopupHost();
        if (_popupResizeHost?.Content is not FrameworkElement content)
        {
            throw new InvalidOperationException("The source drop-down has no transferable content.");
        }

        var host = _popupResizeHost;
        _quickAccessBorrower = new(clone);
        PrepareQuickAccessContent = PrepareOwnQuickAccessContent;
        return new QuickAccessPopupContent(
            content,
            () =>
            {
                if (ReferenceEquals(host.Content, content))
                {
                    host.Content = null;
                    if (IsDropDownOpen)
                    {
                        CloseDropDown();
                    }
                }
            },
            () =>
            {
                host.Content = content;
                QuickAccessItemsOwnerOverride = null;
                _quickAccessBorrower = null;
                RefreshPopupPresentation();
                ResumeQuickAccessPresentation();
            },
            owner =>
            {
                QuickAccessItemsOwnerOverride = owner;
                RefreshPopupPresentation();
            });
    }

    private bool PrepareOwnQuickAccessContent()
    {
        if (_quickAccessBorrower is not { } reference || !reference.TryGetTarget(out var clone))
        {
            return true;
        }

        clone.CloseDropDown();
        return _quickAccessBorrower is null;
    }

    private void ResumeQuickAccessPresentation()
    {
        if (IsDropDownOpen && IsLoaded)
        {
            ShowDropDown();
        }
        else if (_pendingQuickAccessBorrower is { } pending && pending.TryGetTarget(out var clone)
                 && clone.IsDropDownOpen && clone.IsLoaded)
        {
            _pendingQuickAccessBorrower = null;
            clone.OpenDropDownForAutomation();
        }
    }
}
