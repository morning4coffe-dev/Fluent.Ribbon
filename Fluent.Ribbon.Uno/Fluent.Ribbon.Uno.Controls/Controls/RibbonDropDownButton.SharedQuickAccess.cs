namespace Fluent;

public partial class RibbonDropDownButton
{
    private RibbonDropDownButton? _sharedQuickAccessSource;
    private RibbonDropDownButton? _activePopupAnchor;
    private WeakReference<RibbonDropDownButton>? _pendingPopupAnchor;
    private long _popupPresentationVersion;

    private RibbonDropDownButton PopupAnchor => _activePopupAnchor ?? this;

    internal void BindSharedQuickAccessPresentation(RibbonDropDownButton source)
    {
        _sharedQuickAccessSource = source;
        var target = new WeakReference<RibbonDropDownButton>(this);
        QuickAccessBindingSession.For(source, this).Deactivated += () =>
        {
            if (target.TryGetTarget(out var clone))
            {
                clone.CloseDropDown();
            }
        };
    }

    private void ShowSharedPopup(RibbonDropDownButton anchor)
    {
#if WINDOWS
        ShowNativeSharedPopup(anchor);
#else
        if (!anchor.IsDropDownOpen || !anchor.IsLoaded)
        {
            return;
        }

        if (_flyoutIsClosing && _activePopupAnchor is not { IsLoaded: true }
            && _flyout?.IsOpen != true && _popupResizeHost?.IsLoaded != true)
        {
            _pendingPopupAnchor = new(anchor);
            CompleteSharedPopupClose();
            return;
        }

        if (_activePopupAnchor is { } previous && !ReferenceEquals(previous, anchor)
            && (_flyout?.IsOpen == true || _flyoutIsClosing))
        {
            _pendingPopupAnchor = new(anchor);
            previous.IsDropDownOpen = false;
            HideSharedPopup(previous);
            return;
        }

        if (_flyoutIsClosing)
        {
            _pendingPopupAnchor = new(anchor);
            return;
        }

        if (!ReferenceEquals(_activePopupAnchor, anchor))
        {
            CancelMouseDownClose();
            _activePopupAnchor = anchor;
            _popupPresentationVersion++;
            QuickAccessItemsOwnerOverride = ReferenceEquals(anchor, this) ? null : anchor;
        }

        _flyout ??= CreateDropDownFlyout(populateContent: false);
        if (!ReferenceEquals(anchor, this))
        {
            anchor._flyout = _flyout;
        }

        if (_flyout.IsOpen)
        {
            _flyoutIsOpen = true;
            RefreshPopupPresentation();
            return;
        }

        if (!TryPrepareQuickAccessPopup())
        {
            return;
        }

        RefreshPopupPresentation();
        var flyout = _flyout;
        var version = _popupPresentationVersion;
        FlyoutShowHelper.ShowDeferred(
            flyout,
            (FrameworkElement?)anchor._button ?? anchor,
            () => version == _popupPresentationVersion && ReferenceEquals(_activePopupAnchor, anchor)
                  && anchor.IsLoaded && anchor.IsDropDownOpen && !_flyoutIsClosing && !flyout.IsOpen);
#endif
    }

    private void HideSharedPopup(RibbonDropDownButton anchor)
    {
#if WINDOWS
        HideNativeSharedPopup(anchor);
#else
        if (_pendingPopupAnchor is { } pending && pending.TryGetTarget(out var pendingAnchor)
            && ReferenceEquals(pendingAnchor, anchor) && !anchor.IsDropDownOpen)
        {
            _pendingPopupAnchor = null;
        }

        if (_activePopupAnchor is not null && !ReferenceEquals(_activePopupAnchor, anchor))
        {
            return;
        }

        CancelMouseDownClose();
        var wasClosing = _flyoutIsClosing;
        var wasExplicit = _explicitFlyoutClose;
        var wasOpen = _flyout?.IsOpen == true;
        _explicitFlyoutClose = true;
        if (wasOpen || _popupResizeHost?.IsLoaded == true)
        {
            _flyoutIsClosing = true;
        }

        try
        {
            ReleaseQuickAccessPopup();
            if (wasOpen && !wasClosing)
            {
                _flyout?.Hide();
            }
            else if (!wasOpen && !wasClosing)
            {
                CompleteSharedPopupClose();
            }
        }
        finally
        {
            _explicitFlyoutClose = wasExplicit;
        }
#endif
    }

    private void CompleteSharedPopupClose()
    {
#if WINDOWS
        if (_nativePopup?.IsOpen == true)
#else
        if (_flyout?.IsOpen == true)
#endif
        {
            return;
        }

        var old = _activePopupAnchor;
        var wasOpen = _flyoutIsOpen || _flyoutIsClosing;
        var next = _pendingPopupAnchor is { } pending && pending.TryGetTarget(out var requested)
            ? requested : null;
        _pendingPopupAnchor = null;
        _activePopupAnchor = null;
        _popupPresentationVersion++;
        _flyoutIsOpen = false;
        _flyoutIsClosing = false;
        CancelMouseDownClose();
        ObservePopupViewport(null);
#if WINDOWS
        ObserveNativePopupAnchor(null);
#endif
        QuickAccessItemsOwnerOverride = null;
        ReleaseQuickAccessPopup();
        ObservePopupItems();

        if (old is not null && !ReferenceEquals(old, next))
        {
            old.IsDropDownOpen = false;
        }
        if (wasOpen && old is not null)
        {
            old.RaiseDropDownClosed();
        }
        if (next is { IsLoaded: true, IsDropDownOpen: true })
        {
            ShowSharedPopup(next);
        }
    }

    private bool RefreshSharedPopup(bool resetHeight = false)
    {
#if WINDOWS
        if (RefreshNativeMenuOptions(resetHeight))
        {
            return true;
        }
#endif
        if (_sharedQuickAccessSource is not { } source)
        {
            return false;
        }

        if (ReferenceEquals(source._activePopupAnchor, this))
        {
            source.RefreshPopupPresentation();
            source.ApplyPopupDimensions(resetHeight);
        }
        return true;
    }
}
