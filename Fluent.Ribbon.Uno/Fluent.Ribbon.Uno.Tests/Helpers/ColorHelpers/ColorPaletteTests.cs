namespace FluentUno.Tests.Helpers.ColorHelpers;

using Fluent.Helpers.ColorHelpers;
using NUnit.Framework;
using Windows.UI;

#pragma warning disable CS0618

[TestFixture]
public class ColorPaletteTests
{
    [Test]
    public void DefaultPaletteShouldContainBaseColorAtItsCenter()
    {
        var baseColor = Color.FromArgb(255, 0, 120, 212);
        var palette = new ColorPalette(baseColor);

        Assert.That(palette.Palette, Has.Count.EqualTo(11));
        Assert.That(palette.Palette[5].Color, Is.EqualTo(baseColor));
        Assert.That(palette.Palette[0].ContrastColor, Is.EqualTo(Color.FromArgb(255, 0, 0, 0)));
        Assert.That(palette.Palette[10].ContrastColor, Is.EqualTo(Color.FromArgb(255, 255, 255, 255)));
    }

    [Test]
    public void UpdatingPaletteShouldHonorStepCount()
    {
        var palette = new ColorPalette(Color.FromArgb(255, 128, 64, 32))
        {
            Steps = 5,
            InterpolationMode = ColorScaleInterpolationMode.LAB
        };

        palette.UpdatePaletteColors();

        Assert.That(palette.Palette, Has.Count.EqualTo(5));
        Assert.That(palette.Palette[2].Color, Is.EqualTo(palette.BaseColor));
    }
}
