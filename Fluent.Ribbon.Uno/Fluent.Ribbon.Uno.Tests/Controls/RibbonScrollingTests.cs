namespace FluentUno.Tests.Controls;

using Fluent;
using NUnit.Framework;

[TestFixture]
public sealed class RibbonScrollingTests
{
    [TestCase(0, 0, -120, 0)]
    [TestCase(0, 200, 120, 0)]
    [TestCase(200, 200, -120, 200)]
    [TestCase(48, 200, 120, 0)]
    [TestCase(48, 200, -120, 96)]
    [TestCase(190, 200, -120, 200)]
    public void HorizontalWheelTargetShouldClampToScrollableRange(
        double currentOffset,
        double scrollableWidth,
        int delta,
        double expected)
        => Assert.That(
            RibbonGroupsContainerScrollViewer.GetHorizontalWheelTarget(
                currentOffset,
                scrollableWidth,
                delta,
                step: 48),
            Is.EqualTo(expected));
}
