namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;

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
    protected override string GetLocalizedControlTypeCore()
        => global::Fluent.RibbonLocalization.Current.Localization.RibbonControlType;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? global::Fluent.RibbonLocalization.Current.Localization.RibbonName
            : name;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ExpandCollapse
           && OwnerRibbon.CanMinimize
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Collapse()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerRibbon.CanMinimize,
            "The ribbon cannot be minimized.");
        OwnerRibbon.IsMinimized = true;
    }

    /// <inheritdoc/>
    public void Expand()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerRibbon.CanMinimize,
            "The ribbon cannot be restored.");
        OwnerRibbon.IsMinimized = false;
    }

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerRibbon.IsMinimized
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded;

    internal void RaiseIsMinimizedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded,
            newValue ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded);
    }

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
        if (OwnerRibbon.IsCollapsed)
        {
            return peers;
        }

        if (OwnerRibbon.Menu is FrameworkElement menu
            && AutomationPeerHelpers.IsEffectivelyVisible(menu)
            && CreatePeerForMenu() is { } menuPeer)
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
            if (element is not null
                && AutomationPeerHelpers.IsEffectivelyVisible(element)
                && CreatePeerForElement(element) is { } peer)
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
            ? AutomationPeerHelpers.GetObjectName(OwnerBackstage.Header) is { Length: > 0 } header
                ? header
                : global::Fluent.RibbonLocalization.Current.Localization.ApplicationMenuName
            : name;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ExpandCollapse
           && OwnerBackstage.CanChangeIsOpen
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Collapse()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerBackstage.CanChangeIsOpen,
            "The backstage open state cannot be changed.");
        OwnerBackstage.IsOpen = false;
    }

    /// <inheritdoc/>
    public void Expand()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerBackstage.CanChangeIsOpen,
            "The backstage open state cannot be changed.");
        OwnerBackstage.IsOpen = true;
    }

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerBackstage.IsOpen
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed;

    internal void RaiseIsOpenChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        RaiseChildrenVisibilityChanged(newValue);
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        if (!OwnerBackstage.IsOpen)
        {
            return peers;
        }

        if (OwnerBackstage.Content is BackstageTabControl tabControl)
        {
            peers.Add(
                CreatePeerForElement(tabControl)
                ?? new RibbonBackstageTabControlAutomationPeer(tabControl));
        }
        else if (OwnerBackstage.Content is FrameworkElement content
                 && CreatePeerForElement(content) is { } contentPeer)
        {
            peers.Add(contentPeer);
        }

        return peers;
    }

    private void RaiseChildrenVisibilityChanged(bool childrenVisible)
    {
#if WINDOWS
        RaiseStructureChangedEvent(
            childrenVisible
                ? AutomationStructureChangeType.ChildrenBulkAdded
                : AutomationStructureChangeType.ChildrenBulkRemoved,
            this);
#else
        RaiseAutomationEvent(AutomationEvents.LayoutInvalidated);
#endif
    }
}

/// <summary>
/// Exposes <see cref="StartScreen"/> as an expandable application surface.
/// </summary>
public partial class RibbonStartScreenAutomationPeer : RibbonControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonStartScreenAutomationPeer"/> class.
    /// </summary>
    public RibbonStartScreenAutomationPeer(StartScreen owner)
        : base(owner)
    {
    }

    private StartScreen OwnerStartScreen => (StartScreen)Owner;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Menu;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerStartScreen.Header) is { Length: > 0 } header
                ? header
                : global::Fluent.RibbonLocalization.Current.Localization.ApplicationMenuName
            : name;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ExpandCollapse
           && OwnerStartScreen.CanChangeIsOpen
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Collapse()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerStartScreen.CanChangeIsOpen,
            "The start-screen open state cannot be changed.");
        OwnerStartScreen.IsOpen = false;
    }

    /// <inheritdoc/>
    public void Expand()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerStartScreen.CanChangeIsOpen,
            "The start-screen open state cannot be changed.");
        OwnerStartScreen.IsOpen = true;
    }

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => OwnerStartScreen.IsOpen
            ? Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded
            : Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed;

    internal void RaiseIsOpenChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
        RaiseChildrenVisibilityChanged(newValue);
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        if (!OwnerStartScreen.IsOpen)
        {
            return peers;
        }

        AddPeerOrDescendants(OwnerStartScreen.LeftPaneContent, peers);
        AddPeerOrDescendants(OwnerStartScreen.Content, peers);
        return peers;
    }

    private void AddPeerOrDescendants(object? content, ICollection<AutomationPeer> peers)
    {
        if (content is not DependencyObject dependencyObject)
        {
            return;
        }

        if (dependencyObject is UIElement element
            && CreatePeerForElement(element) is { } peer)
        {
            if (!peers.Contains(peer))
            {
                peers.Add(peer);
            }

            return;
        }

        var childCount = VisualTreeHelper.GetChildrenCount(dependencyObject);
        for (var index = 0; index < childCount; index++)
        {
            AddPeerOrDescendants(VisualTreeHelper.GetChild(dependencyObject, index), peers);
        }
    }

    private void RaiseChildrenVisibilityChanged(bool childrenVisible)
    {
#if WINDOWS
        RaiseStructureChangedEvent(
            childrenVisible
                ? AutomationStructureChangeType.ChildrenBulkAdded
                : AutomationStructureChangeType.ChildrenBulkRemoved,
            this);
#else
        RaiseAutomationEvent(AutomationEvents.LayoutInvalidated);
#endif
    }
}

/// <summary>
/// Exposes <see cref="BackstageTabControl"/> as a single-selection tab control.
/// </summary>
public partial class RibbonBackstageTabControlAutomationPeer :
    SelectorAutomationPeer,
    ISelectionProvider
{
    private readonly List<RibbonBackstageTabItemDataAutomationPeer?> itemPeers = [];

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
    protected override object GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Selection
            ? this
            : base.GetPatternCore(patternInterface);

    /// <summary>Creates an item peer for a backstage item.</summary>
    protected new virtual ItemAutomationPeer CreateItemAutomationPeer(object item)
        => GetDataPeer(item);

    /// <inheritdoc/>
    protected override List<AutomationPeer> GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        if (OwnerTabControl.IsBackButtonVisible
            && OwnerTabControl.BackButton is UIElement
            {
                Visibility: Visibility.Visible,
            } backButton
            && CreatePeerForElement(backButton) is { } backButtonPeer)
        {
            peers.Add(backButtonPeer);
        }

        foreach (var item in OwnerTabControl.Items)
        {
            switch (item)
            {
                case BackstageTabItem tabItem
                    when AutomationPeerHelpers.IsEffectivelyVisible(tabItem):
                    peers.Add(
                        CreatePeerForElement(tabItem)
                        ?? new RibbonBackstageTabItemAutomationPeer(tabItem));
                    break;
                case BackstageButton button
                    when AutomationPeerHelpers.IsEffectivelyVisible(button):
                    peers.Add(
                        CreatePeerForElement(button)
                        ?? new RibbonBackstageButtonAutomationPeer(button));
                    break;
                case FrameworkElement element
                    when AutomationPeerHelpers.IsEffectivelyVisible(element)
                         && CreatePeerForElement(element) is { } peer:
                    peers.Add(peer);
                    break;
                default:
                    peers.Add(CreateItemAutomationPeer(item));
                    break;
            }
        }

        return peers;
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    bool ISelectionProvider.CanSelectMultiple => false;

    bool ISelectionProvider.IsSelectionRequired => true;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        if (OwnerTabControl.SelectedItem is not { } selectedItem)
        {
            return [];
        }

        var peer = selectedItem is BackstageTabItem tabItem
            ? CreatePeerForElement(tabItem)
            : GetDataPeer(selectedItem);
        return peer is null ? [] : [ProviderFromPeer(peer)];
    }

    internal void RaiseSelectionChanged(object? oldItem, object? newItem)
    {
        if (ReferenceEquals(oldItem, newItem))
        {
            return;
        }

        RaiseDataItemSelectionChanged(oldItem, true, false);
        RaiseDataItemSelectionChanged(newItem, false, true);
    }

    private RibbonBackstageTabItemDataAutomationPeer GetDataPeer(object item)
    {
        for (var index = 0; index < itemPeers.Count; index++)
        {
            if (itemPeers[index] is { } peer
                && ReferenceEquals(peer.Item, item))
            {
                return peer;
            }
        }

        var newPeer = new RibbonBackstageTabItemDataAutomationPeer(item, this);
        itemPeers.Add(newPeer);
        return newPeer;
    }

    private void RaiseDataItemSelectionChanged(object? item, bool oldValue, bool newValue)
    {
        if (item is null
            || item is UIElement)
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
}

/// <summary>
/// Exposes <see cref="StartScreenTabControl"/> as a single-selection tab control.
/// </summary>
public partial class RibbonStartScreenTabControlAutomationPeer :
    SelectorAutomationPeer,
    ISelectionProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonStartScreenTabControlAutomationPeer"/> class.
    /// </summary>
    public RibbonStartScreenTabControlAutomationPeer(StartScreenTabControl owner)
        : base(owner)
    {
    }

    private StartScreenTabControl OwnerTabControl => (StartScreenTabControl)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(StartScreenTabControl);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Tab;

    /// <inheritdoc/>
    protected override object GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Selection
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    protected override List<AutomationPeer> GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        foreach (var item in OwnerTabControl.Items)
        {
            switch (item)
            {
                case BackstageTabItem tabItem
                    when AutomationPeerHelpers.IsEffectivelyVisible(tabItem):
                    peers.Add(
                        CreatePeerForElement(tabItem)
                        ?? new RibbonBackstageTabItemAutomationPeer(tabItem));
                    break;
                case BackstageButton button
                    when AutomationPeerHelpers.IsEffectivelyVisible(button):
                    peers.Add(
                        CreatePeerForElement(button)
                        ?? new RibbonBackstageButtonAutomationPeer(button));
                    break;
                case FrameworkElement element
                    when AutomationPeerHelpers.IsEffectivelyVisible(element)
                         && CreatePeerForElement(element) is { } peer:
                    peers.Add(peer);
                    break;
            }
        }

        AddPeer(OwnerTabControl.LeftContent, peers);
        AddPeer(OwnerTabControl.RightContent, peers);
        return peers;
    }

    bool ISelectionProvider.CanSelectMultiple => false;

    bool ISelectionProvider.IsSelectionRequired => true;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
    {
        var selectedItem = OwnerTabControl.Items
            .OfType<BackstageTabItem>()
            .FirstOrDefault(item => item.IsSelected);
        if (selectedItem is null)
        {
            return [];
        }

        var peer = CreatePeerForElement(selectedItem)
                   ?? new RibbonBackstageTabItemAutomationPeer(selectedItem);
        return [ProviderFromPeer(peer)];
    }

    internal void RaiseSelectionChanged(
        BackstageTabItem? oldItem,
        BackstageTabItem? newItem)
    {
        if (ReferenceEquals(oldItem, newItem))
        {
            return;
        }

    }

    private void AddPeer(object? content, ICollection<AutomationPeer> peers)
    {
        if (content is UIElement element
            && CreatePeerForElement(element) is { } peer
            && !peers.Contains(peer))
        {
            peers.Add(peer);
        }
    }
}

internal sealed partial class RibbonBackstageButtonAutomationPeer : FrameworkElementAutomationPeer,
    IInvokeProvider
{
    internal RibbonBackstageButtonAutomationPeer(BackstageButton owner)
        : base(owner)
    {
    }

    private BackstageButton OwnerButton => (BackstageButton)Owner;

    protected override string GetClassNameCore() => nameof(BackstageButton);

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Button;

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerButton.Header)
            : name;
    }

    protected override string GetAccessKeyCore()
    {
        var accessKey = base.GetAccessKeyCore();
        return string.IsNullOrWhiteSpace(accessKey)
            ? OwnerButton.KeyTip ?? string.Empty
            : accessKey;
    }

    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Invoke
            ? this
            : base.GetPatternCore(patternInterface);

    public new object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    public void Invoke()
    {
        var dispatcherQueue = OwnerButton.DispatcherQueue;
        if (dispatcherQueue.HasThreadAccess)
        {
            InvokeOnOwnerThread();
            return;
        }

        using var completion = new System.Threading.ManualResetEventSlim();
        Exception? dispatchException = null;
        if (!dispatcherQueue.TryEnqueue(
                () =>
                {
                    try
                    {
                        InvokeOnOwnerThread();
                    }
                    catch (Exception exception)
                    {
                        dispatchException = exception;
                    }
                    finally
                    {
                        completion.Set();
                    }
                }))
        {
            throw new InvalidOperationException("Could not dispatch the Backstage button action.");
        }

        if (!completion.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("The Backstage button automation action timed out.");
        }

        if (dispatchException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo
                .Capture(dispatchException)
                .Throw();
        }
    }

    private void InvokeOnOwnerThread()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        OwnerButton.InvokeForAutomation();
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
    public void AddToSelection()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        if (IsSelected)
        {
            return;
        }

        if (HasOtherSelection())
        {
            throw new InvalidOperationException(
                "The owning tab control supports only one selected item.");
        }

        Select();
    }

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        if (IsSelected)
        {
            throw new InvalidOperationException(
                "The owning tab control requires one selected item.");
        }
    }

    /// <inheritdoc/>
    public void Select()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        if (AutomationPeerHelpers.FindAncestor<StartScreenTabControl>(OwnerTabItem)
            is { } startScreenTabControl)
        {
            startScreenTabControl.SelectTabForAutomation(OwnerTabItem);
        }
        else if (AutomationPeerHelpers.FindAncestor<BackstageTabControl>(OwnerTabItem) is { } tabControl)
        {
            tabControl.SelectTabForAutomation(OwnerTabItem);
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
                AutomationPeerHelpers.FindAncestor<StartScreenTabControl>(OwnerTabItem);
            container ??= AutomationPeerHelpers.FindAncestor<BackstageTabControl>(OwnerTabItem);
            var peer = container is null ? null : CreatePeerForElement(container);
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
        RaisePropertyChangedEvent(
            SelectionItemPatternIdentifiers.IsSelectedProperty,
            oldValue,
            newValue);
        RaiseAutomationEvent(
            newValue
                ? AutomationEvents.SelectionItemPatternOnElementSelected
                : AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
    }

    private bool HasOtherSelection()
    {
        if (AutomationPeerHelpers.FindAncestor<StartScreenTabControl>(OwnerTabItem)
            is { } startScreenTabControl)
        {
            return startScreenTabControl.Items
                .OfType<BackstageTabItem>()
                .Any(item => item.IsSelected);
        }

        return AutomationPeerHelpers.FindAncestor<BackstageTabControl>(OwnerTabItem)
            is { SelectedItem: not null };
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        var peers = new List<AutomationPeer>();
        if (OwnerTabItem.Header is UIElement header)
        {
            AddPeerOrDescendants(header, peers);
        }

        if (OwnerTabItem.IsSelected && OwnerTabItem.Content is DependencyObject content)
        {
            AddPeerOrDescendants(content, peers);
        }

        return peers;
    }

    private void AddPeerOrDescendants(
        DependencyObject element,
        ICollection<AutomationPeer> peers)
    {
        if (element is UIElement uiElement
            && CreatePeerForElement(uiElement) is { } peer)
        {
            peers.Add(peer);
            return;
        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(element); index++)
        {
            AddPeerOrDescendants(VisualTreeHelper.GetChild(element, index), peers);
        }
    }

}

internal sealed class RibbonBackstageTabItemDataAutomationPeer : SelectorItemAutomationPeer
{
    internal RibbonBackstageTabItemDataAutomationPeer(
        object item,
        RibbonBackstageTabControlAutomationPeer parent)
        : base(item, parent)
    {
    }

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
}
