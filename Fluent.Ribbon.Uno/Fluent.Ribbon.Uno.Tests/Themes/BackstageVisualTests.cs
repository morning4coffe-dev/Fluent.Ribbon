namespace FluentUno.Tests.Themes;

using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class BackstageVisualTests
{
    private const string DisabledBrush = "RibbonBackstageForegroundDisabledBrush";

    [Test]
    public void BackstageDisabledStatesShouldUseTheBackstageContrastBrush()
    {
        foreach (var fileName in new[]
                 {
                     "Backstage.xaml",
                     "BackstageButton.xaml",
                 })
        {
            var source = File.ReadAllText(Path.Combine(FindThemesDirectory(), fileName));
            Assert.That(
                source,
                Does.Contain("DisabledPortable").And.Contain($"ThemeResource {DisabledBrush}"),
                fileName);
        }

        foreach (var fileName in new[] { "BackstageTabItem.cs", "BackstageButton.cs" })
        {
            Assert.That(
                File.ReadAllText(Path.Combine(FindControlsDirectory(), fileName)),
                Does.Contain("DisabledPortable").And.Contain("#if WINDOWS"),
                fileName);
        }
    }

    [Test]
    public void BackstageDisabledBrushShouldExistForEveryTheme()
    {
        var document = XDocument.Load(Path.Combine(FindThemesDirectory(), "Common.xaml"));
        var dictionaries = document.Descendants()
            .Where(element => element.Name.LocalName == "ResourceDictionary")
            .Where(element => Attribute(element, "x:Key") is "Light" or "Dark" or "HighContrast")
            .ToArray();

        Assert.That(dictionaries, Has.Length.EqualTo(3));
        Assert.That(
            dictionaries.All(
                dictionary => dictionary.Elements().Any(
                    element => Attribute(element, "x:Key") == DisabledBrush)),
            Is.True);
    }

    [Test]
    public void SaveAsSeparatorShouldBeHiddenOnlyOnPortableHeads()
    {
        var document = XDocument.Load(Path.Combine(FindShowcaseDirectory(), "MainPage.xaml"));
        var saveAs = document.Descendants()
            .Single(element => Attribute(element, "AutomationProperties.AutomationId") == "MniSaveAs");
        var nextItem = saveAs.ElementsAfterSelf().First();
        var codeBehind = File.ReadAllText(
            Path.Combine(FindShowcaseDirectory(), "MainPage.xaml.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(nextItem.Name.LocalName, Is.EqualTo("RibbonSeparator"));
            Assert.That(Attribute(nextItem, "x:Name"), Is.EqualTo("SaveAsSeparator"));
            Assert.That(
                codeBehind,
                Does.Contain("#if !WINDOWS")
                    .And.Contain("SaveAsSeparator.Visibility = Visibility.Collapsed;"));
        });
    }

    private static string Attribute(XElement element, string name)
        => element.Attributes().FirstOrDefault(
            attribute => attribute.Name.LocalName == name.Split(':')[^1])?.Value;

    private static string FindThemesDirectory()
    {
        var unoDirectory = FindUnoDirectory();
        return Path.Combine(unoDirectory, "Fluent.Ribbon.Uno.Controls", "Themes");
    }

    private static string FindShowcaseDirectory()
    {
        var unoDirectory = FindUnoDirectory();
        return Path.Combine(
            unoDirectory,
            "Fluent.Ribbon.Uno.Showcase",
            "Fluent.Ribbon.Uno.Showcase");
    }

    private static string FindControlsDirectory()
    {
        var unoDirectory = FindUnoDirectory();
        return Path.Combine(unoDirectory, "Fluent.Ribbon.Uno.Controls", "Controls");
    }

    private static string FindUnoDirectory()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Fluent.Ribbon.Uno");
            if (File.Exists(
                    Path.Combine(
                        candidate,
                        "Fluent.Ribbon.Uno.Controls",
                        "Fluent.Ribbon.Uno.Controls.csproj")))
            {
                return candidate;
            }

            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "Fluent.Ribbon.Uno.Controls",
                        "Fluent.Ribbon.Uno.Controls.csproj")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not find the Fluent.Ribbon.Uno source directory.");
    }
}
