namespace FluentUno.Tests.Controls;

using System;
using Fluent;
using NUnit.Framework;
using Windows.Foundation;

[TestFixture]
public sealed class PortPopupBehaviorTests
{
    [TestCase("_Open", "Open", 0)]
    [TestCase("Save__As", "Save_As", -1)]
    [TestCase("__Open", "_Open", -1)]
    [TestCase("Save_", "Save_", -1)]
    [TestCase("_One_Two", "One_Two", 0)]
    [TestCase("A___B", "A_B", 2)]
    public void AccessTextRecognizesTheFirstUnescapedMarker(string input, string expectedText, int expectedIndex)
    {
        Assert.That(MenuItem.ParseAccessText(input), Is.EqualTo((expectedText, expectedIndex)));
    }

    [TestCase(100, false, 140)]
    [TestCase(100, true, 140)]
    [TestCase(550, false, 350)]
    [TestCase(550, true, 350)]
    public void SubmenusChooseTheAvailableSide(double ownerX, bool rightToLeft, double expectedX)
    {
        var point = PopupResizeHelper.PlaceSubmenu(
            new Rect(ownerX, 50, 40, 24), new Size(200, 150), new Size(640, 480), rightToLeft);
        Assert.That(point, Is.EqualTo(new Point(expectedX, 50)));
    }

    [Test]
    public void SubmenusRemainWithinShortOrNarrowViewports()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                PopupResizeHelper.PlaceSubmenu(new Rect(10, 450, 40, 24), new Size(200, 150), new Size(640, 480), false),
                Is.EqualTo(new Point(50, 330)));
            Assert.That(
                PopupResizeHelper.PlaceSubmenu(new Rect(10, 30, 40, 24), new Size(200, 150), new Size(120, 80), true),
                Is.EqualTo(new Point(0, 0)));
        });
    }

    [Test]
    public void UnspecifiedPopupMaximumIsUnboundedButZeroRemainsAConstraint()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PopupResizeHelper.NormalizeMaximum(double.NaN), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PopupResizeHelper.NormalizeMaximum(0), Is.Zero);
            Assert.That(PopupResizeHelper.NormalizeMaximum(120), Is.EqualTo(120));
        });
    }
}
