namespace FluentUno.Compatibility.XamlTests;

using Fluent;
using Microsoft.UI.Xaml;

/// <summary>Consumer compilation coverage for WPF calls and the existing Uno provider API.</summary>
public static class PortRibbonApiFixture
{
    public static void ExerciseConcreteControls(
        Ribbon ribbon,
        Fluent.Button button,
        Fluent.ToggleButton toggle,
        Fluent.CheckBox checkBox,
        Fluent.RadioButton radio,
        Fluent.DropDownButton dropDown,
        Fluent.SplitButton split,
        Fluent.ComboBox comboBox,
        Fluent.TextBox textBox,
        Fluent.Spinner spinner,
        Fluent.MenuItem menuItem,
        RibbonGroupBox group,
        RibbonButton nativeButton)
    {
        ribbon.AddToQuickAccessToolBar(button);
        _ = ribbon.IsInQuickAccessToolBar(button);
        ribbon.RemoveFromQuickAccessToolBar(button);
        ribbon.AddToQuickAccessToolBar(toggle);
        _ = ribbon.IsInQuickAccessToolBar(toggle);
        ribbon.RemoveFromQuickAccessToolBar(toggle);
        ribbon.AddToQuickAccessToolBar(checkBox);
        _ = ribbon.IsInQuickAccessToolBar(checkBox);
        ribbon.RemoveFromQuickAccessToolBar(checkBox);
        ribbon.AddToQuickAccessToolBar(radio);
        _ = ribbon.IsInQuickAccessToolBar(radio);
        ribbon.RemoveFromQuickAccessToolBar(radio);
        ribbon.AddToQuickAccessToolBar(dropDown);
        _ = ribbon.IsInQuickAccessToolBar(dropDown);
        ribbon.RemoveFromQuickAccessToolBar(dropDown);
        ribbon.AddToQuickAccessToolBar(split);
        _ = ribbon.IsInQuickAccessToolBar(split);
        ribbon.RemoveFromQuickAccessToolBar(split);
        ribbon.AddToQuickAccessToolBar(comboBox);
        _ = ribbon.IsInQuickAccessToolBar(comboBox);
        ribbon.RemoveFromQuickAccessToolBar(comboBox);
        ribbon.AddToQuickAccessToolBar(textBox);
        _ = ribbon.IsInQuickAccessToolBar(textBox);
        ribbon.RemoveFromQuickAccessToolBar(textBox);
        ribbon.AddToQuickAccessToolBar(spinner);
        _ = ribbon.IsInQuickAccessToolBar(spinner);
        ribbon.RemoveFromQuickAccessToolBar(spinner);
        ribbon.AddToQuickAccessToolBar(menuItem);
        _ = ribbon.IsInQuickAccessToolBar(menuItem);
        ribbon.RemoveFromQuickAccessToolBar(menuItem);
        ribbon.AddToQuickAccessToolBar(group);
        _ = ribbon.IsInQuickAccessToolBar(group);
        ribbon.RemoveFromQuickAccessToolBar(group);
        ribbon.AddToQuickAccessToolBar(nativeButton);
        _ = ribbon.IsInQuickAccessToolBar(nativeButton);
        ribbon.RemoveFromQuickAccessToolBar(nativeButton);

        Action<Fluent.Button> add = ribbon.AddToQuickAccessToolBar;
        Func<Fluent.Button, bool> contains = ribbon.IsInQuickAccessToolBar;
        Action<Fluent.Button> remove = ribbon.RemoveFromQuickAccessToolBar;
        add(button);
        _ = contains(button);
        remove(button);
    }

    public static void ExerciseTypedVariables(
        Ribbon ribbon,
        UIElement? element,
        FrameworkElement frameworkElement,
        IQuickAccessItemProvider? provider,
        NonVisualProvider nonVisualProvider)
    {
        ribbon.AddToQuickAccessToolBar(element);
        _ = ribbon.IsInQuickAccessToolBar(element);
        ribbon.RemoveFromQuickAccessToolBar(element);
        ribbon.AddToQuickAccessToolBar(frameworkElement);
        _ = ribbon.IsInQuickAccessToolBar(frameworkElement);
        ribbon.RemoveFromQuickAccessToolBar(frameworkElement);
        ribbon.AddToQuickAccessToolBar(provider);
        _ = ribbon.IsInQuickAccessToolBar(provider);
        ribbon.RemoveFromQuickAccessToolBar(provider);
        ribbon.AddToQuickAccessToolBar(nonVisualProvider);
        _ = ribbon.IsInQuickAccessToolBar(nonVisualProvider);
        ribbon.RemoveFromQuickAccessToolBar(nonVisualProvider);
        ribbon.AddToQuickAccessToolBar(null);
        _ = ribbon.IsInQuickAccessToolBar(null);
        ribbon.RemoveFromQuickAccessToolBar(null);
    }

    public static void ExerciseGenericProvider<T>(Ribbon ribbon, T provider)
        where T : IQuickAccessItemProvider
    {
        ribbon.AddToQuickAccessToolBar(provider);
        _ = ribbon.IsInQuickAccessToolBar(provider);
        ribbon.RemoveFromQuickAccessToolBar(provider);
    }

    public sealed class NonVisualProvider : IQuickAccessItemProvider
    {
        public bool CanAddToQuickAccessToolBar { get; set; } = true;

        public FrameworkElement CreateQuickAccessItem() =>
            new Microsoft.UI.Xaml.Controls.Button { Content = "Nonvisual provider" };
    }
}
