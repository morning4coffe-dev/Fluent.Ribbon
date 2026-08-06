namespace Fluent;

using System.Collections;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

public partial class MenuItem :
    IDropDownItemOwner,
    IQuickAccessItemProvider,
    IRibbonControl,
    IDropDownControl,
    IToggleButton
{
    private ItemsControl? submenuItemsHost;
    private DependencyObject? dropDownOwner;
    private bool focusFirstSubmenuItemWhenOpened;

    /// <summary>Identifies whether access-key markers are recognized.</summary>
    public static readonly DependencyProperty RecognizesAccessKeyProperty =
        DependencyProperty.RegisterAttached(
            nameof(RecognizesAccessKey),
            typeof(bool),
            typeof(MenuItem),
            new PropertyMetadata(true));

    /// <summary>Gets whether access-key markers are recognized.</summary>
    public bool RecognizesAccessKey
    {
        get => GetRecognizesAccessKey(this);
        set => SetRecognizesAccessKey(this, value);
    }

    /// <summary>Gets whether access-key markers are recognized on an element.</summary>
    public static bool GetRecognizesAccessKey(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(RecognizesAccessKeyProperty);
    }

    /// <summary>Sets whether access-key markers are recognized on an element.</summary>
    public static void SetRecognizesAccessKey(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(RecognizesAccessKeyProperty, value);
    }

    /// <summary>Gets the current submenu popup when the template provides one.</summary>
    public Popup? DropDownPopup { get; private set; }

    /// <summary>Gets the logical parent used by non-menu hosts.</summary>
    public object? LogicalParent =>
        Parent ?? VisualTreeHelper.GetParent(this);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        DropDownPopup = GetTemplateChild("PART_Popup") as Popup ?? DropDownPopup;
    }

    /// <summary>Creates the default item container.</summary>
    protected virtual DependencyObject GetContainerForItemOverride()
    {
        return new MenuItem();
    }

    /// <summary>Gets whether an item is already its own container.</summary>
    protected virtual bool IsItemItsOwnContainerOverride(object item)
    {
        return item is UIElement;
    }

    /// <summary>Handles context-menu opening.</summary>
    protected virtual void OnContextMenuOpening(ContextMenuEventArgs e)
    {
        IsContextMenuOpened = true;
        IsDropDownOpen = false;
    }

    /// <summary>Handles context-menu closing.</summary>
    protected virtual void OnContextMenuClosing(ContextMenuEventArgs e)
    {
        IsContextMenuOpened = false;
    }

    /// <summary>Handles keyboard-focus changes.</summary>
    protected virtual void OnIsKeyboardFocusedChanged(
        DependencyPropertyChangedEventArgs e)
    {
    }

    /// <summary>Handles pointer entry.</summary>
    protected virtual void OnMouseEnter(PointerRoutedEventArgs e)
    {
        if (!IsContextMenuOpened
            && HasSubItems
            && dropDownOwner is RibbonDropDownButton or MenuItem)
        {
            IsDropDownOpen = true;
        }
    }

    /// <summary>Handles pointer exit.</summary>
    protected virtual void OnMouseLeave(PointerRoutedEventArgs e)
    {
        if (!IsContextMenuOpened
            && HasSubItems
            && dropDownOwner is RibbonDropDownButton)
        {
            IsDropDownOpen = false;
        }
    }

    /// <summary>Handles primary-pointer release.</summary>
    protected virtual void OnMouseLeftButtonUp(PointerRoutedEventArgs e)
    {
    }

    /// <summary>Handles pointer-wheel input.</summary>
    protected virtual void OnMouseWheel(PointerRoutedEventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        OnMouseEnter(e);
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        OnMouseLeave(e);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        OnMouseLeftButtonUp(e);
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        OnMouseWheel(e);
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
    {
        get
        {
            foreach (var item in Items)
            {
                yield return item;
            }

            if (Icon is not null)
            {
                yield return Icon;
            }

            if (Header is not null)
            {
                yield return Header;
            }
        }
    }

    void IDropDownItemOwner.SetDropDownOwner(DependencyObject owner)
    {
        dropDownOwner = owner;
    }

    internal DependencyObject? DropDownOwner => dropDownOwner;

    internal bool HandleMenuNavigationKey(VirtualKey key)
    {
        if (key is VirtualKey.Up or VirtualKey.Down or VirtualKey.Home or VirtualKey.End)
        {
            var target = GetSiblingNavigationTarget(key);
            return target is not null && target.Focus(FocusState.Keyboard);
        }

        var openKey = FlowDirection == FlowDirection.RightToLeft
            ? VirtualKey.Left
            : VirtualKey.Right;
        var closeKey = openKey == VirtualKey.Right
            ? VirtualKey.Left
            : VirtualKey.Right;

        if (key == openKey && HasSubItems)
        {
            OpenSubmenuAndFocusFirstItem();
            return true;
        }

        if (key == closeKey || key == VirtualKey.Escape)
        {
            return CloseSubmenuOrParent();
        }

        return false;
    }

    internal MenuItem? GetSiblingNavigationTarget(VirtualKey key)
    {
        var siblings = GetSiblingMenuItems().ToList();
        if (siblings.Count == 0)
        {
            return null;
        }

        if (key == VirtualKey.Home)
        {
            return siblings.FirstOrDefault(IsKeyboardNavigable);
        }

        if (key == VirtualKey.End)
        {
            return siblings.LastOrDefault(IsKeyboardNavigable);
        }

        var currentIndex = siblings.IndexOf(this);
        if (currentIndex < 0)
        {
            return key == VirtualKey.Up
                ? siblings.LastOrDefault(IsKeyboardNavigable)
                : siblings.FirstOrDefault(IsKeyboardNavigable);
        }

        var targetIndex = FindSiblingTargetIndex(
            siblings.Count,
            currentIndex,
            key == VirtualKey.Up ? -1 : 1,
            index => IsKeyboardNavigable(siblings[index]));
        return targetIndex < 0 ? null : siblings[targetIndex];
    }

    internal static int FindSiblingTargetIndex(
        int count,
        int currentIndex,
        int direction,
        Func<int, bool> canFocus)
    {
        for (var offset = 1; offset < count; offset++)
        {
            var candidateIndex = (currentIndex + (direction * offset) + count) % count;
            if (canFocus(candidateIndex))
            {
                return candidateIndex;
            }
        }

        return -1;
    }

    internal IEnumerable<MenuItem> GetSiblingMenuItems()
    {
        if (dropDownOwner is MenuItem parentMenuItem)
        {
            return parentMenuItem.Items.OfType<MenuItem>();
        }

        if (dropDownOwner is RibbonDropDownButton dropDownButton)
        {
            var items = dropDownButton.ItemsSource as IEnumerable
                        ?? dropDownButton.Items;
            return items.Cast<object>().OfType<MenuItem>();
        }

        for (DependencyObject? current = this;
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is ItemsControl itemsControl)
            {
                return itemsControl.Items.Cast<object>().OfType<MenuItem>();
            }
        }

        return [];
    }

    internal void OpenSubmenuAndFocusFirstItem()
    {
        if (!HasSubItems)
        {
            return;
        }

        focusFirstSubmenuItemWhenOpened = true;
        IsDropDownOpen = true;

        if (DropDownPopup?.IsOpen == true)
        {
            FocusFirstEnabledSubmenuItem();
        }
    }

    private bool CloseSubmenuOrParent()
    {
        if (IsDropDownOpen)
        {
            IsDropDownOpen = false;
            Focus(FocusState.Keyboard);
            return true;
        }

        if (dropDownOwner is MenuItem parentMenuItem)
        {
            parentMenuItem.IsDropDownOpen = false;
            parentMenuItem.Focus(FocusState.Keyboard);
            return true;
        }

        if (dropDownOwner is IDropDownControl dropDownControl)
        {
            dropDownControl.IsDropDownOpen = false;
            if (dropDownOwner is Control ownerControl)
            {
                ownerControl.Focus(FocusState.Keyboard);
            }

            return true;
        }

        return false;
    }

    private void FocusFirstEnabledSubmenuItem()
    {
        focusFirstSubmenuItemWhenOpened = false;
        Items.OfType<MenuItem>()
            .FirstOrDefault(IsKeyboardNavigable)
            ?.Focus(FocusState.Keyboard);
    }

    private static bool IsKeyboardNavigable(MenuItem item)
        => item.IsEnabled
           && item.IsTabStop
           && item.Visibility == Visibility.Visible;

    private void ShowCompatibilitySubmenu()
    {
        if (Items.Count == 0 || XamlRoot is null)
        {
            return;
        }

        if (DropDownPopup is null)
        {
            foreach (var item in Items.OfType<IDropDownItemOwner>())
            {
                item.SetDropDownOwner(this);
            }

            submenuItemsHost = new ItemsControl
            {
                ItemsSource = Items,
                MinWidth = Math.Max(160, ActualWidth),
                FlowDirection = FlowDirection,
            };
            DropDownPopup = new Popup
            {
                Child = submenuItemsHost,
                IsLightDismissEnabled = true,
                XamlRoot = XamlRoot
            };
            DropDownPopup.Opened += OnCompatibilitySubmenuOpened;
            DropDownPopup.Closed += (_, _) =>
            {
                if (IsDropDownOpen)
                {
                    IsDropDownOpen = false;
                }
            };
        }
        else
        {
            foreach (var item in Items.OfType<IDropDownItemOwner>())
            {
                item.SetDropDownOwner(this);
            }

            DropDownPopup.XamlRoot = XamlRoot;
            if (submenuItemsHost is not null)
            {
                submenuItemsHost.ItemsSource = Items;
                submenuItemsHost.MinWidth = Math.Max(160, ActualWidth);
                submenuItemsHost.FlowDirection = FlowDirection;
            }
        }

        var origin = TransformToVisual(null)
            .TransformPoint(new Windows.Foundation.Point(ActualWidth, 0));
        DropDownPopup.HorizontalOffset = origin.X;
        DropDownPopup.VerticalOffset = origin.Y;
        FlyoutShowHelper.OpenDeferred(DropDownPopup);
    }

    private void HideCompatibilitySubmenu()
    {
        if (DropDownPopup is not null)
        {
            var shouldRestoreFocus =
                DropDownPopup.Child is DependencyObject child
                && XamlRoot is { } xamlRoot
                && FocusManager.GetFocusedElement(xamlRoot) is DependencyObject focused
                && FocusRoutingHelper.IsDescendantOf(focused, child);
            DropDownPopup.IsOpen = false;
            if (shouldRestoreFocus)
            {
                Focus(FocusState.Keyboard);
            }
        }
    }

    private void OnCompatibilitySubmenuOpened(object? sender, object args)
    {
        if (focusFirstSubmenuItemWhenOpened)
        {
            FocusFirstEnabledSubmenuItem();
        }
    }

    private void RaiseInvokedAutomationEvent()
    {
        if (FrameworkElementAutomationPeer.FromElement(this)
            is RibbonMenuItemAutomationPeer peer)
        {
            peer.RaiseInvoked();
        }
    }

}
