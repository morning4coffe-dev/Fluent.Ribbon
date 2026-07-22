using System.Reflection;
using Microsoft.UI.Xaml;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class CompatibilityWrapperTests
{
    private const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredPublicStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    public static IEnumerable<TestCaseData> Wrappers
    {
        get
        {
            yield return Wrapper<global::Fluent.CheckBox, global::Fluent.RibbonCheckBox>();
            yield return Wrapper<global::Fluent.ComboBox, global::Fluent.RibbonComboBox>();
            yield return Wrapper<global::Fluent.Gallery, global::Fluent.RibbonGallery>();
            yield return Wrapper<global::Fluent.GalleryItem, global::Fluent.RibbonGalleryItem>();
            yield return Wrapper<global::Fluent.RadioButton, global::Fluent.RibbonRadioButton>();
            yield return Wrapper<global::Fluent.Spinner, global::Fluent.RibbonSpinner>();
            yield return Wrapper<global::Fluent.SplitButton, global::Fluent.RibbonSplitButton>();
            yield return Wrapper<global::Fluent.StatusBar, global::Fluent.RibbonStatusBar>();
            yield return Wrapper<global::Fluent.TextBox, global::Fluent.RibbonTextBox>();
            yield return Wrapper<global::Fluent.ToggleButton, global::Fluent.RibbonToggleButton>();
        }
    }

    [TestCaseSource(nameof(Wrappers))]
    public void Wrapper_ExistsWithExpectedDirectBaseType(Type wrapper, Type expectedBase)
    {
        Assert.Multiple(() =>
        {
            Assert.That(wrapper.IsPublic, Is.True);
            Assert.That(wrapper.BaseType, Is.EqualTo(expectedBase));
            Assert.That(wrapper.Assembly.GetName().Name, Is.EqualTo("Fluent.Ribbon.Uno.Compatibility"));
        });
    }

    [Test]
    public void DropDownButton_IsCoreFacadeWithExpectedDirectBaseType()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(global::Fluent.DropDownButton).IsPublic, Is.True);
            Assert.That(
                typeof(global::Fluent.DropDownButton).BaseType,
                Is.EqualTo(typeof(global::Fluent.RibbonDropDownButton)));
            Assert.That(
                typeof(global::Fluent.DropDownButton).Assembly.GetName().Name,
                Is.EqualTo("Fluent.Ribbon.Uno"));
        });
    }

    [Test]
    public void Button_IsCoreFacadeWithExpectedDirectBaseType()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(global::Fluent.Button).IsPublic, Is.True);
            Assert.That(
                typeof(global::Fluent.Button).BaseType,
                Is.EqualTo(typeof(global::Fluent.RibbonButton)));
            Assert.That(
                typeof(global::Fluent.Button).Assembly.GetName().Name,
                Is.EqualTo("Fluent.Ribbon.Uno"));
        });
    }

    public static IEnumerable<TestCaseData> DeclaredCompatibilityProperties
    {
        get
        {
            var sizedWrappers = new[]
            {
                typeof(global::Fluent.Button),
                typeof(global::Fluent.ToggleButton),
                typeof(global::Fluent.CheckBox),
                typeof(global::Fluent.RadioButton),
                typeof(global::Fluent.TextBox),
                typeof(global::Fluent.ComboBox)
            };

            foreach (var wrapper in sizedWrappers)
            {
                yield return Property(wrapper, "SizeDefinition", typeof(global::Fluent.RibbonControlSizeDefinition));
                yield return Property(wrapper, "SimplifiedSizeDefinition", typeof(global::Fluent.RibbonControlSizeDefinition));
                yield return Property(wrapper, "MediumIcon", typeof(object));
            }

            foreach (var wrapper in new[]
                     {
                         typeof(global::Fluent.Button),
                         typeof(global::Fluent.ToggleButton),
                         typeof(global::Fluent.CheckBox),
                         typeof(global::Fluent.RadioButton)
                     })
            {
                yield return Property(wrapper, "LargeIcon", typeof(object));
            }

            yield return Property(typeof(global::Fluent.CheckBox), "Icon", typeof(object));
            yield return Property(typeof(global::Fluent.RadioButton), "Icon", typeof(object));

            foreach (var wrapper in new[]
                     {
                         typeof(global::Fluent.CheckBox),
                         typeof(global::Fluent.RadioButton),
                         typeof(global::Fluent.TextBox),
                         typeof(global::Fluent.ComboBox)
                     })
            {
                yield return Property(wrapper, "CanAddToQuickAccessToolBar", typeof(bool));
            }
        }
    }

    [TestCaseSource(nameof(DeclaredCompatibilityProperties))]
    public void CompatibilityProperty_HasRoundTrippableDeclaredContract(
        Type wrapper,
        string propertyName,
        Type propertyType)
    {
        var property = wrapper.GetProperty(propertyName, DeclaredPublicInstance);
        var dependencyPropertyField = wrapper.GetField($"{propertyName}Property", DeclaredPublicStatic);

        Assert.Multiple(() =>
        {
            Assert.That(property, Is.Not.Null);
            Assert.That(property!.PropertyType, Is.EqualTo(propertyType));
            Assert.That(property.CanRead, Is.True);
            Assert.That(property.CanWrite, Is.True);
            Assert.That(property.GetMethod, Is.Not.Null);
            Assert.That(property.SetMethod, Is.Not.Null);

            Assert.That(dependencyPropertyField, Is.Not.Null);
            Assert.That(dependencyPropertyField!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(dependencyPropertyField.IsInitOnly, Is.True);
            Assert.That(dependencyPropertyField.GetValue(null), Is.Not.Null);
        });
    }

    public static IEnumerable<TestCaseData> ReRegisteredProperties
    {
        get
        {
            yield return ReRegistered<global::Fluent.Button, global::Fluent.RibbonButton>("SizeDefinition");
            yield return ReRegistered<global::Fluent.Button, global::Fluent.RibbonButton>("SimplifiedSizeDefinition");
            yield return ReRegistered<global::Fluent.Button, global::Fluent.RibbonButton>("LargeIcon");
            yield return ReRegistered<global::Fluent.Button, global::Fluent.RibbonButton>("MediumIcon");
            yield return ReRegistered<global::Fluent.ToggleButton, global::Fluent.RibbonToggleButton>("SizeDefinition");
            yield return ReRegistered<global::Fluent.ToggleButton, global::Fluent.RibbonToggleButton>("SimplifiedSizeDefinition");
            yield return ReRegistered<global::Fluent.ToggleButton, global::Fluent.RibbonToggleButton>("LargeIcon");
            yield return ReRegistered<global::Fluent.ToggleButton, global::Fluent.RibbonToggleButton>("MediumIcon");
            yield return ReRegistered<global::Fluent.CheckBox, global::Fluent.RibbonCheckBox>("MediumIcon");
            yield return ReRegistered<global::Fluent.RadioButton, global::Fluent.RibbonRadioButton>("MediumIcon");
            yield return ReRegistered<global::Fluent.TextBox, global::Fluent.RibbonTextBox>("MediumIcon");
            yield return ReRegistered<global::Fluent.ComboBox, global::Fluent.RibbonComboBox>("MediumIcon");
        }
    }

    [TestCaseSource(nameof(ReRegisteredProperties))]
    public void CompatibilityProperty_UsesDistinctDependencyPropertyWhenRegisteredTypeDiffers(
        Type wrapper,
        Type baseType,
        string propertyName)
    {
        var wrapperField = wrapper.GetField($"{propertyName}Property", DeclaredPublicStatic);
        var baseField = baseType.GetField($"{propertyName}Property", BindingFlags.Public | BindingFlags.Static);

        Assert.Multiple(() =>
        {
            Assert.That(wrapperField, Is.Not.Null);
            Assert.That(baseField, Is.Not.Null);
            Assert.That(wrapperField!.GetValue(null), Is.Not.SameAs(baseField!.GetValue(null)));
        });
    }

    [Test]
    public void QuickAccessDependencyProperty_UsesExistingRibbonStorage()
    {
        var wrappers = new[]
        {
            typeof(global::Fluent.Button),
            typeof(global::Fluent.ToggleButton),
            typeof(global::Fluent.CheckBox),
            typeof(global::Fluent.RadioButton),
            typeof(global::Fluent.TextBox),
            typeof(global::Fluent.ComboBox)
        };

        foreach (var wrapper in wrappers)
        {
            var field = wrapper.GetField("CanAddToQuickAccessToolBarProperty", DeclaredPublicStatic);
            Assert.That(
                field!.GetValue(null),
                Is.SameAs(global::Fluent.RibbonProperties.CanAddToQuickAccessToolBarProperty),
                wrapper.FullName);
        }
    }

    [Test]
    public void SizeDefinitionSynchronizationConversion_RoundTrips()
    {
        var expected = new global::Fluent.RibbonControlSizeDefinition(
            global::Fluent.RibbonControlSize.Large,
            global::Fluent.RibbonControlSize.Medium,
            global::Fluent.RibbonControlSize.Small);
        var converterType = typeof(global::Fluent.ToggleButton).Assembly.GetType(
            "Fluent.CompatibilityValueConverter",
            throwOnError: true);
        var converter = converterType!.GetMethod(
            "ToSizeDefinitionString",
            BindingFlags.NonPublic | BindingFlags.Static);
        var serialized = (string)converter!.Invoke(null, [expected])!;
        var actual = new global::Fluent.RibbonControlSizeDefinition(serialized);

        Assert.That(actual, Is.EqualTo(expected));
    }

    private static TestCaseData Wrapper<TWrapper, TBase>()
        => new TestCaseData(typeof(TWrapper), typeof(TBase))
            .SetName($"{typeof(TWrapper).Name}_derives_from_{typeof(TBase).Name}");

    private static TestCaseData Property(Type wrapper, string propertyName, Type propertyType)
        => new TestCaseData(wrapper, propertyName, propertyType)
            .SetName($"{wrapper.Name}_{propertyName}_declares_{propertyType.Name}");

    private static TestCaseData ReRegistered<TWrapper, TBase>(string propertyName)
        => new TestCaseData(typeof(TWrapper), typeof(TBase), propertyName)
            .SetName($"{typeof(TWrapper).Name}_{propertyName}_uses_compatibility_DP");
}
