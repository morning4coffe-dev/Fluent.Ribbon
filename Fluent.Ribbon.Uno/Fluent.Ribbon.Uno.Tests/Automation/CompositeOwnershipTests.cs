#nullable enable

namespace FluentUno.Tests.Automation;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using NUnit.Framework;

[TestFixture]
public sealed class CompositeOwnershipTests
{
    [TestCase("RibbonCheckBox.xaml", "CheckBox")]
    [TestCase("RibbonRadioButton.xaml", "RadioButton")]
    public void IndicatorPartsAreRawNonFocusableAndPointerTransparent(
        string themeName,
        string elementName)
    {
        var part = LoadTheme(themeName)
            .Descendants()
            .Single(element => element.Name.LocalName == elementName);

        AssertImplementationPart(part);
        Assert.That(Attribute(part, "IsHitTestVisible"), Is.EqualTo("False"));
        AssertOuterTabStop(themeName, expected: true);
    }

    [TestCase("ApplicationMenu.xaml", "PART_Button")]
    [TestCase("RibbonDropDownButton.xaml", "PART_Button")]
    [TestCase("RibbonSplitButton.xaml", "PART_Button")]
    [TestCase("RibbonSplitButton.xaml", "PART_DropDownButton")]
    [TestCase("RibbonGroupBox.xaml", "PART_CollapsedButton")]
    [TestCase("RibbonSpinner.xaml", "PART_TextBox")]
    [TestCase("RibbonSpinner.xaml", "PART_UpButton")]
    [TestCase("RibbonSpinner.xaml", "PART_DownButton")]
    [TestCase("RibbonTextBox.xaml", "PART_TextBox")]
    public void ImplementationPartsAreRawAndNotTabStops(string themeName, string partName)
    {
        var part = FindNamedPart(themeName, partName);
        AssertImplementationPart(part);
        if (part.Name.LocalName == "Button")
        {
            Assert.That(Attribute(part, "AllowFocusOnInteraction"), Is.EqualTo("False"));
        }
    }

    [Test]
    public void SingleActionOwnersAreTheOnlyTabStops()
    {
        AssertOuterTabStop("ApplicationMenu.xaml", expected: true);
        AssertOuterTabStop("RibbonDropDownButton.xaml", expected: true);
        AssertOuterTabStop("RibbonSplitButton.xaml", expected: true);
        AssertOuterTabStop("RibbonSpinner.xaml", expected: true);
        AssertOuterTabStop("RibbonTextBox.xaml", expected: true);
        AssertOuterTabStop("RibbonGroupBox.xaml", expected: false);
    }

    [Test]
    public void RibbonTextBoxEditorRetainsEditingBindings()
    {
        var editor = FindNamedPart("RibbonTextBox.xaml", "PART_TextBox");

        Assert.Multiple(() =>
        {
            Assert.That(Attribute(editor, "Text"), Does.Contain("Mode=TwoWay"));
            Assert.That(Attribute(editor, "IsReadOnly"), Is.EqualTo("{TemplateBinding IsReadOnly}"));
            Assert.That(Attribute(editor, "AcceptsReturn"), Is.EqualTo("{TemplateBinding AcceptsReturn}"));
            Assert.That(Attribute(editor, "TextWrapping"), Is.EqualTo("{TemplateBinding TextWrapping}"));
            Assert.That(Attribute(editor, "MaxLength"), Is.EqualTo("{TemplateBinding MaxLength}"));
            Assert.That(typeof(RibbonTextBox).GetMethod(nameof(RibbonTextBox.Select), [typeof(int), typeof(int)]), Is.Not.Null);
            Assert.That(typeof(RibbonTextBox).GetMethod(nameof(RibbonTextBox.SelectAll), Type.EmptyTypes), Is.Not.Null);
            Assert.That(
                typeof(RibbonTextBoxAutomationPeer).GetMethod(
                    "SetFocusCore",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
        });
    }

    [Test]
    public void RibbonSpinnerRetainsOuterOwnedEditingAndStepping()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(RibbonSpinner).GetMethod(
                    "FocusEditorForAutomation",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
            Assert.That(
                typeof(RibbonSpinner).GetMethod(
                    "OnTextBoxKeyDown",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
            Assert.That(
                typeof(RibbonSpinner).GetMethod(
                    "OnUpButtonClick",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
            Assert.That(
                typeof(RibbonSpinner).GetMethod(
                    "OnDownButtonClick",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
        });
    }

    [Test]
    public void SplitButtonUsesOneOuterPeerForBothActions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(typeof(IInvokeProvider).IsAssignableFrom(typeof(RibbonSplitButtonAutomationPeer)), Is.True);
            Assert.That(typeof(IExpandCollapseProvider).IsAssignableFrom(typeof(RibbonSplitButtonAutomationPeer)), Is.True);
            Assert.That(
                typeof(RibbonSplitButton).GetMethod(
                    "OnKeyDown",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
            Assert.That(
                typeof(RibbonDropDownButton).GetMethod(
                    "OnKeyDown",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
        });
    }

    [Test]
    public void GroupLauncherRemainsASeparateSemanticAction()
    {
        var launcher = FindNamedPart("RibbonGroupBox.xaml", "LauncherButton");

        Assert.Multiple(() =>
        {
            Assert.That(Attribute(launcher, "AutomationProperties.AccessibilityView"), Is.Null);
            Assert.That(Attribute(launcher, "IsTabStop"), Is.Not.EqualTo("False"));
            Assert.That(
                typeof(RibbonGroupBoxAutomationPeer).GetMethod(
                    "GetChildrenCore",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
                Is.Not.Null);
        });
    }

    private static XElement FindNamedPart(string themeName, string partName)
        => LoadTheme(themeName)
            .Descendants()
            .Single(element => Attribute(element, "x:Name") == partName);

    private static void AssertImplementationPart(XElement part)
    {
        Assert.Multiple(() =>
        {
            Assert.That(Attribute(part, "IsTabStop"), Is.EqualTo("False"));
            Assert.That(Attribute(part, "AutomationProperties.AccessibilityView"), Is.EqualTo("Raw"));
        });
    }

    private static void AssertOuterTabStop(string themeName, bool expected)
    {
        var setter = LoadTheme(themeName)
            .Descendants()
            .Single(
                element => element.Name.LocalName == "Setter"
                           && Attribute(element, "Property") == "IsTabStop");

        Assert.That(Attribute(setter, "Value"), Is.EqualTo(expected.ToString()));
    }

    private static string? Attribute(XElement element, string name)
    {
        var localName = name[(name.LastIndexOf(':') + 1)..];
        return element.Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName == localName)
            ?.Value;
    }

    private static XDocument LoadTheme(string themeName)
        => XDocument.Load(Path.Combine(FindThemeDirectory(), themeName));

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
