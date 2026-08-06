namespace FluentUno.Tests.Architecture;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class RendererFeatureConfigurationTests
{
    private const string SkiaCondition =
        "'$(TargetFramework)' == 'net10.0-desktop' Or '$(TargetFramework)' == 'net10.0-browserwasm'";

    private static readonly string[] ProjectPaths =
    {
        Path.Combine("Fluent.Ribbon.Uno.Controls", "Fluent.Ribbon.Uno.Controls.csproj"),
        Path.Combine("Fluent.Ribbon.Uno.Compatibility", "Fluent.Ribbon.Uno.Compatibility.csproj"),
        Path.Combine(
            "Fluent.Ribbon.Uno.Showcase",
            "Fluent.Ribbon.Uno.Showcase",
            "Fluent.Ribbon.Uno.Showcase.csproj"),
    };

    [TestCaseSource(nameof(ProjectPaths))]
    public void MobileTargetsShouldUseTheNativeRenderer(string relativeProjectPath)
    {
        var project = XDocument.Load(Path.Combine(FindUnoDirectory(), relativeProjectPath));
        var features = project.Descendants("UnoFeatures").ToArray();
        var skiaFeatures = features
            .Where(element => Tokens(element.Value).Contains("SkiaRenderer", StringComparer.Ordinal))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                GetTargetFrameworks(project),
                Does.Contain("net10.0-android").And.Contain("net10.0-ios"));
            Assert.That(skiaFeatures, Has.Length.EqualTo(1));
            Assert.That(
                Normalize(skiaFeatures.Single().Attribute("Condition")?.Value ?? string.Empty),
                Is.EqualTo(Normalize(SkiaCondition)));
            Assert.That(
                features
                    .Where(element => element.Attribute("Condition") is null)
                    .SelectMany(element => Tokens(element.Value)),
                Has.None.EqualTo("SkiaRenderer"));
        });
    }

    [TestCaseSource(nameof(ProjectPaths))]
    public void DesktopAndBrowserTargetsShouldKeepSkiaRenderer(string relativeProjectPath)
    {
        var project = XDocument.Load(Path.Combine(FindUnoDirectory(), relativeProjectPath));
        var frameworks = GetTargetFrameworks(project);
        var skiaFeatures = project.Descendants("UnoFeatures")
            .Single(element => Tokens(element.Value).Contains("SkiaRenderer", StringComparer.Ordinal));

        Assert.Multiple(() =>
        {
            Assert.That(
                frameworks,
                Does.Contain("net10.0-desktop").And.Contain("net10.0-browserwasm"));
            Assert.That(
                Normalize(skiaFeatures.Attribute("Condition")?.Value ?? string.Empty),
                Is.EqualTo(Normalize(SkiaCondition)));
        });
    }

    private static string[] GetTargetFrameworks(XDocument project)
        => project.Descendants("TargetFrameworks")
            .Single()
            .Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<string> Tokens(string value)
        => value.Split(
            new[] { ';', '\r', '\n', ' ', '\t' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Normalize(string value)
        => string.Concat(value.Where(character => !char.IsWhiteSpace(character)));

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
