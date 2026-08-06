#nullable enable

namespace FluentUno.Tests.Themes;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Fluent.Helpers;
using NUnit.Framework;

[TestFixture]
public sealed class TouchTargetGeometryTests
{
    private const string CompactTarget = "{ThemeResource RibbonCompactTargetSize}";
    private const string StackedSpinnerHeight = "{ThemeResource RibbonStackedSpinnerHeight}";
    private const string SpinnerButtonWidth = "{ThemeResource RibbonSpinnerButtonWidth}";
    private const string SpinnerButtonHeight = "{ThemeResource RibbonSpinnerButtonHeight}";

    private static readonly (string Theme, string Part, string[] Attributes)[] CompactParts =
    [
        ("ApplicationMenu.xaml", "PART_Button", ["MinHeight"]),
        ("BackstageTabControl.xaml", "PART_BackButton", ["MinHeight"]),
        ("ColorGallery.xaml", "PART_AutomaticButton", ["MinHeight"]),
        ("ColorGallery.xaml", "PART_NoColorButton", ["MinHeight"]),
        ("ColorGallery.xaml", "PART_MoreColorsButton", ["MinHeight"]),
        ("InRibbonGallery.xaml", "PART_UpButton", ["MinWidth", "MinHeight"]),
        ("InRibbonGallery.xaml", "PART_DownButton", ["MinWidth", "MinHeight"]),
        ("InRibbonGallery.xaml", "PART_ExpandButton", ["MinWidth", "MinHeight"]),
        ("InRibbonGallery.xaml", "CollapsedContent", ["MinHeight"]),
        ("QuickAccessToolBar.xaml", "PART_OverflowButton", ["MinWidth", "MinHeight"]),
        ("QuickAccessToolBar.xaml", "PART_MenuButton", ["MinWidth", "MinHeight"]),
        ("RibbonGroupBox.xaml", "LauncherButton", ["Width", "Height"]),
        ("RibbonGroupBox.xaml", "PART_CollapsedButton", ["MinHeight"]),
        ("RibbonSplitButton.xaml", "PART_Button", ["MinWidth", "MinHeight"]),
        ("RibbonSplitButton.xaml", "PART_DropDownButton", ["MinWidth", "MinHeight"]),
        ("RibbonScrollViewer.xaml", "PART_LeftButton", ["Width", "MinHeight"]),
        ("RibbonScrollViewer.xaml", "PART_RightButton", ["Width", "MinHeight"]),
        ("ResizeableContentControl.xaml", "PART_ResizeVerticalThumb", ["Width", "MinWidth", "MinHeight"]),
        ("ResizeableContentControl.xaml", "PART_ResizeBothThumb", ["Width", "Height"]),
    ];

    [Test]
    public void SharedDensityTokensMeetDefaultAndTouchMinimums()
    {
        var common = LoadTheme("Common.xaml");
        var touch = LoadTheme(Path.Combine("Modern", "RibbonTouchDensity.xaml"));

        Assert.Multiple(() =>
        {
            Assert.That(ResourceDouble(common, "RibbonCompactTargetSize"), Is.EqualTo(24));
            Assert.That(ResourceDouble(touch, "RibbonCompactTargetSize"), Is.EqualTo(44));
            Assert.That(ResourceDouble(common, "RibbonInlineGalleryMinHeight"), Is.EqualTo(72));
            Assert.That(ResourceDouble(touch, "RibbonInlineGalleryMinHeight"), Is.EqualTo(132));
            Assert.That(ResourceDouble(common, "RibbonStackedSpinnerHeight"), Is.EqualTo(22));
            Assert.That(ResourceDouble(touch, "RibbonStackedSpinnerHeight"), Is.EqualTo(44));
            Assert.That(ResourceDouble(common, "RibbonSpinnerButtonWidth"), Is.EqualTo(17));
            Assert.That(ResourceDouble(touch, "RibbonSpinnerButtonWidth"), Is.EqualTo(44));
            Assert.That(ResourceDouble(common, "RibbonSpinnerButtonHeight"), Is.EqualTo(11));
            Assert.That(ResourceDouble(touch, "RibbonSpinnerButtonHeight"), Is.EqualTo(22));

            Assert.That(
                ResourceDouble(common, "RibbonContentHeight"),
                Is.GreaterThanOrEqualTo((3 * 24) + 24 + 12));
            Assert.That(
                ResourceDouble(touch, "RibbonContentHeight"),
                Is.GreaterThanOrEqualTo((3 * 44) + 44 + 12));
        });
    }

    [Test]
    public void KnownInteractiveTemplatePartsConsumeCompactTargetToken()
    {
        var failures = new List<string>();

        foreach (var (theme, part, attributes) in CompactParts)
        {
            var element = FindNamedPart(theme, part);
            foreach (var attribute in attributes)
            {
                if (Attribute(element, attribute) != CompactTarget)
                {
                    failures.Add($"{theme}:{part}.{attribute}");
                }
            }
        }

        Assert.That(failures, Is.Empty);
    }

    [Test]
    public void AdjacentActionLayoutsReserveDistinctTargetRectangles()
    {
        var galleryStrip = FindNamedPart("InRibbonGallery.xaml", "PART_ActionStrip");
        var galleryRows = galleryStrip
            .Descendants()
            .Where(element => element.Name.LocalName == "RowDefinition")
            .Select(element => Attribute(element, "Height"))
            .ToArray();
        var spinner = LoadTheme("RibbonSpinner.xaml");
        var spinnerRows = FindNamedPart(spinner, "PART_UpButton")
            .Ancestors()
            .First(element => element.Name.LocalName == "Grid")
            .Descendants()
            .Where(element => element.Name.LocalName == "RowDefinition")
            .Select(element => Attribute(element, "Height"))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(Attribute(galleryStrip, "Width"), Is.EqualTo(CompactTarget));
            Assert.That(
                Attribute(FindNamedPart("InRibbonGallery.xaml", "InlineContent"), "MinHeight"),
                Is.EqualTo("{ThemeResource RibbonInlineGalleryMinHeight}"));
            Assert.That(galleryRows.Count(value => value == CompactTarget), Is.EqualTo(3));
            Assert.That(spinnerRows, Is.EqualTo(new[] { SpinnerButtonHeight, SpinnerButtonHeight }));
            Assert.That(Attribute(FindNamedPart("RibbonSpinner.xaml", "InputRoot"), "MinHeight"), Is.EqualTo(StackedSpinnerHeight));
            Assert.That(Attribute(FindNamedPart("RibbonSpinner.xaml", "PART_TextBox"), "MinHeight"), Is.EqualTo(StackedSpinnerHeight));
            Assert.That(Attribute(FindNamedPart("RibbonSpinner.xaml", "PART_UpButton"), "MinWidth"), Is.EqualTo(SpinnerButtonWidth));
            Assert.That(Attribute(FindNamedPart("RibbonSpinner.xaml", "PART_UpButton"), "MinHeight"), Is.EqualTo(SpinnerButtonHeight));
            Assert.That(Attribute(FindNamedPart("RibbonSpinner.xaml", "PART_DownButton"), "MinWidth"), Is.EqualTo(SpinnerButtonWidth));
            Assert.That(Attribute(FindNamedPart("RibbonSpinner.xaml", "PART_DownButton"), "MinHeight"), Is.EqualTo(SpinnerButtonHeight));
            Assert.That(
                spinner.Descendants()
                    .Single(element => element.Name.LocalName == "Setter"
                                       && Attribute(element, "Property") == "MinHeight")
                    .Attribute("Value")?.Value,
                Is.EqualTo(StackedSpinnerHeight));

            var ribbonStyle = LoadTheme("Ribbon.xaml")
                .Descendants()
                .Single(element => element.Name.LocalName == "Setter"
                                   && Attribute(element, "Property") == "ContentHeight");
            Assert.That(
                Attribute(ribbonStyle, "Value"),
                Is.EqualTo("{ThemeResource RibbonContentHeight}"));
        });
    }

    [TestCase(13, 13, 24, 24, 24)]
    [TestCase(13, 13, 44, 44, 44)]
    [TestCase(50, 10, 44, 50, 44)]
    [TestCase(13, 13, double.NaN, 24, 24)]
    public void SwatchHitGeometryPreservesChipSizeAndMinimumTarget(
        double chipWidth,
        double chipHeight,
        double target,
        double expectedWidth,
        double expectedHeight)
    {
        var result = TouchTargetGeometry.GetSwatchHitSize(chipWidth, chipHeight, target);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(expectedWidth));
            Assert.That(result.Height, Is.EqualTo(expectedHeight));
        });
    }

    [TestCase(22, 11)]
    [TestCase(44, 22)]
    public void SpinnerHeightMatchesTwoStackedActionRows(
        double spinnerHeight,
        double actionHeight)
        => Assert.That(spinnerHeight, Is.EqualTo(actionHeight * 2));

    private static XDocument LoadTheme(string relativePath)
        => XDocument.Load(Path.Combine(FindThemeDirectory(), relativePath));

    private static XElement FindNamedPart(string themeName, string partName)
        => FindNamedPart(LoadTheme(themeName), partName);

    private static XElement FindNamedPart(XDocument document, string partName)
        => document.Descendants().Single(element => Attribute(element, "x:Name") == partName);

    private static double ResourceDouble(XDocument document, string key)
        => double.Parse(
            document.Root!.Elements()
                .Single(element => Attribute(element, "x:Key") == key)
                .Value,
            System.Globalization.CultureInfo.InvariantCulture);

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
