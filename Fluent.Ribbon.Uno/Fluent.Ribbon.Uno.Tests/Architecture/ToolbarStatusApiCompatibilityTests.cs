namespace FluentUno.Tests.Architecture;

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using Fluent;
using Fluent.Automation.Peers;
using Fluent.Extensibility;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using NUnit.Framework;
using Windows.Foundation;

[TestFixture]
public sealed class ToolbarStatusApiCompatibilityTests
{
    [Test]
    public void QuickAccessToolBarShouldExposeCollectionRefreshAndKeyTipContracts()
    {
        var type = typeof(QuickAccessToolBar);

        Assert.Multiple(() =>
        {
            Assert.That(typeof(ILogicalChildSupport).IsAssignableFrom(type), Is.True);
            Assert.That(
                type.GetEvent(nameof(QuickAccessToolBar.ItemsChanged))?.EventHandlerType,
                Is.EqualTo(typeof(NotifyCollectionChangedEventHandler)));
            Assert.That(
                type.GetProperty(nameof(QuickAccessToolBar.QuickAccessItems))?.PropertyType,
                Is.EqualTo(
                    typeof(Fluent.Collections.ItemCollectionWithLogicalTreeSupport<QuickAccessMenuItem>)));
            Assert.That(type.GetField(nameof(QuickAccessToolBar.UpdateKeyTipsActionProperty)), Is.Not.Null);
            Assert.That(
                type.GetProperty(nameof(QuickAccessToolBar.UpdateKeyTipsAction))?.PropertyType,
                Is.EqualTo(typeof(Action<QuickAccessToolBar>)));
            Assert.That(type.GetMethod(nameof(QuickAccessToolBar.Refresh)), Is.Not.Null);
            Assert.That(
                type.GetMethod(
                    "MeasureOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Size) },
                    null)?.IsVirtual,
                Is.True);
            Assert.That(
                type.GetProperty(
                    "LogicalChildren",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.PropertyType,
                Is.EqualTo(typeof(IEnumerator)));
        });
    }

    [Test]
    public void RibbonToolBarShouldExposePortableLayoutAndSizeContracts()
    {
        var type = typeof(RibbonToolBar);

        Assert.Multiple(() =>
        {
            Assert.That(typeof(IRibbonControl).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IQuickAccessItemProvider).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(IRibbonSizeChangedSink).IsAssignableFrom(type), Is.True);
            Assert.That(typeof(ISimplifiedStateControl).IsAssignableFrom(type), Is.True);
            Assert.That(type.GetField(nameof(RibbonToolBar.SeparatorStyleProperty)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(RibbonToolBar.Children))?.PropertyType,
                Is.EqualTo(typeof(System.Collections.ObjectModel.ObservableCollection<FrameworkElement>)));
            Assert.That(type.GetMethod(nameof(RibbonToolBar.OnSizePropertyChanged)), Is.Not.Null);
            Assert.That(type.GetMethod(nameof(RibbonToolBar.CreateQuickAccessItem))?.IsVirtual, Is.True);
            Assert.That(
                type.GetMethod(
                    "GetVisualChild",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.IsVirtual,
                Is.True);
            Assert.That(
                type.GetProperty(
                    "VisualChildrenCount",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.GetMethod?.IsVirtual,
                Is.True);
        });
    }

    [Test]
    public void ToolBarDefinitionsShouldExposeWpfCollectionAndNotificationContracts()
    {
        var controlDefinition = typeof(RibbonToolBarControlDefinition);
        var layoutDefinition = typeof(RibbonToolBarLayoutDefinition);

        Assert.Multiple(() =>
        {
            Assert.That(controlDefinition.IsSealed, Is.True);
            Assert.That(typeof(INotifyPropertyChanged).IsAssignableFrom(controlDefinition), Is.True);
            Assert.That(typeof(IRibbonSizeChangedSink).IsAssignableFrom(controlDefinition), Is.True);
            Assert.That(controlDefinition.GetEvent(nameof(INotifyPropertyChanged.PropertyChanged)), Is.Not.Null);
            Assert.That(
                controlDefinition.GetProperty(nameof(RibbonToolBarControlDefinition.SizeDefinition))?.PropertyType,
                Is.EqualTo(typeof(RibbonControlSizeDefinition)));
            Assert.That(
                controlDefinition.GetField(nameof(RibbonToolBarControlDefinition.SizeDefinitionProperty))?.GetValue(null),
                Is.Not.Null);
            Assert.That(
                layoutDefinition.GetProperty(nameof(RibbonToolBarLayoutDefinition.SizeDefinition))?.PropertyType,
                Is.EqualTo(typeof(RibbonControlSizeDefinition)));
            Assert.That(
                layoutDefinition.GetField(nameof(RibbonToolBarLayoutDefinition.SizeDefinitionProperty))?.GetValue(null),
                Is.Not.Null);
            Assert.That(
                typeof(RibbonToolBarControlGroupDefinition)
                    .GetEvent(nameof(RibbonToolBarControlGroupDefinition.ChildrenChanged)),
                Is.Not.Null);
            Assert.That(
                typeof(RibbonToolBarRow)
                    .GetProperty(nameof(RibbonToolBarRow.Children))?.PropertyType,
                Is.EqualTo(typeof(System.Collections.ObjectModel.ObservableCollection<DependencyObject>)));
        });
    }

    [Test]
    public void StatusTypesShouldExposePortableContainerMenuAndAutomationContracts()
    {
        var statusType = typeof(RibbonStatusBar);
        var menuType = typeof(StatusBarMenuItem);

        Assert.Multiple(() =>
        {
            Assert.That(
                statusType.GetMethod(
                    "GetContainerForItemOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                statusType.GetMethod(
                    "IsItemItsOwnContainerOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
            Assert.That(
                statusType.GetMethod(
                    "OnItemsChanged",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(NotifyCollectionChangedEventArgs) },
                    null),
                Is.Not.Null);
            Assert.That(
                menuType.GetConstructor(new[] { typeof(StatusBarItem) }),
                Is.Not.Null);
            Assert.That(
                menuType.GetProperty(nameof(StatusBarMenuItem.StatusBarItem))?.PropertyType,
                Is.EqualTo(typeof(StatusBarItem)));
            Assert.That(typeof(IToggleProvider).IsAssignableFrom(typeof(StatusBarMenuItemAutomationPeer)), Is.True);
            Assert.That(typeof(IInvokeProvider).IsAssignableFrom(typeof(StatusBarMenuItemAutomationPeer)), Is.True);
            Assert.That(
                typeof(StatusBarPanel).GetMethod(
                    "MeasureOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                Is.Not.Null);
        });
    }
}
