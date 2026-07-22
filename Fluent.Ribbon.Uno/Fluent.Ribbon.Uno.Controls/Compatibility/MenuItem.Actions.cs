namespace Fluent;

using Microsoft.UI.Xaml.Automation.Provider;

public partial class MenuItem
{
    /// <inheritdoc />
    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        return new MenuItem
        {
            Header = Header,
            Description = Description,
            Icon = Icon,
            Command = Command,
            CommandParameter = CommandParameter,
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false,
            IsCheckable = IsCheckable,
            IsChecked = IsChecked,
            GroupName = GroupName,
            IsDefinitive = IsDefinitive
        };
    }

    /// <inheritdoc />
    public virtual KeyTipPressedResult OnKeyTipPressed()
    {
        if (HasSubItems)
        {
            IsDropDownOpen = true;
            Focus(FocusState.Programmatic);
            return new KeyTipPressedResult(true, true);
        }

        ((IInvokeProvider)new RibbonMenuItemAutomationPeer(this)).Invoke();
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
