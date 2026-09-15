namespace Fluent;

public partial class MenuItem
{
    /// <summary>Invokes the WPF-compatible primary action.</summary>
    protected virtual void OnClick()
    {
        if (HasSubItems && IsSplit is false)
        {
            if (CanOpenSubmenu)
            {
                IsDropDownOpen = true;
            }

            return;
        }

        if (!CanInvoke)
        {
            return;
        }

        if (IsCheckable)
        {
            if (string.IsNullOrEmpty(GroupName))
            {
                IsChecked = IsChecked is not true;
            }
            else
            {
                IsChecked = true;
            }
        }

        InvokeItem();
        RaiseInvokedAutomationEvent();

        if (IsDefinitive)
        {
            IsDropDownOpen = false;
            PopupService.RaiseDismissPopupEvent(this, DismissPopupMode.Always);
        }
    }

    private void UncheckGroupPeers()
    {
        if (string.IsNullOrEmpty(GroupName))
        {
            return;
        }

        foreach (var peer in GetSiblingMenuItems())
        {
            if (!ReferenceEquals(peer, this)
                && peer.IsCheckable
                && string.Equals(
                    peer.GroupName,
                    GroupName,
                    StringComparison.Ordinal))
            {
                peer.IsChecked = false;
            }
        }
    }
}
