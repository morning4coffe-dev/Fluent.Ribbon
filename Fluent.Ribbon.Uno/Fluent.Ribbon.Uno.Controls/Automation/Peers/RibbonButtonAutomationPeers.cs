namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Dispatching;

/// <summary>
/// Automation peer for <see cref="RibbonButton"/>.
/// </summary>
public partial class RibbonButtonAutomationPeer : ButtonAutomationPeer, IInvokeProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonButtonAutomationPeer"/> class.
    /// </summary>
    public RibbonButtonAutomationPeer(RibbonButton owner)
        : base(owner)
    {
    }

    private RibbonButton OwnerButton => (RibbonButton)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonButton";

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetHeaderOrPlaceholderName(OwnerButton)
            : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
    {
        var accessKey = base.GetAccessKeyCore();
        return string.IsNullOrWhiteSpace(accessKey) ? OwnerButton.KeyTip ?? string.Empty : accessKey;
    }

    /// <inheritdoc/>
    protected override string GetHelpTextCore()
    {
        var helpText = base.GetHelpTextCore();
        return string.IsNullOrWhiteSpace(helpText) ? OwnerButton.ScreenTipText : helpText;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Invoke
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public new void Invoke()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        base.Invoke();
    }
}

/// <summary>
/// Automation peer for <see cref="RibbonCheckBox"/>.
/// </summary>
public partial class RibbonCheckBoxAutomationPeer : CheckBoxAutomationPeer, IToggleProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonCheckBoxAutomationPeer"/> class.
    /// </summary>
    public RibbonCheckBoxAutomationPeer(RibbonCheckBox owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetHeaderOrPlaceholderName((FrameworkElement)Owner)
            : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
        => AutomationPeerHelpers.GetAccessKey(
            (FrameworkElement)Owner,
            base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore() => [];

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Toggle
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public new void Toggle()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        base.Toggle();
    }
}

/// <summary>
/// Automation peer for <see cref="RibbonComboBox"/>.
/// </summary>
public partial class RibbonComboBoxAutomationPeer : ComboBoxAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonComboBoxAutomationPeer"/> class.
    /// </summary>
    public RibbonComboBoxAutomationPeer(RibbonComboBox owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonComboBox);

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
/// Runtime automation peer used by <see cref="RibbonComboBox"/>.
/// </summary>
internal sealed partial class RibbonComboBoxAccessibleAutomationPeer : RibbonComboBoxAutomationPeer,
    IExpandCollapseProvider,
    IItemContainerProvider,
    IScrollItemProvider,
    ISelectionProvider,
    IValueProvider
{
    private ExpandCollapseState expandCollapseState;
    private readonly List<RibbonComboBoxItemDataAutomationPeer> itemPeers = [];
    private DispatcherQueueTimer? pendingDropDownTimer;
    private bool pendingDropDownState;

    internal RibbonComboBoxAccessibleAutomationPeer(RibbonComboBox owner)
        : base(owner)
    {
        expandCollapseState = owner.IsDropDownOpen
            ? ExpandCollapseState.Expanded
            : ExpandCollapseState.Collapsed;
        owner.DropDownOpened += OnDropDownOpened;
        owner.DropDownClosed += OnDropDownClosed;
        owner.Items.VectorChanged += OnItemsVectorChanged;
    }

    private RibbonComboBox OwnerComboBox => (RibbonComboBox)Owner;

    protected override string GetClassNameCore() => nameof(RibbonComboBox);

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

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ComboBox;

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ExpandCollapse => this,
            PatternInterface.ItemContainer => this,
            PatternInterface.ScrollItem => this,
            PatternInterface.Selection => this,
            PatternInterface.Value when OwnerComboBox.IsEditable => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    protected override IList<AutomationPeer>? GetChildrenCore()
    {
        var children = base.GetChildrenCore() is { Count: > 0 } baseChildren
            ? new List<AutomationPeer>(baseChildren)
            : [];

        if (OwnerComboBox.IsDropDownOpen)
        {
            for (var index = 0; index < OwnerComboBox.Items.Count; index++)
            {
                var peer = GetContainerPeer(index) ?? GetDataPeer(index);
                if (peer is not null && !children.Contains(peer))
                {
                    children.Add(peer);
                }
            }
        }

        return children.Count == 0 ? null : children;
    }

    public new object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    public new void Expand()
    {
        RunOnOwnerThread(() => AutomationProviderGuard.EnsureEnabled(this));
        QueueDropDownState(isOpen: true, delay: TimeSpan.FromMilliseconds(100));
    }

    public new void Collapse()
    {
        RunOnOwnerThread(() => AutomationProviderGuard.EnsureEnabled(this));
        QueueDropDownState(isOpen: false, delay: TimeSpan.Zero);
    }

    public new ExpandCollapseState ExpandCollapseState => expandCollapseState;

    public new bool CanSelectMultiple => false;

    public new bool IsSelectionRequired => false;

    public new bool IsReadOnly => !OwnerComboBox.IsEditable;

    public new string Value => RunOnOwnerThread(
        () => OwnerComboBox.Text
              ?? AutomationPeerHelpers.GetObjectName(OwnerComboBox.SelectedItem));

    public new IRawElementProviderSimple[] GetSelection()
        => RunOnOwnerThread(GetSelectionProviders);

    public void ScrollIntoView()
        => RunOnOwnerThread(
            () =>
            {
                AutomationProviderGuard.EnsureEnabled(this);
                OwnerComboBox.StartBringIntoView();
            });

    public new void SetValue(string value)
    {
        RunOnOwnerThread(
            () =>
            {
                AutomationProviderGuard.Validate(
                    this,
                    OwnerComboBox.IsEditable,
                    "The ComboBox is not editable.");
                OwnerComboBox.Text = value;
            });
    }

    public new IRawElementProviderSimple? FindItemByProperty(
        IRawElementProviderSimple? startAfter,
        AutomationProperty? automationProperty,
        object? value)
        => RunOnOwnerThread(
            () => FindItemByPropertyCore(startAfter, automationProperty, value));

    private IRawElementProviderSimple? FindItemByPropertyCore(
        IRawElementProviderSimple? startAfter,
        AutomationProperty? automationProperty,
        object? value)
    {
        var startIndex = -1;
        if (startAfter is not null)
        {
            var startPeer = PeerFromProvider(startAfter);
            startIndex = startPeer is RibbonComboBoxItemDataAutomationPeer dataPeer
                ? dataPeer.ItemIndex
                : startPeer is null
                    ? -1
                    : FindContainerPeerIndex(startPeer);
        }

        for (var index = startIndex + 1; index < OwnerComboBox.Items.Count; index++)
        {
            var peer = GetContainerPeer(index) ?? GetDataPeer(index);
            if (peer is null)
            {
                continue;
            }

            var propertyValue = automationProperty switch
            {
                null => null,
                _ when ReferenceEquals(
                    automationProperty,
                    AutomationElementIdentifiers.NameProperty) => peer.GetName(),
                _ when ReferenceEquals(
                    automationProperty,
                    AutomationElementIdentifiers.AutomationIdProperty) => peer.GetAutomationId(),
                _ => null,
            };

            if (automationProperty is null || Equals(propertyValue, value))
            {
                return ProviderFromPeer(peer);
            }
        }

        return null;
    }

    private void QueueDropDownState(bool isOpen, TimeSpan delay)
    {
        if (!OwnerComboBox.DispatcherQueue.TryEnqueue(
                () => ScheduleDropDownState(isOpen, delay)))
        {
            throw new InvalidOperationException("Could not dispatch the ComboBox drop-down state change.");
        }
    }

    private void ScheduleDropDownState(bool isOpen, TimeSpan delay)
    {
        pendingDropDownTimer?.Stop();
        pendingDropDownTimer = null;

        if (delay <= TimeSpan.Zero)
        {
            ApplyDropDownState(isOpen);
            return;
        }

        // Native ComboBox automation performs a trailing close after the provider returns.
        // Apply the requested open state after that native action has completed.
        pendingDropDownState = isOpen;
        pendingDropDownTimer = OwnerComboBox.DispatcherQueue.CreateTimer();
        pendingDropDownTimer.Interval = delay;
        pendingDropDownTimer.IsRepeating = false;
        pendingDropDownTimer.Tick += OnPendingDropDownTimerTick;
        pendingDropDownTimer.Start();
    }

    private void OnPendingDropDownTimerTick(DispatcherQueueTimer sender, object args)
    {
        sender.Stop();
        sender.Tick -= OnPendingDropDownTimerTick;
        if (!ReferenceEquals(sender, pendingDropDownTimer))
        {
            return;
        }

        pendingDropDownTimer = null;
        ApplyDropDownState(pendingDropDownState);
    }

    private void ApplyDropDownState(bool isOpen)
    {
        if (!OwnerComboBox.IsEnabled)
        {
            return;
        }

        OwnerComboBox.IsDropDownOpen = isOpen;
        UpdateExpandCollapseState(
            isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
    }

    private void OnDropDownOpened(object? sender, EventArgs e)
    {
        UpdateExpandCollapseState(ExpandCollapseState.Expanded);
    }

    private void OnDropDownClosed(object? sender, EventArgs e)
    {
        UpdateExpandCollapseState(ExpandCollapseState.Collapsed);
    }

    private void UpdateExpandCollapseState(ExpandCollapseState value)
    {
        if (expandCollapseState == value)
        {
            return;
        }

        var previous = expandCollapseState;
        expandCollapseState = value;
        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            previous,
            value);
    }

    internal void RaiseSelectionChanged(
        object? oldItem,
        int oldIndex,
        object? newItem,
        int newIndex)
    {
        if (oldIndex == newIndex && ReferenceEquals(oldItem, newItem))
        {
            return;
        }

        RaiseDataItemSelectionChanged(oldItem, oldIndex, true, false);
        RaiseDataItemSelectionChanged(newItem, newIndex, false, true);
    }

    internal void RaiseValueChanged(string oldValue, string newValue)
    {
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal)
            || !OwnerComboBox.IsEditable)
        {
            return;
        }

        RaisePropertyChangedEvent(
            ValuePatternIdentifiers.ValueProperty,
            oldValue,
            newValue);
    }

    private void RaiseDataItemSelectionChanged(
        object? item,
        int index,
        bool oldValue,
        bool newValue)
    {
        if (item is null
            || index < 0
            || index >= OwnerComboBox.Items.Count
            || !ReferenceEquals(OwnerComboBox.Items[index], item)
            || GetContainerPeer(index) is not null)
        {
            return;
        }

        GetDataPeer(index)?.RaiseIsSelectedChanged(oldValue, newValue);
    }

    private IRawElementProviderSimple[] GetSelectionProviders()
    {
        if (OwnerComboBox.SelectedIndex < 0)
        {
            return [];
        }

        var peer = GetContainerPeer(OwnerComboBox.SelectedIndex)
                   ?? GetDataPeer(OwnerComboBox.SelectedIndex);
        return peer is null ? [] : [ProviderFromPeer(peer)];
    }

    private AutomationPeer? GetContainerPeer(int index)
    {
        return OwnerComboBox.ContainerFromIndex(index) is UIElement container
            ? CreatePeerForElement(container)
            : null;
    }

    private int FindContainerPeerIndex(AutomationPeer startPeer)
    {
        for (var index = 0; index < OwnerComboBox.Items.Count; index++)
        {
            if (ReferenceEquals(GetContainerPeer(index), startPeer))
            {
                return index;
            }
        }

        return -1;
    }

    private RibbonComboBoxItemDataAutomationPeer? GetDataPeer(int index)
    {
        if (index < 0 || index >= OwnerComboBox.Items.Count)
        {
            return null;
        }

        var item = OwnerComboBox.Items[index];
        var occurrence = GetItemOccurrence(item, index);
        var peer = itemPeers.FirstOrDefault(
            candidate => ReferenceEquals(candidate.Item, item)
                         && candidate.ItemOccurrence == occurrence);
        if (peer is null)
        {
            peer = new RibbonComboBoxItemDataAutomationPeer(
                item,
                index,
                occurrence,
                GetItemName(index),
                this);
            itemPeers.Add(peer);
        }
        else
        {
            peer.UpdateIndex(index);
        }

        return peer;
    }

    private int GetItemOccurrence(object item, int index)
    {
        var occurrence = 0;
        for (var currentIndex = 0; currentIndex < index; currentIndex++)
        {
            if (ReferenceEquals(OwnerComboBox.Items[currentIndex], item))
            {
                occurrence++;
            }
        }

        return occurrence;
    }

    internal int ResolveItemIndex(object item, int occurrence, int preferredIndex)
        => RunOnOwnerThread(
            () => ResolveItemIndexCore(item, occurrence, preferredIndex));

    private int ResolveItemIndexCore(
        object item,
        int occurrence,
        int preferredIndex)
    {
        if (preferredIndex >= 0
            && preferredIndex < OwnerComboBox.Items.Count
            && ReferenceEquals(OwnerComboBox.Items[preferredIndex], item)
            && GetItemOccurrence(item, preferredIndex) == occurrence)
        {
            return preferredIndex;
        }

        var currentOccurrence = 0;
        for (var index = 0; index < OwnerComboBox.Items.Count; index++)
        {
            if (!ReferenceEquals(OwnerComboBox.Items[index], item))
            {
                continue;
            }

            if (currentOccurrence == occurrence)
            {
                return index;
            }

            currentOccurrence++;
        }

        return -1;
    }

    internal string GetItemName(int index)
        => RunOnOwnerThread(
            () =>
            {
                var item = OwnerComboBox.Items[index];
                return item is ComboBoxItem comboBoxItem
                    ? AutomationPeerHelpers.GetObjectName(comboBoxItem.Content)
                    : AutomationPeerHelpers.GetObjectName(item);
            });

    internal bool IsItemSelected(int index)
        => RunOnOwnerThread(() => OwnerComboBox.SelectedIndex == index);

    internal bool IsItemEnabled(int index)
        => RunOnOwnerThread(
            () =>
                index >= 0
                && index < OwnerComboBox.Items.Count
                && OwnerComboBox.IsEnabled
                && (OwnerComboBox.ContainerFromIndex(index) is not Control container
                    || container.IsEnabled)
                && (OwnerComboBox.Items[index] is not Control item
                    || item.IsEnabled));

    internal void AddItemToSelection(int index)
        => RunOnOwnerThread(
            () =>
            {
                EnsureItemEnabled(index);
                if (OwnerComboBox.SelectedIndex == index)
                {
                    return;
                }

                if (OwnerComboBox.SelectedIndex >= 0)
                {
                    throw new InvalidOperationException(
                        "The ComboBox supports only one selected item.");
                }

                OwnerComboBox.SelectedIndex = index;
            });

    internal void SelectItem(int index)
        => RunOnOwnerThread(
            () =>
            {
                EnsureItemEnabled(index);
                OwnerComboBox.SelectedIndex = index;
            });

    internal void RemoveItemFromSelection(int index)
        => RunOnOwnerThread(
            () =>
            {
                EnsureItemEnabled(index);
                if (OwnerComboBox.SelectedIndex == index)
                {
                    OwnerComboBox.SelectedIndex = -1;
                }
            });

    internal void ScrollItemIntoView(int index)
        => RunOnOwnerThread(
            () =>
            {
                EnsureItemEnabled(index);
                var container = OwnerComboBox.ContainerFromIndex(index) as UIElement;
                if (container is null)
                {
                    OwnerComboBox.IsDropDownOpen = true;
                    OwnerComboBox.UpdateLayout();
                    container = OwnerComboBox.ContainerFromIndex(index) as UIElement;
                }

                AutomationProviderGuard.EnsureAvailable(
                    container is not null,
                    "The ComboBox item could not be realized for scrolling.");
                container!.StartBringIntoView();
            });

    private void EnsureItemEnabled(int index)
    {
        AutomationProviderGuard.EnsureEnabled(this);
        if (index < 0 || index >= OwnerComboBox.Items.Count)
        {
            throw new ElementNotAvailableException();
        }

        if (OwnerComboBox.Items[index] is Control control)
        {
            AutomationProviderGuard.EnsureEnabled(control.IsEnabled);
        }

        if (OwnerComboBox.ContainerFromIndex(index) is Control container)
        {
            AutomationProviderGuard.EnsureEnabled(container.IsEnabled);
        }
    }

    private void OnItemsVectorChanged(
        Windows.Foundation.Collections.IObservableVector<object> sender,
        Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        for (var index = itemPeers.Count - 1; index >= 0; index--)
        {
            var peer = itemPeers[index];
            var itemIndex = ResolveItemIndexCore(
                peer.Item,
                peer.ItemOccurrence,
                peer.LastKnownIndex);
            if (itemIndex < 0)
            {
                itemPeers.RemoveAt(index);
            }
            else
            {
                peer.UpdateIndex(itemIndex);
            }
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
        if (OwnerComboBox.DispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        using var completion = new System.Threading.ManualResetEventSlim();
        Exception? dispatchException = null;
        T result = default!;
        if (!OwnerComboBox.DispatcherQueue.TryEnqueue(
                () =>
                {
                    try
                    {
                        result = action();
                    }
                    catch (Exception ex)
                    {
                        dispatchException = ex;
                    }
                    finally
                    {
                        completion.Set();
                    }
                }))
        {
            throw new InvalidOperationException("Could not dispatch the ComboBox automation action.");
        }

        if (!completion.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("The ComboBox automation action timed out.");
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

internal sealed partial class RibbonComboBoxItemDataAutomationPeer : AutomationPeer,
    IScrollItemProvider,
    ISelectionItemProvider
{
    private readonly RibbonComboBoxAccessibleAutomationPeer owner;
    private readonly int itemOccurrence;
    private readonly string itemName;
    private int lastKnownIndex;

    internal RibbonComboBoxItemDataAutomationPeer(
        object item,
        int itemIndex,
        int itemOccurrence,
        string itemName,
        RibbonComboBoxAccessibleAutomationPeer owner)
    {
        Item = item;
        lastKnownIndex = itemIndex;
        this.itemOccurrence = itemOccurrence;
        this.itemName = itemName;
        this.owner = owner;
    }

    internal object Item { get; }

    internal int ItemOccurrence => itemOccurrence;

    internal int LastKnownIndex => lastKnownIndex;

    internal int ItemIndex =>
        owner.ResolveItemIndex(Item, itemOccurrence, lastKnownIndex);

    internal void UpdateIndex(int index) => lastKnownIndex = index;

    protected override string GetClassNameCore() => "ComboBoxItem";

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.ListItem;

    protected override string GetNameCore() => itemName;

    protected override bool IsEnabledCore()
    {
        var index = ItemIndex;
        return index >= 0 && owner.IsItemEnabled(index);
    }

    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ScrollItem => this,
            PatternInterface.SelectionItem when ItemIndex >= 0 => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    public new object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    public void ScrollIntoView() => owner.ScrollItemIntoView(GetCurrentIndex());

    public void AddToSelection() => owner.AddItemToSelection(GetCurrentIndex());

    public void RemoveFromSelection() => owner.RemoveItemFromSelection(GetCurrentIndex());

    public void Select() => owner.SelectItem(GetCurrentIndex());

    public bool IsSelected
    {
        get
        {
            var index = ItemIndex;
            return index >= 0 && owner.IsItemSelected(index);
        }
    }

    public IRawElementProviderSimple SelectionContainer => ProviderFromPeer(owner);

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

    private int GetCurrentIndex()
    {
        var index = ItemIndex;
        if (index < 0)
        {
            throw new ElementNotAvailableException();
        }

        return index;
    }
}

/// <summary>
/// Automation peer for <see cref="RibbonDropDownButton"/>.
/// </summary>
public partial class RibbonDropDownButtonAutomationPeer :
    RibbonHeaderedControlAutomationPeer,
    IExpandCollapseProvider,
    ISelectionProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonDropDownButtonAutomationPeer"/> class.
    /// </summary>
    public RibbonDropDownButtonAutomationPeer(RibbonDropDownButton owner)
        : base(owner)
    {
    }

    /// <summary>
    /// Initializes a peer for a derived split-button implementation.
    /// </summary>
    protected RibbonDropDownButtonAutomationPeer(FrameworkElement owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Button;

    /// <inheritdoc/>
    protected override string GetLocalizedControlTypeCore()
        => global::Fluent.RibbonLocalization.Current.Localization.DropDownButtonControlType;

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
    {
        var accessKey = base.GetAccessKeyCore();
        var keyTip = Owner switch
        {
            RibbonSplitButton splitButton => splitButton.KeyTip,
            RibbonDropDownButton dropDownButton => dropDownButton.KeyTip,
            _ => null,
        };
        return string.IsNullOrWhiteSpace(accessKey) ? keyTip ?? string.Empty : accessKey;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
    {
        return patternInterface switch
        {
            PatternInterface.ExpandCollapse => this,
            PatternInterface.Selection when GroupedMenuItems.Count > 0 => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore() => [];

    /// <inheritdoc/>
    protected override void SetFocusCore()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        AutomationProviderGuard.EnsureAvailable(
            ((Control)Owner).Focus(FocusState.Programmatic),
            "The drop-down button could not receive focus.");
    }

    /// <inheritdoc/>
    public void Collapse()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        switch (Owner)
        {
            case RibbonDropDownButton dropDownButton:
                dropDownButton.CloseDropDown();
                break;
        }
    }

    /// <inheritdoc/>
    public void Expand()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        switch (Owner)
        {
            case RibbonDropDownButton dropDownButton:
                dropDownButton.OpenDropDownForAutomation();
                break;
        }
    }

    /// <inheritdoc/>
    public Microsoft.UI.Xaml.Automation.ExpandCollapseState ExpandCollapseState
        => Owner switch
        {
            RibbonDropDownButton { IsDropDownOpen: true } => Microsoft.UI.Xaml.Automation.ExpandCollapseState.Expanded,
            _ => Microsoft.UI.Xaml.Automation.ExpandCollapseState.Collapsed,
        };

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

    private IReadOnlyList<MenuItem> GroupedMenuItems
    {
        get
        {
            if (Owner is not RibbonDropDownButton dropDownButton)
            {
                return [];
            }

            var items = dropDownButton.ItemsSource as System.Collections.IEnumerable
                        ?? dropDownButton.Items;
            return items.Cast<object>()
                .OfType<MenuItem>()
                .Where(item => item.IsCheckable && !string.IsNullOrEmpty(item.GroupName))
                .ToList();
        }
    }

    bool ISelectionProvider.CanSelectMultiple
        => GroupedMenuItems
            .Select(item => item.GroupName)
            .Distinct(StringComparer.Ordinal)
            .Skip(1)
            .Any();

    bool ISelectionProvider.IsSelectionRequired => true;

    IRawElementProviderSimple[] ISelectionProvider.GetSelection()
        => GroupedMenuItems
            .Where(item => item.IsChecked is true)
            .Select(
                item => CreatePeerForElement(item)
                        ?? new RibbonMenuItemAutomationPeer(item))
            .Select(ProviderFromPeer)
            .ToArray();
}

/// <summary>
/// Automation peer for <see cref="RibbonRadioButton"/>.
/// </summary>
public partial class RibbonRadioButtonAutomationPeer : RadioButtonAutomationPeer, ISelectionItemProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonRadioButtonAutomationPeer"/> class.
    /// </summary>
    public RibbonRadioButtonAutomationPeer(RibbonRadioButton owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonRadioButton);

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetHeaderName((FrameworkElement)Owner)
            : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
        => AutomationPeerHelpers.GetAccessKey(
            (FrameworkElement)Owner,
            base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore() => [];

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.SelectionItem
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public new void AddToSelection()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        base.AddToSelection();
    }

    /// <inheritdoc/>
    public new void RemoveFromSelection()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        base.RemoveFromSelection();
    }

    /// <inheritdoc/>
    public new void Select()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        base.Select();
    }

    /// <inheritdoc/>
    public new bool IsSelected => base.IsSelected;

    /// <inheritdoc/>
    public new IRawElementProviderSimple SelectionContainer => base.SelectionContainer;
}

/// <summary>
/// Automation peer for <see cref="RibbonSplitButton"/>.
/// The outer peer is the single control-view element and exposes both the primary
/// <see cref="IInvokeProvider"/> action and the inherited expand/collapse action.
/// </summary>
public partial class RibbonSplitButtonAutomationPeer : RibbonDropDownButtonAutomationPeer, IInvokeProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonSplitButtonAutomationPeer"/> class.
    /// </summary>
    public RibbonSplitButtonAutomationPeer(RibbonSplitButton owner)
        : base(owner)
    {
    }

    private RibbonSplitButton OwnerSplitButton => (RibbonSplitButton)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonSplitButton);

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.SplitButton;

    /// <inheritdoc/>
    protected override string GetAutomationIdCore()
        => base.GetAutomationIdCore();

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Invoke
           && OwnerSplitButton.IsButtonEnabled
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Invoke()
    {
        AutomationProviderGuard.Validate(
            this,
            OwnerSplitButton.IsButtonEnabled,
            "The split-button primary action is disabled.");
        OwnerSplitButton.InvokePrimaryAction();
    }
}

/// <summary>
/// Automation peer for <see cref="RibbonTextBox"/>.
/// </summary>
public partial class RibbonTextBoxAutomationPeer : TextBoxAutomationPeer, IValueProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTextBoxAutomationPeer"/> class.
    /// </summary>
    public RibbonTextBoxAutomationPeer(RibbonTextBox owner)
        : base(owner)
    {
    }

    private RibbonTextBox OwnerTextBox => (RibbonTextBox)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonTextBox);

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

    /// <inheritdoc/>
    protected override void SetFocusCore()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        AutomationProviderGuard.EnsureAvailable(
            ((RibbonTextBox)Owner).FocusEditorForAutomation(),
            "The ribbon text editor could not receive focus.");
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore() => [];

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Value
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public
#if !WINDOWS
        new
#endif
        bool IsReadOnly => RunOnOwnerThread(() => OwnerTextBox.IsReadOnly);

    /// <inheritdoc/>
    public
#if !WINDOWS
        new
#endif
        string Value => RunOnOwnerThread(() => OwnerTextBox.Text ?? string.Empty);

    /// <inheritdoc/>
    public
#if !WINDOWS
        new
#endif
        void SetValue(string value)
    {
        RunOnOwnerThread(
            () =>
            {
                AutomationProviderGuard.Validate(
                    this,
                    !OwnerTextBox.IsReadOnly,
                    "The ribbon text editor is read-only.");
                ArgumentNullException.ThrowIfNull(value);
                OwnerTextBox.Text = value;
                return true;
            });
    }

    private T RunOnOwnerThread<T>(Func<T> action)
    {
        if (OwnerTextBox.DispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        using var completion = new System.Threading.ManualResetEventSlim();
        Exception? dispatchException = null;
        T result = default!;
        if (!OwnerTextBox.DispatcherQueue.TryEnqueue(
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
            throw new InvalidOperationException(
                "Could not dispatch the ribbon text automation action.");
        }

        if (!completion.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException(
                "The ribbon text automation action timed out.");
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

/// <summary>
/// Automation peer for <see cref="RibbonToggleButton"/>.
/// </summary>
public partial class RibbonToggleButtonAutomationPeer : ToggleButtonAutomationPeer, IToggleProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonToggleButtonAutomationPeer"/> class.
    /// </summary>
    public RibbonToggleButtonAutomationPeer(RibbonToggleButton owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore() => "RibbonToggleButton";

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetHeaderOrPlaceholderName((FrameworkElement)Owner)
            : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
        => AutomationPeerHelpers.GetAccessKey(
            (FrameworkElement)Owner,
            base.GetAccessKeyCore());

    /// <inheritdoc/>
    protected override string GetHelpTextCore()
    {
        var helpText = base.GetHelpTextCore();
        return string.IsNullOrWhiteSpace(helpText)
            ? ((RibbonToggleButton)Owner).ScreenTipText
            : helpText;
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore() => [];

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Toggle
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public new void Toggle()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        base.Toggle();
    }
}
