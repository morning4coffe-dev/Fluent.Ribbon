#nullable enable

namespace FluentUno.Tests.Automation;

using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using NUnit.Framework;
using System.Reflection;

[TestFixture]
public class StartScreenAutomationPeerTests
{
    [Test]
    public void StartScreenPeerOwnsStartScreenState()
    {
        var ownerProperty = typeof(RibbonStartScreenAutomationPeer).GetProperty(
            "OwnerStartScreen",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.Multiple(() =>
        {
            Assert.That(ownerProperty, Is.Not.Null);
            Assert.That(ownerProperty!.PropertyType, Is.EqualTo(typeof(StartScreen)));
            Assert.That(
                typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonStartScreenAutomationPeer)),
                Is.True);
            Assert.That(
                typeof(RibbonStartScreenAutomationPeer).BaseType,
                Is.EqualTo(typeof(RibbonControlAutomationPeer)));
        });
    }

    [Test]
    public void StartScreenTabPeerOwnsCustomItemCollection()
    {
        var ownerProperty = typeof(RibbonStartScreenTabControlAutomationPeer).GetProperty(
            "OwnerTabControl",
            BindingFlags.Instance | BindingFlags.NonPublic);
        var childrenOverride = typeof(RibbonStartScreenTabControlAutomationPeer).GetMethod(
            "GetChildrenCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.Multiple(() =>
        {
            Assert.That(ownerProperty, Is.Not.Null);
            Assert.That(ownerProperty!.PropertyType, Is.EqualTo(typeof(StartScreenTabControl)));
            Assert.That(childrenOverride, Is.Not.Null);
            Assert.That(
                typeof(ISelectionProvider).IsAssignableFrom(typeof(RibbonStartScreenTabControlAutomationPeer)),
                Is.True);
        });
    }
}
