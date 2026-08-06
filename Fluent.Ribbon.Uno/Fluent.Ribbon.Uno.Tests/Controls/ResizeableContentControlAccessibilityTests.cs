#nullable enable

namespace FluentUno.Tests.Controls;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Fluent;
using Fluent.Automation.Peers;
using Fluent.Localization.Languages;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Provider;
using NUnit.Framework;
using Windows.System;

[TestFixture]
public sealed class ResizeableContentControlAccessibilityTests
{
    [Test]
    public void ResizePeerExposesTransformPattern()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                typeof(ITransformProvider).IsAssignableFrom(
                    typeof(ResizeHandleAutomationPeer)),
                Is.True);
            Assert.That(typeof(ResizeHandle).IsAssignableTo(typeof(Control)), Is.True);
            Assert.That(typeof(ResizeHandle).IsPublic, Is.False);
            Assert.That(typeof(ResizeHandleAutomationPeer).IsPublic, Is.False);
            Assert.That(
                typeof(ResizeHandleAutomationPeer).GetMethod(
                    "GetPatternCore",
                    BindingFlags.Instance
                    | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly),
                Is.Not.Null);
        });
    }

    [Test]
    public void ResizePeerDeclaresThumbPatternAndProviderGuards()
    {
        var source = File.ReadAllText(FindControlsPath("ResizeHandle.cs"));

        Assert.Multiple(() =>
        {
            Assert.That(source, Does.Contain("AutomationControlType.Thumb"));
            Assert.That(source, Does.Contain("PatternInterface.Transform"));
            Assert.That(source, Does.Contain("ITransformProvider"));
            Assert.That(source, Does.Contain("AutomationProviderGuard.Validate"));
        });
    }

    [TestCase(50, 100, 200, 100)]
    [TestCase(250, 100, 200, 200)]
    [TestCase(150, 100, 200, 150)]
    public void ResizeDimensionsUseSharedClamping(
        double value,
        double minimum,
        double maximum,
        double expected)
    {
        Assert.That(
            ResizeableContentControl.ClampResizeDimension(
                value,
                minimum,
                maximum),
            Is.EqualTo(expected));
    }

    [TestCase(VirtualKey.Up, false, false, 0, -10, true)]
    [TestCase(VirtualKey.Down, false, true, 0, 50, true)]
    [TestCase(VirtualKey.Left, false, false, 0, 0, false)]
    [TestCase(VirtualKey.Left, true, false, -10, 0, true)]
    [TestCase(VirtualKey.Right, true, true, 50, 0, true)]
    public void ArrowMappingMatchesHandleCapabilities(
        VirtualKey key,
        bool bothDirections,
        bool shiftDown,
        double expectedHorizontal,
        double expectedVertical,
        bool expectedHandled)
    {
        var handled = ResizeableContentControl.TryGetKeyboardResizeDelta(
            key,
            bothDirections,
            shiftDown,
            out var horizontal,
            out var vertical);

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.EqualTo(expectedHandled));
            Assert.That(horizontal, Is.EqualTo(expectedHorizontal));
            Assert.That(vertical, Is.EqualTo(expectedVertical));
        });
    }

    [Test]
    public void EnglishResizeStringsAreMeaningful()
    {
        var localization = new English();

        Assert.Multiple(() =>
        {
            Assert.That(localization.ResizeVerticalHandleName, Does.Contain("vertically"));
            Assert.That(localization.ResizeBothHandleName, Does.Contain("width").And.Contain("height"));
            Assert.That(localization.ResizeVerticalHandleHelpText, Does.Contain("Up").And.Contain("Down"));
            Assert.That(localization.ResizeBothHandleHelpText, Does.Contain("arrow").And.Contain("Shift"));
        });
    }

    [Test]
    public void ResizeModesExposeExactlyOneApplicableHandle()
    {
        Assert.Multiple(() =>
        {
            Assert.That(IsAvailable(ContextMenuResizeMode.None, false), Is.False);
            Assert.That(IsAvailable(ContextMenuResizeMode.None, true), Is.False);
            Assert.That(IsAvailable(ContextMenuResizeMode.Vertical, false), Is.True);
            Assert.That(IsAvailable(ContextMenuResizeMode.Vertical, true), Is.False);
            Assert.That(IsAvailable(ContextMenuResizeMode.Both, false), Is.False);
            Assert.That(IsAvailable(ContextMenuResizeMode.Both, true), Is.True);
            Assert.That(
                ResizeableContentControl.IsResizeHandleAvailable(
                    ContextMenuResizeMode.Both,
                    canResizeVertical: true,
                    canResizeBothDirections: false,
                    isEnabled: true,
                    resizesBothDirections: true),
                Is.False);
        });

        static bool IsAvailable(
            ContextMenuResizeMode mode,
            bool bothDirections)
            => ResizeableContentControl.IsResizeHandleAvailable(
                mode,
                canResizeVertical: true,
                canResizeBothDirections: true,
                isEnabled: true,
                bothDirections);
    }

    [TestCase(300, 1.5, 200)]
    [TestCase(200, 2, 100)]
    [TestCase(100, 0, 100)]
    public void TransformDimensionsConvertPhysicalPixelsToDips(
        double physicalPixels,
        double rasterizationScale,
        double expectedDips)
    {
        Assert.That(
            ResizeHandleAutomationPeer.PhysicalPixelsToDips(
                physicalPixels,
                rasterizationScale),
            Is.EqualTo(expectedDips));
    }

    [Test]
    public void TemplateUsesSingleFocusableDensityBackedHandlePerMode()
    {
        var document = XDocument.Load(FindThemePath());
        var handles = document
            .Descendants()
            .Where(element => element.Name.LocalName == nameof(ResizeHandle))
            .Where(element => Attribute(element, "x:Name")?.StartsWith("PART_Resize", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.That(handles, Has.Length.EqualTo(2));
        Assert.Multiple(() =>
        {
            foreach (var handle in handles)
            {
                Assert.That(Attribute(handle, "IsTabStop"), Is.EqualTo("True"));
                Assert.That(Attribute(handle, "UseSystemFocusVisuals"), Is.EqualTo("True"));
                Assert.That(
                    Attribute(handle, "Width") ?? Attribute(handle, "MinWidth"),
                    Is.EqualTo("{ThemeResource RibbonCompactTargetSize}"));
                Assert.That(
                    Attribute(handle, "Height") ?? Attribute(handle, "MinHeight"),
                    Is.EqualTo("{ThemeResource RibbonCompactTargetSize}"));
                Assert.That(Attribute(handle, "Visibility"), Is.EqualTo("Collapsed"));
                Assert.That(
                    Attribute(handle, "AutomationProperties.AutomationId"),
                    Is.Not.Null.And.Not.Empty);
            }

            Assert.That(
                ReadResourceDouble("Common.xaml", "RibbonCompactTargetSize"),
                Is.EqualTo(24));
            Assert.That(
                ReadResourceDouble(
                    Path.Combine("Modern", "RibbonTouchDensity.xaml"),
                    "RibbonCompactTargetSize"),
                Is.EqualTo(44));
        });
    }

    private static double ReadResourceDouble(string relativePath, string key)
        => double.Parse(
            XDocument.Load(Path.Combine(Path.GetDirectoryName(FindThemePath())!, relativePath))
                .Root!
                .Elements()
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

    private static string FindThemePath()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno.Controls",
                "Themes",
                "ResizeableContentControl.xaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            candidate = Path.Combine(
                directory.FullName,
                "Fluent.Ribbon.Uno",
                "Fluent.Ribbon.Uno.Controls",
                "Themes",
                "ResizeableContentControl.xaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Could not locate ResizeableContentControl.xaml.");
    }

    private static string FindControlsPath(string fileName)
    {
        var themePath = FindThemePath();
        return Path.Combine(
            Directory.GetParent(Path.GetDirectoryName(themePath)!)!.FullName,
            "Controls",
            fileName);
    }
}
