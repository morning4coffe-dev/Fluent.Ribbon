namespace FluentUno.Tests.Data;

using NUnit.Framework;

[TestFixture]
public class DismissPopupEventArgsTests
{
    [Test]
    public void ConstructorsShouldPreserveModeAndReason()
    {
        var args = new Fluent.DismissPopupEventArgs(
            Fluent.DismissPopupMode.MouseNotOver,
            Fluent.DismissPopupReason.ShowingKeyTips);

        Assert.Multiple(() =>
        {
            Assert.That(args.DismissMode, Is.EqualTo(Fluent.DismissPopupMode.MouseNotOver));
            Assert.That(args.DismissReason, Is.EqualTo(Fluent.DismissPopupReason.ShowingKeyTips));
        });
    }
}
