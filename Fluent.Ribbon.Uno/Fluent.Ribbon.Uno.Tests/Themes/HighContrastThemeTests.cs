#nullable enable

namespace FluentUno.Tests.Themes;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class HighContrastThemeTests
{
    private static readonly IReadOnlyDictionary<string, string> StatePairs =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["RibbonHoverBrush"] = "RibbonHoverForegroundBrush",
            ["RibbonPressedBrush"] = "RibbonPressedForegroundBrush",
            ["RibbonCheckedBrush"] = "RibbonCheckedForegroundBrush",
            ["RibbonSelectedBrush"] = "RibbonSelectedForegroundBrush",
            ["RibbonBackstageBackgroundHoverBrush"] = "RibbonBackstageForegroundHoverBrush",
            ["RibbonBackstageBackgroundPressedBrush"] = "RibbonBackstageForegroundPressedBrush",
            ["RibbonBackstageBackgroundSelectedBrush"] = "RibbonBackstageForegroundSelectedBrush",
        };

    private static readonly (string Background, string Foreground)[] FixedPairs =
    {
        ("RibbonAccentBrush", "RibbonAccentForegroundBrush"),
        ("RibbonHoverBrush", "RibbonHoverForegroundBrush"),
        ("RibbonPressedBrush", "RibbonPressedForegroundBrush"),
        ("RibbonCheckedBrush", "RibbonCheckedForegroundBrush"),
        ("RibbonSelectedBrush", "RibbonSelectedForegroundBrush"),
        ("RibbonBackstageBackgroundBrush", "RibbonBackstageForegroundBrush"),
        ("RibbonBackstageBackgroundHoverBrush", "RibbonBackstageForegroundHoverBrush"),
        ("RibbonBackstageBackgroundPressedBrush", "RibbonBackstageForegroundPressedBrush"),
        ("RibbonBackstageBackgroundSelectedBrush", "RibbonBackstageForegroundSelectedBrush"),
        ("RibbonStatusBarBrush", "RibbonStatusBarTextBrush"),
    };

    [Test]
    public void HighContrastStateResourcesUsePairedSystemColors()
    {
        var dictionary = ThemeDictionary("HighContrast");

        Assert.Multiple(() =>
        {
            foreach (var (background, foreground) in FixedPairs)
            {
                Assert.That(
                    BrushValue(dictionary, background),
                    Does.Contain("SystemColorHighlightColor"),
                    background);
                Assert.That(
                    BrushValue(dictionary, foreground),
                    Does.Contain("SystemColorHighlightTextColor"),
                    foreground);
            }

            Assert.That(
                BrushValue(dictionary, "RibbonBackgroundBrush"),
                Does.Contain("SystemColorWindowColor"));
            Assert.That(
                BrushValue(dictionary, "RibbonTextBrush"),
                Does.Contain("SystemColorWindowTextColor"));
            Assert.That(
                BrushValue(dictionary, "RibbonDisabledForegroundBrush"),
                Does.Contain("SystemColorGrayTextColor"));
        });
    }

    [TestCase("Light")]
    [TestCase("Dark")]
    public void FixedStatePairsMeetNormalTextContrast(string theme)
    {
        var dictionary = ThemeDictionary(theme);

        Assert.Multiple(() =>
        {
            foreach (var (background, foreground) in FixedPairs)
            {
                Assert.That(
                    ContrastRatio(
                        BrushColor(dictionary, foreground),
                        BrushColor(dictionary, background)),
                    Is.GreaterThanOrEqualTo(4.5),
                    $"{theme}: {foreground} on {background}");
            }
        });
    }

    [Test]
    public void HighlightBackedVisualStatesSetThePairedForeground()
    {
        var failures = new List<string>();

        foreach (var path in Directory.EnumerateFiles(
                     FindThemeDirectory(),
                     "*.xaml",
                     SearchOption.AllDirectories))
        {
            var document = XDocument.Load(path);
            foreach (var state in document.Descendants()
                         .Where(element => element.Name.LocalName == "VisualState"))
            {
                var stateText = state.ToString(SaveOptions.DisableFormatting);
                foreach (var (background, foreground) in StatePairs)
                {
                    if (stateText.Contains($"ThemeResource {background}", StringComparison.Ordinal)
                        && !stateText.Contains($"ThemeResource {foreground}", StringComparison.Ordinal))
                    {
                        failures.Add(
                            $"{Path.GetRelativePath(FindThemeDirectory(), path)}:"
                            + $"{Attribute(state, "x:Name")} uses {background} without {foreground}");
                    }
                }
            }
        }

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void InteractiveTemplatesDoNotHardCodeWhite()
    {
        var white = new Regex(
            "(?:Foreground|Background|Color)\\s*=\\s*\"(?:White|#(?:[0-9A-F]{2})?FFFFFF)\"",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var failures = Directory.EnumerateFiles(
                FindThemeDirectory(),
                "*.xaml",
                SearchOption.AllDirectories)
            .Where(path => !path.EndsWith("Common.xaml", StringComparison.OrdinalIgnoreCase)
                           && !path.EndsWith(
                               "ModernAccentBrushes.xaml",
                               StringComparison.OrdinalIgnoreCase))
            .Where(path => white.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(FindThemeDirectory(), path))
            .ToArray();

        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    [Test]
    public void CustomDisabledStatesUseTheDisabledForeground()
    {
        var expectedTemplates = new[]
        {
            "ApplicationMenu.xaml",
            "Backstage.xaml",
            "BackstageButton.xaml",
            "BackstageTabControl.xaml",
            "QuickAccessMenuItem.xaml",
            "RibbonButton.xaml",
            Path.Combine("Modern", "ModernRibbonButton.xaml"),
            "RibbonCheckBox.xaml",
            "RibbonDropDownButton.xaml",
            "RibbonGallery.xaml",
            "RibbonGroupBox.xaml",
            "RibbonMenuItem.xaml",
            "RibbonRadioButton.xaml",
            "RibbonSplitButton.xaml",
            "RibbonToggleButton.xaml",
            "StatusBarMenuItem.xaml",
        };

        Assert.Multiple(() =>
        {
            foreach (var relativePath in expectedTemplates)
            {
                var document = XDocument.Load(Path.Combine(FindThemeDirectory(), relativePath));
                var disabledStates = document.Descendants()
                    .Where(
                        element => element.Name.LocalName == "VisualState"
                                   && Attribute(element, "x:Name") == "Disabled")
                    .ToArray();
                Assert.That(disabledStates, Is.Not.Empty, relativePath);
                Assert.That(
                    disabledStates.All(
                        state => state.ToString(SaveOptions.DisableFormatting)
                            .Contains(
                                "ThemeResource RibbonDisabledForegroundBrush",
                                StringComparison.Ordinal)),
                    Is.True,
                    relativePath);
            }
        });
    }

    [TestCase("RibbonButton.xaml", "PART_Icon")]
    [TestCase("RibbonToggleButton.xaml", "PART_Icon")]
    [TestCase("RibbonDropDownButton.xaml", "PART_Icon")]
    [TestCase("RibbonSplitButton.xaml", "PART_Icon")]
    [TestCase("RibbonCheckBox.xaml", "PART_Icon")]
    [TestCase("RibbonRadioButton.xaml", "PART_Icon")]
    [TestCase("Modern/ModernRibbonButton.xaml", "PART_Icon")]
    public void DisabledRasterIconsRemainVisuallyDistinct(
        string relativePath,
        string imageName)
    {
        var document = XDocument.Load(
            Path.Combine(FindThemeDirectory(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var disabled = document.Descendants()
            .Single(
                element => element.Name.LocalName == "VisualState"
                           && Attribute(element, "x:Name") == "Disabled");

        Assert.That(
            disabled.Descendants().Any(
                element => element.Name.LocalName == "Setter"
                           && Attribute(element, "Target") == $"{imageName}.Opacity"
                           && Attribute(element, "Value") == "0.5"),
            Is.True,
            relativePath);
    }

    private static XElement ThemeDictionary(string key)
    {
        var document = XDocument.Load(Path.Combine(FindThemeDirectory(), "Common.xaml"));
        return document.Descendants()
            .Single(
                element => element.Name.LocalName == "ResourceDictionary"
                           && Attribute(element, "x:Key") == key);
    }

    private static (byte Red, byte Green, byte Blue) BrushColor(
        XElement dictionary,
        string key)
    {
        var value = BrushValue(dictionary, key);
        Assert.That(value, Does.StartWith("#"), key);
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
