#nullable enable

namespace FluentUno.Tests.Themes;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class AccessibleNameThemeTests
{
    private static readonly (string Theme, string Part)[] CodeNamedButtons =
    [
        ("BackstageTabControl.xaml", "PART_BackButton"),
        ("ColorGallery.xaml", "PART_AutomaticButton"),
        ("ColorGallery.xaml", "PART_NoColorButton"),
        ("ColorGallery.xaml", "PART_MoreColorsButton"),
        ("InRibbonGallery.xaml", "PART_UpButton"),
        ("InRibbonGallery.xaml", "PART_DownButton"),
        ("InRibbonGallery.xaml", "PART_ExpandButton"),
        ("InRibbonGallery.xaml", "CollapsedContent"),
        ("QuickAccessToolBar.xaml", "PART_OverflowButton"),
        ("QuickAccessToolBar.xaml", "PART_MenuButton"),
        ("RibbonGroupBox.xaml", "LauncherButton"),
        ("RibbonScrollViewer.xaml", "PART_LeftButton"),
        ("RibbonScrollViewer.xaml", "PART_RightButton"),
    ];

    [Test]
    public void SemanticIconOnlyButtonsDeclareANameOrLabel()
    {
        var unnamedButtons = Directory
            .EnumerateFiles(FindThemeDirectory(), "*.xaml", SearchOption.AllDirectories)
            .SelectMany(
                path => XDocument.Load(path)
                    .Descendants()
                    .Where(element => element.Name.LocalName == "Button")
                    .Select(element => new
                    {
                        ThemeName = Path.GetRelativePath(FindThemeDirectory(), path),
                        Element = element,
                    }))
            .Where(candidate => Attribute(candidate.Element, "AutomationProperties.AccessibilityView") != "Raw")
            .Where(candidate => Attribute(candidate.Element, "IsEnabled") != "False")
            .Where(candidate => !HasVisibleText(candidate.Element))
            .Where(candidate => !HasAccessibleLabel(candidate.Element))
            .Where(
                candidate => !CodeNamedButtons.Contains(
                    (
                        candidate.ThemeName.Replace(Path.DirectorySeparatorChar, '/'),
                        Attribute(candidate.Element, "x:Name") ?? string.Empty)))
            .Select(candidate => $"{candidate.ThemeName}: {Describe(candidate.Element)}")
            .ToArray();

        Assert.That(unnamedButtons, Is.Empty);
    }

    [Test]
    public void DeclaredButtonNamesAreHumanReadable()
    {
        var invalidNames = Directory
            .EnumerateFiles(FindThemeDirectory(), "*.xaml", SearchOption.AllDirectories)
            .SelectMany(
                path => XDocument.Load(path)
                    .Descendants()
                    .Where(element => element.Name.LocalName == "Button")
                    .Select(element => new
                    {
                        ThemeName = Path.GetRelativePath(FindThemeDirectory(), path),
                        Element = element,
                        Name = Attribute(element, "AutomationProperties.Name"),
                    }))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Name))
            .Where(candidate => !IsBinding(candidate.Name!))
            .Where(
                candidate => candidate.Name!.Any(
                    character => character is >= '\uE000' and <= '\uF8FF')
                             || string.Equals(
                                 candidate.Name,
                                 candidate.Element.Name.LocalName,
                                 StringComparison.OrdinalIgnoreCase)
                             || string.Equals(
                                 candidate.Name,
                                 Attribute(candidate.Element, "x:Name"),
                                 StringComparison.Ordinal))
            .Select(candidate => $"{candidate.ThemeName}: {Describe(candidate.Element)} = {candidate.Name}")
            .ToArray();

        Assert.That(invalidNames, Is.Empty);
    }

    [Test]
    public void EditablePartsFollowTheOwnershipModel()
    {
        var comboEditor = FindNamedPart("RibbonComboBox.xaml", "EditableText");
        var rawEditors = new[]
        {
            FindNamedPart("RibbonSpinner.xaml", "PART_TextBox"),
            FindNamedPart("RibbonTextBox.xaml", "PART_TextBox"),
            FindNamedPart(Path.Combine("Modern", "RibbonSearchBox.xaml"), "PART_AutoSuggestBox"),
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                Attribute(comboEditor, "AutomationProperties.LabeledBy"),
                Is.Not.Null.And.Not.Empty);
            Assert.That(
                Attribute(comboEditor, "PlaceholderText"),
                Is.EqualTo("{TemplateBinding PlaceholderText}"));
            foreach (var editor in rawEditors)
            {
                Assert.That(
                    Attribute(editor, "AutomationProperties.AccessibilityView"),
                    Is.EqualTo("Raw"),
                    Attribute(editor, "x:Name"));
            }

            Assert.That(
                Attribute(FindNamedPart("RibbonTextBox.xaml", "PART_TextBox"), "PlaceholderText"),
                Is.EqualTo("{TemplateBinding PlaceholderText}"));
        });
    }

    private static bool HasAccessibleLabel(XElement element)
        => !string.IsNullOrWhiteSpace(Attribute(element, "AutomationProperties.Name"))
           || !string.IsNullOrWhiteSpace(Attribute(element, "AutomationProperties.LabeledBy"));

    private static bool HasVisibleText(XElement element)
    {
        var content = Attribute(element, "Content");
        if (!string.IsNullOrWhiteSpace(content) && !IsBinding(content))
        {
            return content.Any(character => !char.IsWhiteSpace(character));
        }

        return element
            .Descendants()
            .Where(descendant => descendant.Name.LocalName is "TextBlock" or "ContentPresenter")
            .Select(
                descendant => Attribute(descendant, "Text")
                              ?? Attribute(descendant, "Content"))
            .Any(text => !string.IsNullOrWhiteSpace(text) && !IsBinding(text));
    }

    private static bool IsBinding(string value)
        => value.StartsWith("{", StringComparison.Ordinal);

    private static string Describe(XElement element)
        => Attribute(element, "x:Name")
           ?? Attribute(element, "AutomationProperties.AutomationId")
           ?? element.Name.LocalName;

    private static XElement FindNamedPart(string themeName, string partName)
        => XDocument.Load(Path.Combine(FindThemeDirectory(), themeName))
            .Descendants()
            .Single(element => Attribute(element, "x:Name") == partName);

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
