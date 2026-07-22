namespace Fluent.Modern.Automation;

using Fluent.Modern.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

/// <summary>
/// <para><b>Modern extension</b> — exposes <see cref="RibbonSearchBox"/> to UI Automation.</para>
/// </summary>
[ModernExtension]
public partial class RibbonSearchBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonSearchBoxAutomationPeer"/> class.
    /// </summary>
    /// <param name="owner">The owning search box.</param>
    public RibbonSearchBoxAutomationPeer(RibbonSearchBox owner)
        : base(owner)
    {
    }

    private RibbonSearchBox OwnerSearchBox => (RibbonSearchBox)Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Edit;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonSearchBox);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return string.IsNullOrWhiteSpace(OwnerSearchBox.PlaceholderText)
            ? "Ribbon search"
            : OwnerSearchBox.PlaceholderText;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface == PatternInterface.Value
            ? this
            : base.GetPatternCore(patternInterface);
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public bool IsReadOnly => OwnerSearchBox.IsAutomationReadOnly;

    /// <inheritdoc/>
    public string Value => OwnerSearchBox.AutomationValue;

    /// <inheritdoc/>
    public void SetValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        OwnerSearchBox.SetAutomationValue(value);
    }
}
