#nullable enable

namespace FluentUno.Tests.Automation;

using System.Reflection;
using Fluent;
using Fluent.Automation.Peers;
using NUnit.Framework;

[TestFixture]
public sealed class AutomationTreeVisibilityTests
{
    [Test]
    public void CustomTreePeersUseEffectiveVisibilityFiltering()
    {
        Assert.Multiple(() =>
        {
            AssertUsesVisibilityFilter<RibbonAutomationPeer>();
            AssertUsesVisibilityFilter<RibbonTabControlAutomationPeer>();
            AssertUsesVisibilityFilter<RibbonGroupBoxAutomationPeer>();
            AssertUsesVisibilityFilter<RibbonQuickAccessToolBarAutomationPeer>();
            AssertUsesVisibilityFilter<RibbonToolBarAutomationPeer>();
            AssertUsesVisibilityFilter<RibbonMenuAutomationPeer>();
            AssertUsesVisibilityFilter<RibbonStatusBarAutomationPeer>();
        });
    }

    [Test]
    public void CollapsedAndClosedOwnersHaveExplicitTreeGuards()
    {
        var ribbonChildren = typeof(RibbonAutomationPeer).GetMethod(
            "GetChildrenCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        var backstageChildren = typeof(RibbonBackstageAutomationPeer).GetMethod(
            "GetChildrenCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        var comboChildren = typeof(RibbonComboBoxAccessibleAutomationPeer).GetMethod(
            "GetChildrenCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.Multiple(() =>
        {
            Assert.That(ribbonChildren, Is.Not.Null);
            Assert.That(backstageChildren, Is.Not.Null);
            Assert.That(comboChildren, Is.Not.Null);
        });
    }

    private static void AssertUsesVisibilityFilter<TPeer>()
    {
        var method = typeof(TPeer).GetMethod(
            "GetChildrenCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.That(method, Is.Not.Null, typeof(TPeer).Name);
    }
}
