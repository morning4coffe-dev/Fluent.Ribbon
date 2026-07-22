namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes a gallery data item through the WinUI selector-item automation model.
/// </summary>
public partial class GalleryItemAutomationPeer : SelectorItemAutomationPeer, IScrollItemProvider
{
    private readonly object _item;

    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryItemAutomationPeer"/> class.
    /// </summary>
    public GalleryItemAutomationPeer(object owner, SelectorAutomationPeer selectorAutomationPeer)
        : base(owner, selectorAutomationPeer)
    {
        _item = owner;
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "GalleryItem";

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ListItem;

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.ScrollItem
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void ScrollIntoView()
    {
        if (_item is UIElement element)
        {
            element.StartBringIntoView();
        }
    }
}

/// <summary>
/// Exposes a realized <see cref="RibbonGalleryItem"/> as a selectable, invokable list item.
/// </summary>
public partial class GalleryItemWrapperAutomationPeer : FrameworkElementAutomationPeer,
    IInvokeProvider,
    IScrollItemProvider,
    ISelectionItemProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryItemWrapperAutomationPeer"/> class.
    /// </summary>
    public GalleryItemWrapperAutomationPeer(RibbonGalleryItem owner)
        : base(owner)
    {
    }

    private RibbonGalleryItem OwnerItem => (RibbonGalleryItem)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "ListBoxItem";

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ListItem;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerItem.Content)
            : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
    {
        var accessKey = base.GetAccessKeyCore();
        return string.IsNullOrWhiteSpace(accessKey) ? OwnerItem.KeyTip ?? string.Empty : accessKey;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.Invoke => this,
            PatternInterface.ScrollItem => this,
            PatternInterface.SelectionItem => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc/>
    public void Invoke()
    {
        if (OwnerItem.IsEnabled)
        {
            OwnerItem.OnKeyTipPressed();
        }
    }

    /// <inheritdoc/>
    public void ScrollIntoView() => OwnerItem.StartBringIntoView();

    /// <inheritdoc/>
    public void AddToSelection() => Select();

    /// <inheritdoc/>
    public void RemoveFromSelection() => OwnerItem.IsSelected = false;

    /// <inheritdoc/>
    public void Select() => OwnerItem.IsSelected = true;

    /// <inheritdoc/>
    public bool IsSelected => OwnerItem.IsSelected;

    /// <inheritdoc/>
    public IRawElementProviderSimple? SelectionContainer
    {
        get
        {
            var gallery = AutomationPeerHelpers.FindAncestor<InRibbonGallery>(OwnerItem);
            var peer = gallery is null ? null : CreatePeerForElement(gallery);
            return peer is null ? null : ProviderFromPeer(peer);
        }
    }
}
