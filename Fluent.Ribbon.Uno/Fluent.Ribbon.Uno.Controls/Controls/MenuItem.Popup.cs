namespace Fluent;

using Windows.Foundation;
using Windows.System;

public partial class MenuItem
{
    private ResizeableContentControl? submenuResizeHost;
    private XamlRoot? submenuXamlRoot;
    private bool submenuOpenedNotified;
    private bool updatingSubmenuLayout;
    private Popup? closingSubmenuPopup;
    private long submenuRequestVersion;
    private bool validatingSubmenuGenerator;

    private void InitializeSubmenuOptions()
    {
#if WINDOWS
        InitializeNativeSubmenu();
#endif
        foreach (var property in new[] { ResizeModeProperty, MaxDropDownHeightProperty, FlowDirectionProperty })
        {
            RegisterPropertyChangedCallback(property, (_, _) => UpdateSubmenuDimensions());
        }

        SizeChanged += (_, _) => UpdateSubmenuDimensions();
        ActualThemeChanged += (_, _) => UpdateSubmenuDimensions();
        Loaded += (_, _) =>
        {
            if (IsDropDownOpen)
            {
                ShowCompatibilitySubmenu();
            }
        };
    }

    private void ApplySubmenuTemplate()
    {
#if WINDOWS
        ApplyNativeSubmenuTemplate();
#else
        submenuRequestVersion++;
        closingSubmenuPopup = null;
        var wasOpen = IsDropDownOpen;
        if (DropDownPopup is not null)
        {
            DropDownPopup.Opened -= OnCompatibilitySubmenuOpened;
            DropDownPopup.Closed -= OnCompatibilitySubmenuClosed;
            DropDownPopup.IsOpen = false;
        }

        if (submenuResizeHost is not null)
        {
            submenuResizeHost.SizeChanged -= OnSubmenuSizeChanged;
            submenuResizeHost.KeyDown -= OnSubmenuKeyDown;
        }

        DropDownPopup = GetTemplateChild("PART_Popup") as Popup;
        submenuResizeHost = GetTemplateChild("PART_PopupContentControl") as ResizeableContentControl;
        submenuItemsHost = GetTemplateChild("PART_SubmenuItemsHost") as ItemsControl;
        ConnectSubmenuPopup();
        if (wasOpen)
        {
            ShowCompatibilitySubmenu();
        }
#endif
    }

    private void CreateCompatibilitySubmenu()
    {
#if WINDOWS
        ApplyTemplate();
        if (DropDownPopup is null || submenuItemsHost is null)
        {
            throw new InvalidOperationException("The menu template has no canonical submenu presenter.");
        }
#else
        submenuItemsHost = new ItemsControl();
        submenuResizeHost = new ResizeableContentControl
        {
            Name = "PART_PopupContentControl",
            Content = new ScrollViewer
            {
                Content = submenuItemsHost,
                HorizontalScrollMode = ScrollMode.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsTabStop = false,
            },
        };
        DropDownPopup = new Popup { Child = submenuResizeHost };
        ConnectSubmenuPopup();
#endif
    }

    private void ValidateSubmenuGenerator()
    {
#if WINDOWS
        if (!nativeSubmenuPrepared || !NativeSubmenuRequested || !NativeSubmenuAnchor.IsLoaded)
        {
            return;
        }
#endif
        if (validatingSubmenuGenerator || submenuItemsHost is null)
        {
            return;
        }
        validatingSubmenuGenerator = true;
        try
        {
#if WINDOWS
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
#else
            if (!ReferenceEquals(submenuItemsHost.ItemsSource, Items))
            {
                submenuItemsHost.ItemsSource = Items;
            }
            if (!ReferenceEquals(submenuItemsHost.ItemsPanel, ItemsPanel))
            {
                submenuItemsHost.ItemsPanel = ItemsPanel;
            }
#endif
        }
        finally
        {
            validatingSubmenuGenerator = false;
        }
    }

    private void ConnectSubmenuPopup()
    {
        if (DropDownPopup is not null)
        {
            DropDownPopup.IsLightDismissEnabled = true;
            DropDownPopup.FlowDirection = FlowDirection.LeftToRight;
            DropDownPopup.Opened += OnCompatibilitySubmenuOpened;
            DropDownPopup.Closed += OnCompatibilitySubmenuClosed;
        }

        if (submenuResizeHost is not null)
        {
            submenuResizeHost.SizeChanged += OnSubmenuSizeChanged;
            submenuResizeHost.KeyDown += OnSubmenuKeyDown;
        }

        UpdateSubmenuDimensions();
    }

    private void UpdateSubmenuDimensions()
    {
#if WINDOWS
        if (!NativeSubmenuRequested || !NativeSubmenuAnchor.IsLoaded)
        {
            return;
        }
#endif
        if (submenuResizeHost is null || updatingSubmenuLayout)
        {
            return;
        }

        updatingSubmenuLayout = true;
        try
        {
#if WINDOWS
            var anchor = NativeSubmenuAnchor;
            if (NativeQuickAccessAnchor is { } clone)
            {
                PopupResizeHelper.Apply(
                    submenuResizeHost, clone, clone.ResizeMode, Math.Max(200, clone.ActualWidth), 0,
                    clone.MaxDropDownHeight, clone.DropDownHeight);
                DropDownPopup!.IsLightDismissEnabled = clone.NativeDismissOnClickOutside;
            }
            else
            {
                PopupResizeHelper.Apply(
                    submenuResizeHost, this, ResizeMode, Math.Max(160, ActualWidth), ActualHeight,
                    MaxDropDownHeight);
                DropDownPopup!.IsLightDismissEnabled = true;
            }
            if (submenuItemsHost is not null)
            {
                submenuItemsHost.FlowDirection = anchor.FlowDirection;
            }
#else
            PopupResizeHelper.Apply(
                submenuResizeHost, this, ResizeMode, Math.Max(160, ActualWidth), ActualHeight,
                MaxDropDownHeight);
            if (submenuItemsHost is not null)
            {
                submenuItemsHost.FlowDirection = FlowDirection;
            }
#endif
        }
        finally
        {
            updatingSubmenuLayout = false;
        }

        UpdateSubmenuPosition();
    }

    private void OnSubmenuSizeChanged(object sender, SizeChangedEventArgs args) => UpdateSubmenuPosition();

    private void UpdateSubmenuPosition()
    {
#if WINDOWS
        if (updatingSubmenuLayout || !NativeSubmenuRequested || !NativeSubmenuAnchor.IsLoaded
            || DropDownPopup?.Child is not FrameworkElement child)
        {
            return;
        }
        updatingSubmenuLayout = true;
        try
        {
            NativePopupLayoutHelper.Position(
                DropDownPopup, child, NativeSubmenuAnchor, submenu: NativeQuickAccessAnchor is null);
        }
        finally
        {
            updatingSubmenuLayout = false;
        }
#else
        if (updatingSubmenuLayout || !IsDropDownOpen || XamlRoot is null
            || DropDownPopup?.Child is not FrameworkElement child)
        {
            return;
        }

        updatingSubmenuLayout = true;
        try
        {
            var viewport = XamlRoot.Size;
            child.Measure(new Size(
                Math.Max(0, viewport.Width - 2 * PopupResizeHelper.ViewportMargin),
                Math.Max(0, viewport.Height - 2 * PopupResizeHelper.ViewportMargin)));
            var width = child.DesiredSize.Width;
            var height = child.DesiredSize.Height;
            var ownerBounds = TransformToVisual(null).TransformBounds(new Rect(0, 0, ActualWidth, ActualHeight));
            var position = PopupResizeHelper.PlaceSubmenu(
                ownerBounds, new Size(width, height), viewport, FlowDirection == FlowDirection.RightToLeft);
            if (VisualTreeHelper.GetParent(DropDownPopup) is not null)
            {
                // Keep placement in physical coordinates; the content itself
                // follows the menu's FlowDirection independently.
                var origin = DropDownPopup.TransformToVisual(null).TransformPoint(new Point());
                position = new Point(position.X - origin.X, position.Y - origin.Y);
            }

            DropDownPopup.HorizontalOffset = position.X;
            DropDownPopup.VerticalOffset = position.Y;
        }
        finally
        {
            updatingSubmenuLayout = false;
        }
#endif
    }

    private void OnSubmenuKeyDown(object sender, KeyRoutedEventArgs args)
    {
#if WINDOWS
        if (NativeQuickAccessAnchor is { } clone && !args.Handled && clone.HandleNativeMenuKey(args.Key))
        {
            args.Handled = true;
            return;
        }
#endif
        if (!args.Handled && args.Key == VirtualKey.Escape)
        {
            IsDropDownOpen = false;
            Focus(FocusState.Keyboard);
            args.Handled = true;
        }
    }

    private void OnCompatibilitySubmenuClosed(object? sender, object args)
    {
#if WINDOWS
        OnNativeSubmenuClosed(sender);
#else
        if (sender is not Popup popup || !ReferenceEquals(popup, DropDownPopup))
        {
            return;
        }

        var requestedClose = ReferenceEquals(closingSubmenuPopup, popup);
        closingSubmenuPopup = null;
        if (requestedClose && IsDropDownOpen)
        {
            // The old close completed after a new UIA/KeyTip opening request.
            // Keep its requested state and focus intent, then reopen after teardown.
            ShowCompatibilitySubmenu();
            return;
        }

        if (popup.IsOpen)
        {
            return;
        }

        IsDropDownOpen = false;
        NotifySubmenuClosed();
#endif
    }

    private void NotifySubmenuClosed()
    {
        focusFirstSubmenuItemWhenOpened = false;
        if (submenuOpenedNotified)
        {
            submenuOpenedNotified = false;
            DropDownClosed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ObserveSubmenuViewport(XamlRoot? root)
    {
        if (submenuXamlRoot is not null)
        {
            submenuXamlRoot.Changed -= OnSubmenuViewportChanged;
        }

        submenuXamlRoot = root;
        if (root is not null)
        {
            root.Changed += OnSubmenuViewportChanged;
        }
    }

    private void OnSubmenuViewportChanged(XamlRoot sender, XamlRootChangedEventArgs args) =>
        UpdateSubmenuDimensions();
}
