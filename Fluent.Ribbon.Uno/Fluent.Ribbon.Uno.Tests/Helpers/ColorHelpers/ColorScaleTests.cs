namespace FluentUno.Tests.Helpers.ColorHelpers;

using Fluent.Helpers.ColorHelpers;
using NUnit.Framework;
using Windows.UI;

#pragma warning disable CS0618

[TestFixture]
public class ColorScaleTests
{
    [Test]
    public void RgbInterpolationShouldInterpolateEachChannel()
    {
        var scale = new ColorScale(new[]
        {
            Color.FromArgb(255, 255, 255, 255),
            Color.FromArgb(255, 255, 0, 0),
            Color.FromArgb(255, 0, 0, 0)
        });

        Assert.That(scale.GetColor(0.25), Is.EqualTo(Color.FromArgb(255, 255, 128, 128)));
        Assert.That(scale.GetColor(0.75), Is.EqualTo(Color.FromArgb(255, 128, 0, 0)));
    }

    [Test]
    public void LabInterpolationShouldUsePerceptualColorSpace()
    {
        var scale = new ColorScale(new[]
        {
            Color.FromArgb(255, 0, 0, 0),
            Color.FromArgb(255, 255, 255, 255)
        });

        Assert.That(scale.GetColor(0.5, ColorScaleInterpolationMode.LAB), Is.EqualTo(Color.FromArgb(255, 119, 119, 119)));
    }

    [Test]
    public void TrimShouldPreserveSelectedRangeAndRescaleIt()
    {
        var scale = new ColorScale(new[]
        {
            Color.FromArgb(255, 0, 0, 0),
            Color.FromArgb(255, 255, 255, 255)
        });

        var trimmed = scale.Trim(0.25, 0.75);

        Assert.That(trimmed.GetColor(0), Is.EqualTo(Color.FromArgb(255, 64, 64, 64)));
        Assert.That(trimmed.GetColor(1), Is.EqualTo(Color.FromArgb(255, 191, 191, 191)));
    }
}
