namespace FluentUno.Tests.Data;

using NUnit.Framework;

[TestFixture]
public class KeyTipPressedResultTests
{
    [Test]
    public void ResultShouldPreserveWpfContract()
    {
        var result = new Fluent.KeyTipPressedResult(
            pressedElementAquiredFocus: true,
            pressedElementOpenedPopup: false);

        Assert.Multiple(() =>
        {
            Assert.That(result.PressedElementAquiredFocus, Is.True);
            Assert.That(result.PressedElementOpenedPopup, Is.False);
            Assert.That(Fluent.KeyTipPressedResult.Empty.PressedElementAquiredFocus, Is.False);
            Assert.That(Fluent.KeyTipPressedResult.Empty.PressedElementOpenedPopup, Is.False);
        });
    }
}
