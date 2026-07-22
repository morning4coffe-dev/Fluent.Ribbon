using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace Fluent;

internal sealed record QuickAccessBindingContract(
    string SourceProperty,
    Func<DependencyProperty> SourcePropertyAccessor,
    string TargetDependencyProperty,
    Func<DependencyProperty> TargetPropertyAccessor,
    BindingMode Mode);

internal static class CompatibilityQuickAccessBindings
{
    private static readonly IReadOnlyList<QuickAccessBindingContract> ButtonContracts =
    [
        OneWay(nameof(Button.Header), static () => RibbonButton.HeaderProperty),
        OneWay(nameof(Button.Icon), static () => Button.IconProperty),
        OneWay(nameof(Button.LargeIcon), static () => Button.LargeIconProperty),
        OneWay(nameof(Button.MediumIcon), static () => Button.MediumIconProperty),
        OneWay(nameof(Button.Command), static () => Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandProperty),
        OneWay(nameof(Button.CommandParameter), static () => Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandParameterProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> ToggleButtonContracts =
    [
        OneWay(nameof(ToggleButton.Header), static () => RibbonToggleButton.HeaderProperty),
        OneWay(nameof(ToggleButton.Icon), static () => ToggleButton.IconProperty),
        OneWay(nameof(ToggleButton.LargeIcon), static () => ToggleButton.LargeIconProperty),
        OneWay(nameof(ToggleButton.MediumIcon), static () => ToggleButton.MediumIconProperty),
        OneWay(nameof(ToggleButton.Command), static () => Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandProperty),
        OneWay(nameof(ToggleButton.CommandParameter), static () => Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandParameterProperty),
        TwoWay(nameof(ToggleButton.IsChecked), static () => Microsoft.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> CheckBoxContracts =
    [
        OneWay(nameof(CheckBox.Header), static () => RibbonCheckBox.HeaderProperty),
        OneWay(nameof(CheckBox.Icon), static () => CheckBox.IconProperty),
        OneWay(nameof(CheckBox.LargeIcon), static () => CheckBox.LargeIconProperty),
        OneWay(nameof(CheckBox.MediumIcon), static () => CheckBox.MediumIconProperty),
        OneWay(nameof(CheckBox.Command), static () => Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandProperty),
        OneWay(nameof(CheckBox.CommandParameter), static () => Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandParameterProperty),
        TwoWay(nameof(CheckBox.IsChecked), static () => Microsoft.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> RadioButtonContracts =
    [
        OneWay(nameof(RadioButton.Header), static () => RibbonRadioButton.HeaderProperty),
        OneWay(nameof(RadioButton.Icon), static () => RadioButton.IconProperty),
        OneWay(nameof(RadioButton.LargeIcon), static () => RadioButton.LargeIconProperty),
        OneWay(nameof(RadioButton.MediumIcon), static () => RadioButton.MediumIconProperty),
        OneWay(nameof(RadioButton.Command), static () => Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandProperty),
        OneWay(nameof(RadioButton.CommandParameter), static () => Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandParameterProperty),
        TwoWay(nameof(RadioButton.IsChecked), static () => Microsoft.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> TextBoxContracts =
    [
        OneWay(nameof(TextBox.Header), static () => RibbonTextBox.HeaderProperty),
        OneWay(nameof(TextBox.Icon), static () => TextBox.IconProperty),
        OneWay(nameof(TextBox.MediumIcon), static () => TextBox.MediumIconProperty),
        TwoWay(nameof(TextBox.Text), static () => Microsoft.UI.Xaml.Controls.TextBox.TextProperty),
        OneWay(nameof(TextBox.IsReadOnly), static () => Microsoft.UI.Xaml.Controls.TextBox.IsReadOnlyProperty),
        OneWay(nameof(TextBox.MaxLength), static () => Microsoft.UI.Xaml.Controls.TextBox.MaxLengthProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> ComboBoxContracts =
    [
        OneWay(nameof(ComboBox.Header), static () => RibbonComboBox.HeaderProperty),
        OneWay(nameof(ComboBox.Icon), static () => ComboBox.IconProperty),
        OneWay(nameof(ComboBox.MediumIcon), static () => ComboBox.MediumIconProperty),
        OneWay(nameof(ComboBox.ItemsSource), static () => Microsoft.UI.Xaml.Controls.ItemsControl.ItemsSourceProperty),
        TwoWay(nameof(ComboBox.SelectedItem), static () => Microsoft.UI.Xaml.Controls.Primitives.Selector.SelectedItemProperty),
        TwoWay(nameof(ComboBox.SelectedIndex), static () => Microsoft.UI.Xaml.Controls.Primitives.Selector.SelectedIndexProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> DropDownButtonContracts =
    [
        OneWay(nameof(DropDownButton.Header), static () => RibbonDropDownButton.HeaderProperty),
        OneWay(nameof(DropDownButton.Icon), static () => DropDownButton.IconProperty),
        OneWay(nameof(DropDownButton.LargeIcon), static () => DropDownButton.LargeIconProperty),
        OneWay(nameof(DropDownButton.MediumIcon), static () => DropDownButton.MediumIconProperty),
        OneWay(nameof(DropDownButton.MenuHeader), static () => RibbonDropDownButton.MenuHeaderProperty),
        OneWay(nameof(DropDownButton.HasTriangle), static () => RibbonDropDownButton.HasTriangleProperty),
        OneWay(nameof(DropDownButton.ResizeMode), static () => RibbonDropDownButton.ResizeModeProperty),
        OneWay(nameof(DropDownButton.MaxDropDownHeight), static () => RibbonDropDownButton.MaxDropDownHeightProperty),
        OneWay(nameof(DropDownButton.DropDownHeight), static () => RibbonDropDownButton.DropDownHeightProperty),
        OneWay(nameof(DropDownButton.ClosePopupOnMouseDown), static () => RibbonDropDownButton.ClosePopupOnMouseDownProperty),
        OneWay(nameof(DropDownButton.ClosePopupOnMouseDownDelay), static () => RibbonDropDownButton.ClosePopupOnMouseDownDelayProperty),
        TwoWay(nameof(DropDownButton.IsDropDownOpen), static () => RibbonDropDownButton.IsDropDownOpenProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> SplitButtonContracts =
    [
        OneWay(nameof(SplitButton.DropDownToolTip), static () => RibbonSplitButton.DropDownToolTipProperty),
        OneWay(nameof(SplitButton.IsCheckable), static () => RibbonSplitButton.IsCheckableProperty),
        OneWay(nameof(SplitButton.IsButtonEnabled), static () => RibbonSplitButton.IsButtonEnabledProperty),
        OneWay(nameof(SplitButton.IsDefinitive), static () => RibbonSplitButton.IsDefinitiveProperty),
        TwoWay(nameof(SplitButton.IsChecked), static () => RibbonSplitButton.IsCheckedProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> MenuItemContracts =
    [
        OneWay(nameof(MenuItem.Header), static () => RibbonMenuItem.HeaderProperty),
        OneWay(nameof(MenuItem.Description), static () => RibbonMenuItem.DescriptionProperty),
        OneWay(nameof(MenuItem.Icon), static () => MenuItem.IconProperty),
        OneWay(nameof(MenuItem.Command), static () => RibbonMenuItem.CommandProperty),
        OneWay(nameof(MenuItem.CommandParameter), static () => RibbonMenuItem.CommandParameterProperty),
        OneWay(nameof(MenuItem.Items), static () => RibbonMenuItem.ItemsProperty),
        OneWay(nameof(MenuItem.IsCheckable), static () => MenuItem.IsCheckableProperty),
        OneWay(nameof(MenuItem.GroupName), static () => MenuItem.GroupNameProperty),
        OneWay(nameof(MenuItem.IsDefinitive), static () => MenuItem.IsDefinitiveProperty),
        TwoWay(nameof(MenuItem.IsChecked), static () => MenuItem.IsCheckedProperty)
    ];

    private static readonly IReadOnlyList<QuickAccessBindingContract> SpinnerContracts =
    [
        OneWay(nameof(Spinner.Header), static () => RibbonSpinner.HeaderProperty),
        OneWay(nameof(Spinner.Icon), static () => Spinner.IconProperty),
        OneWay(nameof(Spinner.MediumIcon), static () => Spinner.MediumIconProperty),
        TwoWay(nameof(Spinner.Value), static () => RibbonSpinner.ValueProperty),
        TwoWay(nameof(Spinner.Text), static () => RibbonSpinner.TextProperty),
        OneWay(nameof(Spinner.Minimum), static () => RibbonSpinner.MinimumProperty),
        OneWay(nameof(Spinner.Maximum), static () => RibbonSpinner.MaximumProperty),
        OneWay(nameof(Spinner.Increment), static () => RibbonSpinner.IncrementProperty),
        OneWay(nameof(Spinner.Format), static () => RibbonSpinner.FormatProperty),
        OneWay(nameof(Spinner.Delay), static () => RibbonSpinner.DelayProperty),
        OneWay(nameof(Spinner.Interval), static () => RibbonSpinner.IntervalProperty),
        OneWay(nameof(Spinner.SelectAllTextOnFocus), static () => RibbonSpinner.SelectAllTextOnFocusProperty)
    ];

    internal static IReadOnlyList<QuickAccessBindingContract> GetContracts(Type wrapperType)
    {
        ArgumentNullException.ThrowIfNull(wrapperType);

        if (typeof(Button).IsAssignableFrom(wrapperType))
        {
            return ButtonContracts;
        }

        if (typeof(ToggleButton).IsAssignableFrom(wrapperType))
        {
            return ToggleButtonContracts;
        }

        if (typeof(CheckBox).IsAssignableFrom(wrapperType))
        {
            return CheckBoxContracts;
        }

        if (typeof(RadioButton).IsAssignableFrom(wrapperType))
        {
            return RadioButtonContracts;
        }

        if (typeof(TextBox).IsAssignableFrom(wrapperType))
        {
            return TextBoxContracts;
        }

        if (typeof(ComboBox).IsAssignableFrom(wrapperType))
        {
            return ComboBoxContracts;
        }

        if (typeof(SplitButton).IsAssignableFrom(wrapperType))
        {
            return SplitButtonContracts;
        }

        if (typeof(DropDownButton).IsAssignableFrom(wrapperType))
        {
            return DropDownButtonContracts;
        }

        if (typeof(MenuItem).IsAssignableFrom(wrapperType))
        {
            return MenuItemContracts;
        }

        if (typeof(Spinner).IsAssignableFrom(wrapperType))
        {
            return SpinnerContracts;
        }

        return [];
    }

    internal static void Apply(FrameworkElement source, FrameworkElement target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        foreach (var contract in GetContracts(source.GetType()))
        {
            Synchronize(
                source,
                contract.SourcePropertyAccessor(),
                target,
                contract.TargetPropertyAccessor(),
                contract.Mode is BindingMode.TwoWay);
        }
    }

    internal static bool CanResolveTarget(Type targetType, string fieldName)
    {
        return GetContracts(targetType).Any(
            contract => contract.TargetDependencyProperty.Equals(fieldName, StringComparison.Ordinal));
    }

    internal static void Synchronize(
        DependencyObject source,
        DependencyProperty sourceProperty,
        DependencyObject target,
        DependencyProperty targetProperty,
        bool twoWay = false)
    {
        CopyValue(source, sourceProperty, target, targetProperty);
        RegisterWeakSynchronization(source, sourceProperty, target, targetProperty);

        if (twoWay)
        {
            RegisterWeakSynchronization(target, targetProperty, source, sourceProperty);
        }
    }

    private static void RegisterWeakSynchronization(
        DependencyObject source,
        DependencyProperty sourceProperty,
        DependencyObject target,
        DependencyProperty targetProperty)
    {
        var weakTarget = new WeakReference<DependencyObject>(target);
        long token = 0;
        token = source.RegisterPropertyChangedCallback(
            sourceProperty,
            (sender, changedProperty) =>
            {
                if (weakTarget.TryGetTarget(out var liveTarget))
                {
                    CopyValue(sender, changedProperty, liveTarget, targetProperty);
                }
                else
                {
                    sender.UnregisterPropertyChangedCallback(changedProperty, token);
                }
            });
    }

    private static void CopyValue(
        DependencyObject source,
        DependencyProperty sourceProperty,
        DependencyObject target,
        DependencyProperty targetProperty)
    {
        var value = source.GetValue(sourceProperty);
        if (Equals(target.GetValue(targetProperty), value) is false)
        {
            target.SetValue(targetProperty, value);
        }
    }

    private static QuickAccessBindingContract OneWay(
        string propertyName,
        Func<DependencyProperty> dependencyPropertyAccessor) =>
        new(
            propertyName,
            dependencyPropertyAccessor,
            $"{propertyName}Property",
            dependencyPropertyAccessor,
            BindingMode.OneWay);

    private static QuickAccessBindingContract TwoWay(
        string propertyName,
        Func<DependencyProperty> dependencyPropertyAccessor) =>
        new(
            propertyName,
            dependencyPropertyAccessor,
            $"{propertyName}Property",
            dependencyPropertyAccessor,
            BindingMode.TwoWay);
}
