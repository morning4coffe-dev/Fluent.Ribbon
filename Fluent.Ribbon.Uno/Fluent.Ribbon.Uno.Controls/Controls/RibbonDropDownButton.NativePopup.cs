#if WINDOWS
namespace Fluent;

using Windows.Foundation;

public partial class RibbonDropDownButton
{
    private Popup? _nativePopup;
    private Panel? _nativeItemsOwner;
    private Panel? _nativePopupItemsPanel;
    private Binding? _nativeTemplateInitialization;
    private bool _nativePresenterPrepared;
    private bool _nativeOpenQueued;
    private bool _nativePositioning;
    private MenuItem? _nativeMenuPopupSource;
    private FrameworkElement? _nativeLayoutAnchor;

    internal Popup? OpenNativePopup
    {
        get
        {
            if (_nativeMenuPopupSource is { } menu)
            {
                return menu.GetNativeQuickAccessPopup(this);
            }
            var source = _sharedQuickAccessSource ?? this;
            return IsDropDownOpen && ReferenceEquals(source._activePopupAnchor, this)
                   && source._nativePopup?.IsOpen == true
                ? source._nativePopup : null;
        }
    }

    private void InitializeNativePopup()
    {
        Loading += (_, _) => NativePopupTemplateHelper.Release(this, ref _nativeTemplateInitialization);
        RegisterPropertyChangedCallback(StyleProperty, (_, _) =>
        {
            NativePopupTemplateHelper.Release(this, ref _nativeTemplateInitialization);
            InvalidateNativePopupTemplate();
        });
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => InvalidateNativePopupTemplate());
        RegisterPropertyChangedCallback(ItemsPanelProperty, (_, _) =>
        {
            if (_nativePopup is not null)
            {
                QueueNativePopupOpen();
            }
        });
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => RefreshNativeMenuOptions());
    }

    private void InvalidateNativePopupTemplate()
    {
        if (!UsesDefaultDropDownButtonTemplateBehavior || _nativePopup is null)
        {
            return;
        }

        var popup = _nativePopup;
        var resize = _popupResizeHost;
        var gallery = _popupGalleryHost;
        var completeClosing = _flyoutIsClosing || _pendingPopupAnchor is not null;
        _nativePopup = null;
        _nativeItemsOwner = null;
        _nativePopupItemsPanel = null;
        _dropDownItemsHost = null;
        _popupMenuHeader = null;
        _popupGalleryHost = null;
        _popupHeaderSeparator = null;
        _popupGallerySeparator = null;
        _nativePresenterPrepared = false;
        _popupPresentationVersion++;
        popup.Opened -= OnNativePopupOpened;
        popup.Closed -= OnNativePopupClosed;
        if (resize is not null)
        {
            resize.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnDropDownPopupPointerPressed));
            resize.KeyDown -= OnDropDownPopupKeyDown;
            resize.SizeChanged -= OnNativePopupSizeChanged;
        }
        var wasOpen = _flyoutIsOpen || popup.IsOpen;
        if (!completeClosing)
        {
            _flyoutIsOpen = false;
            _flyoutIsClosing = false;
        }
        CancelMouseDownClose();
        ObservePopupViewport(null);
        ObserveNativePopupAnchor(null);
        popup.IsOpen = false;
        ReleaseQuickAccessPopup();
        if (gallery is not null)
        {
            gallery.Content = null;
        }
        _popupResizeHost = null;
        _unborrowedPopupContent = null;
        if (completeClosing)
        {
            CompleteSharedPopupClose();
        }
        else if (wasOpen)
        {
            _activePopupAnchor?.RaiseDropDownClosed();
        }
        QueueNativePopupOpen();
    }

    private void EnsureNativePopupTemplate(RibbonDropDownButton anchor)
    {
        var root = anchor.XamlRoot ?? throw new InvalidOperationException("The active drop-down anchor has no XamlRoot.");
        NativePopupTemplateHelper.Ensure(
            this, DefaultStyleKey, DefaultStyleResourceUri, root, ref _nativeTemplateInitialization);
        ApplyTemplate();
        var popup = GetTemplateChild("PART_Popup") as Popup
                    ?? throw new InvalidOperationException("The native drop-down template has no source-owned Popup.");
        if (!ReferenceEquals(_nativePopup, popup))
        {
            if (_nativePopup is not null)
            {
                InvalidateNativePopupTemplate();
            }
            _nativePopup = popup;
            _popupResizeHost = GetTemplateChild("PART_PopupContentControl") as ResizeableContentControl
                               ?? throw new InvalidOperationException("The native drop-down template has no resize content root.");
            _nativeItemsOwner = GetTemplateChild("PART_ItemsOwner") as Panel
                                ?? throw new InvalidOperationException("The native drop-down template has no canonical items owner.");
            _nativePopupItemsPanel = GetTemplateChild("PART_PopupItemsPanel") as Panel
                                    ?? throw new InvalidOperationException("The native drop-down template has no popup items panel.");
            _dropDownItemsHost = GetTemplateChild("ItemsPresenter") as ItemsPresenter
                                ?? throw new InvalidOperationException("The native drop-down template has no canonical ItemsPresenter.");
            _popupMenuHeader = GetTemplateChild("PART_MenuHeader") as TextBlock;
            _popupGalleryHost = GetTemplateChild("PART_Gallery") as ContentControl;
            _popupHeaderSeparator = GetTemplateChild("PART_HeaderSeparator") as Rectangle;
            _popupGallerySeparator = GetTemplateChild("PART_GallerySeparator") as Rectangle;
            popup.FlowDirection = FlowDirection.LeftToRight;
            popup.Opened += OnNativePopupOpened;
            popup.Closed += OnNativePopupClosed;
            _popupResizeHost.AddHandler(
                UIElement.PointerPressedEvent, new PointerEventHandler(OnDropDownPopupPointerPressed), true);
            _popupResizeHost.KeyDown += OnDropDownPopupKeyDown;
            _popupResizeHost.SizeChanged += OnNativePopupSizeChanged;
        }
        popup.XamlRoot = root;
        if (!_nativePresenterPrepared)
        {
            var owner = _nativeItemsOwner!;
            var items = _dropDownItemsHost!;
            var panel = _nativePopupItemsPanel!;
            if (panel.Children.Contains(items))
            {
                panel.Children.Remove(items);
                owner.Children.Add(items);
            }
            if (!owner.Children.Contains(items))
            {
                throw new InvalidOperationException("The native ItemsPresenter is outside its source template.");
            }
            var visibility = owner.Visibility;
            owner.Visibility = Visibility.Visible;
            try
            {
                items.InvalidateMeasure();
                items.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                owner.Children.Remove(items);
                panel.Children.Add(items);
                _nativePresenterPrepared = true;
            }
            finally
            {
                owner.Visibility = visibility;
            }
        }
    }

    private void ShowNativeSharedPopup(RibbonDropDownButton anchor)
    {
        if (!anchor.IsDropDownOpen || !anchor.IsLoaded)
        {
            return;
        }
        if (_activePopupAnchor is { } previous && !ReferenceEquals(previous, anchor)
            && (_nativePopup?.IsOpen == true || _flyoutIsClosing))
        {
            _pendingPopupAnchor = new(anchor);
            previous.IsDropDownOpen = false;
            HideNativeSharedPopup(previous);
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
        QueueNativePopupOpen();
    }

    private void QueueNativePopupOpen()
    {
        if (_nativeOpenQueued || _activePopupAnchor is not { IsLoaded: true, IsDropDownOpen: true })
        {
            return;
        }
        _nativeOpenQueued = true;
        if (!DispatcherQueue.TryEnqueue(() =>
            {
                _nativeOpenQueued = false;
                if (_activePopupAnchor is not { IsLoaded: true, IsDropDownOpen: true } anchor || _flyoutIsClosing)
                {
                    return;
                }
                var version = _popupPresentationVersion;
                EnsureNativePopupTemplate(anchor);
                if (version != _popupPresentationVersion || !ReferenceEquals(_activePopupAnchor, anchor)
                    || !anchor.IsDropDownOpen || !anchor.IsLoaded || !TryPrepareQuickAccessPopup())
                {
                    return;
                }
                RefreshPopupPresentation();
                UpdateNativePopupPosition();
                if (_nativePopup is { IsOpen: false } popup && anchor.IsDropDownOpen
                    && ReferenceEquals(_activePopupAnchor, anchor) && version == _popupPresentationVersion)
                {
                    popup.IsOpen = true;
                }
            }))
        {
            _nativeOpenQueued = false;
            throw new InvalidOperationException("The native drop-down could not be dispatched.");
        }
    }

    private void HideNativeSharedPopup(RibbonDropDownButton anchor)
    {
        if (_pendingPopupAnchor is { } pending && pending.TryGetTarget(out var next)
            && ReferenceEquals(next, anchor) && !anchor.IsDropDownOpen)
        {
            _pendingPopupAnchor = null;
        }
        if (_activePopupAnchor is not null && !ReferenceEquals(_activePopupAnchor, anchor))
        {
            return;
        }
        CancelMouseDownClose();
        if (_nativePopup is { IsOpen: true } popup)
        {
            _flyoutIsClosing = true;
            ReleaseQuickAccessPopup();
            popup.IsOpen = false;
        }
        else if (!_flyoutIsClosing)
        {
            ReleaseQuickAccessPopup();
            CompleteSharedPopupClose();
        }
    }

    private void OnNativePopupOpened(object? sender, object args)
    {
        if (!ReferenceEquals(sender, _nativePopup))
        {
            return;
        }
        _flyoutIsOpen = true;
        if (_activePopupAnchor is not { IsLoaded: true, IsDropDownOpen: true } anchor)
        {
            HideNativeSharedPopup(PopupAnchor);
            return;
        }
        ObservePopupViewport(anchor.XamlRoot);
        ObserveNativePopupAnchor(anchor);
        ApplyPopupDimensions();
        UpdateNativePopupPosition();
        anchor.RaiseDropDownOpened();
    }

    private void OnNativePopupClosed(object? sender, object args)
    {
        if (!ReferenceEquals(sender, _nativePopup) || _nativePopup?.IsOpen == true
            || (!_flyoutIsOpen && !_flyoutIsClosing))
        {
            return;
        }
        // Outside dismissal is a real closure. Only explicit reanchor/template
        // retirement records a replacement request; ordinary Closed never reopens.
        CompleteSharedPopupClose();
    }

    private void OnNativePopupSizeChanged(object sender, SizeChangedEventArgs args) => UpdateNativePopupPosition();

    private void ObserveNativePopupAnchor(FrameworkElement? anchor)
    {
        if (ReferenceEquals(anchor, _nativeLayoutAnchor))
        {
            return;
        }
        if (_nativeLayoutAnchor is not null)
        {
            _nativeLayoutAnchor.LayoutUpdated -= OnNativeAnchorLayout;
        }
        _nativeLayoutAnchor = anchor;
        if (anchor is not null)
        {
            anchor.LayoutUpdated += OnNativeAnchorLayout;
        }
    }

    private void OnNativeAnchorLayout(object? sender, object args) => UpdateNativePopupPosition();

    private void UpdateNativePopupPosition()
    {
        if (_nativePositioning || _nativePopup is not { } popup || _popupResizeHost is not { } content
            || _activePopupAnchor is not { IsLoaded: true, IsDropDownOpen: true } anchor)
        {
            return;
        }
        _nativePositioning = true;
        try
        {
            NativePopupLayoutHelper.Position(popup, content, anchor, submenu: false);
        }
        finally
        {
            _nativePositioning = false;
        }
    }

    internal void BindNativeMenuPopup(MenuItem source)
    {
        _nativeMenuPopupSource = source;
        var weak = new WeakReference<RibbonDropDownButton>(this);
        QuickAccessBindingSession.For(source, this).Deactivated += () =>
        {
            if (weak.TryGetTarget(out var clone))
            {
                clone.CloseDropDown();
            }
        };
    }

    private bool RefreshNativeMenuOptions(bool resetHeight = false)
    {
        if (_nativeMenuPopupSource is not { } source)
        {
            return false;
        }
        source.RefreshNativeQuickAccessPopup(this, resetHeight);
        return true;
    }

    internal void NotifyNativeMenuOpened(MenuItem source)
    {
        if (ReferenceEquals(_nativeMenuPopupSource, source) && IsDropDownOpen)
        {
            _flyoutIsOpen = true;
            _flyoutIsClosing = false;
            RaiseDropDownOpened();
        }
    }

    internal void NotifyNativeMenuClosed(MenuItem source)
    {
        if (ReferenceEquals(_nativeMenuPopupSource, source))
        {
            var wasOpen = _flyoutIsOpen;
            _flyoutIsOpen = false;
            _flyoutIsClosing = false;
            CancelMouseDownClose();
            if (wasOpen)
            {
                RaiseDropDownClosed();
            }
        }
    }

    internal bool NativeDismissOnClickOutside => DismissOnClickOutsideCore;
    internal void HandleNativeMenuMouseDown(DependencyObject? originalSource) => HandleDropDownPopupMouseDown(originalSource);
    internal bool HandleNativeMenuKey(Windows.System.VirtualKey key) => HandleDropDownPopupKey(key);
}
#endif
