namespace FluentUno.Tests.Architecture;

using System.Linq;
using Microsoft.UI.Xaml;
using NUnit.Framework;
using Windows.UI;

[TestFixture]
public sealed class ColorGalleryApiCompatibilityTests
{
    [Test]
    public void ColorGalleryShouldExposeWpfPaletteAndGradientSurface()
    {
        var type = typeof(Fluent.ColorGallery);

        Assert.Multiple(() =>
        {
            Assert.That(type.GetField(nameof(Fluent.ColorGallery.HighlightColors))?.FieldType, Is.EqualTo(typeof(Color[])));
            Assert.That(type.GetField(nameof(Fluent.ColorGallery.StandardColors))?.FieldType, Is.EqualTo(typeof(Color[])));
            Assert.That(type.GetField(nameof(Fluent.ColorGallery.StandardThemeColors))?.FieldType, Is.EqualTo(typeof(Color[])));
            Assert.That(type.GetProperty(nameof(Fluent.ColorGallery.MutableStandardColors)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(Fluent.ColorGallery.StandardColorGridRows)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(Fluent.ColorGallery.ThemeColorGridRows)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(Fluent.ColorGallery.ThemeColorsSource)), Is.Not.Null);
            Assert.That(type.GetProperty(nameof(Fluent.ColorGallery.ThemeGradients))?.PropertyType, Is.EqualTo(typeof(Color[])));
            Assert.That(type.GetProperty(nameof(Fluent.ColorGallery.StandardGradients))?.PropertyType, Is.EqualTo(typeof(Color[])));
            Assert.That(type.GetEvent(nameof(Fluent.ColorGallery.SelectedColorChanged))?.EventHandlerType, Is.EqualTo(typeof(RoutedEventHandler)));
            Assert.That(type.GetField(nameof(Fluent.ColorGallery.SelectedColorChangedEvent)), Is.Not.Null);
        });
    }

    [Test]
    public void CustomColorPickerShouldExposeInjectableResultContract()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(Fluent.IColorGalleryCustomColorPicker).GetMethod("PickColorAsync"), Is.Not.Null);
            Assert.That(typeof(Fluent.ColorGalleryCustomColorPickerResult).GetProperty("IsAccepted"), Is.Not.Null);
            Assert.That(typeof(Fluent.ColorGalleryCustomColorPickerResult).GetProperty("Color"), Is.Not.Null);
            Assert.That(typeof(Fluent.ColorGallery).GetProperty(nameof(Fluent.ColorGallery.CustomColorPicker)), Is.Not.Null);
            Assert.That(typeof(Fluent.ColorGallery).GetEvent(nameof(Fluent.ColorGallery.CustomColorPickerRequested)), Is.Not.Null);
        });
    }

    [Test]
    public void GradientGeneratorShouldReturnStableLightToDarkColors()
    {
        var source = Color.FromArgb(200, 40, 100, 180);

        var gradient = Fluent.ColorGalleryGradientGenerator.GetGradient(source, 5);

        Assert.Multiple(() =>
        {
            Assert.That(gradient, Has.Length.EqualTo(5));
            Assert.That(gradient.All(color => color.A == source.A), Is.True);
            Assert.That(Brightness(gradient[0]), Is.GreaterThan(Brightness(gradient[^1])));
            Assert.That(Fluent.ColorGalleryGradientGenerator.GetGradient(source, 0), Is.Empty);
        });
    }

    private static int Brightness(Color color) => color.R + color.G + color.B;
}
