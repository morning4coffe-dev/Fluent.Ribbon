namespace FluentUno.Tests.Helpers.ColorHelpers;

using Fluent.Helpers.ColorHelpers;
using NUnit.Framework;
using Windows.UI;

#pragma warning disable CS0618

[TestFixture]
public class ColorUtilsTests
{
    [Test]
    public void PrimaryRedShouldConvertToExpectedColorSpaces()
    {
        var red = Color.FromArgb(255, 255, 0, 0);

        Assert.That(ColorUtils.RGBToHSL(red), Is.EqualTo(new HSL(0, 1, 0.5)));
        Assert.That(ColorUtils.RGBToHSV(red), Is.EqualTo(new HSV(0, 1, 1)));
        Assert.That(ColorUtils.RGBToXYZ(red), Is.EqualTo(new XYZ(0.41246, 0.21267, 0.01933)));
    }

    [Test]
    public void ColorSpaceConversionsShouldRoundTrip()
    {
        var original = Color.FromArgb(255, 37, 128, 219);

        AssertColorClose(original, ColorUtils.HSLToRGB(ColorUtils.RGBToHSL(original)).Denormalize());
        AssertColorClose(original, ColorUtils.HSVToRGB(ColorUtils.RGBToHSV(original)).Denormalize());
        AssertColorClose(original, ColorUtils.LABToRGB(ColorUtils.RGBToLAB(original)).Denormalize());
        AssertColorClose(original, ColorUtils.LCHToRGB(ColorUtils.RGBToLCH(original)).Denormalize());
        AssertColorClose(original, ColorUtils.XYZToRGB(ColorUtils.RGBToXYZ(original)).Denormalize());
    }

    [Test]
    public void ContrastShouldChooseBlackForLightBackground()
    {
        var black = Color.FromArgb(255, 0, 0, 0);
        var white = Color.FromArgb(255, 255, 255, 255);
        var background = Color.FromArgb(255, 240, 240, 240);

        Assert.That(ColorUtils.ChooseColorForContrast(new[] { white, black }, background), Is.EqualTo(black));
        Assert.That(ColorUtils.ContrastRatio(black, white), Is.EqualTo(21));
    }

    private static void AssertColorClose(Color expected, Color actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.A, Is.EqualTo(expected.A));
            Assert.That(actual.R, Is.InRange((byte)(expected.R - 1), (byte)(expected.R + 1)));
            Assert.That(actual.G, Is.InRange((byte)(expected.G - 1), (byte)(expected.G + 1)));
            Assert.That(actual.B, Is.InRange((byte)(expected.B - 1), (byte)(expected.B + 1)));
        });
    }
}
