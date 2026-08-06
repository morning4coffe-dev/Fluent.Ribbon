#nullable enable

namespace FluentUno.Tests.Themes;

using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class HeadingSemanticsTests
{
    [TestCase("ColorGallery.xaml", "PART_ThemeColorsHeader", "Level3")]
    [TestCase("ColorGallery.xaml", "PART_StandardColorsHeader", "Level3")]
    [TestCase("ColorGallery.xaml", "PART_RecentColorsHeader", "Level3")]
    [TestCase("RibbonGallery.xaml", "PART_GalleryHeader", "Level2")]
    public void VisualSectionHeadingsExposeHeadingLevels(
        string theme,
        string part,
        string expectedLevel)
    {
        var element = XDocument.Load(Path.Combine(FindThemeDirectory(), theme))
            .Descendants()
            .Single(candidate => Attribute(candidate, "x:Name") == part);

        Assert.That(
            Attribute(element, "AutomationProperties.HeadingLevel"),
            Is.EqualTo(expectedLevel));
    }

    [Test]
    public void GalleryFilterButtonsAreProgrammaticallyLabeled()
    {
        var source = File.ReadAllText(
            Path.Combine(
                Directory.GetParent(FindThemeDirectory())!.FullName,
                "Controls",
                "RibbonGallery.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("AutomationProperties.SetLabeledBy"));
            Assert.That(source, Does.Contain("AutomationHeadingLevel.Level3"));
        });
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
