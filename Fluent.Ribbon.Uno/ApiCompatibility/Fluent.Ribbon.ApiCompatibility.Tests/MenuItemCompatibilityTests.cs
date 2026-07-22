using System.Collections;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class MenuItemCompatibilityTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredPublicStatic =
        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredNonPublicInstance =
        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Test]
    public void MenuItem_UsesPortableFrameworkSubstitute()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(global::Fluent.MenuItem).Assembly.GetName().Name,
                Is.EqualTo("Fluent.Ribbon.Uno"));
            Assert.That(
                typeof(global::Fluent.MenuItem).BaseType,
                Is.EqualTo(typeof(global::Fluent.InteractiveMenuItemBase)));
            Assert.That(
                typeof(global::Fluent.RibbonMenuItem).BaseType,
                Is.EqualTo(typeof(global::Fluent.MenuItem)));
        });
    }

    [Test]
    public void MenuItem_DeclaresAccessKeyAndPopupSurface()
    {
        var type = typeof(global::Fluent.MenuItem);

        Assert.Multiple(() =>
        {
            Assert.That(
                type.GetField("RecognizesAccessKeyProperty", DeclaredPublicStatic),
                Is.Not.Null);
            Assert.That(
                type.GetProperty("RecognizesAccessKey", DeclaredPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(bool)));
            Assert.That(
                type.GetMethod("GetRecognizesAccessKey", DeclaredPublicStatic),
                Is.Not.Null);
            Assert.That(
                type.GetMethod("SetRecognizesAccessKey", DeclaredPublicStatic),
                Is.Not.Null);
            Assert.That(
                type.GetProperty("DropDownPopup", DeclaredPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(Popup)));
            Assert.That(
                type.GetProperty("LogicalParent", DeclaredPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(object)));
        });
    }

    [Test]
    public void MenuItem_DeclaresWpfCompatibleHooks()
    {
        var type = typeof(global::Fluent.MenuItem);

        Assert.Multiple(() =>
        {
            AssertVirtual(type, "GetContainerForItemOverride");
            AssertVirtual(type, "IsItemItsOwnContainerOverride", typeof(object));
            AssertVirtual(type, "OnContextMenuOpening", typeof(ContextMenuEventArgs));
            AssertVirtual(type, "OnContextMenuClosing", typeof(ContextMenuEventArgs));
            AssertVirtual(
                type,
                "OnIsKeyboardFocusedChanged",
                typeof(DependencyPropertyChangedEventArgs));
            AssertVirtual(type, "OnMouseEnter", typeof(PointerRoutedEventArgs));
            AssertVirtual(type, "OnMouseLeave", typeof(PointerRoutedEventArgs));
            AssertVirtual(type, "OnMouseLeftButtonUp", typeof(PointerRoutedEventArgs));
            AssertVirtual(type, "OnMouseWheel", typeof(PointerRoutedEventArgs));
            Assert.That(
                type.GetProperty("LogicalChildren", DeclaredNonPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(IEnumerator)));
        });
    }

    private static void AssertVirtual(
        Type type,
        string name,
        params Type[] parameterTypes)
    {
        var method = type.GetMethod(
            name,
            DeclaredNonPublicInstance,
            binder: null,
            parameterTypes,
            modifiers: null);

        Assert.That(method, Is.Not.Null, name);
        Assert.That(method!.IsVirtual, Is.True, name);
    }
}
