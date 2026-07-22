using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;

namespace Fluent;

public partial class ToggleButton
{
    /// <inheritdoc />
    public new virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new ToggleButton
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        RibbonControl.BindQuickAccessItem(this, clone);
        CompatibilityQuickAccessBindings.Apply(this, clone);
        return clone;
    }

    /// <inheritdoc />
    public new virtual KeyTipPressedResult OnKeyTipPressed() => base.OnKeyTipPressed();

    /// <inheritdoc />
    public new virtual void OnKeyTipBack() => base.OnKeyTipBack();

    /// <inheritdoc />
    public new void UpdateSimplifiedState(bool isSimplified) => base.UpdateSimplifiedState(isSimplified);

    RibbonControlSizeDefinition IRibbonControl.SizeDefinition
    {
        get => SizeDefinition;
        set => SizeDefinition = value;
    }

    RibbonControlSizeDefinition ISimplifiedRibbonControl.SimplifiedSizeDefinition
    {
        get => SimplifiedSizeDefinition;
        set => SimplifiedSizeDefinition = value;
    }

    object? ILargeIconProvider.LargeIcon
    {
        get => LargeIcon;
        set => LargeIcon = value;
    }

    object? IMediumIconProvider.MediumIcon
    {
        get => MediumIcon;
        set => MediumIcon = value;
    }
}

public partial class CheckBox
{
    /// <inheritdoc />
    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new CheckBox
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        RibbonControl.BindQuickAccessItem(this, clone);
        CompatibilityQuickAccessBindings.Apply(this, clone);
        return clone;
    }

    /// <inheritdoc />
    public virtual KeyTipPressedResult OnKeyTipPressed()
    {
        ((IToggleProvider)new Automation.Peers.RibbonCheckBoxAutomationPeer(this)).Toggle();
        return KeyTipPressedResult.Empty;
    }

    /// <inheritdoc />
    public virtual void OnKeyTipBack()
    {
    }

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
    }

    RibbonControlSizeDefinition IRibbonControl.SizeDefinition
    {
        get => SizeDefinition;
        set => SizeDefinition = value;
    }

    RibbonControlSizeDefinition ISimplifiedRibbonControl.SimplifiedSizeDefinition
    {
        get => SimplifiedSizeDefinition;
        set => SimplifiedSizeDefinition = value;
    }

    object? ILargeIconProvider.LargeIcon
    {
        get => LargeIcon;
        set => LargeIcon = value;
    }

    object? IMediumIconProvider.MediumIcon
    {
        get => MediumIcon;
        set => MediumIcon = value;
    }

    string? IKeyTipedControl.KeyTip
    {
        get => KeyTip;
        set => KeyTip = value ?? string.Empty;
    }
}

public partial class RadioButton
{
    /// <inheritdoc />
    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new RadioButton
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        RibbonControl.BindQuickAccessItem(this, clone);
        CompatibilityQuickAccessBindings.Apply(this, clone);
        return clone;
    }

    /// <inheritdoc />
    public virtual KeyTipPressedResult OnKeyTipPressed()
    {
        ((ISelectionItemProvider)new Automation.Peers.RibbonRadioButtonAutomationPeer(this)).Select();
        return KeyTipPressedResult.Empty;
    }

    /// <inheritdoc />
    public virtual void OnKeyTipBack()
    {
    }

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
    }

    RibbonControlSizeDefinition IRibbonControl.SizeDefinition
    {
        get => SizeDefinition;
        set => SizeDefinition = value;
    }

    RibbonControlSizeDefinition ISimplifiedRibbonControl.SimplifiedSizeDefinition
    {
        get => SimplifiedSizeDefinition;
        set => SimplifiedSizeDefinition = value;
    }

    object? ILargeIconProvider.LargeIcon
    {
        get => LargeIcon;
        set => LargeIcon = value;
    }

    object? IMediumIconProvider.MediumIcon
    {
        get => MediumIcon;
        set => MediumIcon = value;
    }

    string? IKeyTipedControl.KeyTip
    {
        get => KeyTip;
        set => KeyTip = value ?? string.Empty;
    }
}

public partial class TextBox
{
    /// <inheritdoc />
    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new TextBox
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        BindQuickAccessItem(clone);
        return clone;
    }

    /// <inheritdoc />
    public virtual KeyTipPressedResult OnKeyTipPressed()
    {
        SelectAll();
        Focus(FocusState.Programmatic);
        return new KeyTipPressedResult(true, false);
    }

    /// <inheritdoc />
    public virtual void OnKeyTipBack()
    {
    }

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
    }

    RibbonControlSizeDefinition IRibbonControl.SizeDefinition
    {
        get => SizeDefinition;
        set => SizeDefinition = value;
    }

    RibbonControlSizeDefinition ISimplifiedRibbonControl.SimplifiedSizeDefinition
    {
        get => SimplifiedSizeDefinition;
        set => SimplifiedSizeDefinition = value;
    }

    object? IMediumIconProvider.MediumIcon
    {
        get => MediumIcon;
        set => MediumIcon = value;
    }

    string? IKeyTipedControl.KeyTip
    {
        get => KeyTip;
        set => KeyTip = value ?? string.Empty;
    }
}

public partial class ComboBox
{
    /// <inheritdoc />
    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new ComboBox
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        RibbonControl.BindQuickAccessItem(this, clone);
        CompatibilityQuickAccessBindings.Apply(this, clone);
        return clone;
    }

    /// <inheritdoc />
    public virtual KeyTipPressedResult OnKeyTipPressed()
    {
        Focus(FocusState.Programmatic);
        IsDropDownOpen = true;
        return new KeyTipPressedResult(true, true);
    }

    /// <inheritdoc />
    public virtual void OnKeyTipBack()
    {
    }

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
    {
        IsSimplified = isSimplified;
    }

    RibbonControlSizeDefinition IRibbonControl.SizeDefinition
    {
        get => SizeDefinition;
        set => SizeDefinition = value;
    }

    RibbonControlSizeDefinition ISimplifiedRibbonControl.SimplifiedSizeDefinition
    {
        get => SimplifiedSizeDefinition;
        set => SimplifiedSizeDefinition = value;
    }

    object? IMediumIconProvider.MediumIcon
    {
        get => MediumIcon;
        set => MediumIcon = value;
    }

    string? IKeyTipedControl.KeyTip
    {
        get => KeyTip;
        set => KeyTip = value ?? string.Empty;
    }
}
