namespace FluentUno.Tests.Architecture;

using System;
using System.Collections;
using System.Reflection;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NUnit.Framework;

[TestFixture]
public sealed class PortDataContractTests
{
    [TestCase(typeof(Fluent.MenuItem))]
    [TestCase(typeof(RibbonStatusBar))]
    [TestCase(typeof(RibbonGroupBox))]
    public void PortableItemsControlsShouldInheritTheNativeBindingSurface(Type controlType)
    {
        Assert.That(typeof(ItemsControl).IsAssignableFrom(controlType), Is.True);
        foreach (var propertyName in new[]
                 {
                     nameof(ItemsControl.ItemsSource),
                     nameof(ItemsControl.ItemTemplate),
                     nameof(ItemsControl.ItemTemplateSelector),
                     nameof(ItemsControl.ItemContainerStyle),
                     nameof(ItemsControl.ItemContainerStyleSelector),
                     nameof(ItemsControl.DisplayMemberPath),
                     nameof(ItemsControl.ItemsPanel),
                 })
        {
            var property = controlType.GetProperty(propertyName);
            Assert.That(property?.DeclaringType, Is.EqualTo(typeof(ItemsControl)), propertyName);
            Assert.That(property?.CanRead, Is.True, propertyName);
            Assert.That(property?.CanWrite, Is.True, propertyName);
        }

        Assert.That(controlType.GetProperty("HasItems")?.PropertyType, Is.EqualTo(typeof(bool)));
    }

    [TestCase(typeof(Fluent.MenuItem))]
    [TestCase(typeof(RibbonStatusBar))]
    [TestCase(typeof(RibbonGroupBox))]
    [TestCase(typeof(RibbonGallery))]
    public void ContainerHooksShouldOverrideRatherThanHideTheNativeHooks(Type controlType)
    {
        foreach (var name in new[]
                 {
                     "GetContainerForItemOverride",
                     "IsItemItsOwnContainerOverride",
                     "PrepareContainerForItemOverride",
                     "ClearContainerForItemOverride",
                 })
        {
            var method = controlType.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, name);
            Assert.That(method!.GetBaseDefinition().DeclaringType, Is.EqualTo(typeof(ItemsControl)), name);
        }
    }

    [Test]
    public void GalleryShouldExposeTheNormalizedSelectionChangedDelegate()
    {
        var selectionChanged = typeof(RibbonGallery).GetEvent(
            nameof(RibbonGallery.SelectionChanged),
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.That(selectionChanged?.EventHandlerType, Is.EqualTo(typeof(SelectionChangedEventHandler)));
    }

    // This method intentionally compiles ordinary consumer expressions, including a named handler.
    private static void CompileConsumer(
        Fluent.MenuItem menu,
        RibbonStatusBar status,
        RibbonGroupBox group,
        RibbonGallery gallery,
        IEnumerable data,
        DataTemplate template,
        DataTemplateSelector templateSelector,
        Style style,
        StyleSelector styleSelector,
        ItemsPanelTemplate itemsPanel)
    {
        menu.ItemsSource = data;
        menu.ItemTemplate = template;
        menu.HeaderTemplate = template;
        menu.HeaderTemplateSelector = templateSelector;
        status.ItemsSource = data;
        status.ItemTemplateSelector = templateSelector;
        group.ItemsSource = data;
        group.ItemContainerStyle = style;
        group.ItemContainerStyleSelector = styleSelector;
        group.DisplayMemberPath = "Title";
        group.ItemsPanel = itemsPanel;
        gallery.ItemsSource = data;
        gallery.ItemTemplate = template;
        gallery.SelectionChanged += OnSelectionChanged;
        gallery.SelectionChanged -= OnSelectionChanged;
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        _ = args.RemovedItems;
        _ = args.AddedItems;
    }
}
