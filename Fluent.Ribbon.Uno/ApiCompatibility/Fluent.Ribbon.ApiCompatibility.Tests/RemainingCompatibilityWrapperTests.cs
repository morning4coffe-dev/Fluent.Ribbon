using System.Collections;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class RemainingCompatibilityWrapperTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredPublicStatic =
        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IEnumerable<TestCaseData> InterfaceContracts
    {
        get
        {
            yield return Interfaces<global::Fluent.DropDownButton>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IDropDownControl),
                typeof(global::Fluent.ISimplifiedRibbonControl));
            yield return Interfaces<global::Fluent.SplitButton>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IDropDownControl),
                typeof(global::Fluent.IToggleButton),
                typeof(global::Fluent.Extensibility.IKeyTipInformationProvider),
                typeof(global::Fluent.ISimplifiedRibbonControl));
            yield return Interfaces<global::Fluent.MenuItem>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.IDropDownControl),
                typeof(global::Fluent.IToggleButton));
            yield return Interfaces<global::Fluent.GalleryItem>(
                typeof(global::Fluent.IKeyTipedControl));
            yield return Interfaces<global::Fluent.Spinner>(
                typeof(global::Fluent.IQuickAccessItemProvider),
                typeof(global::Fluent.IRibbonControl),
                typeof(global::Fluent.ISimplifiedRibbonControl));
            yield return Interfaces<global::Fluent.RibbonTabItem>(
                typeof(global::Fluent.IKeyTipedControl),
                typeof(global::Fluent.ISimplifiedStateControl));
        }
    }

    [TestCaseSource(nameof(InterfaceContracts))]
    public void Facade_ImplementsPortableWpfInterfaces(Type facade, Type[] interfaces)
    {
        Assert.Multiple(() =>
        {
            foreach (var contract in interfaces)
            {
                Assert.That(contract.IsAssignableFrom(facade), Is.True, contract.FullName);
            }
        });
    }

    public static IEnumerable<TestCaseData> DeclaredProperties
    {
        get
        {
            foreach (var type in new[]
                     {
                         typeof(global::Fluent.DropDownButton)
                     })
            {
                yield return Property(type, "SizeDefinition", typeof(global::Fluent.RibbonControlSizeDefinition));
                yield return Property(type, "SimplifiedSizeDefinition", typeof(global::Fluent.RibbonControlSizeDefinition));
                yield return Property(type, "LargeIcon", typeof(object));
                yield return Property(type, "MediumIcon", typeof(object));
                yield return Property(type, "CanAddToQuickAccessToolBar", typeof(bool));
            }

            yield return Property(typeof(global::Fluent.SplitButton), "CommandTarget", typeof(UIElement));
            yield return Property(typeof(global::Fluent.SplitButton), "PrimaryActionKeyTipPostfix", typeof(string));
            yield return Property(typeof(global::Fluent.SplitButton), "SecondaryActionKeyTipPostfix", typeof(string));
            yield return Property(typeof(global::Fluent.SplitButton), "SecondaryKeyTip", typeof(string));
            yield return Property(typeof(global::Fluent.SplitButton), "CanAddButtonToQuickAccessToolBar", typeof(bool));

            yield return Property(typeof(global::Fluent.MenuItem), "Size", typeof(global::Fluent.RibbonControlSize));
            yield return Property(typeof(global::Fluent.MenuItem), "SizeDefinition", typeof(global::Fluent.RibbonControlSizeDefinition));
            yield return Property(typeof(global::Fluent.MenuItem), "Icon", typeof(object));
            yield return Property(typeof(global::Fluent.MenuItem), "IsCheckable", typeof(bool));
            yield return Property(typeof(global::Fluent.MenuItem), "IsChecked", typeof(bool?));
            yield return Property(typeof(global::Fluent.MenuItem), "GroupName", typeof(string));
            yield return Property(typeof(global::Fluent.MenuItem), "IsDefinitive", typeof(bool));
            yield return Property(typeof(global::Fluent.MenuItem), "IsDropDownOpen", typeof(bool));

            yield return Property(typeof(global::Fluent.Gallery), "SelectedFilterGroups", typeof(string), canWrite: false);
            yield return Property(typeof(global::Fluent.GalleryItem), "IsDefinitive", typeof(bool));
            yield return Property(typeof(global::Fluent.GalleryItem), "CommandTarget", typeof(UIElement));

            yield return Property(typeof(global::Fluent.Spinner), "SizeDefinition", typeof(global::Fluent.RibbonControlSizeDefinition));
            yield return Property(typeof(global::Fluent.Spinner), "SimplifiedSizeDefinition", typeof(global::Fluent.RibbonControlSizeDefinition));
            yield return Property(typeof(global::Fluent.Spinner), "MediumIcon", typeof(object));
            yield return Property(typeof(global::Fluent.Spinner), "Icon", typeof(object));
            yield return Property(typeof(global::Fluent.Spinner), "CanAddToQuickAccessToolBar", typeof(bool));

            yield return Property(typeof(global::Fluent.RibbonTabItem), "Header", typeof(object));
            yield return Property(typeof(global::Fluent.RibbonTabItem), "HeaderTemplate", typeof(DataTemplate));
            yield return Property(typeof(global::Fluent.RibbonTabItem), "IsSelected", typeof(bool));
            yield return Property(typeof(global::Fluent.RibbonTabItem), "IsSimplified", typeof(bool));

            yield return Property(typeof(global::Fluent.StatusBarItem), "Value", typeof(string));
            yield return Property(typeof(global::Fluent.StatusBarItem), "IsCheckable", typeof(bool));
        }
    }

    [TestCaseSource(nameof(DeclaredProperties))]
    public void Facade_DeclaresExpectedDependencyPropertyContract(
        Type facade,
        string propertyName,
        Type propertyType,
        bool canWrite)
    {
        var property = facade.GetProperty(propertyName, DeclaredPublicInstance);
        var field = facade.GetField($"{propertyName}Property", DeclaredPublicStatic);

        Assert.Multiple(() =>
        {
            Assert.That(property, Is.Not.Null);
            Assert.That(property!.PropertyType, Is.EqualTo(propertyType));
            Assert.That(property.CanRead, Is.True);
            Assert.That(property.CanWrite, Is.EqualTo(canWrite));
            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(field.IsInitOnly, Is.True);
        });
    }

    public static IEnumerable<TestCaseData> ActionContracts
    {
        get
        {
            foreach (var type in new[]
                     {
                         typeof(global::Fluent.DropDownButton),
                         typeof(global::Fluent.SplitButton),
                         typeof(global::Fluent.MenuItem),
                         typeof(global::Fluent.Spinner)
                     })
            {
                yield return Method(type, "CreateQuickAccessItem", typeof(FrameworkElement));
            }

            foreach (var type in new[]
                     {
                         typeof(global::Fluent.DropDownButton),
                         typeof(global::Fluent.SplitButton),
                         typeof(global::Fluent.MenuItem),
                         typeof(global::Fluent.GalleryItem),
                         typeof(global::Fluent.Spinner),
                         typeof(global::Fluent.RibbonTabItem)
                     })
            {
                yield return Method(type, "OnKeyTipPressed", typeof(global::Fluent.KeyTipPressedResult));
                yield return Method(type, "OnKeyTipBack", typeof(void));
            }

            yield return Method(typeof(global::Fluent.GalleryItem), "RaiseClick", typeof(void));
            yield return Method(typeof(global::Fluent.Spinner), "SelectAll", typeof(void));
            yield return Method(
                typeof(global::Fluent.SplitButton),
                "GetKeyTipInformations",
                typeof(IEnumerable<global::Fluent.KeyTipInformation>),
                typeof(bool));
            yield return Method(
                typeof(global::Fluent.RibbonTabItem),
                "UpdateSimplifiedState",
                typeof(void),
                typeof(bool));
        }
    }

    [TestCaseSource(nameof(ActionContracts))]
    public void Facade_DeclaresPortableAction(
        Type facade,
        string methodName,
        Type returnType,
        Type[] parameters)
    {
        var method = facade.GetMethod(
            methodName,
            DeclaredPublicInstance,
            binder: null,
            parameters,
            modifiers: null);

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(returnType));
    }

    [TestCase(typeof(global::Fluent.DropDownButton), typeof(global::Fluent.RibbonDropDownButton))]
    [TestCase(typeof(global::Fluent.SplitButton), typeof(global::Fluent.RibbonSplitButton))]
    [TestCase(typeof(global::Fluent.MenuItem), typeof(global::Fluent.MenuItem))]
    [TestCase(typeof(global::Fluent.GalleryItem), typeof(global::Fluent.RibbonGalleryItem))]
    [TestCase(typeof(global::Fluent.RibbonTabItem), typeof(global::Fluent.RibbonTabItem))]
    public void Facade_InheritsCoreAutomationPeerOverride(Type facade, Type coreBase)
    {
        var method = facade.GetMethod(
            "OnCreateAutomationPeer",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.DeclaringType, Is.EqualTo(coreBase));
    }

    public static IEnumerable<TestCaseData> BindingContracts
    {
        get
        {
            yield return Contracts<global::Fluent.DropDownButton>(
                OneWay("Header"), OneWay("Icon"), OneWay("LargeIcon"), OneWay("MediumIcon"),
                OneWay("MenuHeader"), OneWay("HasTriangle"),
                OneWay("ResizeMode"), OneWay("MaxDropDownHeight"), OneWay("DropDownHeight"),
                OneWay("ClosePopupOnMouseDown"), OneWay("ClosePopupOnMouseDownDelay"),
                TwoWay("IsDropDownOpen"));
            yield return Contracts<global::Fluent.SplitButton>(
                OneWay("DropDownToolTip"), OneWay("IsCheckable"), OneWay("IsButtonEnabled"),
                OneWay("IsDefinitive"), TwoWay("IsChecked"));
            yield return Contracts<global::Fluent.MenuItem>(
                OneWay("Header"), OneWay("Description"), OneWay("Icon"), OneWay("Command"),
                OneWay("CommandParameter"), OneWay("Items"), OneWay("IsCheckable"),
                OneWay("GroupName"), OneWay("IsDefinitive"), TwoWay("IsChecked"));
            yield return Contracts<global::Fluent.Spinner>(
                OneWay("Header"), OneWay("Icon"), OneWay("MediumIcon"), TwoWay("Value"),
                TwoWay("Text"), OneWay("Minimum"), OneWay("Maximum"), OneWay("Increment"),
                OneWay("Format"), OneWay("Delay"), OneWay("Interval"),
                OneWay("SelectAllTextOnFocus"));
        }
    }

    [TestCaseSource(nameof(BindingContracts))]
    public void QuickAccessBindings_ExposeExpectedMetadata(
        Type facade,
        (string Property, BindingMode Mode)[] expected)
    {
        var actual = ReadBindingContracts(facade);
        Assert.That(
            actual.Select(contract => (contract.Property, contract.Mode)),
            Is.EquivalentTo(expected));
        Assert.That(actual.All(contract => contract.SourcePropertyAccessor is not null), Is.True);
        Assert.That(actual.All(contract => contract.TargetPropertyAccessor is not null), Is.True);
        Assert.That(
            actual.All(contract => CanResolveBindingTarget(facade, contract.TargetDependencyProperty)),
            Is.True);
    }

    [Test]
    public void StatusBarItem_DeclaresPortableCheckEvents()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(global::Fluent.StatusBarItem).GetEvent("Checked", DeclaredPublicInstance),
                Is.Not.Null);
            Assert.That(
                typeof(global::Fluent.StatusBarItem).GetEvent("Unchecked", DeclaredPublicInstance),
                Is.Not.Null);
        });
    }

    private static IReadOnlyList<(
        string Property,
        Func<DependencyProperty> SourcePropertyAccessor,
        string TargetDependencyProperty,
        Func<DependencyProperty> TargetPropertyAccessor,
        BindingMode Mode)>
        ReadBindingContracts(Type facade)
    {
        var bindingsType = typeof(global::Fluent.ToggleButton).Assembly.GetType(
            "Fluent.CompatibilityQuickAccessBindings",
            throwOnError: true)!;
        var getContracts = bindingsType.GetMethod(
            "GetContracts",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var contracts = (IEnumerable)getContracts.Invoke(null, [facade])!;

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

    private static bool CanResolveBindingTarget(Type facade, string fieldName)
    {
        var bindingsType = typeof(global::Fluent.ToggleButton).Assembly.GetType(
            "Fluent.CompatibilityQuickAccessBindings",
            throwOnError: true)!;
        var canResolve = bindingsType.GetMethod(
            "CanResolveTarget",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)canResolve.Invoke(null, [facade, fieldName])!;
    }

    private static TestCaseData Interfaces<TFacade>(params Type[] interfaces) =>
        new(typeof(TFacade), interfaces);

    private static TestCaseData Property(
        Type facade,
        string propertyName,
        Type propertyType,
        bool canWrite = true) =>
        new(facade, propertyName, propertyType, canWrite);

    private static TestCaseData Method(
        Type facade,
        string methodName,
        Type returnType,
        params Type[] parameters) =>
        new(facade, methodName, returnType, parameters);

    private static TestCaseData Contracts<TFacade>(
        params (string Property, BindingMode Mode)[] contracts) =>
        new(typeof(TFacade), contracts);

    private static (string Property, BindingMode Mode) OneWay(string property) =>
        (property, BindingMode.OneWay);

    private static (string Property, BindingMode Mode) TwoWay(string property) =>
        (property, BindingMode.TwoWay);
}
