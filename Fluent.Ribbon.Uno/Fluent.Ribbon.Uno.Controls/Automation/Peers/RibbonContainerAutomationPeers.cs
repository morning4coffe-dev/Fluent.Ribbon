namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Exposes <see cref="RibbonToolBar"/> with toolbar semantics.
/// </summary>
public partial class RibbonToolBarAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonToolBarAutomationPeer"/> class.
    /// </summary>
    public RibbonToolBarAutomationPeer(RibbonToolBar owner)
        : base(owner)
    {
    }

    private RibbonToolBar OwnerToolBar => (RibbonToolBar)Owner;

    protected override string GetClassNameCore() => nameof(RibbonToolBar);

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ToolBar;

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        name = AutomationPeerHelpers.GetHeaderName(OwnerToolBar);
        return string.IsNullOrWhiteSpace(name)
            ? global::Fluent.RibbonLocalization.Current.Localization.RibbonToolBarName
            : name;
    }

    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        AddPeersOrDescendants(OwnerToolBar.AutomationContentRoot, peers);
        return peers;
    }

    private void AddPeersOrDescendants(
        DependencyObject? element,
        ICollection<AutomationPeer> peers)
    {
        if (element is null)
        {
            return;
        }

        if (element is FrameworkElement frameworkElement
            && !AutomationPeerHelpers.IsEffectivelyVisible(frameworkElement))
        {
            return;
        }

        if (element is UIElement uiElement
            && !ReferenceEquals(uiElement, OwnerToolBar)
            && CreatePeerForElement(uiElement) is { } peer)
        {
            peers.Add(peer);
            return;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            AddPeersOrDescendants(VisualTreeHelper.GetChild(element, index), peers);
        }
    }
}

/// <summary>
/// Exposes <see cref="RibbonMenu"/> with menu and grouped-selection semantics.
/// </summary>
public partial class RibbonMenuAutomationPeer : ItemsControlAutomationPeer, ISelectionProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonMenuAutomationPeer"/> class.
    /// </summary>
    public RibbonMenuAutomationPeer(RibbonMenu owner)
        : base(owner)
    {
    }

    private RibbonMenu OwnerMenu => (RibbonMenu)Owner;

    private IReadOnlyList<MenuItem> GroupedItems
        => OwnerMenu.Items
            .OfType<MenuItem>()
            .Where(item => item.IsCheckable && !string.IsNullOrEmpty(item.GroupName))
            .ToList();

    protected override string GetClassNameCore() => nameof(RibbonMenu);

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Menu;

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? global::Fluent.RibbonLocalization.Current.Localization.RibbonMenuName
            : name;
    }

    protected override object GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Selection && GroupedItems.Count > 0
            ? this
            : base.GetPatternCore(patternInterface);

    protected override List<AutomationPeer> GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        foreach (var item in OwnerMenu.Items)
        {
            if (AutomationPeerHelpers.IsEffectivelyVisible(item)
                && CreatePeerForElement(item) is { } peer)
            {
                peers.Add(peer);
            }
        }

        return peers;
    }

    bool ISelectionProvider.CanSelectMultiple
        => GroupedItems
            .Select(item => item.GroupName)
            .Distinct(StringComparer.Ordinal)
            .Skip(1)
            .Any();

    bool ISelectionProvider.IsSelectionRequired => true;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        => GroupedItems
            .Where(item => item.IsChecked is true)
            .Select(
                item => CreatePeerForElement(item)
                        ?? new RibbonMenuItemAutomationPeer(item))
            .Select(ProviderFromPeer)
            .ToArray();
}
