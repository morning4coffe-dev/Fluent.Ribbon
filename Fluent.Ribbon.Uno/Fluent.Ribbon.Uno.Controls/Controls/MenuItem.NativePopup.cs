#if WINDOWS
namespace Fluent;

using Windows.Foundation;

public partial class MenuItem
{
    private Panel? nativeSubmenuItemsOwner;
    private ScrollViewer? nativeSubmenuScroller;
    private Binding? nativeSubmenuTemplateInitialization;
    private IDisposable? nativeQuickAccessObservation;
    private bool nativeSubmenuPrepared;
    private bool nativeSubmenuOpenQueued;
    private bool nativeSubmenuOpened;
    private FrameworkElement? nativeSubmenuLayoutAnchor;

    private RibbonDropDownButton? NativeQuickAccessAnchor =>
        quickAccessBorrower is { } reference && reference.TryGetTarget(out var clone) ? clone : null;
    private FrameworkElement NativeSubmenuAnchor => NativeQuickAccessAnchor ?? (FrameworkElement)this;
    private bool NativeSubmenuRequested => NativeQuickAccessAnchor?.IsDropDownOpen ?? IsDropDownOpen;

    private void InitializeNativeSubmenu()
    {
        Loaded += (_, _) => QueueNativeSubmenuOpen();
        Loading += (_, _) => NativePopupTemplateHelper.Release(this, ref nativeSubmenuTemplateInitialization);
        RegisterPropertyChangedCallback(StyleProperty, (_, _) =>
        {
            NativePopupTemplateHelper.Release(this, ref nativeSubmenuTemplateInitialization);
            RetireNativeSubmenuTemplate();
        });
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => RetireNativeSubmenuTemplate());
        RegisterPropertyChangedCallback(ItemsPanelProperty, (_, _) =>
        {
            if (DropDownPopup is not null)
            {
                QueueNativeSubmenuOpen();
            }
        });
        IsEnabledChanged += (_, _) =>
        {
            if (NativeQuickAccessAnchor is { } clone && !IsEnabled)
            {
                clone.CloseDropDown();
            }
        };
    }

    private void RetireNativeSubmenuTemplate()
    {
        if (DropDownPopup is not { } popup)
        {
            return;
        }
        var root = submenuResizeHost;
        var completeClosing = closingSubmenuPopup is not null || pendingQuickAccessBorrower is not null;
        DropDownPopup = null;
        submenuResizeHost = null;
        submenuItemsHost = null;
        nativeSubmenuItemsOwner = null;
        nativeSubmenuScroller = null;
        nativeSubmenuPrepared = false;
        closingSubmenuPopup = null;
        submenuRequestVersion++;
        popup.Opened -= OnCompatibilitySubmenuOpened;
        popup.Closed -= OnCompatibilitySubmenuClosed;
        if (root is not null)
        {
            root.SizeChanged -= OnSubmenuSizeChanged;
            root.KeyDown -= OnSubmenuKeyDown;
            root.RemoveHandler(UIElement.PointerPressedEvent, new PointerEventHandler(OnNativeSubmenuPointerPressed));
        }
        ObserveSubmenuViewport(null);
        ObserveNativeSubmenuAnchor(null);
        popup.IsOpen = false;
        if (completeClosing)
        {
            CompleteNativeSubmenuClose(controlled: true);
        }
        else
        {
            NotifyNativeSubmenuRetired();
        }
        QueueNativeSubmenuOpen();
    }

    private void NotifyNativeSubmenuRetired()
    {
        if (!nativeSubmenuOpened)
        {
            return;
        }
        nativeSubmenuOpened = false;
        if (NativeQuickAccessAnchor is { } clone)
        {
            clone.NotifyNativeMenuClosed(this);
        }
        else if (submenuOpenedNotified)
        {
            submenuOpenedNotified = false;
            DropDownClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ApplyNativeSubmenuTemplate()
    {
        var popup = GetTemplateChild("PART_Popup") as Popup;
        if (ReferenceEquals(popup, DropDownPopup))
        {
            return;
        }
        RetireNativeSubmenuTemplate();
        DropDownPopup = popup;
        submenuResizeHost = GetTemplateChild("PART_PopupContentControl") as ResizeableContentControl;
        submenuItemsHost = GetTemplateChild("ItemsPresenter") as ItemsPresenter;
        nativeSubmenuItemsOwner = GetTemplateChild("PART_ItemsOwner") as Panel;
        nativeSubmenuScroller = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
        nativeSubmenuPrepared = false;
        ConnectSubmenuPopup();
        if (submenuResizeHost is not null)
        {
            submenuResizeHost.AddHandler(
                UIElement.PointerPressedEvent, new PointerEventHandler(OnNativeSubmenuPointerPressed), true);
        }
        QueueNativeSubmenuOpen();
    }

    private void PrepareNativeSubmenu()
    {
        if (nativeSubmenuPrepared)
        {
            return;
        }
        var owner = nativeSubmenuItemsOwner
                    ?? throw new InvalidOperationException("The native menu template has no canonical owner panel.");
        var presenter = submenuItemsHost
                        ?? throw new InvalidOperationException("The native menu template has no canonical ItemsPresenter.");
        var scroller = nativeSubmenuScroller
                       ?? throw new InvalidOperationException("The native menu template has no source-owned ScrollViewer.");
        if (ReferenceEquals(scroller.Content, presenter))
        {
            scroller.Content = null;
            owner.Children.Add(presenter);
        }
        if (!owner.Children.Contains(presenter) || scroller.Content is not null)
        {
            throw new InvalidOperationException("The native menu presenter is outside its source ControlTemplate.");
        }
        var wasValidating = validatingSubmenuGenerator;
        var visibility = owner.Visibility;
        validatingSubmenuGenerator = true;
        owner.Visibility = Visibility.Visible;
        try
        {
            itemsBinding.Refresh();
            presenter.InvalidateMeasure();
            presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            owner.Children.Remove(presenter);
            scroller.Content = presenter;
            nativeSubmenuPrepared = true;
        }
        finally
        {
            owner.Visibility = visibility;
            validatingSubmenuGenerator = wasValidating;
        }
    }

    private void ShowNativeOwnSubmenu()
    {
        if (!IsLoaded || !IsDropDownOpen)
        {
            return;
        }
        if (!CanOpenSubmenu)
        {
            IsDropDownOpen = false;
            return;
        }
        if (NativeQuickAccessAnchor is { } clone)
        {
            clone.CloseDropDown();
            if (NativeQuickAccessAnchor is not null)
            {
                return;
            }
        }
        foreach (var sibling in GetSiblingMenuItems())
        {
            if (!ReferenceEquals(sibling, this))
            {
                sibling.IsDropDownOpen = false;
            }
        }
        QueueNativeSubmenuOpen();
    }

    internal void ShowNativeQuickAccessPopup(RibbonDropDownButton clone)
    {
        if (!clone.IsLoaded || !clone.IsDropDownOpen)
        {
            return;
        }
        if (NativeQuickAccessAnchor is { } previous && !ReferenceEquals(previous, clone))
        {
            pendingQuickAccessBorrower = new(clone);
            previous.CloseDropDown();
            if (NativeQuickAccessAnchor is not null)
            {
                return;
            }
        }
        if (closingSubmenuPopup is not null)
        {
            pendingQuickAccessBorrower = new(clone);
            return;
        }
        if (NativeQuickAccessAnchor is null && IsDropDownOpen)
        {
            pendingQuickAccessBorrower = new(clone);
            IsDropDownOpen = false;
            if (closingSubmenuPopup is not null || NativeQuickAccessAnchor is not null)
            {
                return;
            }
            pendingQuickAccessBorrower = null;
        }
        if (!ReferenceEquals(NativeQuickAccessAnchor, clone))
        {
            quickAccessBorrower = new(clone);
            submenuRequestVersion++;
            nativeQuickAccessObservation = itemsBinding.AcquirePresentationLease();
        }
        SetQuickAccessSubmenuOwner(clone);
        QueueNativeSubmenuOpen();
    }

    internal void HideNativeQuickAccessPopup(RibbonDropDownButton clone)
    {
        if (pendingQuickAccessBorrower is { } pending && pending.TryGetTarget(out var next)
            && ReferenceEquals(next, clone) && !clone.IsDropDownOpen)
        {
            pendingQuickAccessBorrower = null;
        }
        if (ReferenceEquals(NativeQuickAccessAnchor, clone))
        {
            HideNativeCurrentSubmenu();
        }
    }

    private void HideNativeOwnSubmenu()
    {
        if (NativeQuickAccessAnchor is null)
        {
            HideNativeCurrentSubmenu();
        }
    }

    private void HideNativeCurrentSubmenu()
    {
        ObserveSubmenuViewport(null);
        if (DropDownPopup is { IsOpen: true } popup)
        {
            closingSubmenuPopup = popup;
            popup.IsOpen = false;
        }
        else if (closingSubmenuPopup is null)
        {
            CompleteNativeSubmenuClose(controlled: true);
        }
    }

    private void QueueNativeSubmenuOpen()
    {
        if (nativeSubmenuOpenQueued || !NativeSubmenuRequested || !NativeSubmenuAnchor.IsLoaded)
        {
            return;
        }
        nativeSubmenuOpenQueued = true;
        if (!DispatcherQueue.TryEnqueue(() =>
            {
                nativeSubmenuOpenQueued = false;
                var anchor = NativeSubmenuAnchor;
                if (!NativeSubmenuRequested || !anchor.IsLoaded || closingSubmenuPopup is not null)
                {
                    return;
                }
                if (!HasSubItems || !IsEnabled)
                {
                    if (NativeQuickAccessAnchor is { } clone)
                    {
                        clone.CloseDropDown();
                    }
                    else
                    {
                        IsDropDownOpen = false;
                    }
                    return;
                }
                var root = anchor.XamlRoot ?? throw new InvalidOperationException("The native submenu anchor has no XamlRoot.");
                NativePopupTemplateHelper.Ensure(
                    this, DefaultStyleKey, DefaultStyleResourceUri, root, ref nativeSubmenuTemplateInitialization);
                ApplyTemplate();
                if (DropDownPopup is null || submenuResizeHost is null)
                {
                    throw new InvalidOperationException("The native menu template has no source-owned Popup content.");
                }
                var version = submenuRequestVersion;
                PrepareNativeSubmenu();
                if (!NativeSubmenuRequested || !ReferenceEquals(anchor, NativeSubmenuAnchor)
                    || version != submenuRequestVersion)
                {
                    return;
                }
                SetQuickAccessSubmenuOwner(NativeQuickAccessAnchor ?? (DependencyObject)this);
                DropDownPopup.XamlRoot = root;
                ObserveSubmenuViewport(root);
                UpdateSubmenuDimensions();
                UpdateSubmenuPosition();
                if (!DropDownPopup.IsOpen)
                {
                    DropDownPopup.IsOpen = true;
                }
            }))
        {
            nativeSubmenuOpenQueued = false;
            throw new InvalidOperationException("The native submenu could not be dispatched.");
        }
    }

    private void OnNativeSubmenuOpened(object? sender)
    {
        if (!ReferenceEquals(sender, DropDownPopup) || !NativeSubmenuRequested || DropDownPopup?.IsOpen != true)
        {
            return;
        }
        nativeSubmenuOpened = true;
        ObserveNativeSubmenuAnchor(NativeSubmenuAnchor);
        UpdateSubmenuPosition();
        if (NativeQuickAccessAnchor is { } clone)
        {
            clone.NotifyNativeMenuOpened(this);
        }
        else
        {
            if (!submenuOpenedNotified)
            {
                submenuOpenedNotified = true;
                DropDownOpened?.Invoke(this, EventArgs.Empty);
            }
            if (focusFirstSubmenuItemWhenOpened)
            {
                FocusFirstEnabledSubmenuItem();
            }
        }
    }

    private void OnNativeSubmenuClosed(object? sender)
    {
        if (!ReferenceEquals(sender, DropDownPopup) || DropDownPopup?.IsOpen == true)
        {
            return;
        }
        CompleteNativeSubmenuClose(ReferenceEquals(closingSubmenuPopup, DropDownPopup));
    }

    private void CompleteNativeSubmenuClose(bool controlled)
    {
        var old = NativeQuickAccessAnchor;
        var next = pendingQuickAccessBorrower is { } pending && pending.TryGetTarget(out var requested)
            ? requested : null;
        pendingQuickAccessBorrower = null;
        quickAccessBorrower = null;
        closingSubmenuPopup = null;
        nativeSubmenuOpened = false;
        submenuRequestVersion++;
        ObserveSubmenuViewport(null);
        ObserveNativeSubmenuAnchor(null);
        SetQuickAccessSubmenuOwner(this);
        nativeQuickAccessObservation?.Dispose();
        nativeQuickAccessObservation = null;
        if (old is not null)
        {
            if (!ReferenceEquals(old, next))
            {
                old.IsDropDownOpen = false;
            }
            old.NotifyNativeMenuClosed(this);
        }
        else if (!controlled)
        {
            IsDropDownOpen = false;
        }
        if (!IsDropDownOpen)
        {
            NotifySubmenuClosed();
        }
        if (IsDropDownOpen && IsLoaded)
        {
            QueueNativeSubmenuOpen();
        }
        else if (next is { IsLoaded: true, IsDropDownOpen: true })
        {
            ShowNativeQuickAccessPopup(next);
        }
    }

    internal Popup? GetNativeQuickAccessPopup(RibbonDropDownButton clone) =>
        clone.IsDropDownOpen && ReferenceEquals(NativeQuickAccessAnchor, clone) && DropDownPopup?.IsOpen == true
            ? DropDownPopup : null;

    internal void RefreshNativeQuickAccessPopup(RibbonDropDownButton clone, bool resetHeight)
    {
        if (!ReferenceEquals(NativeQuickAccessAnchor, clone) || submenuResizeHost is null)
        {
            return;
        }
        PopupResizeHelper.Apply(
            submenuResizeHost, clone, clone.ResizeMode, Math.Max(200, clone.ActualWidth), 0,
            clone.MaxDropDownHeight, clone.DropDownHeight, resetHeight);
        if (DropDownPopup is not null)
        {
            DropDownPopup.IsLightDismissEnabled = clone.NativeDismissOnClickOutside;
        }
        UpdateSubmenuPosition();
    }

    private void OnNativeSubmenuPointerPressed(object sender, PointerRoutedEventArgs args) =>
        NativeQuickAccessAnchor?.HandleNativeMenuMouseDown(args.OriginalSource as DependencyObject);

    private void ObserveNativeSubmenuAnchor(FrameworkElement? anchor)
    {
        if (ReferenceEquals(nativeSubmenuLayoutAnchor, anchor))
        {
            return;
        }
        if (nativeSubmenuLayoutAnchor is not null)
        {
            nativeSubmenuLayoutAnchor.LayoutUpdated -= OnNativeSubmenuAnchorLayout;
        }
        nativeSubmenuLayoutAnchor = anchor;
        if (anchor is not null)
        {
            anchor.LayoutUpdated += OnNativeSubmenuAnchorLayout;
        }
    }

    private void OnNativeSubmenuAnchorLayout(object? sender, object args) => UpdateSubmenuPosition();
}
#endif
