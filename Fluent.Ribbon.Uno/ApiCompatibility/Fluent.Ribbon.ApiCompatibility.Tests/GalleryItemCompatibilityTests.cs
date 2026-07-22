using System.Reflection;
using Microsoft.UI.Xaml.Input;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class GalleryItemCompatibilityTests
{
    private const BindingFlags DeclaredNonPublicInstance =
        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Test]
    public void GalleryItem_UsesPortableFrameworkSubstituteAndCommandContract()
    {
        var type = typeof(global::Fluent.GalleryItem);

        Assert.Multiple(() =>
        {
            Assert.That(type.BaseType, Is.EqualTo(typeof(global::Fluent.RibbonGalleryItem)));
            Assert.That(typeof(global::Fluent.ICommandSource).IsAssignableFrom(type), Is.True);
        });
    }

    [Test]
    public void GalleryItem_DeclaresWpfCompatibleInputHooks()
    {
        var type = typeof(global::Fluent.GalleryItem);

        Assert.Multiple(() =>
        {
            AssertVirtual(type, "OnKeyUp", typeof(KeyRoutedEventArgs));
            AssertVirtual(type, "OnLostMouseCapture", typeof(PointerRoutedEventArgs));
            AssertVirtual(type, "OnMouseEnter", typeof(PointerRoutedEventArgs));
            AssertVirtual(type, "OnMouseLeave", typeof(PointerRoutedEventArgs));
            AssertVirtual(type, "OnMouseLeftButtonDown", typeof(PointerRoutedEventArgs));
            AssertVirtual(type, "OnMouseLeftButtonUp", typeof(PointerRoutedEventArgs));
            Assert.That(
                type.GetProperty("IsEnabledCore", DeclaredNonPublicInstance),
                Is.Not.Null);
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
