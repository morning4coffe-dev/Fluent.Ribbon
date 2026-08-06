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
    private readonly List<RibbonTabItemDataAutomationPeer?> itemPeers = [];

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
        => GetDataPeer(item);

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        foreach (var item in OwnerTabControl.TabItems)
        {
            if (item is FrameworkElement element)
            {
                if (AutomationPeerHelpers.IsEffectivelyVisible(element)
                    && CreatePeerForElement(element) is { } peer)
                {
                    peers.Add(peer);
                }
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
        if (OwnerTabControl.SelectedItem is not { } selectedItem)
        {
            return [];
        }

        var peer = selectedItem is FrameworkElement element
            ? CreatePeerForElement(element)
            : GetDataPeer(selectedItem);
        return peer is null ? [] : [ProviderFromPeer(peer)];
    }

    internal void RaiseSelectionChanged(
        object? oldItem,
        object? newItem,
        bool wasDropDownOpen)
    {
        if (ReferenceEquals(oldItem, newItem))
        {
            return;
        }

        InvalidatePeer();
        RaiseDataItemSelectionChanged(oldItem, true, false);
        RaiseDataItemSelectionChanged(newItem, false, true);

        if (OwnerTabControl.IsMinimized && wasDropDownOpen)
        {
            RaiseItemExpandCollapseChanged(
                oldItem,
                ExpandCollapseState.Expanded,
                ExpandCollapseState.Collapsed);
            RaiseItemExpandCollapseChanged(
                newItem,
                ExpandCollapseState.Collapsed,
                ExpandCollapseState.Expanded);
        }

    }

    internal void RaiseSelectedTabExpandCollapseChanged(
        ExpandCollapseState oldValue,
        ExpandCollapseState newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaiseItemExpandCollapseChanged(
            OwnerTabControl.SelectedItem,
            oldValue,
            newValue);
    }

    private RibbonTabItemDataAutomationPeer GetDataPeer(object item)
    {
        for (var index = 0; index < itemPeers.Count; index++)
        {
            if (itemPeers[index] is { } peer
                && ReferenceEquals(peer.Item, item))
            {
                return peer;
            }
        }

        var newPeer = new RibbonTabItemDataAutomationPeer(item, this);
        itemPeers.Add(newPeer);
        return newPeer;
    }

    private void RaiseDataItemSelectionChanged(
        object? item,
        bool oldValue,
        bool newValue)
    {
        if (item is null || item is UIElement)
        {
            return;
        }

        for (var index = 0; index < itemPeers.Count; index++)
        {
            if (itemPeers[index] is { } peer
                && ReferenceEquals(peer.Item, item))
            {
                peer.RaiseIsSelectedChanged(oldValue, newValue);
                return;
            }
        }
    }

    private void RaiseItemExpandCollapseChanged(
        object? item,
        ExpandCollapseState oldValue,
        ExpandCollapseState newValue)
    {
        switch (item)
        {
            case FrameworkElement element
                when FrameworkElementAutomationPeer.FromElement(element)
                     is RibbonTabItemAutomationPeer peer:
                peer.RaiseExpandCollapseStateChanged(oldValue, newValue);
                break;
            case not null:
                for (var index = 0; index < itemPeers.Count; index++)
                {
                    if (itemPeers[index] is { } dataPeer
                        && ReferenceEquals(dataPeer.Item, item))
                    {
                        dataPeer.RaiseExpandCollapseStateChanged(oldValue, newValue);
                        break;
                    }
                }

                break;
        }
    }

    /// <inheritdoc/>
    protected override Point GetClickablePointCore()
        => new(double.NaN, double.NaN);
}

/// <summary>
/// Automation peer for a realized <see cref="RibbonTabItem"/>.
/// </summary>
public partial class RibbonTabItemAutomationPeer : FrameworkElementAutomationPeer,
    IExpandCollapseProvider,
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
            PatternInterface.ExpandCollapse
                when GetOwningTabControlOrNull()?.CanMinimize == true => this,
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
                if (AutomationPeerHelpers.IsEffectivelyVisible(group)
                    && CreatePeerForElement(group) is { } groupPeer)
                {
                    peers.Add(groupPeer);
                }
            }
        }

        return peers;
    }

    /// <inheritdoc/>
    public void ScrollIntoView()
    {
        GetOwningTabControl();
        OwnerTab.StartBringIntoView();
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        var tabControl = GetOwningTabControl();
        AutomationProviderGuard.EnsureAvailable(
            tabControl.CanMinimize,
            "The ribbon tab cannot be collapsed.");
        if (tabControl.IsMinimized && IsSelected)
        {
            tabControl.IsDropDownOpen = false;
        }
    }

    /// <inheritdoc/>
    public void Expand()
    {
        var tabControl = GetOwningTabControl();
        AutomationProviderGuard.EnsureAvailable(
            tabControl.CanMinimize,
            "The ribbon tab cannot be expanded.");
        tabControl.SelectedItem = OwnerTab;
        if (tabControl.IsMinimized)
        {
            tabControl.IsDropDownOpen = true;
        }
    }

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState
    {
        get
        {
            var tabControl = GetOwningTabControlOrNull();
            if (tabControl?.IsMinimized != true)
            {
                return ExpandCollapseState.Expanded;
            }

            return IsSelected && tabControl.IsDropDownOpen
                ? ExpandCollapseState.Expanded
                : ExpandCollapseState.Collapsed;
        }
    }

    /// <inheritdoc/>
    public void AddToSelection()
    {
        var tabControl = GetOwningTabControl();
        if (IsSelected)
        {
            return;
        }

        if (tabControl.SelectedItem is not null)
        {
            throw new InvalidOperationException(
                "The ribbon tab control supports only one selected item.");
        }

        Select();
    }

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        GetOwningTabControl();
        if (IsSelected)
        {
            throw new InvalidOperationException(
                "The ribbon tab control requires one selected item.");
        }
    }

    /// <inheritdoc/>
    public void Select()
    {
        var tabControl = GetOwningTabControl();
        tabControl.SelectedItem = OwnerTab;
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

    internal void RaiseIsSelectedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        InvalidatePeer();
        foreach (var group in OwnerTab.Groups)
        {
            FrameworkElementAutomationPeer.FromElement(group)?.InvalidatePeer();
        }

        RaisePropertyChangedEvent(
            SelectionItemPatternIdentifiers.IsSelectedProperty,
            oldValue,
            newValue);
        RaiseAutomationEvent(
            newValue
                ? AutomationEvents.SelectionItemPatternOnElementSelected
                : AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
    }

    internal void RaiseExpandCollapseStateChanged(
        ExpandCollapseState oldValue,
        ExpandCollapseState newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue,
            newValue);
    }

    private RibbonTabControl GetOwningTabControl()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        var tabControl = AutomationPeerHelpers.FindAncestor<RibbonTabControl>(OwnerTab);
        AutomationProviderGuard.EnsureAvailable(
            tabControl is not null && tabControl.TabItems.Contains(OwnerTab),
            "The ribbon tab item is not available in its tab control.");
        return tabControl!;
    }

    private RibbonTabControl? GetOwningTabControlOrNull()
        => AutomationPeerHelpers.FindAncestor<RibbonTabControl>(OwnerTab);
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

    internal object Item => _item;

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
    protected override string GetAutomationIdCore()
        => WrapperTab is null
            ? string.Empty
            : AutomationProperties.GetAutomationId(WrapperTab);

    /// <inheritdoc/>
    protected override string GetAccessKeyCore() => WrapperTab?.KeyTip ?? string.Empty;

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ExpandCollapse
                when _tabControlAutomationPeer.OwnerTabControl.CanMinimize => this,
            PatternInterface.ScrollItem when WrapperTab is not null => this,
            PatternInterface.SelectionItem => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        ValidateItemAction();
        var tabControl = _tabControlAutomationPeer.OwnerTabControl;
        AutomationProviderGuard.EnsureAvailable(
            tabControl.CanMinimize,
            "The ribbon tab cannot be collapsed.");
        if (tabControl.IsMinimized)
        {
            tabControl.IsDropDownOpen = false;
        }
    }

    /// <inheritdoc/>
    public void Expand()
    {
        ValidateItemAction();
        var tabControl = _tabControlAutomationPeer.OwnerTabControl;
        AutomationProviderGuard.EnsureAvailable(
            tabControl.CanMinimize,
            "The ribbon tab cannot be expanded.");
        tabControl.SelectedItem = _item;
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
    public void ScrollIntoView()
    {
        ValidateItemAction();
        WrapperTab!.StartBringIntoView();
    }

    /// <inheritdoc/>
    public void AddToSelection()
    {
        ValidateItemAction();
        if (IsSelected)
        {
            return;
        }

        if (_tabControlAutomationPeer.OwnerTabControl.SelectedItem is not null)
        {
            throw new InvalidOperationException(
                "The ribbon tab control supports only one selected item.");
        }

        Select();
    }

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        ValidateItemAction();
        if (IsSelected)
        {
            throw new InvalidOperationException(
                "The ribbon tab control requires one selected item.");
        }
    }

    /// <inheritdoc/>
    public void Select()
    {
        ValidateItemAction();
        _tabControlAutomationPeer.OwnerTabControl.SelectedItem = _item;
    }

    /// <inheritdoc/>
    public bool IsSelected
        => ReferenceEquals(_tabControlAutomationPeer.OwnerTabControl.SelectedItem, _item);

    /// <inheritdoc/>
    public IRawElementProviderSimple SelectionContainer
        => ProviderFromPeer(_tabControlAutomationPeer);

    internal void RaiseIsSelectedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            SelectionItemPatternIdentifiers.IsSelectedProperty,
            oldValue,
            newValue);
        RaiseAutomationEvent(
            newValue
                ? AutomationEvents.SelectionItemPatternOnElementSelected
                : AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
    }

    internal void RaiseExpandCollapseStateChanged(
        ExpandCollapseState oldValue,
        ExpandCollapseState newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue,
            newValue);
    }

    private void ValidateItemAction()
    {
        AutomationProviderGuard.EnsureEnabled(_tabControlAutomationPeer);
        AutomationProviderGuard.EnsureAvailable(
            _tabControlAutomationPeer.OwnerTabControl.TabItems.Contains(_item),
            "The ribbon tab item is not available.");
        if (_item is Control control)
        {
            AutomationProviderGuard.EnsureEnabled(control.IsEnabled);
        }
    }
}
