using System.Collections;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class InputFacadeCompatibilityTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredNonPublicInstance =
        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Test]
    public void ComboBox_DeclaresPopupKeyboardAndLogicalContracts()
    {
        var type = typeof(global::Fluent.ComboBox);

        Assert.Multiple(() =>
        {
            Assert.That(
                type.GetProperty("DropDownPopup", DeclaredPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(Popup)));
            AssertVirtual(type, "OnKeyDown", typeof(KeyRoutedEventArgs));
            AssertVirtual(type, "OnPreviewKeyDown", typeof(KeyRoutedEventArgs));
            Assert.That(
                type.GetProperty("LogicalChildren", DeclaredNonPublicInstance)!.PropertyType,
                Is.EqualTo(typeof(IEnumerator)));
        });
    }

    [Test]
    public void TextBox_DeclaresContextKeyboardAndLogicalContracts()
    {
        var type = typeof(global::Fluent.TextBox);

        Assert.Multiple(() =>
        {
            AssertVirtual(type, "OnContextMenuOpening", typeof(ContextMenuEventArgs));
            AssertVirtual(type, "OnContextMenuClosing", typeof(ContextMenuEventArgs));
            AssertVirtual(type, "OnKeyUp", typeof(KeyRoutedEventArgs));
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
