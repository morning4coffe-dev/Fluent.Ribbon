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

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
        => AutomationPeerHelpers.GetAccessKey(
            (FrameworkElement)Owner,
            base.GetAccessKeyCore());
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
    protected override object GetPatternCore(PatternInterface patternInterface)
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
        var name = AutomationProperties.GetName(Owner);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        name = AutomationPeerHelpers.GetHeaderOrPlaceholderName((FrameworkElement)Owner);
        return string.IsNullOrWhiteSpace(name) ? base.GetNameCore() : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
        => AutomationPeerHelpers.GetAccessKey(
            (FrameworkElement)Owner,
            base.GetAccessKeyCore());

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
    protected override string GetAccessKeyCore()
        => AutomationPeerHelpers.GetAccessKey(
            OwnerGroup,
            base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => OwnerGroup.IsInButtonState ? AutomationControlType.Button : AutomationControlType.Group;

    /// <inheritdoc/>
    protected override bool IsControlElementCore()
        => OwnerGroup.AutomationOwnerTab is not { IsSelected: false }
           && base.IsControlElementCore();

    /// <inheritdoc/>
    protected override bool IsContentElementCore()
        => OwnerGroup.AutomationOwnerTab is not { IsSelected: false }
           && base.IsContentElementCore();

    /// <inheritdoc/>
    protected override bool IsOffscreenCore()
        => OwnerGroup.AutomationOwnerTab is { IsSelected: false }
           || base.IsOffscreenCore();

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
        if (OwnerGroup.IsInButtonState && !OwnerGroup.IsDropDownOpen)
        {
            return [];
        }

        var peers = new List<AutomationPeer>();
        foreach (var item in OwnerGroup.Items)
        {
            if (AutomationPeerHelpers.IsEffectivelyVisible(item)
                && CreatePeerForElement(item) is { } peer)
            {
                peers.Add(peer);
            }
        }

        // The dialog launcher is a real, clickable control, so it must be reachable by assistive
        // technology and keyboard users rather than only by pointing at the chevron.
        if (!OwnerGroup.IsInButtonState
            && OwnerGroup is { IsLauncherVisible: true, LauncherButton: { } launcher }
            && AutomationPeerHelpers.IsEffectivelyVisible(launcher)
            && CreatePeerForElement(launcher) is { } launcherPeer)
        {
            peers.Add(launcherPeer);
        }

        return peers;
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerGroup.IsInButtonState,
            "This ribbon group cannot be collapsed.");
        OwnerGroup.CollapseForAutomation();
    }

    /// <inheritdoc/>
    public void Expand()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerGroup.IsInButtonState,
            "This ribbon group cannot be expanded.");
        OwnerGroup.ExpandForAutomation();
    }

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerGroup.IsDropDownOpen
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed;

    internal void RaiseIsDropDownOpenChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }

    /// <inheritdoc/>
    public void ScrollIntoView()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        OwnerGroup.StartBringIntoView();
    }

    /// <inheritdoc/>
    protected override void SetFocusCore()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerGroup.IsInButtonState,
            "Only a collapsed ribbon group can receive automation focus.");
        AutomationProviderGuard.EnsureAvailable(
            OwnerGroup.Focus(FocusState.Programmatic),
            "The collapsed ribbon group could not receive focus.");
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

        return AutomationPeerHelpers.GetObjectName(
            AutomationPeerHelpers.FindAncestor<RibbonGroupBox>((FrameworkElement)Owner)?.Header);
    }
}

/// <summary>
/// Automation peer for <see cref="RibbonGallery"/>.
/// </summary>
public partial class RibbonGalleryAutomationPeer : SelectorAutomationPeer,
    ISelectionProvider
{
    private readonly Dictionary<UIElement, AutomationPeer> _itemPeers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGalleryAutomationPeer"/> class.
    /// </summary>
    public RibbonGalleryAutomationPeer(RibbonGallery owner)
        : base(owner)
    {
    }

    internal RibbonGallery OwnerGallery => (RibbonGallery)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonGallery);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.List;

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Selection
           && OwnerGallery.Selectable
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    protected override List<AutomationPeer> GetChildrenCore()
        => OwnerGallery.GetAutomationItems().Select(CreateGalleryItemPeer).ToList();

    bool ISelectionProvider.CanSelectMultiple => false;

    bool ISelectionProvider.IsSelectionRequired => false;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        if (OwnerGallery.SelectedItem is not UIElement selected
            || !OwnerGallery.GetAutomationItems().Contains(selected))
        {
            return [];
        }

        return [ProviderFromPeer(CreateGalleryItemPeer(selected))];
    }

    private AutomationPeer CreateGalleryItemPeer(UIElement item)
    {
        if (item is RibbonGalleryItem galleryItem)
        {
            galleryItem.GalleryOwner = OwnerGallery;
            return CreatePeerForElement(galleryItem)
                   ?? new GalleryItemWrapperAutomationPeer(galleryItem);
        }

        if (!_itemPeers.TryGetValue(item, out var peer))
        {
            peer = new GalleryItemAutomationPeer(item, this);
            _itemPeers[item] = peer;
        }

        return peer;
    }

    internal void RaiseSelectionChanged(UIElement? oldItem, UIElement? newItem)
    {
        if (ReferenceEquals(oldItem, newItem))
        {
            return;
        }

        if (oldItem is not null
            && oldItem is not RibbonGalleryItem
            && _itemPeers.TryGetValue(oldItem, out var oldPeer)
            && oldPeer is GalleryItemAutomationPeer oldGalleryPeer)
        {
            oldGalleryPeer.RaiseIsSelectedChanged(true, false);
        }

        if (newItem is not null
            && newItem is not RibbonGalleryItem
            && CreateGalleryItemPeer(newItem) is GalleryItemAutomationPeer newGalleryPeer)
        {
            newGalleryPeer.RaiseIsSelectedChanged(false, true);
        }
    }
}

/// <summary>
/// Automation peer for <see cref="InRibbonGallery"/>.
/// </summary>
public partial class RibbonInRibbonGalleryAutomationPeer : SelectorAutomationPeer,
    IExpandCollapseProvider,
    ISelectionProvider
{
    private readonly Dictionary<UIElement, AutomationPeer> _itemPeers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonInRibbonGalleryAutomationPeer"/> class.
    /// </summary>
    public RibbonInRibbonGalleryAutomationPeer(InRibbonGallery owner)
        : base(owner)
    {
    }

    internal InRibbonGallery OwnerGallery => (InRibbonGallery)Owner;

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

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
        => AutomationPeerHelpers.GetAccessKey(
            OwnerGallery,
            base.GetAccessKeyCore());

    /// <summary>Creates an item peer for a gallery item.</summary>
    protected new virtual ItemAutomationPeer CreateItemAutomationPeer(object item)
        => new GalleryItemAutomationPeer(item, this);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.List;

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ExpandCollapse => this,
            PatternInterface.Selection when OwnerGallery.Selectable => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    protected override List<AutomationPeer> GetChildrenCore()
        => OwnerGallery.GetAutomationItems().Select(CreateGalleryItemPeer).ToList();

    /// <inheritdoc/>
    public void Collapse()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        OwnerGallery.CollapseForAutomation();
    }

    /// <inheritdoc/>
    public void Expand()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        OwnerGallery.ExpandForAutomation();
    }

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerGallery.IsDropDownOpen
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed;

    internal void RaiseIsDropDownOpenChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }

    bool ISelectionProvider.CanSelectMultiple => false;

    bool ISelectionProvider.IsSelectionRequired => false;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        var selected = OwnerGallery.FindSelectionContainer(OwnerGallery.SelectedItem);
        if (selected is null || !OwnerGallery.GetAutomationItems().Contains(selected))
        {
            return [];
        }

        return [ProviderFromPeer(CreateGalleryItemPeer(selected))];
    }

    private AutomationPeer CreateGalleryItemPeer(UIElement item)
    {
        if (item is RibbonGalleryItem galleryItem)
        {
            galleryItem.GalleryOwner = OwnerGallery;
            return CreatePeerForElement(galleryItem)
                   ?? new GalleryItemWrapperAutomationPeer(galleryItem);
        }

        if (!_itemPeers.TryGetValue(item, out var peer))
        {
            peer = new GalleryItemAutomationPeer(item, this);
            _itemPeers[item] = peer;
        }

        return peer;
    }

    internal void RaiseSelectionChanged(UIElement? oldItem, UIElement? newItem)
    {
        if (ReferenceEquals(oldItem, newItem))
        {
            return;
        }

        if (oldItem is not null
            && oldItem is not RibbonGalleryItem
            && _itemPeers.TryGetValue(oldItem, out var oldPeer)
            && oldPeer is GalleryItemAutomationPeer oldGalleryPeer)
        {
            oldGalleryPeer.RaiseIsSelectedChanged(true, false);
        }

        if (newItem is not null
            && newItem is not RibbonGalleryItem
            && CreateGalleryItemPeer(newItem) is GalleryItemAutomationPeer newGalleryPeer)
        {
            newGalleryPeer.RaiseIsSelectedChanged(false, true);
        }
    }
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
        return string.IsNullOrWhiteSpace(name)
            ? global::Fluent.RibbonLocalization.Current.Localization.QuickAccessToolBarName
            : name;
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var children = new List<AutomationPeer>();
        foreach (var item in OwnerToolBar.Items)
        {
            if (!AutomationPeerHelpers.IsEffectivelyVisible(item))
            {
                continue;
            }

            var peer = CreatePeerForElement(item);
            if (peer is not null)
            {
                children.Add(peer);
            }
        }

        if (OwnerToolBar.OverflowButtonForAutomation is { } overflowButton
            && AutomationPeerHelpers.IsEffectivelyVisible(overflowButton))
        {
            var peer = CreatePeerForElement(overflowButton);
            if (peer is not null)
            {
                children.Add(peer);
            }
        }

        if (OwnerToolBar.MenuButtonForAutomation is { } menuButton
            && AutomationPeerHelpers.IsEffectivelyVisible(menuButton))
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
            ? AutomationPeerHelpers.GetObjectName(OwnerScreenTip.Text)
            : helpText;
    }

    /// <inheritdoc/>
    protected override string GetAcceleratorKeyCore()
    {
        var acceleratorKey = base.GetAcceleratorKeyCore();
        return string.IsNullOrWhiteSpace(acceleratorKey)
               && OwnerScreenTip.HelpTopic is not null
            ? "F1"
            : acceleratorKey;
    }

    internal void RaiseAcceleratorKeyChanged(bool hadHelpTopic, bool hasHelpTopic)
    {
        if (hadHelpTopic == hasHelpTopic)
        {
            return;
        }

        RaisePropertyChangedEvent(
            AutomationElementIdentifiers.AcceleratorKeyProperty,
            hadHelpTopic ? "F1" : string.Empty,
            hasHelpTopic ? "F1" : string.Empty);
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
