#nullable enable

namespace FluentUno.Tests.Themes;

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class ContextualTabContrastTests
{
    [TestCase("Light")]
    [TestCase("Dark")]
    public void ContextualTabTextMeetsNormalTextContrast(string theme)
    {
        var document = XDocument.Load(Path.Combine(FindThemeDirectory(), "Common.xaml"));
        var dictionary = document.Descendants()
            .Single(
                element => element.Name.LocalName == "ResourceDictionary"
                           && Attribute(element, "x:Key") == theme);
        var background = BrushColor(dictionary, "RibbonAccentBrush");
        var foreground = BrushColor(dictionary, "RibbonAccentForegroundBrush");

        Assert.That(ContrastRatio(foreground, background), Is.GreaterThanOrEqualTo(4.5));
    }

    [Test]
    public void HighContrastUsesPairedSystemColors()
    {
        var document = XDocument.Load(Path.Combine(FindThemeDirectory(), "Common.xaml"));
        var dictionary = document.Descendants()
            .Single(
                element => element.Name.LocalName == "ResourceDictionary"
                           && Attribute(element, "x:Key") == "HighContrast");

        Assert.Multiple(() =>
        {
            Assert.That(
                BrushValue(dictionary, "RibbonAccentBrush"),
                Does.Contain("SystemColorHighlightColor"));
            Assert.That(
                BrushValue(dictionary, "RibbonAccentForegroundBrush"),
                Does.Contain("SystemColorHighlightTextColor"));
        });
    }

    [Test]
    public void ContextualTabUsesAccentForeground()
    {
        var document = XDocument.Load(
            Path.Combine(FindThemeDirectory(), "RibbonContextualTabGroup.xaml"));
        var foreground = document.Descendants()
            .Single(
                element => element.Name.LocalName == "Setter"
                           && Attribute(element, "Property") == "Foreground");

        Assert.That(
            Attribute(foreground, "Value"),
            Is.EqualTo("{ThemeResource RibbonAccentForegroundBrush}"));
    }

    private static (byte Red, byte Green, byte Blue) BrushColor(
        XElement dictionary,
        string key)
    {
        var value = BrushValue(dictionary, key);
        Assert.That(value, Does.StartWith("#"));
        return (
            byte.Parse(value.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(value.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            byte.Parse(value.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
    }

    private static string BrushValue(XElement dictionary, string key)
        => Attribute(
               dictionary.Elements()
                   .Single(element => Attribute(element, "x:Key") == key),
               "Color")
           ?? string.Empty;

    private static double ContrastRatio(
        (byte Red, byte Green, byte Blue) first,
        (byte Red, byte Green, byte Blue) second)
    {
        var firstLuminance = Luminance(first);
        var secondLuminance = Luminance(second);
        return (Math.Max(firstLuminance, secondLuminance) + 0.05)
               / (Math.Min(firstLuminance, secondLuminance) + 0.05);
    }

    private static double Luminance((byte Red, byte Green, byte Blue) color)
        => (0.2126 * Linear(color.Red))
           + (0.7152 * Linear(color.Green))
           + (0.0722 * Linear(color.Blue));

    private static double Linear(byte component)
    {
        var value = component / 255D;
        return value <= 0.04045
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static string? Attribute(XElement element, string name)
    {
        var localName = name[(name.LastIndexOf(':') + 1)..];
        return element.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName == localName)
            ?.Value;
    }

    private static string FindThemeDirectory()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno.Controls",
                "Themes");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno",
                "Fluent.Ribbon.Uno.Controls",
                "Themes");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not locate Fluent.Ribbon.Uno.Controls\\Themes.");
    }
}
