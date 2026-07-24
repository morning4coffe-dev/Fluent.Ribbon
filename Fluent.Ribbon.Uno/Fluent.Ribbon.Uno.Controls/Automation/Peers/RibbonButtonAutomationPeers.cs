namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Dispatching;

/// <summary>
/// Automation peer for <see cref="RibbonButton"/>.
/// </summary>
public partial class RibbonButtonAutomationPeer : ButtonAutomationPeer
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
            ? AutomationPeerHelpers.GetObjectName(OwnerButton.Header)
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
}

/// <summary>
/// Automation peer for <see cref="RibbonCheckBox"/>.
/// </summary>
public partial class RibbonCheckBoxAutomationPeer : CheckBoxAutomationPeer
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
            ? AutomationPeerHelpers.GetHeaderName((FrameworkElement)Owner)
            : name;
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
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetHeaderName((FrameworkElement)Owner)
            : name;
    }
}

/// <summary>
/// Runtime automation peer used by <see cref="RibbonComboBox"/>.
/// </summary>
internal sealed partial class RibbonComboBoxAccessibleAutomationPeer : FrameworkElementAutomationPeer,
    IExpandCollapseProvider,
    IItemContainerProvider,
    IScrollItemProvider,
    ISelectionProvider,
    IValueProvider
{
    private ExpandCollapseState expandCollapseState;
    private readonly List<RibbonComboBoxItemDataAutomationPeer?> itemPeers = [];
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
        var name = base.GetNameCore();
        return string.IsNullOrWhiteSpace(name)
            ? AutomationPeerHelpers.GetHeaderName((FrameworkElement)Owner)
            : name;
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

        for (var index = 0; index < OwnerComboBox.Items.Count; index++)
        {
            var peer = GetContainerPeer(index) ?? GetDataPeer(index);
            if (peer is not null && !children.Contains(peer))
            {
                children.Add(peer);
            }
        }

        return children.Count == 0 ? null : children;
    }

    public new object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    public void Expand()
    {
        QueueDropDownState(isOpen: true, delay: TimeSpan.FromMilliseconds(100));
    }

    public void Collapse() => QueueDropDownState(isOpen: false, delay: TimeSpan.Zero);

    public ExpandCollapseState ExpandCollapseState => expandCollapseState;

    public bool CanSelectMultiple => false;

    public bool IsSelectionRequired => false;

    public bool IsReadOnly => !OwnerComboBox.IsEditable;

    public string Value => RunOnOwnerThread(
        () => OwnerComboBox.Text
              ?? AutomationPeerHelpers.GetObjectName(OwnerComboBox.SelectedItem));

    public IRawElementProviderSimple[] GetSelection()
        => RunOnOwnerThread(GetSelectionProviders);

    public void ScrollIntoView()
        => RunOnOwnerThread(() => OwnerComboBox.StartBringIntoView());

    public void SetValue(string value)
    {
        if (!OwnerComboBox.IsEditable)
        {
            throw new InvalidOperationException("The ComboBox is not editable.");
        }

        RunOnOwnerThread(() => OwnerComboBox.Text = value);
    }

    public IRawElementProviderSimple? FindItemByProperty(
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

        while (itemPeers.Count <= index)
        {
            itemPeers.Add(null);
        }

        var item = OwnerComboBox.Items[index];
        var peer = itemPeers[index];
        if (peer is null || !ReferenceEquals(peer.Item, item))
        {
            peer = new RibbonComboBoxItemDataAutomationPeer(
                item,
                index,
                GetItemOccurrence(item, index),
                GetItemName(index),
                this);
            itemPeers[index] = peer;
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
            () =>
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
            });

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
                && (OwnerComboBox.Items[index] is not Control control || control.IsEnabled));

    internal void SelectItem(int index)
        => RunOnOwnerThread(
            () =>
            {
                if (!IsItemEnabled(index))
                {
                    throw new ElementNotEnabledException();
                }

                OwnerComboBox.SelectedIndex = index;
            });

    internal void RemoveItemFromSelection(int index)
        => RunOnOwnerThread(
            () =>
            {
                if (OwnerComboBox.SelectedIndex == index)
                {
                    OwnerComboBox.SelectedIndex = -1;
                }
            });

    internal void ScrollItemIntoView(int index)
        => RunOnOwnerThread(
            () =>
            {
                if (OwnerComboBox.ContainerFromIndex(index) is UIElement container)
                {
                    container.StartBringIntoView();
                }
            });

    private void OnItemsVectorChanged(
        Windows.Foundation.Collections.IObservableVector<object> sender,
        Windows.Foundation.Collections.IVectorChangedEventArgs args)
    {
        itemPeers.Clear();
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
            PatternInterface.SelectionItem when IsEnabledCore() => this,
            _ => base.GetPatternCore(patternInterface),
        };
    }

    public new object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    public void ScrollIntoView() => owner.ScrollItemIntoView(GetCurrentIndex());

    public void AddToSelection() => Select();

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
public partial class RibbonDropDownButtonAutomationPeer : RibbonHeaderedControlAutomationPeer, IExpandCollapseProvider
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
    protected override string GetLocalizedControlTypeCore() => "drop-down button";

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
        => patternInterface == PatternInterface.ExpandCollapse
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface) => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Collapse()
    {
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
}

/// <summary>
/// Automation peer for <see cref="RibbonRadioButton"/>.
/// </summary>
public partial class RibbonRadioButtonAutomationPeer : RadioButtonAutomationPeer
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
}

/// <summary>
/// Automation peer for <see cref="RibbonSplitButton"/>.
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
    {
        var automationId = base.GetAutomationIdCore();
        return string.IsNullOrWhiteSpace(automationId)
            ? nameof(RibbonSplitButton)
            : automationId;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Invoke
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc/>
    public void Invoke()
    {
        if (OwnerSplitButton.IsEnabled)
        {
            OwnerSplitButton.InvokePrimaryAction();
        }
    }
}

/// <summary>
/// Automation peer for <see cref="RibbonTextBox"/>.
/// </summary>
public partial class RibbonTextBoxAutomationPeer : TextBoxAutomationPeer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTextBoxAutomationPeer"/> class.
    /// </summary>
    public RibbonTextBoxAutomationPeer(RibbonTextBox owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string GetClassNameCore() => nameof(RibbonTextBox);

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
/// Automation peer for <see cref="RibbonToggleButton"/>.
/// </summary>
public partial class RibbonToggleButtonAutomationPeer : ToggleButtonAutomationPeer
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
            ? AutomationPeerHelpers.GetHeaderName((FrameworkElement)Owner)
            : name;
    }
}
