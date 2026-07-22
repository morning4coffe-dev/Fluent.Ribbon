namespace Fluent;

using Microsoft.UI.Xaml.Automation.Peers;

public partial class Ribbon
{
    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonAutomationPeer(this);
}
