using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Fluent;

/// <summary>
/// Exposes <see cref="MenuItem"/> to UI Automation. Because <see cref="MenuItem"/>
/// derives from a bare <see cref="Control"/> (which provides no automation peer by default),
/// assistive technologies would otherwise see the File-menu items as unnamed, untyped, and
/// non-invokable. This peer reports the item as a MenuItem, surfaces its <c>Header</c> as the
/// accessible name and <c>Description</c> as help text, and implements the Invoke pattern.
/// </summary>
public partial class RibbonMenuItemAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonMenuItemAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The owning menu item.</param>
    public RibbonMenuItemAutomationPeer(MenuItem owner)
        : base(owner)
    {
    }

    private MenuItem OwnerItem => (MenuItem)Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.MenuItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MenuItem);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        return OwnerItem.Header?.ToString() ?? string.Empty;
    }

    /// <inheritdoc/>
    protected override string GetHelpTextCore()
    {
        var help = base.GetHelpTextCore();
        return string.IsNullOrEmpty(help) ? OwnerItem.Description ?? string.Empty : help;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.Invoke)
        {
            return this;
        }

        return base.GetPatternCore(patternInterface);
    }

    /// <inheritdoc/>
    public void Invoke() => OwnerItem.InvokeFromAutomation();
}
