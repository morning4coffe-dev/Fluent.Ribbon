#nullable enable

namespace FluentUno.Tests.Automation;

using System.Reflection;
using Fluent.Automation.Peers;
using NUnit.Framework;

[TestFixture]
public sealed class SplitButtonAutomationIdTests
{
    [Test]
    public void SplitButtonsPreserveFrameworkAutomationIds()
    {
        var overrideMethod = typeof(RibbonSplitButtonAutomationPeer).GetMethod(
            "GetAutomationIdCore",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        Assert.That(overrideMethod, Is.Not.Null);
    }
}
