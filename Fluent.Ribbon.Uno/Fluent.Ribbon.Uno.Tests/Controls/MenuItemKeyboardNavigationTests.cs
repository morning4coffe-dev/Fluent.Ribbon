namespace FluentUno.Tests.Controls;

using Fluent;
using NUnit.Framework;

[TestFixture]
public sealed class MenuItemKeyboardNavigationTests
{
    [Test]
    public void SiblingTraversalSkipsUnavailableItemsAndWraps()
    {
        var focusable = new[] { true, false, false, true };

        Assert.Multiple(() =>
        {
            Assert.That(
                MenuItem.FindSiblingTargetIndex(4, 0, 1, index => focusable[index]),
                Is.EqualTo(3));
            Assert.That(
                MenuItem.FindSiblingTargetIndex(4, 3, 1, index => focusable[index]),
                Is.EqualTo(0));
            Assert.That(
                MenuItem.FindSiblingTargetIndex(4, 0, -1, index => focusable[index]),
                Is.EqualTo(3));
        });
    }

    [Test]
    public void SiblingTraversalDoesNotTrapFocusWhenNoOtherItemIsAvailable()
    {
        Assert.That(
            MenuItem.FindSiblingTargetIndex(3, 1, 1, _ => false),
            Is.EqualTo(-1));
    }
}
