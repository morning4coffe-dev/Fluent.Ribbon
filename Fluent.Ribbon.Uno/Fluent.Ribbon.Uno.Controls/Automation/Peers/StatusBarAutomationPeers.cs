namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

/// <summary>
/// Automation peer for <see cref="RibbonStatusBar"/>.
/// </summary>
public sealed class RibbonStatusBarAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>Initializes a status-bar automation peer.</summary>
    public RibbonStatusBarAutomationPeer(RibbonStatusBar owner)
        : base(owner)
    {
    }

    private RibbonStatusBar OwnerStatusBar => (RibbonStatusBar)Owner;

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.StatusBar;

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(RibbonStatusBar);

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? global::Fluent.RibbonLocalization.Current.Localization.StatusBarName
            : name;
    }

    /// <inheritdoc />
    protected override IList<AutomationPeer>? GetChildrenCore()
    {
        var children = new List<AutomationPeer>();
        foreach (var item in OwnerStatusBar.Items.Concat(OwnerStatusBar.RightItems))
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

        return children;
    }
}

/// <summary>
/// Automation peer for <see cref="StatusBarItem"/>.
/// </summary>
public sealed class RibbonStatusBarItemAutomationPeer : FrameworkElementAutomationPeer
{
    /// <summary>Initializes a status-bar item automation peer.</summary>
    public RibbonStatusBarItemAutomationPeer(StatusBarItem owner)
        : base(owner)
    {
    }

    private StatusBarItem OwnerStatusItem => (StatusBarItem)Owner;

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Text;

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(StatusBarItem);

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(
                OwnerStatusItem.Title ?? OwnerStatusItem.Content)
            : name;
    }
}

/// <summary>
/// Automation peer for <see cref="StatusBarMenuItem"/>.
/// </summary>
public sealed class StatusBarMenuItemAutomationPeer :
    FrameworkElementAutomationPeer,
    IToggleProvider,
    IInvokeProvider
{
    /// <summary>Initializes a status-bar menu-item automation peer.</summary>
    public StatusBarMenuItemAutomationPeer(StatusBarMenuItem owner)
        : base(owner)
    {
    }

    private StatusBarMenuItem OwnerMenuItem => (StatusBarMenuItem)Owner;

    /// <inheritdoc />
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.MenuItem;

    /// <inheritdoc />
    protected override string GetClassNameCore() => nameof(StatusBarMenuItem);

    /// <inheritdoc />
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetObjectName(OwnerMenuItem.Header)
            : name;
    }

    /// <inheritdoc />
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => OwnerMenuItem.StatusBarItem is { } statusBarItem
           && StatusBarMenuItem.IsStatusItemCheckable(statusBarItem)
           && patternInterface is PatternInterface.Toggle or PatternInterface.Invoke
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc />
    public ToggleState ToggleState =>
        OwnerMenuItem.IsChecked ? ToggleState.On : ToggleState.Off;

    internal void RaiseIsCheckedChanged(bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            TogglePatternIdentifiers.ToggleStateProperty,
            oldValue ? ToggleState.On : ToggleState.Off,
            newValue ? ToggleState.On : ToggleState.Off);
    }

    /// <inheritdoc />
    public void Toggle()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerMenuItem.StatusBarItem is { } statusBarItem
            && StatusBarMenuItem.IsStatusItemCheckable(statusBarItem),
            "This status-bar menu item is not linked to an actionable item.");
        OwnerMenuItem.InvokeForAutomation();
    }

    /// <inheritdoc />
    public void Invoke() => Toggle();
}
