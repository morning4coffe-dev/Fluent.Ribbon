namespace FluentUno.Tests.Architecture;

using System;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NUnit.Framework;
using Windows.Foundation;

[TestFixture]
public class CoreRibbonApiCompatibilityTests
{
    [Test]
    public void RibbonShouldExposePortableWpfMembers()
    {
        var type = typeof(Fluent.Ribbon);
        var expectedDependencyProperties = new[]
        {
            nameof(Fluent.Ribbon.AutomaticStateManagementProperty),
            nameof(Fluent.Ribbon.CanCustomizeQuickAccessToolBarItemsProperty),
            nameof(Fluent.Ribbon.CanCustomizeQuickAccessToolBarProperty),
            nameof(Fluent.Ribbon.CanCustomizeRibbonProperty),
            nameof(Fluent.Ribbon.CanQuickAccessLocationChangingProperty),
            nameof(Fluent.Ribbon.CanUseSimplifiedProperty),
            nameof(Fluent.Ribbon.ContentHeightProperty),
            nameof(Fluent.Ribbon.IsBackstageOrStartScreenOpenProperty),
            nameof(Fluent.Ribbon.IsDefaultContextMenuEnabledProperty),
            nameof(Fluent.Ribbon.IsDisplayOptionsButtonVisibleProperty),
            nameof(Fluent.Ribbon.IsKeyTipHandlingEnabledProperty),
            nameof(Fluent.Ribbon.IsMouseWheelScrollingEnabledEverywhereProperty),
            nameof(Fluent.Ribbon.IsMouseWheelScrollingEnabledProperty),
            nameof(Fluent.Ribbon.IsQuickAccessToolBarMenuDropDownVisibleProperty),
            nameof(Fluent.Ribbon.IsToolBarVisibleProperty),
            nameof(Fluent.Ribbon.QuickAccessToolBarHeightProperty),
            nameof(Fluent.Ribbon.QuickAccessToolBarProperty),
            nameof(Fluent.Ribbon.StartScreenProperty),
            nameof(Fluent.Ribbon.TabControlProperty),
            nameof(Fluent.Ribbon.TitleBarProperty),
        };
        var expectedCommands = new[]
        {
            nameof(Fluent.Ribbon.AddToQuickAccessCommand),
            nameof(Fluent.Ribbon.RemoveFromQuickAccessCommand),
            nameof(Fluent.Ribbon.ShowQuickAccessAboveCommand),
            nameof(Fluent.Ribbon.ShowQuickAccessBelowCommand),
            nameof(Fluent.Ribbon.ToggleMinimizeTheRibbonCommand),
            nameof(Fluent.Ribbon.SwitchToTheClassicRibbonCommand),
            nameof(Fluent.Ribbon.SwitchToTheSimplifiedRibbonCommand),
            nameof(Fluent.Ribbon.CustomizeQuickAccessToolbarCommand),
            nameof(Fluent.Ribbon.CustomizeTheRibbonCommand),
        };

        Assert.Multiple(() =>
        {
            Assert.That(typeof(Fluent.ILogicalChildSupport).IsAssignableFrom(type), Is.True);
            Assert.That(
                type.GetEvent(nameof(Fluent.Ribbon.SelectedTabChanged))?.EventHandlerType,
                Is.EqualTo(typeof(SelectionChangedEventHandler)));
            Assert.That(
                type.GetEvent(nameof(Fluent.Ribbon.IsMinimizedChanged))?.EventHandlerType,
                Is.EqualTo(typeof(EventHandler<DependencyPropertyChangedEventArgs>)));
            Assert.That(
                type.GetMethod(
                    nameof(Fluent.Ribbon.AddToQuickAccessToolBar),
                    new[] { typeof(UIElement) }),
                Is.Not.Null);
            Assert.That(
                type.GetMethod(
                    nameof(Fluent.Ribbon.RemoveFromQuickAccessToolBar),
                    new[] { typeof(UIElement) }),
                Is.Not.Null);
            Assert.That(type.GetMethod(nameof(Fluent.Ribbon.ClearQuickAccessToolBar)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(Fluent.Ribbon.GetQuickAccessElements)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(Fluent.Ribbon.RibbonStateStorage)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(Fluent.Ribbon.AreAnyKeyTipsVisible)), Is.Not.Null);
            Assert.That(Fluent.Ribbon.MinimalVisibleWidth, Is.EqualTo(300D));
            Assert.That(Fluent.Ribbon.MinimalVisibleHeight, Is.EqualTo(250D));

            foreach (var fieldName in expectedDependencyProperties)
            {
                Assert.That(type.GetField(fieldName), Is.Not.Null, fieldName);
            }

            foreach (var fieldName in expectedCommands)
            {
                Assert.That(type.GetField(fieldName)?.FieldType, Is.EqualTo(typeof(XamlUICommand)), fieldName);
            }
        });
    }

    [Test]
    public void RibbonPropertiesShouldExposeWpfNamedAttachedAccessors()
    {
        var type = typeof(Fluent.RibbonProperties);

        Assert.Multiple(() =>
        {
            Assert.That(typeof(DependencyObject).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetConstructor(Type.EmptyTypes), Is.Not.Null);
            Assert.That(
                type.GetMethod(
                    nameof(Fluent.RibbonProperties.SetSizeDefinition),
                    new[] { typeof(DependencyObject), typeof(Fluent.RibbonControlSizeDefinition) }),
                Is.Not.Null);
            Assert.That(
                type.GetMethod(
                    nameof(Fluent.RibbonProperties.GetSizeDefinition),
                    new[] { typeof(DependencyObject) })?.ReturnType,
                Is.EqualTo(typeof(Fluent.RibbonControlSizeDefinition)));
            Assert.That(
                type.GetMethod(
                    nameof(Fluent.RibbonProperties.SetCustomIconSize),
                    new[] { typeof(DependencyObject), typeof(Size) }),
                Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.SimplifiedSizeDefinitionProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.MouseOverBackgroundProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.PressedBackgroundProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.MouseOverForegroundProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.IsSelectedBackgroundProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.LastVisibleWidthProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.IsElementInQuickAccessToolBarProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.IconSizeProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.CustomIconSizeProperty)), Is.Not.Null);
            Assert.That(type.GetField(nameof(Fluent.RibbonProperties.CornerRadiusProperty)), Is.Not.Null);
        });
    }

    [Test]
    public void RibbonStateStorageShouldRetainVirtualWpfContractsAndTemporaryRoundTrip()
    {
        var type = typeof(Fluent.RibbonStateStorage);
        using var storage = new TestRibbonStateStorage
        {
            IsMinimized = true,
            ShowQuickAccessToolBarBelowRibbon = true,
            IsSimplified = true,
        };

        storage.SaveTemporary();
        storage.IsMinimized = false;
        storage.ShowQuickAccessToolBarBelowRibbon = false;
        storage.IsSimplified = false;
        storage.LoadTemporary();

        Assert.Multiple(() =>
        {
            Assert.That(type.GetConstructor(new[] { typeof(Fluent.Ribbon) }), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(Fluent.RibbonStateStorage.Save))?.IsVirtual, Is.True);
            Assert.That(type.GetMethod(nameof(Fluent.RibbonStateStorage.Load))?.IsVirtual, Is.True);
            Assert.That(type.GetMethod(nameof(Fluent.RibbonStateStorage.Reset))?.IsVirtual, Is.True);
            Assert.That(
                type.GetMethod(
                    "CreateStateData",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.IsVirtual,
                Is.True);
            Assert.That(
                type.GetMethod(
                    "Dispose",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(bool) },
                    null)?.IsVirtual,
                Is.True);
            Assert.That(storage.IsMinimized, Is.True);
            Assert.That(storage.ShowQuickAccessToolBarBelowRibbon, Is.True);
            Assert.That(storage.IsSimplified, Is.True);
            Assert.That(storage.ReadState(), Is.EqualTo("True,False,True"));
        });
    }

    private sealed class TestRibbonStateStorage : Fluent.RibbonStateStorage
    {
        public string ReadState() => CreateStateData().ToString();
    }
}
