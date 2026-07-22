namespace FluentUno.Tests.Data;

using NUnit.Framework;
using Windows.UI;

[TestFixture]
public class MoreColorsExecutingEventArgsTests
{
    [Test]
    public void PropertiesShouldPreserveWpfContract()
    {
        var color = Color.FromArgb(255, 10, 20, 30);
        var args = new Fluent.MoreColorsExecutingEventArgs
        {
            Color = color,
            Canceled = true,
        };

        Assert.Multiple(() =>
        {
            Assert.That(args.Color, Is.EqualTo(color));
            Assert.That(args.Canceled, Is.True);
        });
    }
}
