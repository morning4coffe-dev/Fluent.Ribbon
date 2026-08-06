#nullable enable

namespace FluentUno.Tests.Themes;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

[TestFixture]
public sealed class FocusVisualThemeTests
{
    private static readonly FocusStyle[] SystemFocusStyles =
    [
        new("ApplicationMenu.xaml", "fluent:ApplicationMenu", "PART_Button"),
        new("Backstage.xaml", "fluent:BackstageTabItem", "RootBorder"),
        new("BackstageButton.xaml", "fluent:BackstageButton", "RootBorder"),
        new("QuickAccessMenuItem.xaml", "fluent:QuickAccessMenuItem", "RootBorder"),
        new("RibbonButton.xaml", "fluent:RibbonButton", "RootBorder"),
        new("RibbonCheckBox.xaml", "fluent:RibbonCheckBox", "RootBorder"),
        new("RibbonComboBox.xaml", "fluent:RibbonComboBox", "Background"),
        new("RibbonDropDownButton.xaml", "fluent:RibbonDropDownButton", "RootBorder"),
        new("RibbonGallery.xaml", "fluent:RibbonGalleryItem", "RootBorder"),
        new("RibbonMenuItem.xaml", "fluent:MenuItem", "RootBorder"),
        new("RibbonRadioButton.xaml", "fluent:RibbonRadioButton", "RootBorder"),
        new("RibbonSplitButton.xaml", "fluent:RibbonSplitButton", "RootBorder"),
        new("RibbonToggleButton.xaml", "fluent:RibbonToggleButton", "RootBorder"),
        new("StatusBarMenuItem.xaml", "fluent:StatusBarMenuItem", "RootBorder"),
        new(Path.Combine("Modern", "ModernRibbonButton.xaml"), "modernControls:ModernRibbonButton", "RootBorder"),
    ];

    [TestCaseSource(nameof(SystemFocusStyles))]
    public void FocusableOwnerStylesUseSystemFocusVisuals(FocusStyle focusStyle)
    {
        var document = LoadTheme(focusStyle.ThemeName);
        var style = FindStyle(document, focusStyle.TargetType);
        var target = FindNamedPart(document, focusStyle.FocusTargetName);

        Assert.Multiple(() =>
        {
            Assert.That(SetterValue(style, "UseSystemFocusVisuals"), Is.EqualTo("True"));
            Assert.That(SetterValue(style, "FocusVisualMargin"), Is.Not.Null.And.Not.Empty);
            Assert.That(Attribute(target, "Control.IsTemplateFocusTarget"), Is.EqualTo("True"));
        });
    }

    [Test]
    public void ExplicitFocusRingsCoverDelegatedEditors()
    {
        foreach (var themeName in new[] { "RibbonTextBox.xaml", "RibbonSpinner.xaml" })
        {
            var document = LoadTheme(themeName);
            var focusVisual = FindNamedPart(document, "FocusVisual");
            var keyboardFocused = FindVisualState(document, "KeyboardFocused");
            var pointerFocused = FindVisualState(document, "PointerFocused");

            Assert.Multiple(() =>
            {
                Assert.That(Attribute(focusVisual, "BorderThickness"), Is.EqualTo("2"), themeName);
                Assert.That(
                    Attribute(focusVisual, "BorderBrush"),
                    Does.Contain("SystemControlFocusVisualPrimaryBrush"),
                    themeName);
                Assert.That(Attribute(focusVisual, "Visibility"), Is.EqualTo("Collapsed"), themeName);
                Assert.That(
                    keyboardFocused.Descendants().Any(
                        element => element.Name.LocalName == "Setter"
                                   && Attribute(element, "Target") == "FocusVisual.Visibility"
                                   && Attribute(element, "Value") == "Visible"),
                    Is.True,
                    themeName);
                Assert.That(
                    pointerFocused.Descendants().Any(
                        element => element.Name.LocalName == "Setter"
                                   && Attribute(element, "Target") == "FocusVisual.Visibility"
                                   && Attribute(element, "Value") == "Visible"),
                    Is.False,
                    themeName);
            });
        }
    }

    [Test]
    public void SearchBoxDelegatesToNativeSystemFocusVisual()
    {
        var document = LoadTheme(Path.Combine("Modern", "RibbonSearchBox.xaml"));
        var style = FindStyle(document, "modernControls:RibbonSearchBox");
        var editor = FindNamedPart(document, "PART_AutoSuggestBox");

        Assert.Multiple(() =>
        {
            Assert.That(SetterValue(style, "IsTabStop"), Is.EqualTo("True"));
            Assert.That(Attribute(editor, "IsTabStop"), Is.EqualTo("False"));
            Assert.That(Attribute(editor, "UseSystemFocusVisuals"), Is.EqualTo("True"));
            Assert.That(Attribute(editor, "FocusVisualMargin"), Is.Not.Null.And.Not.Empty);
            Assert.That(Attribute(editor, "AutomationProperties.AccessibilityView"), Is.EqualTo("Raw"));
        });
    }

    [Test]
    public void GroupLauncherCustomTemplateRetainsSystemFocusVisual()
    {
        var document = LoadTheme("RibbonGroupBox.xaml");
        var launcher = FindNamedPart(document, "LauncherButton");
        var templateTarget = launcher
            .Descendants()
            .Single(
                element => element.Name.LocalName == "Border"
                           && Attribute(element, "Control.IsTemplateFocusTarget") == "True");

        Assert.Multiple(() =>
        {
            Assert.That(Attribute(launcher, "UseSystemFocusVisuals"), Is.EqualTo("True"));
            Assert.That(Attribute(launcher, "FocusVisualMargin"), Is.Not.Null.And.Not.Empty);
            Assert.That(templateTarget, Is.Not.Null);
        });
    }

    [Test]
    public void CollapsedGroupUsesAnExplicitKeyboardOnlyFocusRing()
    {
        var document = LoadTheme("RibbonGroupBox.xaml");
        var style = FindStyle(document, "fluent:RibbonGroupBox");
        var focusVisual = FindNamedPart(document, "CollapsedFocusVisual");
        var collapsedButton = FindNamedPart(document, "PART_CollapsedButton");
        var keyboardFocused = FindVisualState(document, "KeyboardFocused");

        Assert.Multiple(() =>
        {
            Assert.That(SetterValue(style, "UseSystemFocusVisuals"), Is.EqualTo("False"));
            Assert.That(Attribute(collapsedButton, "Control.IsTemplateFocusTarget"), Is.Null);
            Assert.That(Attribute(focusVisual, "BorderThickness"), Is.EqualTo("2"));
            Assert.That(
                Attribute(focusVisual, "BorderBrush"),
                Does.Contain("SystemControlFocusVisualPrimaryBrush"));
            Assert.That(
                keyboardFocused.Descendants().Any(
                    element => element.Name.LocalName == "Setter"
                               && Attribute(element, "Target") == "CollapsedFocusVisual.Visibility"
                               && Attribute(element, "Value") == "Visible"),
                Is.True);
        });
    }

    [Test]
    public void ExplicitlyFocusableCustomStylesAreInTheFocusInventory()
    {
        var inventoried = SystemFocusStyles
            .Select(style => (Normalize(style.ThemeName), style.TargetType))
            .Append((Normalize("RibbonTextBox.xaml"), "fluent:RibbonTextBox"))
            .Append((Normalize("RibbonSpinner.xaml"), "fluent:RibbonSpinner"))
            .Append((Normalize(Path.Combine("Modern", "RibbonSearchBox.xaml")), "modernControls:RibbonSearchBox"))
            .ToHashSet();

        var missing = Directory
            .EnumerateFiles(FindThemeDirectory(), "*.xaml", SearchOption.AllDirectories)
            .SelectMany(
                path => XDocument.Load(path)
                    .Root!
                    .Elements()
                    .Where(element => element.Name.LocalName == "Style")
                    .Select(style => new
                    {
                        ThemeName = Normalize(Path.GetRelativePath(FindThemeDirectory(), path)),
                        Style = style,
                    }))
            .Where(candidate => SetterValue(candidate.Style, "IsTabStop") == "True")
            .Where(candidate => !inventoried.Contains(
                (candidate.ThemeName, Attribute(candidate.Style, "TargetType")!)))
            .Select(candidate => $"{candidate.ThemeName}: {Attribute(candidate.Style, "TargetType")}")
            .ToArray();

        Assert.That(missing, Is.Empty);
    }

    [Test]
    public void CustomControlStylesDeclareTabStopIntent()
    {
        var implicitStyles = Directory
            .EnumerateFiles(FindThemeDirectory(), "*.xaml", SearchOption.AllDirectories)
            .SelectMany(
                path => XDocument.Load(path)
                    .Root!
                    .Elements()
                    .Where(element => element.Name.LocalName == "Style")
                    .Where(
                        element => Attribute(element, "TargetType")
                            ?.Contains(":", System.StringComparison.Ordinal) == true)
                    .Select(style => new
                    {
                        ThemeName = Normalize(Path.GetRelativePath(FindThemeDirectory(), path)),
                        Style = style,
                    }))
            .Where(candidate => SetterValue(candidate.Style, "IsTabStop") is null)
            .Select(candidate => $"{candidate.ThemeName}: {Attribute(candidate.Style, "TargetType")}")
            .ToArray();

        Assert.That(implicitStyles, Is.Empty);
    }

    private static XElement FindStyle(XDocument document, string targetType)
        => document.Root!
            .Elements()
            .Single(
                element => element.Name.LocalName == "Style"
                           && Attribute(element, "TargetType") == targetType);

    private static XElement FindNamedPart(XDocument document, string partName)
        => document.Descendants()
            .Single(element => Attribute(element, "x:Name") == partName);

    private static XElement FindVisualState(XDocument document, string stateName)
        => document.Descendants()
            .Single(
                element => element.Name.LocalName == "VisualState"
                           && Attribute(element, "x:Name") == stateName);

    private static string? SetterValue(XElement style, string propertyName)
        => style.Elements()
            .Where(element => element.Name.LocalName == "Setter")
            .Where(element => Attribute(element, "Property") == propertyName)
            .Select(element => Attribute(element, "Value"))
            .SingleOrDefault();

    private static string? Attribute(XElement element, string name)
    {
        var localName = name[(name.LastIndexOf(':') + 1)..];
        return element.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName == localName)
            ?.Value;
    }

    private static XDocument LoadTheme(string themeName)
        => XDocument.Load(Path.Combine(FindThemeDirectory(), themeName));

    private static string Normalize(string path)
        => path.Replace(Path.DirectorySeparatorChar, '/');

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

    public sealed record FocusStyle(
        string ThemeName,
        string TargetType,
        string FocusTargetName)
    {
        public override string ToString() => $"{ThemeName}: {TargetType}";
    }
}
