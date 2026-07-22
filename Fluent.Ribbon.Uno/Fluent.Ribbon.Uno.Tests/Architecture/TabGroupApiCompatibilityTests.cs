namespace FluentUno.Tests.Architecture;

using System;
using System.Reflection;
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using NUnit.Framework;
using Windows.Foundation;

[TestFixture]
public sealed class TabGroupApiCompatibilityTests
{
    [Test]
    public void RibbonTabControlShouldExposePortableWpfContracts()
    {
        var type = typeof(RibbonTabControl);
        var dependencyProperties = new[]
        {
            nameof(RibbonTabControl.AreTabHeadersVisibleProperty),
            nameof(RibbonTabControl.CanMinimizeProperty),
            nameof(RibbonTabControl.CanUseSimplifiedProperty),
            nameof(RibbonTabControl.ContentGapHeightProperty),
            nameof(RibbonTabControl.HighlightSelectedItemProperty),
            nameof(RibbonTabControl.IsDisplayOptionsButtonVisibleProperty),
            nameof(RibbonTabControl.IsDropDownOpenProperty),
            nameof(RibbonTabControl.IsMouseWheelScrollingEnabledEverywhereProperty),
            nameof(RibbonTabControl.IsMouseWheelScrollingEnabledProperty),
            nameof(RibbonTabControl.IsSimplifiedProperty),
            nameof(RibbonTabControl.IsToolBarVisibleProperty),
            nameof(RibbonTabControl.MenuProperty),
            nameof(RibbonTabControl.SelectedContentProperty),
        };

        Assert.Multiple(() =>
        {
            Assert.That(typeof(TabView).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IDropDownControl).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetEvent(nameof(RibbonTabControl.RequestBackstageClose)), Is.Not.Null);
            Assert.That(type.GetEvent(nameof(RibbonTabControl.DropDownOpened)), Is.Not.Null);
            Assert.That(type.GetEvent(nameof(RibbonTabControl.DropDownClosed)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(RibbonTabControl.SelectFirstTab)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(RibbonTabControl.GetFirstVisibleItem)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(RibbonTabControl.GetFirstVisibleAndEnabledItem)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(RibbonTabControl.RaiseRequestBackstageClose)), Is.Not.Null);
            Assert.That(RibbonTabControl.DefaultContentGapHeight, Is.EqualTo(3D));
            Assert.That(RibbonTabControl.DefaultContentHeight, Is.EqualTo(100D));

            foreach (var property in dependencyProperties)
            {
                Assert.That(type.GetField(property), Is.Not.Null, property);
            }
        });
    }

    [Test]
    public void RibbonTabAndContextualGroupShouldExposeSelectionContracts()
    {
        var tabType = typeof(RibbonTabItem);
        var contextualType = typeof(RibbonContextualTabGroup);

        Assert.Multiple(() =>
        {
            Assert.That(typeof(IKeyTipedControl).IsAssignableFrom(tabType), Is.True);
            Assert.That(typeof(ISimplifiedStateControl).IsAssignableFrom(tabType), Is.True);
            Assert.That(tabType.GetField(nameof(RibbonTabItem.HasLeftGroupBorderProperty)), Is.Not.Null);
            Assert.That(tabType.GetField(nameof(RibbonTabItem.HasRightGroupBorderProperty)), Is.Not.Null);
            Assert.That(tabType.GetField(nameof(RibbonTabItem.IsSimplifiedProperty)), Is.Not.Null);
            Assert.That(tabType.GetProperty(nameof(RibbonTabItem.GroupsContainer))?.PropertyType, Is.EqualTo(typeof(ScrollViewer)));
            Assert.That(
                tabType.GetMethod(
                    "OnSelected",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.IsVirtual,
                Is.True);
            Assert.That(
                tabType.GetMethod(
                    "OnUnselected",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.IsVirtual,
                Is.True);

            var mouseOverField =
                contextualType.GetField(nameof(RibbonContextualTabGroup.TabItemMouseOverForegroundProperty));
            var selectedMouseOverField =
                contextualType.GetField(nameof(RibbonContextualTabGroup.TabItemSelectedMouseOverForegroundProperty));
            Assert.That(mouseOverField, Is.Not.Null);
            Assert.That(mouseOverField?.GetValue(null), Is.Not.Null);
            Assert.That(selectedMouseOverField, Is.Not.Null);
            Assert.That(selectedMouseOverField?.GetValue(null), Is.Not.Null);
            Assert.That(
                contextualType.GetMethod(nameof(RibbonContextualTabGroup.UpdateInnerVisiblityAndGroupBorders)),
                Is.Not.Null);
        });
    }

    [Test]
    public void RibbonGroupBoxShouldExposeLauncherReductionAndDropDownContracts()
    {
        var type = typeof(RibbonGroupBox);
        var dependencyProperties = new[]
        {
            nameof(RibbonGroupBox.CanAddToQuickAccessToolBarProperty),
            nameof(RibbonGroupBox.HeaderTemplateProperty),
            nameof(RibbonGroupBox.HeaderTemplateSelectorProperty),
            nameof(RibbonGroupBox.IsDropDownOpenProperty),
            nameof(RibbonGroupBox.IsLauncherEnabledProperty),
            nameof(RibbonGroupBox.IsSeparatorVisibleProperty),
            nameof(RibbonGroupBox.KeyTipProperty),
            nameof(RibbonGroupBox.LauncherButtonProperty),
            nameof(RibbonGroupBox.LauncherCommandParameterProperty),
            nameof(RibbonGroupBox.LauncherCommandTargetProperty),
            nameof(RibbonGroupBox.LauncherIconProperty),
            nameof(RibbonGroupBox.LauncherKeysProperty),
            nameof(RibbonGroupBox.LauncherTextProperty),
            nameof(RibbonGroupBox.LauncherToolTipProperty),
        };

        Assert.Multiple(() =>
        {
            Assert.That(typeof(IQuickAccessItemProvider).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IDropDownControl).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IKeyTipedControl).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IMediumIconProvider).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(ILargeIconProvider).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetProperty(nameof(RibbonGroupBox.Icon))?.PropertyType, Is.EqualTo(typeof(object)));
            Assert.That(type.GetProperty(nameof(RibbonGroupBox.MediumIcon))?.PropertyType, Is.EqualTo(typeof(object)));
            Assert.That(type.GetProperty(nameof(RibbonGroupBox.LargeIcon))?.PropertyType, Is.EqualTo(typeof(object)));
            Assert.That(type.GetMethod(nameof(RibbonGroupBox.CreateQuickAccessItem)), Is.Not.Null);
            Assert.That(
                type.GetMethod(nameof(RibbonGroupBox.TryClearCacheAndResetStateAndScaleAndNotifyParentRibbonGroupsContainer)),
                Is.Not.Null);

            foreach (var property in dependencyProperties)
            {
                Assert.That(type.GetField(property), Is.Not.Null, property);
            }
        });
    }

    [TestCase(typeof(RibbonGroupsContainer))]
    [TestCase(typeof(RibbonTabsContainer))]
    public void RibbonContainersShouldExposePortableScrollingContracts(Type type)
    {
        var methods = new[]
        {
            "LineDown",
            "LineLeft",
            "LineRight",
            "LineUp",
            "MouseWheelDown",
            "MouseWheelLeft",
            "MouseWheelRight",
            "MouseWheelUp",
            "PageDown",
            "PageLeft",
            "PageRight",
            "PageUp",
            "SetHorizontalOffset",
            "SetVerticalOffset",
        };

        Assert.Multiple(() =>
        {
            foreach (var property in new[]
                     {
                         "CanHorizontallyScroll",
                         "CanVerticallyScroll",
                         "ExtentHeight",
                         "ExtentWidth",
                         "HorizontalOffset",
                         "ScrollOwner",
                         "VerticalOffset",
                         "ViewportHeight",
                         "ViewportWidth",
                     })
            {
                Assert.That(type.GetProperty(property), Is.Not.Null, property);
            }

            foreach (var method in methods)
            {
                Assert.That(type.GetMethod(method), Is.Not.Null, method);
            }

            Assert.That(
                type.GetMethod("MakeVisible", new[] { typeof(UIElement), typeof(Rect) }),
                Is.Not.Null);
        });
    }

    [Test]
    public void TabAndGroupPeersShouldRetainSelectionAndExpandCollapseProviders()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(ISelectionProvider).IsAssignableFrom(typeof(RibbonTabControlAutomationPeer)), Is.True);
            Assert.That(typeof(ISelectionItemProvider).IsAssignableFrom(typeof(RibbonTabItemAutomationPeer)), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonTabItemDataAutomationPeer)), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonGroupBoxAutomationPeer)), Is.True);
        });
    }
}
