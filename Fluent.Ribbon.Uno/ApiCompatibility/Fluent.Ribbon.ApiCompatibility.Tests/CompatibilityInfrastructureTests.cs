using System.Reflection;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using NUnit.Framework;
using Windows.Foundation;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class CompatibilityInfrastructureTests
{
    private const BindingFlags DeclaredPublicInstance = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredPublicStatic = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Test]
    public void RibbonControl_ExposesPortableWpfSurface()
    {
        var type = typeof(global::Fluent.RibbonControl);
        var expectedProperties = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["KeyTip"] = typeof(string),
            ["Header"] = typeof(object),
            ["HeaderTemplate"] = typeof(DataTemplate),
            ["HeaderTemplateSelector"] = typeof(DataTemplateSelector),
            ["Icon"] = typeof(object),
            ["Command"] = typeof(ICommand),
            ["CommandParameter"] = typeof(object),
            ["CommandTarget"] = typeof(UIElement),
            ["Size"] = typeof(global::Fluent.RibbonControlSize),
            ["SizeDefinition"] = typeof(global::Fluent.RibbonControlSizeDefinition),
            ["CanAddToQuickAccessToolBar"] = typeof(bool)
        };

        Assert.Multiple(() =>
        {
            Assert.That(type.IsPublic, Is.True);
            Assert.That(type.IsAbstract, Is.True);
            Assert.That(type.BaseType, Is.EqualTo(typeof(Control)));
            Assert.That(typeof(global::Fluent.ICommandSource).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(global::Fluent.IRibbonControl).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(global::Fluent.IQuickAccessItemProvider).IsAssignableFrom(type), Is.True);
        });

        foreach (var (propertyName, propertyType) in expectedProperties)
        {
            var property = type.GetProperty(propertyName, DeclaredPublicInstance);
            var field = type.GetField($"{propertyName}Property", DeclaredPublicStatic);

            Assert.Multiple(() =>
            {
                Assert.That(property, Is.Not.Null, propertyName);
                Assert.That(property!.PropertyType, Is.EqualTo(propertyType), propertyName);
                Assert.That(property.CanRead, Is.True, propertyName);
                Assert.That(property.CanWrite, Is.True, propertyName);
                Assert.That(field, Is.Not.Null, $"{propertyName}Property");
                Assert.That(field!.FieldType, Is.EqualTo(typeof(DependencyProperty)), $"{propertyName}Property");
                Assert.That(field.IsInitOnly, Is.True, $"{propertyName}Property");
                Assert.That(field.GetValue(null), Is.Not.Null, $"{propertyName}Property");
            });
        }
    }

    [Test]
    public void RibbonControl_ExposesQuickAccessAndPortableHelpers()
    {
        var type = typeof(global::Fluent.RibbonControl);
        var createQuickAccess = type.GetMethod(
            "CreateQuickAccessItem",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        Assert.Multiple(() =>
        {
            Assert.That(createQuickAccess, Is.Not.Null);
            Assert.That(createQuickAccess!.IsAbstract, Is.True);
            Assert.That(createQuickAccess.ReturnType, Is.EqualTo(typeof(FrameworkElement)));
            Assert.That(
                type.GetMethod("BindQuickAccessItem", DeclaredPublicStatic),
                Is.Not.Null);
            Assert.That(
                type.GetMethod("OnCanAddToQuickAccessToolBarChanged", DeclaredPublicStatic),
                Is.Not.Null);
            Assert.That(
                type.GetMethod("GetParentRibbon", DeclaredPublicStatic)!.ReturnType.FullName,
                Is.EqualTo("Fluent.Ribbon"));
            Assert.That(
                type.GetMethod("GetControlWorkArea", DeclaredPublicStatic)!.ReturnType,
                Is.EqualTo(typeof(Rect)));
            Assert.That(
                type.GetMethod("GetControlMonitor", DeclaredPublicStatic)!.ReturnType,
                Is.EqualTo(typeof(Rect)));
        });
    }

    [Test]
    public void RibbonControl_PortableHelpersHandleUnattachedInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(global::Fluent.RibbonControl.GetControlWorkArea(null), Is.EqualTo(default(Rect)));
            Assert.That(global::Fluent.RibbonControl.GetControlMonitor(null), Is.EqualTo(default(Rect)));
            Assert.That(global::Fluent.RibbonControl.GetParentRibbon(null), Is.Null);
            Assert.That(
                () => global::Fluent.RibbonControl.BindQuickAccessItem(null!, null!),
                Throws.ArgumentNullException);
        });
    }

    [Test]
    public void ContextMenu_IsMenuFlyoutWithInformationalResizeMode()
    {
        var type = typeof(global::Fluent.ContextMenu);
        var property = type.GetProperty("ResizeMode", DeclaredPublicInstance);
        var field = type.GetField("ResizeModeProperty", DeclaredPublicStatic);

        Assert.Multiple(() =>
        {
            Assert.That(type.BaseType, Is.EqualTo(typeof(MenuFlyout)));
            Assert.That(property, Is.Not.Null);
            Assert.That(property!.PropertyType, Is.EqualTo(typeof(global::Fluent.ContextMenuResizeMode)));
            Assert.That(field, Is.Not.Null);
            Assert.That(field!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(field.GetValue(null), Is.Not.Null);
        });
    }

    [Test]
    public void ContextMenuService_MapsToContextFlyoutWithoutLiveUi()
    {
        var marker = new object();
        var type = typeof(global::Fluent.ContextMenuService);
        var contextMenuProperty = type.GetProperty("ContextMenuProperty", DeclaredPublicStatic);
        var showOnDisabledProperty = type.GetField("ShowOnDisabledProperty", DeclaredPublicStatic);

        Assert.Multiple(() =>
        {
            Assert.That(contextMenuProperty, Is.Not.Null);
            Assert.That(contextMenuProperty!.PropertyType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(showOnDisabledProperty, Is.Not.Null);
            Assert.That(showOnDisabledProperty!.FieldType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(
                global::Fluent.ContextMenuService.CoerceContextMenu(null, marker),
                Is.SameAs(marker));
            Assert.That(() => global::Fluent.ContextMenuService.Attach(typeof(Control)), Throws.Nothing);
            Assert.That(() => global::Fluent.ContextMenuService.Coerce(null), Throws.Nothing);
        });
    }

    [Test]
    public void ToolTipService_AliasesPortableWinUiService()
    {
        var type = typeof(global::Fluent.ToolTipService);

        Assert.Multiple(() =>
        {
            Assert.That(type.GetProperty("ToolTipProperty", DeclaredPublicStatic)!.PropertyType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(type.GetProperty("PlacementProperty", DeclaredPublicStatic)!.PropertyType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(type.GetProperty("PlacementTargetProperty", DeclaredPublicStatic)!.PropertyType, Is.EqualTo(typeof(DependencyProperty)));
            Assert.That(type.GetMethod("GetToolTip", DeclaredPublicStatic), Is.Not.Null);
            Assert.That(type.GetMethod("SetToolTip", DeclaredPublicStatic), Is.Not.Null);
            Assert.That(type.GetMethod("GetPlacement", DeclaredPublicStatic), Is.Not.Null);
            Assert.That(type.GetMethod("SetPlacement", DeclaredPublicStatic), Is.Not.Null);
            Assert.That(() => global::Fluent.ToolTipService.Attach(typeof(Control)), Throws.Nothing);
        });
    }
}
