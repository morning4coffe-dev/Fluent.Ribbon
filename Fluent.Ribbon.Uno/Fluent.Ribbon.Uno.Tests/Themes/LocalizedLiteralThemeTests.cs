#nullable enable

namespace FluentUno.Tests.Themes;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class LocalizedLiteralThemeTests
{
    private static readonly HashSet<string> ForbiddenUserFacingLiterals =
    [
        "Automatic",
        "No Color",
        "Theme Colors",
        "Standard Colors",
        "Recent Colors",
        "More Colors...",
        "Filter:",
        "Scroll up",
        "Scroll down",
        "More options",
        "More commands",
        "Customize Quick Access Toolbar",
    ];

    [Test]
    public void TargetTemplatesContainNoUserFacingEnglishLiterals()
    {
        var themeDirectory = FindThemeDirectory();
        var violations = Directory
            .EnumerateFiles(themeDirectory, "*.xaml", SearchOption.AllDirectories)
            .SelectMany(
                path => XDocument.Load(path)
                    .Descendants()
                    .SelectMany(
                        element => element.Attributes()
                            .Select(attribute => attribute.Value)
                            .Concat(
                                element.Nodes()
                                    .OfType<XText>()
                                    .Select(text => text.Value.Trim())))
                    .Where(ForbiddenUserFacingLiterals.Contains)
                    .Select(value => $"{Path.GetRelativePath(themeDirectory, path)}: {value}"))
            .ToArray();

        Assert.That(violations, Is.Empty);
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
