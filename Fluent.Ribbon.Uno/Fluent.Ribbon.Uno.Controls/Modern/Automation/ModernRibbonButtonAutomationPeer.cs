namespace Fluent.Modern.Automation;

using Fluent.Modern.Controls;
using Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// <para><b>Modern extension</b> — exposes <see cref="ModernRibbonButton"/> to UI Automation.</para>
/// </summary>
[ModernExtension]
public partial class ModernRibbonButtonAutomationPeer : ButtonAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModernRibbonButtonAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The owning modern ribbon button.</param>
    public ModernRibbonButtonAutomationPeer(ModernRibbonButton owner)
        : base(owner)
    {
    }

    private ModernRibbonButton OwnerButton => (ModernRibbonButton)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(ModernRibbonButton);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(
            OwnerButton.Header);
    }
}
