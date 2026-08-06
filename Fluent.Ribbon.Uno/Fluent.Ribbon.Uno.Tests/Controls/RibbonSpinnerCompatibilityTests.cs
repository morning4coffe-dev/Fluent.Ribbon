namespace FluentUno.Tests.Controls;

using Fluent.Automation.Peers;
using NUnit.Framework;

[TestFixture]
public sealed class RibbonSpinnerCompatibilityTests
{
    [TestCase(-2, 2)]
    [TestCase(0, 0)]
    [TestCase(3, 3)]
    public void AutomationChangeShouldRemainNonNegative(
        double increment,
        double expected)
        => Assert.That(
            RibbonSpinnerAutomationPeer.NormalizeAutomationChange(increment),
            Is.EqualTo(expected));

    [Test]
    public void NonFiniteAutomationChangeShouldBeReportedAsNaN()
        => Assert.That(
            RibbonSpinnerAutomationPeer.NormalizeAutomationChange(double.PositiveInfinity),
            Is.NaN);
}
