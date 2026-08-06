#nullable enable

namespace FluentUno.Tests.Automation;

using System.Reflection;
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using NUnit.Framework;

[TestFixture]
public sealed class ContainerRoleTests
{
    [Test]
    public void RibbonContainersCreateSemanticPeers()
    {
        Assert.Multiple(() =>
        {
            AssertDedicatedPeer<RibbonToolBar, RibbonToolBarAutomationPeer>();
            AssertDedicatedPeer<RibbonMenu, RibbonMenuAutomationPeer>();
            Assert.That(
                typeof(ISelectionProvider).IsAssignableFrom(typeof(RibbonMenuAutomationPeer)),
                Is.True);
        });
    }

    private static void AssertDedicatedPeer<TControl, TPeer>()
    {
        var method = typeof(TControl).GetMethod(
            "OnCreateAutomationPeer",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.That(method, Is.Not.Null, $"{typeof(TControl).Name} -> {typeof(TPeer).Name}");
    }
}
