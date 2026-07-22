namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

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
