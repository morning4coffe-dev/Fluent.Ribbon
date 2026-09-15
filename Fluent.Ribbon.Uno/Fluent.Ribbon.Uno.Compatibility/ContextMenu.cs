using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Fluent;

/// <summary>
/// WinUI context-menu compatibility surface backed by <see cref="MenuFlyout"/>.
/// </summary>
/// <remarks>
/// The native menu presenter, items, keyboard navigation and command routing are
/// retained. Resizable menus wrap the presenter's existing scrolling content.
/// </remarks>
public class ContextMenu : MenuFlyout
{
    private MenuFlyoutPresenter? nativePresenter;
    private ResizeableContentControl? resizeHost;
    private XamlRoot? observedRoot;
    private bool updatingResizePresentation;
    private bool isContextMenuOpen;
    private readonly List<(DependencyProperty Property, long Token)> presenterCallbacks = [];
    private readonly HashSet<MenuFlyoutItemBase> observedItems = [];

    internal bool IsQuickAccessCompatibilityMenu { get; set; }

    /// <summary>Identifies the <see cref="ResizeMode"/> dependency property.</summary>
    public static readonly DependencyProperty ResizeModeProperty =
        DependencyProperty.Register(
            nameof(ResizeMode),
            typeof(ContextMenuResizeMode),
            typeof(ContextMenu),
            new PropertyMetadata(ContextMenuResizeMode.None, static (sender, _) =>
                ((ContextMenu)sender).UpdateResizePresentation()));

    /// <summary>
    /// Gets or sets the requested resize mode.
    /// </summary>
    public ContextMenuResizeMode ResizeMode
    {
        get => (ContextMenuResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    /// <summary>Initializes a native context menu with optional resize handles.</summary>
    public ContextMenu()
    {
        Opening += (_, _) =>
        {
            isContextMenuOpen = true;
            foreach (var item in Items)
            {
                if (observedItems.Add(item))
                {
                    item.Loaded += OnMenuItemLoaded;
                }
            }
        };
        Opened += (_, _) =>
        {
            isContextMenuOpen = true;
            RefreshOpenedPresenter();
            if (nativePresenter?.IsLoaded != true)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (isContextMenuOpen)
                    {
                        RefreshOpenedPresenter();
                    }
                });
            }
        };
        Closed += (_, _) =>
        {
            isContextMenuOpen = false;
            ObserveViewport(null);
            foreach (var item in observedItems)
            {
                item.Loaded -= OnMenuItemLoaded;
            }
            observedItems.Clear();
        };
    }

    private void OnMenuItemLoaded(object sender, RoutedEventArgs args)
    {
        if (isContextMenuOpen)
        {
            RefreshOpenedPresenter();
        }
    }

    private void RefreshOpenedPresenter()
    {
        FindNativePresenter();
        UpdateResizePresentation();
        ObserveViewport(nativePresenter?.XamlRoot);
    }

    /// <summary>Creates the default menu-item container.</summary>
    protected virtual DependencyObject GetContainerForItemOverride()
    {
        return new MenuFlyoutItem();
    }

    /// <summary>Gets whether an item is already its own menu container.</summary>
    protected virtual bool IsItemItsOwnContainerOverride(object item)
    {
        return item is MenuFlyoutItemBase or MenuFlyoutSeparator;
    }

    /// <summary>Applies compatibility template state.</summary>
    public virtual void OnApplyTemplate()
    {
        nativePresenter?.ApplyTemplate();
        UpdateResizePresentation();
    }

    private void FindNativePresenter()
    {
        foreach (var item in Items)
        {
            for (DependencyObject? current = item; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is not MenuFlyoutPresenter presenter)
                {
                    continue;
                }

                if (!ReferenceEquals(presenter, nativePresenter))
                {
                    if (nativePresenter is not null)
                    {
                        nativePresenter.SizeChanged -= OnPresenterSizeChanged;
                        nativePresenter.Loaded -= OnPresenterLoaded;
                        foreach (var callback in presenterCallbacks)
                        {
                            nativePresenter.UnregisterPropertyChangedCallback(callback.Property, callback.Token);
                        }
                    }

                    presenterCallbacks.Clear();
                    nativePresenter = presenter;
                    nativePresenter.SizeChanged += OnPresenterSizeChanged;
                    nativePresenter.Loaded += OnPresenterLoaded;
                    foreach (var property in new[]
                             {
                                 FrameworkElement.MinWidthProperty, FrameworkElement.MaxWidthProperty,
                                 FrameworkElement.MinHeightProperty, FrameworkElement.MaxHeightProperty,
                                 Control.PaddingProperty,
                             })
                    {
                        presenterCallbacks.Add((property, presenter.RegisterPropertyChangedCallback(
                            property, (_, _) => UpdateResizePresentation())));
                    }

                    presenterCallbacks.Add((Control.TemplateProperty, presenter.RegisterPropertyChangedCallback(
                        Control.TemplateProperty, (_, _) => DispatcherQueue.TryEnqueue(UpdateResizePresentation))));
                    resizeHost = null;
                }

                return;
            }
        }
    }

    private void OnPresenterSizeChanged(object sender, SizeChangedEventArgs args) => UpdateResizePresentation();

    private void OnPresenterLoaded(object sender, RoutedEventArgs args) => UpdateResizePresentation();

    private void UpdateResizePresentation()
    {
        if (updatingResizePresentation)
        {
            return;
        }

        if (resizeHost is not null)
        {
            resizeHost.ResizeMode = ResizeMode;
        }

        if (nativePresenter?.IsLoaded != true)
        {
            return;
        }

        updatingResizePresentation = true;
        try
        {
            if (resizeHost is not null && !ContainsVisual(nativePresenter, resizeHost))
            {
                resizeHost = null;
            }

            if (resizeHost is null && ResizeMode != ContextMenuResizeMode.None)
            {
                nativePresenter.ApplyTemplate();
                if (FindScroller(nativePresenter) is { } scroller)
                {
                    WrapNativeScroller(scroller);
                }
            }

            if (resizeHost is null)
            {
                return;
            }

            resizeHost.ResizeMode = ResizeMode;
            var padding = nativePresenter.Padding;
            var viewport = nativePresenter.XamlRoot?.Size;
            var maxWidth = Math.Max(0, Math.Min(
                nativePresenter.MaxWidth,
                Math.Max(0, (viewport?.Width ?? double.PositiveInfinity) - 32))
                - padding.Left - padding.Right);
            var maxHeight = Math.Max(0, Math.Min(
                nativePresenter.MaxHeight,
                Math.Max(0, (viewport?.Height ?? double.PositiveInfinity) - 32))
                - padding.Top - padding.Bottom);
            resizeHost.MinWidth = Math.Min(maxWidth, Math.Max(0, nativePresenter.MinWidth - padding.Left - padding.Right));
            resizeHost.MinHeight = Math.Min(maxHeight, Math.Max(0, nativePresenter.MinHeight - padding.Top - padding.Bottom));
            resizeHost.MaxWidth = maxWidth;
            resizeHost.MaxHeight = maxHeight;
            if (!double.IsNaN(resizeHost.Width))
            {
                resizeHost.Width = Math.Clamp(resizeHost.Width, resizeHost.MinWidth, maxWidth);
            }

            if (!double.IsNaN(resizeHost.Height))
            {
                resizeHost.Height = Math.Clamp(resizeHost.Height, resizeHost.MinHeight, maxHeight);
            }
        }
        finally
        {
            updatingResizePresentation = false;
        }
    }

    private void WrapNativeScroller(ScrollViewer scroller)
    {
        var wrapper = new ResizeableContentControl
        {
            Name = "PART_ContextMenuResizeHost",
            ResizeMode = ResizeMode,
            HorizontalAlignment = scroller.HorizontalAlignment,
            VerticalAlignment = scroller.VerticalAlignment,
        };
        switch (VisualTreeHelper.GetParent(scroller))
        {
            case Border border when ReferenceEquals(border.Child, scroller):
                border.Child = null;
                wrapper.Content = scroller;
                border.Child = wrapper;
                break;
            case Panel panel:
                var index = panel.Children.IndexOf(scroller);
                if (index < 0)
                {
                    return;
                }

                Grid.SetRow(wrapper, Grid.GetRow(scroller));
                Grid.SetColumn(wrapper, Grid.GetColumn(scroller));
                Grid.SetRowSpan(wrapper, Grid.GetRowSpan(scroller));
                Grid.SetColumnSpan(wrapper, Grid.GetColumnSpan(scroller));
                panel.Children.RemoveAt(index);
                wrapper.Content = scroller;
                panel.Children.Insert(index, wrapper);
                break;
            case ContentControl control when ReferenceEquals(control.Content, scroller):
                control.Content = null;
                wrapper.Content = scroller;
                control.Content = wrapper;
                break;
            case ContentPresenter presenter when ReferenceEquals(presenter.Content, scroller):
                presenter.Content = null;
                wrapper.Content = scroller;
                presenter.Content = wrapper;
                break;
            default:
                return;
        }

        resizeHost = wrapper;
    }

    private static ScrollViewer? FindScroller(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is ScrollViewer scroller)
            {
                return scroller;
            }

            if (child is not MenuFlyoutItemBase && FindScroller(child) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }

    private static bool ContainsVisual(DependencyObject root, DependencyObject element)
    {
        for (DependencyObject? current = element; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, root))
            {
                return true;
            }
        }

        return false;
    }

    private void ObserveViewport(XamlRoot? root)
    {
        if (observedRoot is not null)
        {
            observedRoot.Changed -= OnViewportChanged;
        }

        observedRoot = root;
        if (root is not null)
        {
            root.Changed += OnViewportChanged;
        }
    }

    private void OnViewportChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateResizePresentation();
}
