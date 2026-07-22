using System.Collections;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class DropDownButtonCompatibilityTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredNonPublicInstance =
        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Test]
    public void DropDownButton_UsesItemsControlBaseAndContainerContracts()
    {
        var type = typeof(global::Fluent.DropDownButton);

        Assert.Multiple(() =>
        {
            Assert.That(typeof(ItemsControl).IsAssignableFrom(type), Is.True);
            Assert.That(
                type.GetProperty("ItemContainerTemplateSelector", DeclaredPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(DataTemplateSelector)));
            Assert.That(
                type.GetProperty("UsesItemContainerTemplate", DeclaredPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(bool)));
            AssertVirtual(type, "GetContainerForItemOverride");
            AssertVirtual(type, "IsItemItsOwnContainerOverride", typeof(object));
            AssertVirtual(type, "OnKeyDown", typeof(KeyRoutedEventArgs));
            Assert.That(
                type.GetProperty("LogicalChildren", DeclaredNonPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(IEnumerator)));
        });
    }

    [Test]
    public void SplitButton_DeclaresInputHooks()
    {
        var type = typeof(global::Fluent.SplitButton);

        Assert.Multiple(() =>
        {
            Assert.That(typeof(global::Fluent.ICommandSource).IsAssignableFrom(type), Is.True);
            AssertVirtual(type, "OnKeyDown", typeof(KeyRoutedEventArgs));
            AssertVirtual(
                type,
                "OnPreviewMouseLeftButtonDown",
                typeof(PointerRoutedEventArgs));
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
