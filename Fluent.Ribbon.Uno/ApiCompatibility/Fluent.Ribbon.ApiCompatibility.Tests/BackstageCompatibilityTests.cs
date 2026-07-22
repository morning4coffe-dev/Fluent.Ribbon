using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class BackstageCompatibilityTests
{
    private const BindingFlags DeclaredPublicInstance =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredPublicStatic =
        BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredNonPublicInstance =
        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

    [Test]
    public void BackstageControls_UseWpfShapedBaseTypes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(global::Fluent.RibbonControl).Assembly.GetName().Name,
                Is.EqualTo("Fluent.Ribbon.Uno"));
            Assert.That(
                typeof(global::Fluent.Backstage).BaseType,
                Is.EqualTo(typeof(global::Fluent.RibbonControl)));
            Assert.That(
                typeof(global::Fluent.BackstageTabControl).BaseType,
                Is.EqualTo(typeof(Selector)));
            Assert.That(
                typeof(global::Fluent.BackstageTabItem).BaseType,
                Is.EqualTo(typeof(ContentControl)));
            Assert.That(
                typeof(global::Fluent.ILogicalChildSupport)
                    .IsAssignableFrom(typeof(global::Fluent.BackstageTabControl)),
                Is.True);
            Assert.That(
                typeof(global::Fluent.IHeaderedControl)
                    .IsAssignableFrom(typeof(global::Fluent.BackstageTabItem)),
                Is.True);
        });
    }

    [Test]
    public void RibbonTabItem_IsCoreWpfNamedTabBase()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(global::Fluent.RibbonTabItem).Assembly.GetName().Name,
                Is.EqualTo("Fluent.Ribbon.Uno"));
            Assert.That(
                typeof(global::Fluent.RibbonTabItem).BaseType,
                Is.EqualTo(typeof(Microsoft.UI.Xaml.Controls.TabViewItem)));
            Assert.That(
                typeof(global::Fluent.RibbonTab).BaseType,
                Is.EqualTo(typeof(global::Fluent.RibbonTabItem)));
        });
    }

    [Test]
    public void Backstage_DeclaresWpfCompatibleSurface()
    {
        var type = typeof(global::Fluent.Backstage);

        Assert.Multiple(() =>
        {
            AssertProperty(type, "Content", typeof(UIElement));
            AssertProperty(type, "AreAnimationsEnabled", typeof(bool));
            AssertProperty(type, "UseHighestAvailableAdornerLayer", typeof(bool));
            Assert.That(type.GetProperty("AdornerLayer")!.PropertyType, Is.EqualTo(typeof(FrameworkElement)));
            Assert.That(
                type.GetEvent("IsOpenChanged")!.EventHandlerType,
                Is.EqualTo(typeof(EventHandler<DependencyPropertyChangedEventArgs>)));
            AssertVirtualMethod(
                type,
                "OnDismissPopup",
                typeof(object),
                typeof(global::Fluent.DismissPopupEventArgs));
            AssertVirtualMethod(type, "OnKeyDown", typeof(KeyRoutedEventArgs));
            AssertVirtualMethod(type, "OnMouseLeftButtonDown", typeof(PointerRoutedEventArgs));
            Assert.That(
                type.GetMethod("CreateQuickAccessItem", DeclaredPublicInstance),
                Is.Not.Null);
        });
    }

    [Test]
    public void BackstageTabControl_DeclaresSelectionSurface()
    {
        var type = typeof(global::Fluent.BackstageTabControl);

        Assert.Multiple(() =>
        {
            AssertProperty(type, "BackButtonUid", typeof(string));
            AssertProperty(type, "SelectedContent", typeof(object));
            AssertProperty(type, "ContentStringFormat", typeof(string));
            AssertProperty(type, "ContentTemplate", typeof(DataTemplate));
            AssertProperty(type, "ContentTemplateSelector", typeof(DataTemplateSelector));
            AssertProperty(type, "SelectedContentStringFormat", typeof(string));
            AssertProperty(type, "SelectedContentTemplate", typeof(DataTemplate));
            AssertProperty(type, "SelectedContentTemplateSelector", typeof(DataTemplateSelector));
            AssertProperty(type, "ParentBackstage", typeof(global::Fluent.Backstage));
            AssertVirtualMethod(type, "OnInitialized", typeof(EventArgs));
            AssertVirtualMethod(
                type,
                "OnItemsChanged",
                typeof(System.Collections.Specialized.NotifyCollectionChangedEventArgs));
            AssertVirtualMethod(type, "OnSelectionChanged", typeof(SelectionChangedEventArgs));
            AssertVirtualMethod(type, "OnKeyDown", typeof(KeyRoutedEventArgs));
        });
    }

    [Test]
    public void BackstageTabItem_DeclaresHeaderAndSelectionSurface()
    {
        var type = typeof(global::Fluent.BackstageTabItem);

        Assert.Multiple(() =>
        {
            AssertProperty(type, "Icon", typeof(object));
            AssertProperty(type, "HeaderTemplate", typeof(DataTemplate));
            AssertProperty(type, "HeaderTemplateSelector", typeof(DataTemplateSelector));
            AssertVirtualMethod(type, "OnContentChanged", typeof(object), typeof(object));
            AssertVirtualMethod(type, "OnGotFocus", typeof(RoutedEventArgs));
            AssertVirtualMethod(type, "OnMouseLeftButtonDown", typeof(PointerRoutedEventArgs));
            AssertVirtualMethod(type, "OnSelected", typeof(RoutedEventArgs));
            AssertVirtualMethod(type, "OnUnselected", typeof(RoutedEventArgs));
        });
    }

    private static void AssertProperty(Type type, string name, Type propertyType)
    {
        var property = type.GetProperty(name, DeclaredPublicInstance);
        var field = type.GetField($"{name}Property", DeclaredPublicStatic);

        Assert.That(property, Is.Not.Null, $"{type.FullName}.{name}");
        Assert.That(property!.PropertyType, Is.EqualTo(propertyType), $"{type.FullName}.{name}");
        Assert.That(field, Is.Not.Null, $"{type.FullName}.{name}Property");
        Assert.That(field!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
    }

    private static void AssertVirtualMethod(
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

        Assert.That(method, Is.Not.Null, $"{type.FullName}.{name}");
        Assert.That(method!.IsVirtual, Is.True, $"{type.FullName}.{name}");
    }
}
