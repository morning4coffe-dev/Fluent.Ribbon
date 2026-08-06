using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Fluent.Automation.Peers;

namespace Fluent;

/// <summary>
/// Exposes menu-specific invocation, expansion, toggle, and radio-group semantics
/// for <see cref="MenuItem"/>.
/// </summary>
public partial class RibbonMenuItemAutomationPeer :
    FrameworkElementAutomationPeer,
    IExpandCollapseProvider,
    IInvokeProvider,
    ISelectionProvider,
    ISelectionItemProvider,
    IToggleProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonMenuItemAutomationPeer"/> class.
    /// </summary>
    public RibbonMenuItemAutomationPeer(MenuItem owner)
        : base(owner)
    {
    }

    private MenuItem OwnerItem => (MenuItem)Owner;

    private bool IsExpandable => IsPatternApplicable(
        PatternInterface.ExpandCollapse,
        OwnerItem.HasSubItems,
        OwnerItem.IsSplit,
        OwnerItem.IsCheckable,
        OwnerItem.GroupName);

    private bool IsActionable => IsPatternApplicable(
        PatternInterface.Invoke,
        OwnerItem.HasSubItems,
        OwnerItem.IsSplit,
        OwnerItem.IsCheckable,
        OwnerItem.GroupName);

    private bool IsIndependentToggle => IsPatternApplicable(
        PatternInterface.Toggle,
        OwnerItem.HasSubItems,
        OwnerItem.IsSplit,
        OwnerItem.IsCheckable,
        OwnerItem.GroupName);

    private bool IsGroupedSelection => IsPatternApplicable(
        PatternInterface.SelectionItem,
        OwnerItem.HasSubItems,
        OwnerItem.IsSplit,
        OwnerItem.IsCheckable,
        OwnerItem.GroupName);

    private IReadOnlyList<MenuItem> GroupedChildren
        => OwnerItem.Items
            .OfType<MenuItem>()
            .Where(IsGroupedMenuItem)
            .ToList();

    private bool IsSelectionContainer => GroupedChildren.Count > 0;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.MenuItem;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(MenuItem);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name)
            ? Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(
                OwnerItem.Header)
            : name;
    }

    /// <inheritdoc/>
    protected override string GetHelpTextCore()
    {
        var help = base.GetHelpTextCore();
        return string.IsNullOrEmpty(help) ? OwnerItem.Description ?? string.Empty : help;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
    {
        var accessKey = base.GetAccessKeyCore();
        return string.IsNullOrWhiteSpace(accessKey)
            ? OwnerItem.KeyTip ?? string.Empty
            : accessKey;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ExpandCollapse when IsExpandable => this,
            PatternInterface.Invoke when IsActionable => this,
            PatternInterface.Selection when IsSelectionContainer => this,
            PatternInterface.SelectionItem when IsGroupedSelection => this,
            PatternInterface.Toggle when IsIndependentToggle => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface)
        => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore()
    {
        if (!OwnerItem.IsDropDownOpen)
        {
            return base.GetChildrenCore()?.ToList();
        }

        var children = base.GetChildrenCore()?.ToList() ?? [];
        foreach (var item in OwnerItem.Items.OfType<UIElement>()
                     .Where(AutomationPeerHelpers.IsEffectivelyVisible))
        {
            var peer = CreatePeerForElement(item);
            if (peer is not null && !children.Contains(peer))
            {
                children.Add(peer);
            }
        }

        return children;
    }

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState
        => RunOnOwnerThread(
            () =>
            {
                EnsureApplicable(IsExpandable);
                return OwnerItem.IsDropDownOpen
                    ? ExpandCollapseState.Expanded
                    : ExpandCollapseState.Collapsed;
            });

    /// <inheritdoc/>
    public ToggleState ToggleState
        => RunOnOwnerThread(
            () =>
            {
                EnsureApplicable(IsIndependentToggle);
                return OwnerItem.IsChecked switch
                {
                    true => ToggleState.On,
                    false => ToggleState.Off,
                    _ => ToggleState.Indeterminate,
                };
            });

    /// <inheritdoc/>
    public bool IsSelected
        => RunOnOwnerThread(
            () =>
            {
                EnsureApplicable(IsGroupedSelection);
                return OwnerItem.IsChecked is true;
            });

    /// <inheritdoc/>
    public IRawElementProviderSimple? SelectionContainer
        => RunOnOwnerThread(
            () =>
            {
                EnsureApplicable(IsGroupedSelection);
                var owner = OwnerItem.DropDownOwner as UIElement
                            ?? FindVisualItemsOwner();
                if (owner is null)
                {
                    return null;
                }

                var peer = CreatePeerForElement(owner);
                return peer?.GetPattern(PatternInterface.Selection) is ISelectionProvider
                    ? ProviderFromPeer(peer)
                    : null;
            });

    private UIElement? FindVisualItemsOwner()
    {
        for (DependencyObject? current = OwnerItem;
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is ItemsControl itemsControl)
            {
                return itemsControl;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public void Expand()
        => RunOnOwnerThread(
            () =>
            {
                ValidateProviderOperation(
                    IsExpandable,
                    "This menu item cannot be expanded or collapsed.");
                OwnerItem.IsDropDownOpen = true;
            });

    /// <inheritdoc/>
    public void Collapse()
        => RunOnOwnerThread(
            () =>
            {
                ValidateProviderOperation(
                    IsExpandable,
                    "This menu item cannot be expanded or collapsed.");
                OwnerItem.IsDropDownOpen = false;
            });

    /// <inheritdoc/>
    public void Invoke()
        => RunOnOwnerThread(
            () =>
            {
                ValidateProviderOperation(
                    IsActionable,
                    "This menu item cannot be invoked.");
                OwnerItem.InvokeFromAutomation();
            });

    /// <inheritdoc/>
    public void Toggle()
        => RunOnOwnerThread(
            () =>
            {
                ValidateProviderOperation(
                    IsIndependentToggle,
                    "This menu item cannot be toggled.");
                OwnerItem.IsChecked = OwnerItem.IsChecked is not true;
            });

    /// <inheritdoc/>
    public void AddToSelection()
        => RunOnOwnerThread(
            () =>
            {
                ValidateProviderOperation(
                    IsGroupedSelection,
                    "This menu item is not a selectable group item.");
                if (OwnerItem.IsChecked is true)
                {
                    return;
                }

                if (HasSelectedGroupSibling())
                {
                    throw new InvalidOperationException(
                        "This menu group supports only one selected item.");
                }

                OwnerItem.IsChecked = true;
            });

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
        RunOnOwnerThread(
            () =>
            {
                ValidateProviderOperation(
                    IsGroupedSelection,
                    "This menu item is not a selectable group item.");
                if (OwnerItem.IsChecked is true)
                {
                    throw new InvalidOperationException(
                        "A grouped menu item cannot be removed without selecting another item.");
                }
            });
    }

    /// <inheritdoc/>
    public void Select()
        => RunOnOwnerThread(
            () =>
            {
                ValidateProviderOperation(
                    IsGroupedSelection,
                    "This menu item is not a selectable group item.");
                OwnerItem.IsChecked = true;
            });

    bool ISelectionProvider.CanSelectMultiple
        => GroupedChildren
            .Select(item => item.GroupName)
            .Distinct(StringComparer.Ordinal)
            .Skip(1)
            .Any();

    bool ISelectionProvider.IsSelectionRequired => true;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        => GroupedChildren
            .Where(item => item.IsChecked is true)
            .Select(
                item => CreatePeerForElement(item)
                        ?? new RibbonMenuItemAutomationPeer(item))
            .Select(ProviderFromPeer)
            .ToArray();

    internal void RaiseExpandCollapseStateChanged(bool oldValue, bool newValue)
    {
        if (!IsExpandable || oldValue == newValue)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            oldValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
            newValue ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }

    internal void RaiseCheckedStateChanged(bool? oldValue, bool? newValue)
    {
        if (oldValue == newValue)
        {
            return;
        }

        if (IsIndependentToggle)
        {
            RaisePropertyChangedEvent(
                TogglePatternIdentifiers.ToggleStateProperty,
                ToToggleState(oldValue),
                ToToggleState(newValue));
        }
        else if (IsGroupedSelection)
        {
            var wasSelected = oldValue is true;
            var isSelected = newValue is true;
            if (wasSelected == isSelected)
            {
                return;
            }

            RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty,
                wasSelected,
                isSelected);
            RaiseAutomationEvent(
                isSelected
                    ? AutomationEvents.SelectionItemPatternOnElementSelected
                    : AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
        }
    }

    internal void RaiseInvoked()
    {
        if (IsActionable)
        {
            RaiseAutomationEvent(AutomationEvents.InvokePatternOnInvoked);
        }
    }

    internal static ToggleState ToToggleState(bool? value)
        => value switch
        {
            true => ToggleState.On,
            false => ToggleState.Off,
            _ => ToggleState.Indeterminate,
        };

    private static bool IsGroupedMenuItem(MenuItem item)
        => item.IsCheckable && !string.IsNullOrEmpty(item.GroupName);

    private void ValidateProviderOperation(bool isAvailable, string unavailableMessage)
    {
        AutomationProviderGuard.EnsureEnabled(this);
        for (var owner = OwnerItem.DropDownOwner;
             owner is not null;
             owner = owner is MenuItem menuItem ? menuItem.DropDownOwner : null)
        {
            if (owner is Control control)
            {
                AutomationProviderGuard.EnsureEnabled(control.IsEnabled);
            }
        }

        AutomationProviderGuard.EnsureAvailable(isAvailable, unavailableMessage);
    }

    private bool HasSelectedGroupSibling()
    {
        var itemsControl = OwnerItem.DropDownOwner as ItemsControl
                           ?? FindVisualItemsOwner() as ItemsControl;
        return itemsControl?.Items
            .OfType<MenuItem>()
            .Any(
                item => !ReferenceEquals(item, OwnerItem)
                        && string.Equals(item.GroupName, OwnerItem.GroupName, StringComparison.Ordinal)
                        && item.IsChecked is true) == true;
    }

    internal static bool IsPatternApplicable(
        PatternInterface pattern,
        bool hasSubItems,
        bool isSplit,
        bool isCheckable,
        string? groupName)
    {
        var isActionable = !hasSubItems || isSplit;
        return pattern switch
        {
            PatternInterface.ExpandCollapse => hasSubItems,
            PatternInterface.Invoke => isSplit || (!hasSubItems && !isCheckable),
            PatternInterface.SelectionItem =>
                isActionable && isCheckable && !string.IsNullOrEmpty(groupName),
            PatternInterface.Toggle =>
                isActionable && isCheckable && string.IsNullOrEmpty(groupName),
            _ => false,
        };
    }

    private static void EnsureApplicable(bool isApplicable)
    {
        if (!isApplicable)
        {
            throw new InvalidOperationException(
                "The requested automation pattern is not applicable to this menu item.");
        }
    }

    private void RunOnOwnerThread(Action action)
    {
        RunOnOwnerThread(
            () =>
            {
                action();
                return true;
            });
    }

    private T RunOnOwnerThread<T>(Func<T> action)
    {
        var dispatcherQueue = OwnerItem.DispatcherQueue;
        if (dispatcherQueue is null || dispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        using var completion = new System.Threading.ManualResetEventSlim();
        Exception? dispatchException = null;
        T result = default!;
        if (!dispatcherQueue.TryEnqueue(
                () =>
                {
                    try
                    {
                        result = action();
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
            throw new InvalidOperationException("Could not dispatch the menu automation action.");
        }

        if (!completion.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("The menu automation action timed out.");
        }

        if (dispatchException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo
                .Capture(dispatchException)
                .Throw();
        }

        return result;
    }
}
