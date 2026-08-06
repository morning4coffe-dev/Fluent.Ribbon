namespace FluentUno.Tests.Architecture;

using System;
using System.Linq;
using System.Reflection;
using Fluent;
using Fluent.Extensibility;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using NUnit.Framework;

[TestFixture]
public class InRibbonGalleryApiCompatibilityTests
{
    [Test]
    public void InRibbonGalleryShouldExposePortableWpfSurface()
    {
        var type = typeof(InRibbonGallery);
        var expectedDependencyProperties = new[]
        {
            nameof(InRibbonGallery.CanAddToQuickAccessToolBarProperty),
            nameof(InRibbonGallery.DropDownHeightProperty),
            nameof(InRibbonGallery.DropDownWidthProperty),
            nameof(InRibbonGallery.ExpandButtonContentProperty),
            nameof(InRibbonGallery.ExpandButtonContentTemplateProperty),
            nameof(InRibbonGallery.GalleryPanelContainerHeightProperty),
            nameof(InRibbonGallery.GroupByAdvancedProperty),
            nameof(InRibbonGallery.GroupByProperty),
            nameof(InRibbonGallery.HasFilterProperty),
            nameof(InRibbonGallery.HeaderTemplateProperty),
            nameof(InRibbonGallery.HeaderTemplateSelectorProperty),
            nameof(InRibbonGallery.IconProperty),
            nameof(InRibbonGallery.IsSimplifiedProperty),
            nameof(InRibbonGallery.ItemsSourceProperty),
            nameof(InRibbonGallery.MaxDropDownHeightProperty),
            nameof(InRibbonGallery.MaxDropDownWidthProperty),
            nameof(InRibbonGallery.MaxItemsInDropDownRowProperty),
            nameof(InRibbonGallery.MediumIconProperty),
            nameof(InRibbonGallery.MenuProperty),
            nameof(InRibbonGallery.MinItemsInDropDownRowProperty),
            nameof(InRibbonGallery.OrientationProperty),
            nameof(InRibbonGallery.ResizeModeProperty),
            nameof(InRibbonGallery.SelectedFilterGroupsProperty),
            nameof(InRibbonGallery.SelectedFilterProperty),
            nameof(InRibbonGallery.SelectedFilterTitleProperty),
            nameof(InRibbonGallery.SimplifiedSizeDefinitionProperty),
            nameof(InRibbonGallery.SizeDefinitionProperty),
        };

        Assert.Multiple(() =>
        {
            Assert.That(type.BaseType, Is.EqualTo(typeof(Selector)));
            Assert.That(typeof(IDropDownControl).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IQuickAccessItemProvider).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IRibbonControl).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IRibbonSizeChangedSink).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(ISimplifiedRibbonControl).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetEvent(nameof(InRibbonGallery.DropDownOpened)), Is.Not.Null);
            Assert.That(type.GetEvent(nameof(InRibbonGallery.DropDownClosed)), Is.Not.Null);
            Assert.That(type.GetEvent(nameof(InRibbonGallery.Scaled)), Is.Not.Null);
            Assert.That(
                type.GetProperty(nameof(InRibbonGallery.LargeIcon))?.PropertyType,
                Is.EqualTo(typeof(object)));
            Assert.That(
                type.GetProperty(nameof(InRibbonGallery.MediumIcon))?.PropertyType,
                Is.EqualTo(typeof(object)));
            Assert.That(
                type.GetProperty(nameof(InRibbonGallery.SizeDefinition))?.PropertyType,
                Is.EqualTo(typeof(RibbonControlSizeDefinition)));
            Assert.That(
                type.GetProperty(nameof(InRibbonGallery.SimplifiedSizeDefinition))?.PropertyType,
                Is.EqualTo(typeof(RibbonControlSizeDefinition)));
            Assert.That(
                type.GetMethod(nameof(InRibbonGallery.CreateQuickAccessItem))?.IsVirtual,
                Is.True);
            Assert.That(type.GetMethod(nameof(InRibbonGallery.ResetScale)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(InRibbonGallery.ScrollIntoView)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(InRibbonGallery.OnKeyTipPressed)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(InRibbonGallery.OnKeyTipBack)), Is.Not.Null);
            Assert.That(
                type.GetMethod(
                    nameof(InRibbonGallery.OnSizePropertyChanged),
                    new[] { typeof(RibbonControlSize), typeof(RibbonControlSize) }),
                Is.Not.Null);
            Assert.That(
                type.GetMethod(
                    "OnCreateAutomationPeer",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);

            foreach (var fieldName in expectedDependencyProperties)
            {
                Assert.That(type.GetField(fieldName), Is.Not.Null, fieldName);
            }
        });
    }

    [Test]
    public void GalleryGroupFilterShouldExposePortableMetadataHelpers()
    {
        var type = typeof(GalleryGroupFilter);

        Assert.Multiple(() =>
        {
            Assert.That(type.GetProperty(nameof(GalleryGroupFilter.Title)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(GalleryGroupFilter.Groups)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(GalleryGroupFilter.GetGroupNames)), Is.Not.Null);
        });
    }
}
