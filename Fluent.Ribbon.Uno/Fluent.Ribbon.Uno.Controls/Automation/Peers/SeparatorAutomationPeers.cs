namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes a menu group label without actionable menu-item patterns.
/// </summary>
internal sealed partial class GroupSeparatorMenuItemAutomationPeer : FrameworkElementAutomationPeer
{
    internal GroupSeparatorMenuItemAutomationPeer(GroupSeparatorMenuItem owner)
        : base(owner)
    {
    }

    private GroupSeparatorMenuItem OwnerSeparator => (GroupSeparatorMenuItem)Owner;

    protected override string GetClassNameCore() => nameof(GroupSeparatorMenuItem);

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Header;

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? OwnerSeparator.Header
            : name;
    }
}

/// <summary>
/// Keeps a decorative tab separator out of the control and content views.
/// </summary>
internal sealed partial class SeparatorTabItemAutomationPeer : FrameworkElementAutomationPeer
{
    internal SeparatorTabItemAutomationPeer(SeparatorTabItem owner)
        : base(owner)
    {
    }

    protected override string GetClassNameCore() => nameof(SeparatorTabItem);

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Separator;

    protected override bool IsControlElementCore() => false;

    protected override bool IsContentElementCore() => false;
}
