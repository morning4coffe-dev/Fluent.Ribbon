using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class FinalFacadeCompatibilityTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredPublicStatic =
        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredProtectedInstance =
        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    public static IEnumerable<TestCaseData> DependencyPropertyContracts
    {
        get
        {
            foreach (var type in new[]
                     {
                         typeof(global::Fluent.Button),
                         typeof(global::Fluent.ToggleButton),
                         typeof(global::Fluent.CheckBox),
                         typeof(global::Fluent.RadioButton),
                         typeof(global::Fluent.TextBox),
                         typeof(global::Fluent.ComboBox)
                     })
            {
                yield return Property(type, "HeaderTemplate", typeof(DataTemplate));
                yield return Property(type, "HeaderTemplateSelector", typeof(DataTemplateSelector));
            }

            yield return Property(typeof(global::Fluent.Button), "IsDefinitive", typeof(bool));
            yield return Property(typeof(global::Fluent.ToggleButton), "IsDefinitive", typeof(bool));

            yield return Property(
                typeof(global::Fluent.ComboBox),
                "TopPopupContentTemplateSelector",
                typeof(DataTemplateSelector));
            yield return Property(
                typeof(global::Fluent.ComboBox),
                "TopPopupContentStringFormat",
                typeof(string));
            yield return Property(
                typeof(global::Fluent.ComboBox),
                "Menu",
                typeof(global::Fluent.RibbonMenu));

            yield return Property(
                typeof(global::Fluent.DropDownButton),
                "DismissOnClickOutside",
                typeof(bool));
            yield return Property(
                typeof(global::Fluent.DropDownButton),
                "HeaderTemplateSelector",
                typeof(DataTemplateSelector));

            yield return Property(typeof(global::Fluent.MenuItem), "IsSplit", typeof(bool));
            yield return Property(typeof(global::Fluent.MenuItem), "MaxDropDownHeight", typeof(double));
            yield return Property(
                typeof(global::Fluent.MenuItem),
                "ResizeMode",
                typeof(global::Fluent.ContextMenuResizeMode));

            yield return Property(
                typeof(global::Fluent.Gallery),
                "GroupByAdvanced",
                typeof(Func<object, string>));
            yield return Property(
                typeof(global::Fluent.Gallery),
                "IsLastItem",
                typeof(bool),
                canWrite: false);

            yield return Property(
                typeof(global::Fluent.Spinner),
                "TextToValueConverter",
                typeof(IValueConverter));
        }
    }

    [TestCaseSource(nameof(DependencyPropertyContracts))]
    public void Facade_DeclaresPortableDependencyProperty(
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
            Assert.That(property.CanWrite, Is.EqualTo(canWrite));
            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(field.IsInitOnly, Is.True);
        });
    }

    [TestCase(typeof(global::Fluent.ComboBox))]
    [TestCase(typeof(global::Fluent.DropDownButton))]
    [TestCase(typeof(global::Fluent.MenuItem))]
    public void Facade_DeclaresContextMenuState(Type facade)
    {
        var property = facade.GetProperty("IsContextMenuOpened", DeclaredPublicInstance);

        Assert.Multiple(() =>
        {
            Assert.That(property, Is.Not.Null);
            Assert.That(property!.PropertyType, Is.EqualTo(typeof(bool)));
            Assert.That(property.CanRead, Is.True);
            Assert.That(property.CanWrite, Is.True);
        });
    }

    public static IEnumerable<TestCaseData> ProtectedActionContracts
    {
        get
        {
            yield return Method(typeof(global::Fluent.Button), "OnClick", typeof(void));
            yield return Method(typeof(global::Fluent.ToggleButton), "OnClick", typeof(void));
            yield return Method(
                typeof(global::Fluent.ToggleButton),
                "OnChecked",
                typeof(void),
                typeof(RoutedEventArgs));
            yield return Method(
                typeof(global::Fluent.TextBox),
                "BindQuickAccessItem",
                typeof(void),
                typeof(FrameworkElement));
            yield return Method(typeof(global::Fluent.TextBox), "OnApplyTemplate", typeof(void));
            yield return Method(typeof(global::Fluent.ComboBox), "OnApplyTemplate", typeof(void));
            yield return Method(
                typeof(global::Fluent.ComboBox),
                "OnDropDownOpened",
                typeof(void),
                typeof(EventArgs));
            yield return Method(
                typeof(global::Fluent.ComboBox),
                "OnDropDownClosed",
                typeof(void),
                typeof(EventArgs));
            yield return Method(
                typeof(global::Fluent.DropDownButton),
                "BindQuickAccessItemDropDownEvents",
                typeof(void),
                typeof(global::Fluent.DropDownButton));
            yield return Method(
                typeof(global::Fluent.DropDownButton),
                "OnQuickAccessOpened",
                typeof(void),
                typeof(object),
                typeof(EventArgs));
            yield return Method(
                typeof(global::Fluent.DropDownButton),
                "OnQuickAccessMenuClosedOrUnloaded",
                typeof(void),
                typeof(object),
                typeof(EventArgs));
            yield return Method(
                typeof(global::Fluent.DropDownButton),
                "OnIsSimplifiedChanged",
                typeof(void),
                typeof(bool),
                typeof(bool));
            yield return Method(typeof(global::Fluent.MenuItem), "OnClick", typeof(void));
            yield return Method(
                typeof(global::Fluent.MenuItem),
                "OnQuickAccessOpened",
                typeof(void),
                typeof(object),
                typeof(EventArgs));
            yield return Method(
                typeof(global::Fluent.MenuItem),
                "OnQuickAccessMenuClosedOrUnloaded",
                typeof(void),
                typeof(object),
                typeof(EventArgs));
            yield return Method(
                typeof(global::Fluent.GalleryItem),
                "OnClick",
                typeof(void),
                typeof(object),
                typeof(RoutedEventArgs));
            yield return Method(
                typeof(global::Fluent.SplitButton),
                "OnIsSimplifiedChanged",
                typeof(void),
                typeof(bool),
                typeof(bool));
        }
    }

    [TestCaseSource(nameof(ProtectedActionContracts))]
    public void Facade_DeclaresPortableProtectedAction(
        Type facade,
        string methodName,
        Type returnType,
        Type[] parameters)
    {
        var method = facade.GetMethod(
            methodName,
            DeclaredProtectedInstance,
            binder: null,
            parameters,
            modifiers: null);

        Assert.That(method, Is.Not.Null, $"{facade.Name}.{methodName}");
        Assert.That(method!.ReturnType, Is.EqualTo(returnType));
    }

    [Test]
    public void ToggleButton_DeclaresInvokeClick()
    {
        var method = typeof(global::Fluent.ToggleButton).GetMethod(
            "InvokeClick",
            DeclaredPublicInstance,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);

        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(void)));
    }

    [Test]
    public void TextBox_HeaderMetadata_DoesNotInitializeNativeTextBoxStatics()
    {
        Assert.DoesNotThrow(() =>
        {
            Assert.That(global::Fluent.TextBox.HeaderTemplateProperty, Is.Not.Null);
            Assert.That(global::Fluent.TextBox.HeaderTemplateSelectorProperty, Is.Not.Null);
        });
    }

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
}
