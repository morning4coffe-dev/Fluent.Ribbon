namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes <see cref="Ribbon"/> as an expandable ribbon region.
/// </summary>
public partial class RibbonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonAutomationPeer"/> class.
    /// </summary>
    public RibbonAutomationPeer(Ribbon owner)
        : base(owner)
    {
    }

    private Ribbon OwnerRibbon => (Ribbon)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(Ribbon);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string GetLocalizedControlTypeCore() => "ribbon";

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? "Ribbon" : name;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ExpandCollapse
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Collapse()
    {
        if (OwnerRibbon.CanMinimize)
        {
            OwnerRibbon.IsMinimized = true;
        }
    }

    /// <inheritdoc/>
    public void Expand()
    {
        if (OwnerRibbon.CanMinimize)
        {
            OwnerRibbon.IsMinimized = false;
        }
    }

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerRibbon.IsMinimized
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded;

    /// <inheritdoc/>
    protected override bool IsOffscreenCore()
        => OwnerRibbon.IsCollapsed || base.IsOffscreenCore();

    /// <summary>
    /// Creates the automation peer for the ribbon's application menu.
    /// </summary>
    protected virtual AutomationPeer? CreatePeerForMenu()
        => OwnerRibbon.Menu is FrameworkElement menu ? CreatePeerForElement(menu) : null;

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        if (CreatePeerForMenu() is { } menuPeer)
        {
            peers.Add(menuPeer);
        }

        foreach (var element in new FrameworkElement?[]
                 {
                     OwnerRibbon.QuickAccessToolBar,
                     OwnerRibbon.TabControl,
                     OwnerRibbon.StartScreen
                 })
        {
            if (element is not null && CreatePeerForElement(element) is { } peer)
            {
                peers.Add(peer);
            }
        }

        return peers;
    }
}

/// <summary>
/// Exposes <see cref="Backstage"/> as an expandable application menu.
/// </summary>
public partial class RibbonBackstageAutomationPeer : RibbonControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonBackstageAutomationPeer"/> class.
    /// </summary>
    public RibbonBackstageAutomationPeer(Backstage owner)
        : base(owner)
    {
    }

    private Backstage OwnerBackstage => (Backstage)Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Menu;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerBackstage.Header)
            : name;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ExpandCollapse
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Collapse() => OwnerBackstage.IsOpen = false;

    /// <inheritdoc/>
    public void Expand() => OwnerBackstage.IsOpen = true;

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerBackstage.IsOpen
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        if (OwnerBackstage.Content is not null
            && CreatePeerForElement(OwnerBackstage.Content) is { } peer)
        {
            peers.Add(peer);
        }

        return peers;
    }
}

/// <summary>
/// Exposes <see cref="BackstageTabControl"/> as a single-selection tab control.
/// </summary>
public partial class RibbonBackstageTabControlAutomationPeer :
    SelectorAutomationPeer,
    ISelectionProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonBackstageTabControlAutomationPeer"/> class.
    /// </summary>
    public RibbonBackstageTabControlAutomationPeer(BackstageTabControl owner)
        : base(owner)
    {
    }

    private BackstageTabControl OwnerTabControl => (BackstageTabControl)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(BackstageTabControl);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Tab;

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Selection
            ? this
            : base.GetPatternCore(patternInterface);

    /// <summary>Creates an item peer for a backstage item.</summary>
    protected new virtual ItemAutomationPeer CreateItemAutomationPeer(object item)
        => new SelectorItemAutomationPeer(item, this);

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        foreach (var item in OwnerTabControl.Items)
        {
            if (item is UIElement element
                && CreatePeerForElement(element) is { } peer)
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

    bool ISelectionProvider.CanSelectMultiple => false;

    bool ISelectionProvider.IsSelectionRequired => true;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        var selectedItem = OwnerTabControl.SelectedItem as BackstageTabItem
                           ?? OwnerTabControl.ContainerFromItem(
                               OwnerTabControl.SelectedItem) as BackstageTabItem;
        if (selectedItem is null)
        {
            return [];
        }

        var peer = CreatePeerForElement(selectedItem);
        return peer is null ? [] : [ProviderFromPeer(peer)];
    }

}

/// <summary>
/// Exposes <see cref="BackstageTabItem"/> as an invokable, selectable tab item.
/// </summary>
public partial class RibbonBackstageTabItemAutomationPeer : FrameworkElementAutomationPeer,
    IInvokeProvider,
    ISelectionItemProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonBackstageTabItemAutomationPeer"/> class.
    /// </summary>
    public RibbonBackstageTabItemAutomationPeer(BackstageTabItem owner)
        : base(owner)
    {
    }

    private BackstageTabItem OwnerTabItem => (BackstageTabItem)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(BackstageTabItem);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.TabItem;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerTabItem.Header)
            : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
    {
        var accessKey = base.GetAccessKeyCore();
        return string.IsNullOrWhiteSpace(accessKey) ? OwnerTabItem.KeyTip ?? string.Empty : accessKey;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.Invoke => this,
            PatternInterface.SelectionItem => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc/>
    public void Invoke()
    {
        Select();
    }

    /// <inheritdoc/>
    public void AddToSelection() => Select();

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        // Backstage tab controls require one selected item.
    }

    /// <inheritdoc/>
    public void Select()
    {
        if (AutomationPeerHelpers.FindAncestor<BackstageTabControl>(OwnerTabItem) is { } tabControl)
        {
            tabControl.SelectTabForAutomation(OwnerTabItem);
        }
        else if (AutomationPeerHelpers.FindAncestor<StartScreenTabControl>(OwnerTabItem)
                 is { } startScreenTabControl)
        {
            startScreenTabControl.SelectTabForAutomation(OwnerTabItem);
        }
        else
        {
            OwnerTabItem.IsSelected = true;
        }
    }

    /// <inheritdoc/>
    public bool IsSelected => OwnerTabItem.IsSelected;

    /// <inheritdoc/>
    public IRawElementProviderSimple? SelectionContainer
    {
        get
        {
            FrameworkElement? container =
                AutomationPeerHelpers.FindAncestor<BackstageTabControl>(OwnerTabItem);
            container ??= AutomationPeerHelpers.FindAncestor<StartScreenTabControl>(OwnerTabItem);
            var peer = container is null ? null : CreatePeerForElement(container);
            return peer is null ? null : ProviderFromPeer(peer);
        }
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        foreach (var element in new object?[]
                 {
                     OwnerTabItem.Header,
                     OwnerTabItem.Content
                 }.OfType<UIElement>())
        {
            if (CreatePeerForElement(element) is { } peer)
            {
                peers.Add(peer);
            }
        }

        return peers;
    }
}
