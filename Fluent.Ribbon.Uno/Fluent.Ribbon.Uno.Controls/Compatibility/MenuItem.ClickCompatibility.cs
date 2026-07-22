namespace Fluent;

public partial class MenuItem
{
    /// <summary>Invokes the WPF-compatible primary action.</summary>
    protected virtual void OnClick()
    {
        if (HasSubItems && IsSplit is false)
        {
            IsDropDownOpen = true;
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
                UncheckGroupPeers();
            }
        }

        InvokeItem();

        if (IsDefinitive)
        {
            PopupService.RaiseDismissPopupEvent(this, DismissPopupMode.Always);
        }
    }

    private void UncheckGroupPeers()
    {
        if (string.IsNullOrEmpty(GroupName))
        {
            return;
        }

        for (DependencyObject? current = VisualTreeHelper.GetParent(this);
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is not Panel panel)
            {
                continue;
            }

            foreach (var peer in panel.Children.OfType<MenuItem>())
            {
                if (!ReferenceEquals(peer, this)
                    && string.Equals(
                        peer.GroupName,
                        GroupName,
                        StringComparison.Ordinal))
                {
                    peer.IsChecked = false;
                }
            }

            return;
        }
    }
}
