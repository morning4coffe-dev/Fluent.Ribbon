namespace Fluent;

public partial class MenuItem
{
    /// <inheritdoc />
    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        FrameworkElement clone;
        if (HasSubItems)
        {
            clone = IsSplit
                ? new RibbonSplitButton { Size = RibbonControlSize.Small, CanAddToQuickAccessToolBar = false }
                : new DropDownButton { Size = RibbonControlSize.Small, CanAddToQuickAccessToolBar = false };
        }
        else
        {
            clone = IsCheckable
                ? new RibbonToggleButton { Size = RibbonControlSize.Small, CanAddToQuickAccessToolBar = false }
                : new Button { Size = RibbonControlSize.Small, CanAddToQuickAccessToolBar = false };
        }

        BindMenuQuickAccessItem(clone);
        return clone;
    }

    /// <inheritdoc />
    public virtual KeyTipPressedResult OnKeyTipPressed()
    {
        if (HasSubItems)
        {
            if (!CanOpenSubmenu)
            {
                return KeyTipPressedResult.Empty;
            }

            OpenSubmenuAndFocusFirstItem();
            return new KeyTipPressedResult(true, true);
        }

        if (CanInvoke)
        {
            OnClick();
        }

        return KeyTipPressedResult.Empty;
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
        IsDropDownOpen = false;
    }

    /// <summary>Handles a quick-access clone opening.</summary>
    protected void OnQuickAccessOpened(object? sender, EventArgs e)
    {
        if (sender is not MenuItem item)
        {
            return;
        }

        item.DropDownClosed -= OnQuickAccessMenuClosedOrUnloaded;
        item.Unloaded -= OnQuickAccessMenuClosedOrUnloaded;
        item.DropDownClosed += OnQuickAccessMenuClosedOrUnloaded;
        item.Unloaded += OnQuickAccessMenuClosedOrUnloaded;
    }

    /// <summary>Detaches quick-access clone lifecycle forwarding.</summary>
    protected void OnQuickAccessMenuClosedOrUnloaded(object? sender, EventArgs e)
    {
        DetachQuickAccessMenu(sender);
    }

#if WINDOWS
    private void OnQuickAccessMenuClosedOrUnloaded(object? sender, RoutedEventArgs e)
    {
        DetachQuickAccessMenu(sender);
    }
#endif

    private void DetachQuickAccessMenu(object? sender)
    {
        if (sender is not MenuItem item)
        {
            return;
        }

        item.DropDownClosed -= OnQuickAccessMenuClosedOrUnloaded;
        item.Unloaded -= OnQuickAccessMenuClosedOrUnloaded;
    }

    string? IKeyTipedControl.KeyTip
    {
        get => KeyTip;
        set => KeyTip = value ?? string.Empty;
    }
}
