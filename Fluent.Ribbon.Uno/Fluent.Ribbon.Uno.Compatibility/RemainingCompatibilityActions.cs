using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Fluent;

public partial class SplitButton
{
    /// <inheritdoc />
    public override FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new SplitButton
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false,
            CanAddButtonToQuickAccessToolBar = false,
            ItemsSource = CreateQuickAccessItems()
        };

        BindQuickAccessItem(clone);
        BindQuickAccessItemDropDownEvents(clone);
        clone.Click += (_, args) => ForwardQuickAccessPrimaryAction(args);
        return clone;
    }

    /// <summary>Binds the portable quick-access surface to another facade instance.</summary>
    protected override void BindQuickAccessItem(FrameworkElement element)
    {
        base.BindQuickAccessItem(element);
        CompatibilityQuickAccessBindings.Apply(this, element);
    }

    /// <inheritdoc />
    public new KeyTipPressedResult OnKeyTipPressed() => base.OnKeyTipPressed();

    /// <inheritdoc />
    public new void OnKeyTipBack() => base.OnKeyTipBack();

    /// <inheritdoc />
    public new void UpdateSimplifiedState(bool isSimplified) => base.UpdateSimplifiedState(isSimplified);

    /// <inheritdoc />
    public IEnumerable<KeyTipInformation> GetKeyTipInformations(bool hide)
    {
        if (string.IsNullOrEmpty(KeyTip) is false
            && PrimaryActionTarget is not null)
        {
            var primaryKeyTip = string.IsNullOrEmpty(SecondaryKeyTip)
                ? KeyTip + PrimaryActionKeyTipPostfix
                : KeyTip;
            yield return new KeyTipInformation(primaryKeyTip, PrimaryActionTarget, hide)
            {
                VisualTarget = this
            };
        }

        if (string.IsNullOrEmpty(SecondaryKeyTip) is false)
        {
            yield return new KeyTipInformation(SecondaryKeyTip, this, hide);
        }
        else if (string.IsNullOrEmpty(KeyTip) is false)
        {
            yield return new KeyTipInformation(KeyTip + SecondaryActionKeyTipPostfix, this, hide);
        }
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
}

public partial class GalleryItem
{
    /// <summary>Raises the facade click action through the core gallery-item behavior.</summary>
    public void RaiseClick()
    {
        OnKeyTipPressed();
    }

    /// <inheritdoc />
    public new KeyTipPressedResult OnKeyTipPressed()
    {
        return base.OnKeyTipPressed();
    }

    /// <inheritdoc />
    public new void OnKeyTipBack() => base.OnKeyTipBack();
}

public partial class Spinner
{
    /// <inheritdoc />
    public override FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new Spinner
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        BindQuickAccessItem(clone);
        return clone;
    }

    /// <summary>Binds the portable quick-access surface to another facade instance.</summary>
    protected virtual void BindQuickAccessItem(FrameworkElement element)
    {
        RibbonControl.BindQuickAccessItem(this, element);
        CompatibilityQuickAccessBindings.Apply(this, element);
    }

    /// <summary>Selects all text in the inner editor when the template is available.</summary>
    public void SelectAll()
    {
        if (GetTemplateChild("PART_TextBox") is Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            textBox.SelectAll();
        }
    }

    /// <inheritdoc />
    public override KeyTipPressedResult OnKeyTipPressed()
    {
        if (GetTemplateChild("PART_TextBox") is not Microsoft.UI.Xaml.Controls.TextBox textBox)
        {
            return KeyTipPressedResult.Empty;
        }

        textBox.SelectAll();
        textBox.Focus(FocusState.Programmatic);
        return new KeyTipPressedResult(true, false);
    }

    /// <inheritdoc />
    public override void OnKeyTipBack()
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

internal static class CompatibilityVisualTree
{
    internal static void CloseAncestorDropDown(DependencyObject element)
    {
        for (var current = VisualTreeHelper.GetParent(element);
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            switch (current)
            {
                case RibbonDropDownButton dropDownButton:
                    dropDownButton.CloseDropDown();
                    return;
            }
        }
    }
}
