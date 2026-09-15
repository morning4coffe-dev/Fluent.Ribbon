namespace Fluent;

using System.Collections;
using Windows.System;

public partial class RibbonDropDownButton
{
    private ResizeableContentControl? _popupResizeHost;
#if WINDOWS
    private ItemsPresenter? _dropDownItemsHost;
#else
    private ItemsControl? _dropDownItemsHost;
#endif
    private TextBlock? _popupMenuHeader;
    private ContentControl? _popupGalleryHost;
    private Rectangle? _popupHeaderSeparator;
    private Rectangle? _popupGallerySeparator;
    private INotifyCollectionChanged? _observedPopupItems;
    private DispatcherTimer? _mouseDownCloseTimer;
    private XamlRoot? _popupXamlRoot;
    private bool _flyoutIsOpen;
    private bool _flyoutIsClosing;
#if !WINDOWS
    private bool _explicitFlyoutClose;
#endif
    private bool _preparingQuickAccessContent;
    private bool _releasingQuickAccessContent;
    private bool _quickAccessPreparationActive;
    private bool _releaseQuickAccessAfterPreparation;
    private bool _pendingPopupHeightReset;
#if WINDOWS
    private bool _measuringCanonicalItems;
#endif

    private bool DismissOnClickOutsideCore =>
        this is not DropDownButton button || button.DismissOnClickOutside;

    // A closing native presenter can outlive its anchor. Apply pending state on the
    // next opening instead of mutating its composition subtree during teardown.
    private bool CanRefreshPopupPresentation =>
        !_flyoutIsClosing && _activePopupAnchor is { IsLoaded: true, IsDropDownOpen: true };

    private void InitializePopupOptions()
    {
#if WINDOWS
        InitializeNativePopup();
#endif
        foreach (var property in new[]
                 {
                     ResizeModeProperty, MaxDropDownHeightProperty, FlowDirectionProperty,
                     MenuHeaderProperty, GalleryProperty, ItemTemplateProperty,
                     ItemTemplateSelectorProperty, ItemContainerStyleProperty, ItemsPanelProperty,
                 })
        {
            RegisterPropertyChangedCallback(property, (_, _) => RefreshPopupPresentation());
        }

        RegisterPropertyChangedCallback(DropDownHeightProperty, (_, _) => ApplyPopupDimensions(resetHeight: true));
        RegisterPropertyChangedCallback(ClosePopupOnMouseDownProperty, (_, _) =>
        {
            if (!ClosePopupOnMouseDown)
            {
                CancelMouseDownClose();
            }
        });
        SizeChanged += (_, _) => ApplyPopupDimensions();
        ActualThemeChanged += (_, _) => ApplyPopupDimensions();
        IsEnabledChanged += (_, _) => ApplyPopupDimensions();
        Loaded += (_, _) =>
        {
            ObservePopupItems();
            if (IsDropDownOpen)
            {
                ShowDropDown();
            }
        };
        Unloaded += (_, _) =>
        {
            CloseDropDown();
            ObservePopupItems();
            if (ReferenceEquals(PopupAnchor, this))
            {
                ObservePopupViewport(null);
            }
#if WINDOWS
            QueueNativePopupOpen();
#endif
        };
    }

#if !WINDOWS
    private Flyout CreateDropDownFlyout(bool populateContent = true)
    {
        _dropDownItemsHost = new ItemsControl();

        _popupMenuHeader = new TextBlock
        {
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            FontSize = 12,
            Margin = new Thickness(12, 8, 12, 4),
        };
        _popupGalleryHost = new ContentControl
        {
            IsTabStop = false,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        _popupHeaderSeparator = new Rectangle { Height = 1, Margin = new Thickness(0, 2, 0, 4) };
        _popupGallerySeparator = new Rectangle { Height = 1, Margin = new Thickness(0, 4, 0, 4) };
        var panel = new StackPanel();
        panel.Children.Add(_popupMenuHeader);
        panel.Children.Add(_popupHeaderSeparator);
        panel.Children.Add(_popupGalleryHost);
        panel.Children.Add(_popupGallerySeparator);
        panel.Children.Add(_dropDownItemsHost);
        _popupResizeHost = new ResizeableContentControl
        {
            Name = "PART_PopupContentControl",
            Content = new ScrollViewer
            {
                Content = panel,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollMode = ScrollMode.Disabled,
                IsTabStop = false,
            },
        };
        _popupResizeHost.AddHandler(
            UIElement.PointerPressedEvent,
            new PointerEventHandler(OnDropDownPopupPointerPressed),
            handledEventsToo: true);
        _popupResizeHost.KeyDown += OnDropDownPopupKeyDown;
        var style = new Style(typeof(FlyoutPresenter));
        style.Setters.Add(new Setter(MaxWidthProperty, double.PositiveInfinity));
        style.Setters.Add(new Setter(MaxHeightProperty, double.PositiveInfinity));
        var flyout = new Flyout
        {
            Content = _popupResizeHost,
            Placement = FlyoutPlacementMode.Bottom,
            FlyoutPresenterStyle = style,
        };
        flyout.Opened += OnDropDownFlyoutOpened;
        flyout.Closing += OnDropDownFlyoutClosing;
        flyout.Closed += OnDropDownFlyoutClosed;
        if (populateContent)
        {
            RefreshPopupPresentation();
        }

        ApplyPopupDimensions(resetHeight: true);
        return flyout;
    }

    private void OnDropDownFlyoutOpened(object? sender, object args)
    {
        if (!ReferenceEquals(sender, _flyout))
        {
            return;
        }

        _flyoutIsOpen = true;
        var anchor = PopupAnchor;
        if (!anchor.IsDropDownOpen || !anchor.IsLoaded)
        {
            HideSharedPopup(anchor);
            return;
        }

        ObservePopupViewport(anchor.XamlRoot);
        ApplyPopupDimensions();
        PopupDiag.Log($"RibbonDropDownButton Flyout Opened (Header={Header})");
        anchor.RaiseDropDownOpened();
    }

    private void OnDropDownFlyoutClosing(FlyoutBase sender, FlyoutBaseClosingEventArgs args)
    {
        if (!ReferenceEquals(sender, _flyout))
        {
            return;
        }

        if (!sender.IsOpen && !_flyoutIsOpen && _popupResizeHost?.IsLoaded != true)
        {
            // Uno can raise Closing for Hide on a never-opened/already-closed
            // flyout without a matching Closed notification.
            return;
        }

        // Hide/IsDropDownOpen, Escape and command dismissal are explicit. Only the
        // platform's unsolicited light-dismiss request observes DismissOnClickOutside.
        args.Cancel = !_explicitFlyoutClose && ShouldCancelOutsideDismiss();
        _flyoutIsClosing = !args.Cancel;
        if (!args.Cancel)
        {
            var anchor = PopupAnchor;
            if (!_explicitFlyoutClose && anchor.IsDropDownOpen)
            {
                // A lease completion must not mistake a native light-dismiss
                // release for a still-requested opening and reopen the clone.
                anchor.IsDropDownOpen = false;
            }

            ReleaseQuickAccessPopup();
        }
    }

    private bool ShouldCancelOutsideDismiss() =>
        PopupAnchor.IsLoaded && PopupAnchor.IsDropDownOpen && !PopupAnchor.DismissOnClickOutsideCore;
#endif

    private bool TryPrepareQuickAccessPopup()
    {
        if (_preparingQuickAccessContent || _releasingQuickAccessContent)
        {
            return false;
        }

        if (PrepareQuickAccessContent is not { } prepare)
        {
            return PopupAnchor.IsDropDownOpen;
        }

        _quickAccessPreparationActive = true;
        _preparingQuickAccessContent = true;
        var ready = false;
        var failed = false;
        try
        {
            ready = prepare();
        }
        catch
        {
            failed = true;
            IsDropDownOpen = false;
            throw;
        }
        finally
        {
            _preparingQuickAccessContent = false;
            if (_releaseQuickAccessAfterPreparation || !IsDropDownOpen || !IsLoaded)
            {
                ReleaseQuickAccessPopup();
                if (!failed && IsDropDownOpen && IsLoaded)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (IsDropDownOpen && IsLoaded)
                        {
                            ShowDropDown();
                        }
                    });
                }
            }
        }

        return ready && PopupAnchor.IsDropDownOpen && PopupAnchor.IsLoaded && _quickAccessPreparationActive;
    }

    private void ReleaseQuickAccessPopup()
    {
        if (_preparingQuickAccessContent)
        {
            _releaseQuickAccessAfterPreparation = true;
            return;
        }

        if (!_quickAccessPreparationActive || _releasingQuickAccessContent)
        {
            return;
        }

        _quickAccessPreparationActive = false;
        _releaseQuickAccessAfterPreparation = false;
        _releasingQuickAccessContent = true;
        try
        {
            ReleaseQuickAccessContent?.Invoke();
        }
        finally
        {
            _releasingQuickAccessContent = false;
        }
    }

#if !WINDOWS
    private void OnDropDownFlyoutClosed(object? sender, object args)
    {
        if (!ReferenceEquals(sender, _flyout))
        {
            return;
        }

        // A fallback completion may already have admitted a new deferred open.
        // The old native Closed must not complete that new, not-yet-open cycle.
        if (!_flyoutIsOpen && !_flyoutIsClosing)
        {
            return;
        }

        CompleteSharedPopupClose();
    }
#endif

    internal void RefreshPopupDismissalOptions()
    {
        if (RefreshSharedPopup())
        {
            return;
        }
#if WINDOWS
        if (_nativePopup is not null && CanRefreshPopupPresentation)
        {
            _nativePopup.IsLightDismissEnabled = PopupAnchor.DismissOnClickOutsideCore;
        }
#else
        if (_flyout is not null && CanRefreshPopupPresentation)
        {
            var anchor = PopupAnchor;
            _flyout.OverlayInputPassThroughElement = anchor.DismissOnClickOutsideCore
                ? null
                : anchor.XamlRoot?.Content;
        }
#endif
    }

    private void OnDropDownPopupKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (ReferenceEquals(sender, _popupResizeHost)
            && !args.Handled && HandleDropDownPopupKey(args.Key))
        {
            args.Handled = true;
        }
    }

    private bool HandleDropDownPopupKey(VirtualKey key)
    {
        var anchor = PopupAnchor;
        if (key != VirtualKey.Escape || !anchor.IsDropDownOpen)
        {
            return false;
        }

        anchor.CloseDropDown();
        anchor.Focus(FocusState.Keyboard);
        return true;
    }

    private void OnDropDownPopupPointerPressed(object sender, PointerRoutedEventArgs args)
    {
        if (ReferenceEquals(sender, _popupResizeHost))
        {
            HandleDropDownPopupMouseDown(args.OriginalSource as DependencyObject);
        }
    }

    private void HandleDropDownPopupMouseDown(DependencyObject? originalSource)
    {
        var anchor = PopupAnchor;
        if (!anchor.IsDropDownOpen || !anchor.ClosePopupOnMouseDown
            || PopupResizeHelper.IsResizeInteraction(originalSource)
            || _mouseDownCloseTimer is not null)
        {
            return;
        }

        // WPF listens inside the popup, including handled MouseDown events, and
        // waits at least 100 ms so the original input/command can finish.
        _mouseDownCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(100, anchor.ClosePopupOnMouseDownDelay)),
        };
        _mouseDownCloseTimer.Tick += OnMouseDownCloseTick;
        _mouseDownCloseTimer.Start();
    }

    private void OnMouseDownCloseTick(object? sender, object args)
    {
        if (!ReferenceEquals(sender, _mouseDownCloseTimer))
        {
            return;
        }

        CancelMouseDownClose();
        var anchor = PopupAnchor;
        if (anchor.IsDropDownOpen && anchor.ClosePopupOnMouseDown)
        {
            anchor.CloseDropDown();
        }
    }

    private void CancelMouseDownClose()
    {
        if (_mouseDownCloseTimer is not null)
        {
            _mouseDownCloseTimer.Stop();
            _mouseDownCloseTimer.Tick -= OnMouseDownCloseTick;
            _mouseDownCloseTimer = null;
        }
    }

    private void RefreshPopupPresentation()
    {
        if (RefreshSharedPopup())
        {
            return;
        }
        ObservePopupItems();
        if (!CanRefreshPopupPresentation)
        {
            return;
        }
        var anchor = PopupAnchor;
        if (_dropDownItemsHost is not null)
        {
#if WINDOWS
            if (!_measuringCanonicalItems)
            {
                _measuringCanonicalItems = true;
                try
                {
                    // Native ItemsControl validates its generator during its own
                    // measure pass, including when only its QAT popup is loaded.
                    Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                }
                finally
                {
                    _measuringCanonicalItems = false;
                }
            }
#else
            var source = ItemsSource as IEnumerable ?? Items;
            if (!ReferenceEquals(_dropDownItemsHost.ItemsSource, source))
            {
                _dropDownItemsHost.ItemsSource = source;
            }
            if (!ReferenceEquals(_dropDownItemsHost.ItemTemplate, anchor.ItemTemplate))
            {
                _dropDownItemsHost.ItemTemplate = anchor.ItemTemplate;
            }
            if (!ReferenceEquals(_dropDownItemsHost.ItemTemplateSelector, anchor.ItemTemplateSelector))
            {
                _dropDownItemsHost.ItemTemplateSelector = anchor.ItemTemplateSelector;
            }
            if (!ReferenceEquals(_dropDownItemsHost.ItemContainerStyle, anchor.ItemContainerStyle))
            {
                _dropDownItemsHost.ItemContainerStyle = anchor.ItemContainerStyle;
            }
            if (!ReferenceEquals(_dropDownItemsHost.ItemsPanel, anchor.ItemsPanel))
            {
                _dropDownItemsHost.ItemsPanel = anchor.ItemsPanel;
            }
#endif
            var items = ItemsSource as IEnumerable ?? Items;
            _dropDownItemsHost.FlowDirection = anchor.FlowDirection;
            foreach (var item in items.Cast<object>().ToArray())
            {
                if (item is IDropDownItemOwner owned)
                {
                    owned.SetDropDownOwner(QuickAccessItemsOwnerOverride ?? this);
                }

                if (item is ColorGallery gallery)
                {
                    Fluent.Helpers.TouchTargetGeometry.PropagateCompactTargetSize(
                        QuickAccessItemsOwnerOverride as FrameworkElement ?? this, gallery);
                    gallery.RefreshTouchTargetGeometry();
                }
            }
        }

        if (_popupMenuHeader is not null)
        {
            _popupMenuHeader.Text = anchor.MenuHeader ?? string.Empty;
            _popupMenuHeader.Visibility = string.IsNullOrEmpty(anchor.MenuHeader)
                ? Visibility.Collapsed : Visibility.Visible;
#if WINDOWS
            if (_popupHeaderSeparator is not null)
            {
                _popupHeaderSeparator.Visibility = _popupMenuHeader.Visibility;
            }
#else
            _popupHeaderSeparator!.Visibility = _popupMenuHeader.Visibility;
#endif
        }

        if (_popupGalleryHost is not null)
        {
            if (!ReferenceEquals(_popupGalleryHost.Content, Gallery))
            {
                _popupGalleryHost.Content = Gallery;
            }
            _popupGalleryHost.Visibility = Gallery is null ? Visibility.Collapsed : Visibility.Visible;
#if WINDOWS
            if (_popupGallerySeparator is not null)
            {
                _popupGallerySeparator.Visibility = _popupGalleryHost.Visibility;
            }
#else
            _popupGallerySeparator!.Visibility = _popupGalleryHost.Visibility;
#endif
        }

        ApplyPopupDimensions();
        RefreshPopupDismissalOptions();
    }

    private void ApplyPopupDimensions(bool resetHeight = false)
    {
        if (RefreshSharedPopup(resetHeight))
        {
            return;
        }
        if (_popupResizeHost is not null)
        {
            if (!CanRefreshPopupPresentation)
            {
                _pendingPopupHeightReset |= resetHeight;
                return;
            }
            var anchor = PopupAnchor;
            PopupResizeHelper.Apply(
                _popupResizeHost, anchor, anchor.ResizeMode,
                Math.Max(200, anchor.ActualWidth), 0, anchor.MaxDropDownHeight,
                anchor.DropDownHeight, resetHeight || _pendingPopupHeightReset);
            _pendingPopupHeightReset = false;
#if WINDOWS
            UpdateNativePopupPosition();
#endif
        }
    }

    private void ObservePopupItems()
    {
        var source = IsLoaded || QuickAccessItemsOwnerOverride is not null
            ? ItemsSource as INotifyCollectionChanged
            : null;
        if (!ReferenceEquals(_observedPopupItems, source))
        {
            UnobservePopupItems();
            _observedPopupItems = source;
            if (_observedPopupItems is not null)
            {
                _observedPopupItems.CollectionChanged += OnPopupItemsChanged;
            }
        }
    }

    private void UnobservePopupItems()
    {
        if (_observedPopupItems is not null)
        {
            _observedPopupItems.CollectionChanged -= OnPopupItemsChanged;
            _observedPopupItems = null;
        }
    }

    private void OnPopupItemsChanged(object? sender, NotifyCollectionChangedEventArgs args) =>
        RefreshPopupPresentation();

    private void ObservePopupViewport(XamlRoot? root)
    {
        if (_popupXamlRoot is not null)
        {
            _popupXamlRoot.Changed -= OnPopupViewportChanged;
        }

        _popupXamlRoot = root;
        if (root is not null)
        {
            root.Changed += OnPopupViewportChanged;
        }
    }

    private void OnPopupViewportChanged(XamlRoot sender, XamlRootChangedEventArgs args) =>
        ApplyPopupDimensions();
}
