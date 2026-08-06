#nullable enable

namespace FluentUno.Tests.Automation;

using System;
using System.Reflection;
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using NUnit.Framework;

[TestFixture]
public sealed class SeparatorSemanticsTests
{
    [Test]
    public void SeparatorPeersExposeNoActionPatterns()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(IInvokeProvider).IsAssignableFrom(
                    typeof(GroupSeparatorMenuItemAutomationPeer)),
                Is.False);
            Assert.That(
                typeof(ISelectionItemProvider).IsAssignableFrom(
                    typeof(SeparatorTabItemAutomationPeer)),
                Is.False);
        });
    }

    [TestCase(typeof(GroupSeparatorMenuItem), typeof(GroupSeparatorMenuItemAutomationPeer))]
    [TestCase(typeof(SeparatorTabItem), typeof(SeparatorTabItemAutomationPeer))]
    public void SeparatorsCreateDedicatedPeers(Type controlType, Type peerType)
    {
        var method = controlType.GetMethod(
            "OnCreateAutomationPeer",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(peerType.IsSubclassOf(typeof(FrameworkElementAutomationPeer)), Is.True);
        });
    }
}
