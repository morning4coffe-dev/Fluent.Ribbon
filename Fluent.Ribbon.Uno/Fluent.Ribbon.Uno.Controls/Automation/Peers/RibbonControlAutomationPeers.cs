namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

/// <summary>
/// Base automation peer for ribbon controls that do not have a more specific framework peer.
/// </summary>
public partial class RibbonControlAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonControlAutomationPeer"/> class.
    /// </summary>
    public RibbonControlAutomationPeer(Control owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore() => Owner.GetType().Name;
}

/// <summary>
/// Automation peer for ribbon data items hosted by a WinUI items control.
/// </summary>
public partial class RibbonControlDataAutomationPeer : ItemAutomationPeer
{
    private readonly object _item;

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonControlDataAutomationPeer"/> class.
    /// </summary>
    public RibbonControlDataAutomationPeer(object item, ItemsControlAutomationPeer itemsControlPeer)
        : base(item, itemsControlPeer)
    {
        _item = item;
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ListItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore()
        => GetWrapperPeer()?.GetClassName() ?? _item.GetType().Name;

    /// <inheritdoc/>
    protected override string GetNameCore()
        => GetWrapperPeer()?.GetName() ?? AutomationPeerHelpers.GetObjectName(_item);

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => GetWrapperPeer()?.GetPattern(patternInterface) ?? base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    private AutomationPeer? GetWrapperPeer()
    {
        if (_item is not FrameworkElement element)
        {
            return null;
        }

        return FrameworkElementAutomationPeer.CreatePeerForElement(element)
               ?? new FrameworkElementAutomationPeer(element);
    }
}

/// <summary>
/// Base automation peer for controls that expose an <see cref="IHeaderedControl.Header"/>.
/// </summary>
public abstract partial class RibbonHeaderedControlAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonHeaderedControlAutomationPeer"/> class.
    /// </summary>
    protected RibbonHeaderedControlAutomationPeer(FrameworkElement owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore() => Owner.GetType().Name;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetHeaderName((FrameworkElement)Owner)
            : name;
    }

}

/// <summary>
/// Automation peer for <see cref="RibbonGroupBox"/>.
/// </summary>
public partial class RibbonGroupBoxAutomationPeer : FrameworkElementAutomationPeer,
    IExpandCollapseProvider,
    IScrollItemProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGroupBoxAutomationPeer"/> class.
    /// </summary>
    public RibbonGroupBoxAutomationPeer(RibbonGroupBox owner)
        : base(owner)
    {
    }

    private RibbonGroupBox OwnerGroup => (RibbonGroupBox)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonGroupBox);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerGroup.Header)
            : name;
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => OwnerGroup.IsInButtonState ? AutomationControlType.Button : AutomationControlType.Group;

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ExpandCollapse when OwnerGroup.IsInButtonState => this,
            PatternInterface.ScrollItem => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        foreach (var item in OwnerGroup.Items)
        {
            if (CreatePeerForElement(item) is { } peer)
            {
                peers.Add(peer);
            }
        }

        return peers;
    }

    /// <inheritdoc/>
    public void Collapse() => OwnerGroup.CollapseForAutomation();

    /// <inheritdoc/>
    public void Expand() => OwnerGroup.ExpandForAutomation();

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerGroup.IsDropDownOpen
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public void ScrollIntoView() => OwnerGroup.StartBringIntoView();

    /// <inheritdoc/>
    protected override void SetFocusCore()
    {
        // The group itself is a semantic container; focus remains on its interactive children.
    }
}

/// <summary>
/// Automation peer for a realized ribbon-group header.
/// </summary>
public partial class RibbonGroupHeaderAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGroupHeaderAutomationPeer"/> class.
    /// </summary>
    public RibbonGroupHeaderAutomationPeer(FrameworkElement owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Header;

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => Owner.GetType().Name;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        return AutomationPeerHelpers.FindAncestor<RibbonGroupBox>((FrameworkElement)Owner)?.Header?.ToString()
               ?? string.Empty;
    }
}

/// <summary>
/// Automation peer for <see cref="InRibbonGallery"/>.
/// </summary>
public partial class RibbonInRibbonGalleryAutomationPeer : SelectorAutomationPeer,
    IExpandCollapseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonInRibbonGalleryAutomationPeer"/> class.
    /// </summary>
    public RibbonInRibbonGalleryAutomationPeer(InRibbonGallery owner)
        : base(owner)
    {
    }

    private InRibbonGallery OwnerGallery => (InRibbonGallery)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(InRibbonGallery);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerGallery.Header)
            : name;
    }

    /// <summary>Creates an item peer for a gallery item.</summary>
    protected new virtual ItemAutomationPeer CreateItemAutomationPeer(object item)
        => new SelectorItemAutomationPeer(item, this);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.List;

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ExpandCollapse => this,
            PatternInterface.Selection => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Collapse() => OwnerGallery.CollapseForAutomation();

    /// <inheritdoc/>
    public void Expand() => OwnerGallery.ExpandForAutomation();

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerGallery.IsDropDownOpen
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed;

}

/// <summary>
/// Automation peer for <see cref="QuickAccessToolBar"/>.
/// </summary>
public partial class RibbonQuickAccessToolBarAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonQuickAccessToolBarAutomationPeer"/> class.
    /// </summary>
    public RibbonQuickAccessToolBarAutomationPeer(QuickAccessToolBar owner)
        : base(owner)
    {
    }

    private QuickAccessToolBar OwnerToolBar => (QuickAccessToolBar)Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ToolBar;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => Owner.GetType().Name;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? "Quick Access Toolbar" : name;
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var children = new List<AutomationPeer>();
        foreach (var item in OwnerToolBar.Items)
        {
            if (item.Visibility != Visibility.Visible)
            {
                continue;
            }

            var peer = CreatePeerForElement(item);
            if (peer is not null)
            {
                children.Add(peer);
            }
        }

        if (OwnerToolBar.MenuButtonForAutomation is { Visibility: Visibility.Visible } menuButton)
        {
            var peer = CreatePeerForElement(menuButton);
            if (peer is not null)
            {
                children.Add(peer);
            }
        }

        return children;
    }
}

/// <summary>
/// Automation peer for <see cref="ScreenTip"/>.
/// </summary>
public partial class RibbonScreenTipAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonScreenTipAutomationPeer"/> class.
    /// </summary>
    public RibbonScreenTipAutomationPeer(ScreenTip owner)
        : base(owner)
    {
    }

    private ScreenTip OwnerScreenTip => (ScreenTip)Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ToolTip;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(ScreenTip);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? OwnerScreenTip.Title : name;
    }

    /// <inheritdoc/>
    protected override string GetHelpTextCore()
    {
        var helpText = base.GetHelpTextCore();
        return string.IsNullOrWhiteSpace(helpText)
            ? OwnerScreenTip.Text?.ToString() ?? string.Empty
            : helpText;
    }
}

/// <summary>
/// Automation peer for <see cref="RibbonTitleBar"/>.
/// </summary>
public partial class RibbonTitleBarAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTitleBarAutomationPeer"/> class.
    /// </summary>
    public RibbonTitleBarAutomationPeer(RibbonTitleBar owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Header;

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonTitleBar);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name) ? ((RibbonTitleBar)Owner).Header ?? string.Empty : name;
    }
}

/// <summary>
/// Automation peer for <see cref="TwoLineLabel"/>.
/// </summary>
public partial class TwoLineLabelAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwoLineLabelAutomationPeer"/> class.
    /// </summary>
    public TwoLineLabelAutomationPeer(TwoLineLabel owner)
        : base(owner)
    {
    }

    private TwoLineLabel OwnerLabel => (TwoLineLabel)Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Text;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(TwoLineLabel);

    /// <inheritdoc/>
    protected override string GetNameCore() => OwnerLabel.Text ?? string.Empty;

    /// <inheritdoc/>
#if WINDOWS
    protected override bool IsControlElementCore()
        => VisualTreeHelper.GetParent(OwnerLabel) is null or ContentPresenter
            ? base.IsControlElementCore()
            : false;
#else
    protected override bool IsControlElementCore()
        => OwnerLabel.TemplatedParent is null or ContentPresenter
            ? base.IsControlElementCore()
            : false;
#endif

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
        => base.GetChildrenCore()?.ToList() ?? [];
}
