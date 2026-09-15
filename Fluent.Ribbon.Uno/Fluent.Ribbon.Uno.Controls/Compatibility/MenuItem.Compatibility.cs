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
#if WINDOWS
    private ItemsPresenter? submenuItemsHost;
#else
    private ItemsControl? submenuItemsHost;
#endif
    private object? nativeContainerItem;
    private bool hasNativeContainerItem;
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
        ApplyMenuPresentationTemplate();
        ApplySubmenuTemplate();
    }

    /// <summary>Creates the default item container.</summary>
    protected override DependencyObject GetContainerForItemOverride()
    {
        if (hasNativeContainerItem && itemsBinding is { UsesNativeGenerator: true, IsUpdating: false })
        {
            var item = nativeContainerItem;
            hasNativeContainerItem = false;
            nativeContainerItem = null;
            var wasValidating = validatingSubmenuGenerator;
            validatingSubmenuGenerator = true;
            try
            {
                return itemsBinding.AcquireNativeContainer(item);
            }
            finally
            {
                validatingSubmenuGenerator = wasValidating;
            }
        }
        return new MenuItem();
    }

    /// <summary>Gets whether an item is already its own container.</summary>
    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        if (itemsBinding is { UsesNativeGenerator: true, IsUpdating: false } && item is not UIElement)
        {
            nativeContainerItem = item;
            hasNativeContainerItem = true;
        }
        return item is UIElement;
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        => Fluent.Helpers.ItemsControlBinding.PrepareContent(this, element, item);

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        if (itemsBinding is { UsesNativeGenerator: true, IsUpdating: false }
            && itemsBinding.ReleaseNativeContainer(element))
        {
            return;
        }

        if (element is MenuItem menuItem
            && (ReferenceEquals(menuItem.dropDownOwner, this)
                || ReferenceEquals(menuItem.dropDownOwner, QuickAccessSubmenuOwner)))
        {
            menuItem.IsDropDownOpen = false;
            menuItem.dropDownOwner = null;
        }

        Fluent.Helpers.ItemsControlBinding.ClearContent(element, item);
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
        if (IsEnabled && !IsContextMenuOpened && dropDownOwner is RibbonDropDownButton or MenuItem)
        {
            foreach (var sibling in GetSiblingMenuItems())
            {
                if (!ReferenceEquals(sibling, this))
                {
                    sibling.IsDropDownOpen = false;
                }
            }
        }

        if (CanOpenSubmenu
            && !IsContextMenuOpened
            && !IsSplit
            && dropDownOwner is RibbonDropDownButton or MenuItem)
        {
            IsDropDownOpen = true;
        }
    }

    /// <summary>Handles pointer exit.</summary>
    protected virtual void OnMouseLeave(PointerRoutedEventArgs e)
    {
        // The submenu is a separate popup tree. Leaving this item can mean entering
        // that popup; sibling opening and light-dismiss perform the actual closure.
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
        if (ReferenceEquals(dropDownOwner, owner))
        {
            return;
        }

        dropDownOwner = owner;
        ReconcileCheckedGroup();
        UpdateMenuPresentation();
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
            var siblings = items.Cast<object>().OfType<MenuItem>().ToArray();
            if (siblings.Contains(this))
            {
                return siblings;
            }
        }

        for (DependencyObject? current = VisualTreeHelper.GetParent(this);
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is ItemsControl itemsControl)
            {
                return itemsControl.Items.Cast<object>().OfType<MenuItem>();
            }

            if (current is Panel panel && panel.Children.OfType<MenuItem>().Any())
            {
                return panel.Children.OfType<MenuItem>();
            }
        }

        return [];
    }

    internal void OpenSubmenuAndFocusFirstItem()
    {
        if (!CanOpenSubmenu)
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
        var target = Items.OfType<MenuItem>().FirstOrDefault(IsKeyboardNavigable) as Control
                     ?? (DropDownPopup?.Child is { } child
                         ? FocusManager.FindFirstFocusableElement(child) as Control
                         : null);
        target?.Focus(FocusState.Keyboard);
    }

    private static bool IsKeyboardNavigable(MenuItem item)
        => item.IsEnabled
           && item.IsTabStop
           && item.Visibility == Visibility.Visible;

    private void ShowCompatibilitySubmenu()
    {
#if WINDOWS
        ShowNativeOwnSubmenu();
#else
        if (XamlRoot is null || !IsLoaded)
        {
            return;
        }

        if (!CanOpenSubmenu)
        {
            IsDropDownOpen = false;
            return;
        }

        if (closingSubmenuPopup is not null || !PrepareOwnQuickAccessContent())
        {
            return;
        }

        if (DropDownPopup is null)
        {
            CreateCompatibilitySubmenu();
        }

        foreach (var sibling in GetSiblingMenuItems())
        {
            if (!ReferenceEquals(sibling, this))
            {
                sibling.IsDropDownOpen = false;
            }
        }

        foreach (var item in Items.OfType<IDropDownItemOwner>())
        {
            item.SetDropDownOwner(this);
        }

        DropDownPopup!.XamlRoot = XamlRoot;
        if (submenuItemsHost is not null)
        {
            submenuItemsHost.FlowDirection = FlowDirection;
            ValidateSubmenuGenerator();
        }

        ObserveSubmenuViewport(XamlRoot);
        UpdateSubmenuDimensions();
        UpdateSubmenuPosition();
        var requestedPopup = DropDownPopup;
        var requestedVersion = submenuRequestVersion;
        FlyoutShowHelper.OpenDeferred(
            requestedPopup,
            () => IsDropDownOpen && IsLoaded && CanOpenSubmenu && closingSubmenuPopup is null
                  && requestedVersion == submenuRequestVersion && ReferenceEquals(DropDownPopup, requestedPopup));
#endif
    }

    private void HideCompatibilitySubmenu()
    {
#if WINDOWS
        HideNativeOwnSubmenu();
#else
        ObserveSubmenuViewport(null);
        if (DropDownPopup is not null)
        {
            var shouldRestoreFocus =
                DropDownPopup.Child is DependencyObject child
                && XamlRoot is { } xamlRoot
                && FocusManager.GetFocusedElement(xamlRoot) is DependencyObject focused
                && FocusRoutingHelper.IsDescendantOf(focused, child);
            if (DropDownPopup.IsOpen)
            {
                closingSubmenuPopup = DropDownPopup;
                DropDownPopup.IsOpen = false;
            }
            if (shouldRestoreFocus)
            {
                Focus(FocusState.Keyboard);
            }
        }
#endif
    }

    private void OnCompatibilitySubmenuOpened(object? sender, object args)
    {
#if WINDOWS
        OnNativeSubmenuOpened(sender);
#else
        if (!ReferenceEquals(sender, DropDownPopup) || !IsDropDownOpen || DropDownPopup?.IsOpen != true)
        {
            return;
        }

        UpdateSubmenuPosition();
        if (!submenuOpenedNotified)
        {
            submenuOpenedNotified = true;
            DropDownOpened?.Invoke(this, EventArgs.Empty);
        }

        if (focusFirstSubmenuItemWhenOpened)
        {
            FocusFirstEnabledSubmenuItem();
        }
#endif
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
