using System.Collections;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class CompatibilityWrapperActionTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public static IEnumerable<TestCaseData> WrapperInterfaces
    {
        get
        {
            yield return Interfaces<global::Fluent.Button>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IKeyTipedControl),
                typeof(global::Fluent.ISimplifiedRibbonControl));
            yield return Interfaces<global::Fluent.ToggleButton>(
                typeof(global::Fluent.IToggleButton),
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IKeyTipedControl),
                typeof(global::Fluent.ISimplifiedRibbonControl));
            yield return Interfaces<global::Fluent.CheckBox>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IKeyTipedControl),
                typeof(global::Fluent.ISimplifiedRibbonControl));
            yield return Interfaces<global::Fluent.RadioButton>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IKeyTipedControl),
                typeof(global::Fluent.ISimplifiedRibbonControl));
            yield return Interfaces<global::Fluent.TextBox>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IKeyTipedControl),
                typeof(global::Fluent.ISimplifiedRibbonControl));
            yield return Interfaces<global::Fluent.ComboBox>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IKeyTipedControl),
                typeof(global::Fluent.IDropDownControl),
                typeof(global::Fluent.ISimplifiedRibbonControl));
        }
    }

    [TestCaseSource(nameof(WrapperInterfaces))]
    public void Wrapper_ImplementsPortableActionInterfaces(Type wrapper, Type[] interfaces)
    {
        Assert.Multiple(() =>
        {
            foreach (var contract in interfaces)
            {
                Assert.That(contract.IsAssignableFrom(wrapper), Is.True, contract.Name);
            }
        });
    }

    [TestCase(typeof(global::Fluent.Button))]
    [TestCase(typeof(global::Fluent.ToggleButton))]
    [TestCase(typeof(global::Fluent.CheckBox))]
    [TestCase(typeof(global::Fluent.RadioButton))]
    [TestCase(typeof(global::Fluent.TextBox))]
    [TestCase(typeof(global::Fluent.ComboBox))]
    public void Wrapper_DeclaresQuickAccessAndKeyTipActions(Type wrapper)
    {
        Assert.Multiple(() =>
        {
            AssertMethod(wrapper, "CreateQuickAccessItem", typeof(FrameworkElement));
            AssertMethod(wrapper, "OnKeyTipPressed", typeof(global::Fluent.KeyTipPressedResult));
            AssertMethod(wrapper, "OnKeyTipBack", typeof(void));
            AssertMethod(wrapper, "UpdateSimplifiedState", typeof(void), typeof(bool));
        });
    }

    [TestCase(typeof(global::Fluent.Button))]
    [TestCase(typeof(global::Fluent.ToggleButton))]
    [TestCase(typeof(global::Fluent.CheckBox))]
    [TestCase(typeof(global::Fluent.RadioButton))]
    [TestCase(typeof(global::Fluent.TextBox))]
    [TestCase(typeof(global::Fluent.ComboBox))]
    public void QuickAccessInterface_DispatchesToWrapperCloneFactory(Type wrapper)
    {
        var map = wrapper.GetInterfaceMap(typeof(global::Fluent.IQuickAccessItemProvider));
        var index = Array.FindIndex(
            map.InterfaceMethods,
            method => method.Name == nameof(global::Fluent.IQuickAccessItemProvider.CreateQuickAccessItem));

        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        Assert.That(map.TargetMethods[index].DeclaringType, Is.EqualTo(wrapper));
    }

    public static IEnumerable<TestCaseData> BindingContracts
    {
        get
        {
            yield return Contracts<global::Fluent.Button>(
                OneWay("Header"), OneWay("Icon"), OneWay("LargeIcon"), OneWay("MediumIcon"),
                OneWay("Command"), OneWay("CommandParameter"));
            yield return Contracts<global::Fluent.ToggleButton>(
                OneWay("Header"), OneWay("Icon"), OneWay("LargeIcon"), OneWay("MediumIcon"),
                OneWay("Command"), OneWay("CommandParameter"), TwoWay("IsChecked"));
            yield return Contracts<global::Fluent.CheckBox>(
                OneWay("Header"), OneWay("Icon"), OneWay("LargeIcon"), OneWay("MediumIcon"),
                OneWay("Command"), OneWay("CommandParameter"), TwoWay("IsChecked"));
            yield return Contracts<global::Fluent.RadioButton>(
                OneWay("Header"), OneWay("Icon"), OneWay("LargeIcon"), OneWay("MediumIcon"),
                OneWay("Command"), OneWay("CommandParameter"), TwoWay("IsChecked"));
            yield return Contracts<global::Fluent.TextBox>(
                OneWay("Header"), OneWay("Icon"), OneWay("MediumIcon"), TwoWay("Text"),
                OneWay("IsReadOnly"), OneWay("MaxLength"));
            yield return Contracts<global::Fluent.ComboBox>(
                OneWay("Header"), OneWay("Icon"), OneWay("MediumIcon"), OneWay("ItemsSource"),
                TwoWay("SelectedItem"), TwoWay("SelectedIndex"));
        }
    }

    [TestCaseSource(nameof(BindingContracts))]
    public void QuickAccessBindings_DeclareExpectedDirectionAndResolvableTargets(
        Type wrapper,
        (string Property, BindingMode Mode)[] expected)
    {
        var actual = ReadBindingContracts(wrapper);

        Assert.Multiple(() =>
        {
            Assert.That(
                actual.Select(contract => (contract.Property, contract.Mode)),
                Is.EquivalentTo(expected));

            foreach (var contract in actual)
            {
                Assert.That(contract.SourcePropertyAccessor, Is.Not.Null);
                Assert.That(contract.TargetPropertyAccessor, Is.Not.Null);
                Assert.That(
                    CanResolveBindingTarget(wrapper, contract.TargetDependencyProperty),
                    Is.True,
                    $"{wrapper.Name}.{contract.TargetDependencyProperty}");
            }
        });
    }

    [Test]
    public void QuickAccessBindings_UseBaseFacadeContractForConsumerSubclass()
    {
        var baseContracts = ReadBindingContracts(typeof(global::Fluent.Button));
        var subclassContracts = ReadBindingContracts(typeof(ConsumerButton));

        Assert.That(
            subclassContracts.Select(contract => (contract.Property, contract.Mode)),
            Is.EquivalentTo(baseContracts.Select(contract => (contract.Property, contract.Mode))));
    }

    private static IReadOnlyList<(
        string Property,
        Func<DependencyProperty> SourcePropertyAccessor,
        string TargetDependencyProperty,
        Func<DependencyProperty> TargetPropertyAccessor,
        BindingMode Mode)>
        ReadBindingContracts(Type wrapper)
    {
        var assembly = typeof(global::Fluent.ToggleButton).Assembly;
        var bindingsType = assembly.GetType(
            "Fluent.CompatibilityQuickAccessBindings",
            throwOnError: true)!;
        var getContracts = bindingsType.GetMethod(
            "GetContracts",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var contracts = (IEnumerable)getContracts.Invoke(null, [wrapper])!;

        return contracts.Cast<object>()
            .Select(contract =>
            {
                var type = contract.GetType();
                return (
                    (string)type.GetProperty("SourceProperty")!.GetValue(contract)!,
                    (Func<DependencyProperty>)type.GetProperty("SourcePropertyAccessor")!.GetValue(contract)!,
                    (string)type.GetProperty("TargetDependencyProperty")!.GetValue(contract)!,
                    (Func<DependencyProperty>)type.GetProperty("TargetPropertyAccessor")!.GetValue(contract)!,
                    (BindingMode)type.GetProperty("Mode")!.GetValue(contract)!);
            })
            .ToArray();
    }

    private static bool CanResolveBindingTarget(Type wrapper, string fieldName)
    {
        var bindingsType = typeof(global::Fluent.ToggleButton).Assembly.GetType(
            "Fluent.CompatibilityQuickAccessBindings",
            throwOnError: true)!;
        var canResolve = bindingsType.GetMethod(
            "CanResolveTarget",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)canResolve.Invoke(null, [wrapper, fieldName])!;
    }

    private static void AssertMethod(
        Type wrapper,
        string methodName,
        Type returnType,
        params Type[] parameters)
    {
        var method = wrapper.GetMethod(
            methodName,
            DeclaredPublicInstance,
            binder: null,
            parameters,
            modifiers: null);
        Assert.That(method, Is.Not.Null, $"{wrapper.Name}.{methodName}");
        Assert.That(method!.ReturnType, Is.EqualTo(returnType), $"{wrapper.Name}.{methodName}");
    }

    private static TestCaseData Interfaces<TWrapper>(params Type[] interfaces) =>
        new(typeof(TWrapper), interfaces);

    private static TestCaseData Contracts<TWrapper>(
        params (string Property, BindingMode Mode)[] contracts) =>
        new(typeof(TWrapper), contracts);

    private static (string Property, BindingMode Mode) OneWay(string property) =>
        (property, BindingMode.OneWay);

    private static (string Property, BindingMode Mode) TwoWay(string property) =>
        (property, BindingMode.TwoWay);

    private sealed class ConsumerButton : global::Fluent.Button;
}
