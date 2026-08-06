namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

/// <summary>
/// Exposes a gallery data item through the WinUI selector-item automation model.
/// </summary>
public partial class GalleryItemAutomationPeer : SelectorItemAutomationPeer,
    IScrollItemProvider,
    ISelectionItemProvider
{
    private readonly object _item;
    private readonly SelectorAutomationPeer _selectorAutomationPeer;

    /// <summary>
    /// Initializes a new instance of the <see cref="GalleryItemAutomationPeer"/> class.
    /// </summary>
    public GalleryItemAutomationPeer(object owner, SelectorAutomationPeer selectorAutomationPeer)
        : base(owner, selectorAutomationPeer)
    {
        _item = owner;
        _selectorAutomationPeer = selectorAutomationPeer;
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "GalleryItem";

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ListItem;

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(_item)
            : name;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ScrollItem when _item is UIElement => this,
            PatternInterface.SelectionItem
                when GalleryAutomationSelection.IsSelectionAvailable(
                    GetGalleryOwner(),
                    _item as UIElement) => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void ScrollIntoView()
    {
        if (_item is UIElement element)
        {
            GalleryAutomationSelection.EnsureItemEnabled(this, GetGalleryOwner(), element);
            element.StartBringIntoView();
            return;
        }

        throw new InvalidOperationException("This gallery item cannot be scrolled into view.");
    }

    /// <inheritdoc/>
    public new void AddToSelection()
        => GalleryAutomationSelection.AddToSelection(
            this,
            GetGalleryOwner(),
            _item as UIElement);

    /// <inheritdoc/>
    public new void RemoveFromSelection()
        => GalleryAutomationSelection.RemoveFromSelection(
            this,
            GetGalleryOwner(),
            _item as UIElement);

    /// <inheritdoc/>
    public new void Select()
        => GalleryAutomationSelection.Select(
            this,
            GetGalleryOwner(),
            _item as UIElement);

    /// <inheritdoc/>
    public new bool IsSelected
        => GalleryAutomationSelection.IsSelected(GetGalleryOwner(), _item as UIElement);

    /// <inheritdoc/>
    public new IRawElementProviderSimple? SelectionContainer
    {
        get
        {
            var peer = GalleryAutomationSelection.GetSelectionContainerPeer(GetGalleryOwner());
            return peer is null ? null : ProviderFromPeer(peer);
        }
    }

    private object? GetGalleryOwner()
        => _selectorAutomationPeer switch
        {
            RibbonGalleryAutomationPeer peer => peer.OwnerGallery,
            RibbonInRibbonGalleryAutomationPeer peer => peer.OwnerGallery,
            _ => null,
        };

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
            PatternInterface.SelectionItem
                when GalleryAutomationSelection.IsSelectionAvailable(
                    GetGalleryOwner(),
                    OwnerItem) => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc/>
    public void Invoke()
    {
        GalleryAutomationSelection.EnsureItemEnabled(this, GetGalleryOwner(), OwnerItem);
        OwnerItem.Activate();
    }

    /// <inheritdoc/>
    public void ScrollIntoView()
    {
        GalleryAutomationSelection.EnsureItemEnabled(this, GetGalleryOwner(), OwnerItem);
        OwnerItem.StartBringIntoView();
    }

    /// <inheritdoc/>
    public void AddToSelection()
        => GalleryAutomationSelection.AddToSelection(this, GetGalleryOwner(), OwnerItem);

    /// <inheritdoc/>
    public void RemoveFromSelection()
        => GalleryAutomationSelection.RemoveFromSelection(this, GetGalleryOwner(), OwnerItem);

    /// <inheritdoc/>
    public void Select()
        => GalleryAutomationSelection.Select(this, GetGalleryOwner(), OwnerItem);

    /// <inheritdoc/>
    public bool IsSelected
        => GalleryAutomationSelection.IsSelected(GetGalleryOwner(), OwnerItem);

    /// <inheritdoc/>
    public IRawElementProviderSimple? SelectionContainer
    {
        get
        {
            var peer = GalleryAutomationSelection.GetSelectionContainerPeer(GetGalleryOwner());
            return peer is null ? null : ProviderFromPeer(peer);
        }
    }

    private object? GetGalleryOwner()
        => AutomationPeerHelpers.FindAncestor<RibbonGallery>(OwnerItem)
           ?? AutomationPeerHelpers.FindAncestor<InRibbonGallery>(OwnerItem)
           ?? OwnerItem.GalleryOwner;

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

internal static class GalleryAutomationSelection
{
    internal static void AddToSelection(
        AutomationPeer itemPeer,
        object? owner,
        UIElement? item)
    {
        ValidateSelection(itemPeer, owner, item);
        if (IsSelected(owner, item))
        {
            return;
        }

        if (GetSelectedItem(owner) is not null)
        {
            throw new InvalidOperationException("Gallery controls support only one selected item.");
        }

        SelectCore(owner, item!);
    }

    internal static void RemoveFromSelection(
        AutomationPeer itemPeer,
        object? owner,
        UIElement? item)
    {
        ValidateSelection(itemPeer, owner, item);
        if (!IsSelected(owner, item))
        {
            return;
        }

        switch (owner)
        {
            case RibbonGallery gallery:
                gallery.RemoveItemFromSelection(item!);
                break;
            case InRibbonGallery gallery:
                gallery.RemoveItemFromSelection(item!);
                break;
        }
    }

    internal static void Select(
        AutomationPeer itemPeer,
        object? owner,
        UIElement? item)
    {
        ValidateSelection(itemPeer, owner, item);
        SelectCore(owner, item!);
    }

    private static void SelectCore(object? owner, UIElement item)
    {
        switch (owner)
        {
            case RibbonGallery gallery:
                gallery.SelectItem(item);
                break;
            case InRibbonGallery gallery:
                gallery.SelectItem(item);
                break;
        }
    }

    internal static bool IsSelected(object? owner, UIElement? item)
        => item is not null && ReferenceEquals(GetSelectedItem(owner), item);

    internal static bool IsSelectionAvailable(object? owner, UIElement? item)
        => item is not null
           && owner switch
           {
               RibbonGallery gallery => gallery.Selectable && gallery.Items.Contains(item),
               InRibbonGallery gallery => gallery.Selectable && gallery.Items.Contains(item),
               _ => false,
           };

    internal static void EnsureItemEnabled(
        AutomationPeer itemPeer,
        object? owner,
        UIElement? item)
    {
        AutomationProviderGuard.EnsureEnabled(itemPeer);
        if (owner is Control ownerControl)
        {
            AutomationProviderGuard.EnsureEnabled(ownerControl.IsEnabled);
        }

        if (item is Control itemControl)
        {
            AutomationProviderGuard.EnsureEnabled(itemControl.IsEnabled);
        }
    }

    private static void ValidateSelection(
        AutomationPeer itemPeer,
        object? owner,
        UIElement? item)
    {
        EnsureItemEnabled(itemPeer, owner, item);
        AutomationProviderGuard.EnsureAvailable(
            IsSelectionAvailable(owner, item),
            "This gallery item cannot be selected.");
    }

    internal static object? GetSelectedItem(object? owner)
        => owner switch
        {
            RibbonGallery gallery => gallery.SelectedItem,
            InRibbonGallery gallery => gallery.FindSelectionContainer(gallery.SelectedItem),
            _ => null,
        };

    internal static AutomationPeer? GetSelectionContainerPeer(object? owner)
        => owner switch
        {
            RibbonGallery gallery => FrameworkElementAutomationPeer.CreatePeerForElement(gallery),
            InRibbonGallery gallery => FrameworkElementAutomationPeer.CreatePeerForElement(gallery),
            _ => null,
        };
}
