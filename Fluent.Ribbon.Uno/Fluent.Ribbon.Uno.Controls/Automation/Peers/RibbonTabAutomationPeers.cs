namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Windows.Foundation;

/// <summary>
/// Automation peer for <see cref="RibbonTabControl"/>.
/// </summary>
public partial class RibbonTabControlAutomationPeer : TabViewAutomationPeer, ISelectionProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTabControlAutomationPeer"/> class.
    /// </summary>
    public RibbonTabControlAutomationPeer(RibbonTabControl owner)
        : base(owner)
    {
    }

    internal RibbonTabControl OwnerTabControl => (RibbonTabControl)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonTabControl);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Tab;

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Selection
            ? this
            : base.GetPatternCore(patternInterface);

    /// <summary>Creates a portable data peer for a tab item.</summary>
    protected virtual AutomationPeer CreateItemAutomationPeer(object item)
        => new RibbonTabItemDataAutomationPeer(item, this);

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        foreach (var item in OwnerTabControl.TabItems)
        {
            if (item is UIElement element && CreatePeerForElement(element) is { } peer)
            {
                peers.Add(peer);
            }
            else
            {
                peers.Add(CreateItemAutomationPeer(item));
            }
        }

        return peers;
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public bool CanSelectMultiple => false;

    /// <inheritdoc/>
    public bool IsSelectionRequired => true;

    /// <inheritdoc/>
    public IRawElementProviderSimple[] GetSelection()
    {
        if (OwnerTabControl.SelectedItem is not FrameworkElement selectedItem)
        {
            return [];
        }

        var peer = CreatePeerForElement(selectedItem);
        return peer is null ? [] : [ProviderFromPeer(peer)];
    }

    /// <inheritdoc/>
    protected override Point GetClickablePointCore()
        => new(double.NaN, double.NaN);
}

/// <summary>
/// Automation peer for a realized <see cref="RibbonTabItem"/>.
/// </summary>
public partial class RibbonTabItemAutomationPeer : FrameworkElementAutomationPeer,
    IScrollItemProvider,
    ISelectionItemProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTabItemAutomationPeer"/> class.
    /// </summary>
    public RibbonTabItemAutomationPeer(RibbonTabItem owner)
        : base(owner)
    {
    }

    private RibbonTabItem OwnerTab => (RibbonTabItem)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonTabItem";

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.TabItem;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerTab.Header)
            : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
    {
        var accessKey = base.GetAccessKeyCore();
        return string.IsNullOrWhiteSpace(accessKey) ? OwnerTab.KeyTip : accessKey;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ScrollItem => this,
            PatternInterface.SelectionItem => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        if (OwnerTab.Header is UIElement header
            && CreatePeerForElement(header) is { } headerPeer)
        {
            peers.Add(headerPeer);
        }

        if (OwnerTab.IsSelected)
        {
            foreach (var group in OwnerTab.Groups)
            {
                if (CreatePeerForElement(group) is { } groupPeer)
                {
                    peers.Add(groupPeer);
                }
            }
        }

        return peers;
    }

    /// <inheritdoc/>
    public void ScrollIntoView() => OwnerTab.StartBringIntoView();

    /// <inheritdoc/>
    public void AddToSelection() => Select();

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        // Ribbon tab controls require one selected tab.
    }

    /// <inheritdoc/>
    public void Select()
    {
        if (AutomationPeerHelpers.FindAncestor<RibbonTabControl>(OwnerTab) is { } tabControl)
        {
            tabControl.SelectedItem = OwnerTab;
        }
        else
        {
            OwnerTab.IsSelected = true;
        }
    }

    /// <inheritdoc/>
    public bool IsSelected => OwnerTab.IsSelected;

    /// <inheritdoc/>
    public IRawElementProviderSimple? SelectionContainer
    {
        get
        {
            var tabControl = AutomationPeerHelpers.FindAncestor<RibbonTabControl>(OwnerTab);
            var peer = tabControl is null ? null : CreatePeerForElement(tabControl);
            return peer is null ? null : ProviderFromPeer(peer);
        }
    }
}

/// <summary>
/// Portable data-item peer for ribbon tabs.
/// </summary>
/// <remarks>
/// WinUI's <see cref="Microsoft.UI.Xaml.Controls.TabView"/> is not a Selector, so this peer cannot
/// derive from <see cref="SelectorItemAutomationPeer"/> as the WPF peer does. It retains the same
/// tab-selection, scroll-item, and expand/collapse behavior over the Uno tab model.
/// </remarks>
public partial class RibbonTabItemDataAutomationPeer : AutomationPeer,
    IExpandCollapseProvider,
    IScrollItemProvider,
    ISelectionItemProvider
{
    private readonly object _item;
    private readonly RibbonTabControlAutomationPeer _tabControlAutomationPeer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTabItemDataAutomationPeer"/> class.
    /// </summary>
    public RibbonTabItemDataAutomationPeer(
        object item,
        RibbonTabControlAutomationPeer tabControlAutomationPeer)
    {
        _item = item;
        _tabControlAutomationPeer = tabControlAutomationPeer;
    }

    private RibbonTabItem? WrapperTab => _item as RibbonTabItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonTabItem";

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.TabItem;

    /// <inheritdoc/>
    protected override string GetNameCore()
        => WrapperTab is null
            ? AutomationPeerHelpers.GetObjectName(_item)
            : AutomationPeerHelpers.GetObjectName(WrapperTab.Header);

    /// <inheritdoc/>
    protected override string GetAccessKeyCore() => WrapperTab?.KeyTip ?? string.Empty;

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ExpandCollapse => this,
            PatternInterface.ScrollItem => this,
            PatternInterface.SelectionItem => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        var tabControl = _tabControlAutomationPeer.OwnerTabControl;
        if (tabControl.IsMinimized)
        {
            tabControl.IsDropDownOpen = false;
        }
    }

    /// <inheritdoc/>
    public void Expand()
    {
        if (WrapperTab is not null)
        {
            _tabControlAutomationPeer.OwnerTabControl.SelectedItem = WrapperTab;
        }

        var tabControl = _tabControlAutomationPeer.OwnerTabControl;
        if (tabControl.IsMinimized)
        {
            tabControl.IsDropDownOpen = true;
        }
    }

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
    {
        get
        {
            var tabControl = _tabControlAutomationPeer.OwnerTabControl;
            if (!tabControl.IsMinimized)
            {
                return Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded;
            }

            return IsSelected && tabControl.IsDropDownOpen
                ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded
                : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed;
        }
    }

    /// <inheritdoc/>
    public void ScrollIntoView() => WrapperTab?.StartBringIntoView();

    /// <inheritdoc/>
    public void AddToSelection() => Select();

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        // Ribbon tab controls require one selected tab.
    }

    /// <inheritdoc/>
    public void Select()
    {
        if (WrapperTab is not null)
        {
            _tabControlAutomationPeer.OwnerTabControl.SelectedItem = WrapperTab;
        }
    }

    /// <inheritdoc/>
    public bool IsSelected
        => WrapperTab is not null
           && ReferenceEquals(_tabControlAutomationPeer.OwnerTabControl.SelectedItem, WrapperTab);

    /// <inheritdoc/>
    public IRawElementProviderSimple SelectionContainer
        => ProviderFromPeer(_tabControlAutomationPeer);
}
