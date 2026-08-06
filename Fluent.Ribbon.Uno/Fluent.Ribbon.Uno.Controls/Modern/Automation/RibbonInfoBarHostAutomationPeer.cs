namespace Fluent.Modern.Automation;

using Fluent.Modern.Controls;
using Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// <para><b>Modern extension</b> — exposes <see cref="RibbonInfoBarHost"/> to UI Automation.</para>
/// </summary>
[ModernExtension]
public partial class RibbonInfoBarHostAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonInfoBarHostAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The owning InfoBar host.</param>
    public RibbonInfoBarHostAutomationPeer(RibbonInfoBarHost owner)
        : base(owner)
    {
    }

    private RibbonInfoBarHost OwnerInfoBarHost => (RibbonInfoBarHost)Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonInfoBarHost);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (!string.IsNullOrWhiteSpace(OwnerInfoBarHost.Title))
        {
            return OwnerInfoBarHost.Title;
        }

        return string.IsNullOrWhiteSpace(OwnerInfoBarHost.Message)
            ? RibbonLocalization.Current.Localization.RibbonNotificationName
            : OwnerInfoBarHost.Message;
    }
}
