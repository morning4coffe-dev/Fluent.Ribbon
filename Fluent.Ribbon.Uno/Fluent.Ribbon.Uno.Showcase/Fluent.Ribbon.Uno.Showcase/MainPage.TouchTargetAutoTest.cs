namespace FluentRibbon.Uno.Showcase;

using System;
using System.Linq;
using System.Threading.Tasks;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

public sealed partial class MainPage
{
    private async Task VerifyTouchTargetGeometryAsync()
    {
        AutoLog("TOUCH-TARGETS BEGIN");
        if (Content is not Panel root)
        {
            AutoLog("  FAIL TOUCH-TARGETS Showcase content is not a Panel");
            return;
        }

        var normal = CreateTouchTargetProbe(touch: false, expectedTarget: 24);
        var touch = CreateTouchTargetProbe(touch: true, expectedTarget: 44);
        root.Children.Add(normal.Host);
        root.Children.Add(touch.Host);

        try
        {
            await SettleAsync(2, 75);
            ApplyProbeTemplates(normal);
            ApplyProbeTemplates(touch);
            await SettleAsync(2, 75);
            RevealNormallyConditionalTargets(normal);
            RevealNormallyConditionalTargets(touch);
            await SettleAsync(2, 75);

            AssertTouchTargetProbe(normal);
            AssertTouchTargetProbe(touch);
            AutoLog("  TOUCH-TARGETS OK");
        }
        catch (Exception ex)
        {
            AutoLog($"  TOUCH-TARGETS THREW {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            root.Children.Remove(normal.Host);
            root.Children.Remove(touch.Host);
        }

        AutoLog("TOUCH-TARGETS END");
    }

    private static TouchTargetProbe CreateTouchTargetProbe(bool touch, double expectedTarget)
    {
        var host = new StackPanel
        {
            Width = 720,
            HorizontalAlignment = HorizontalAlignment.Left,
            Opacity = 0.01,
        };
        if (touch)
        {
            host.Resources.MergedDictionaries.Add(
                new ResourceDictionary
                {
                    Source = new Uri(
                        "ms-appx:///Fluent.Ribbon.Uno/Themes/Modern/RibbonTouchDensity.xaml"),
                });
        }

        var applicationMenu = new ApplicationMenu { Header = "File" };
        var backstage = new BackstageTabControl();
        var gallery = new InRibbonGallery
        {
            Header = "Target gallery",
            Width = 240,
            Height = (3 * expectedTarget) + 12,
        };
        gallery.Items.Add(new RibbonGalleryItem { Content = "Target item" });

        var quickAccess = new QuickAccessToolBar
        {
            IsMenuDropDownVisible = true,
            Width = expectedTarget * 2,
        };
        quickAccess.Items.Add(
            new RibbonButton
            {
                Header = "Overflow target",
                Size = RibbonControlSize.Small,
            });
        var group = new RibbonGroupBox
        {
            Header = "Target group",
            IsLauncherVisible = true,
            Width = 280,
            Height = expectedTarget + 52,
        };
        var collapsedGroup = new RibbonGroupBox
        {
            Header = "Collapsed target group",
            State = RibbonGroupBoxState.Collapsed,
        };
        var spinner = new RibbonSpinner
        {
            Header = "Target spinner",
            Width = 280,
            Value = 1,
        };
        var splitButton = new RibbonSplitButton
        {
            Header = "Target split",
            Size = RibbonControlSize.Large,
        };
        var scrollViewer = new RibbonScrollViewer
        {
            Width = 220,
            Height = expectedTarget,
            Content = new Border { Width = 600, Height = expectedTarget },
        };
        var verticalResize = new ResizeableContentControl
        {
            Width = 280,
            Height = expectedTarget * 2,
            ResizeMode = ContextMenuResizeMode.Vertical,
            Content = new Border(),
        };
        var bothResize = new ResizeableContentControl
        {
            Width = 280,
            Height = expectedTarget * 2,
            ResizeMode = ContextMenuResizeMode.Both,
            Content = new Border(),
        };
        var colorGallery = new ColorGallery
        {
            Columns = 15,
            Mode = ColorGalleryMode.HighlightColors,
            ChipWidth = 13,
            ChipHeight = 13,
        };

        foreach (var element in new FrameworkElement[]
                 {
                     applicationMenu,
                     backstage,
                     gallery,
                     quickAccess,
                     group,
                     collapsedGroup,
                     spinner,
                     splitButton,
                     scrollViewer,
                     verticalResize,
                     bothResize,
                     colorGallery,
                 })
        {
            host.Children.Add(element);
        }

        return new TouchTargetProbe(
            host,
            expectedTarget,
            applicationMenu,
            backstage,
            gallery,
            quickAccess,
            group,
            collapsedGroup,
            spinner,
            splitButton,
            scrollViewer,
            verticalResize,
            bothResize,
            colorGallery);
    }

    private static void ApplyProbeTemplates(TouchTargetProbe probe)
    {
        foreach (var control in probe.Host.Children.OfType<Control>())
        {
            control.ApplyTemplate();
        }
    }

    private static void RevealNormallyConditionalTargets(TouchTargetProbe probe)
    {
        foreach (var partName in new[]
                 {
                     "PART_OverflowButton",
                     "PART_MenuButton",
                 })
        {
            if (FindDescendantByName(probe.Host, partName) is FrameworkElement part)
            {
                part.Visibility = Visibility.Visible;
            }
        }

        if (FindDescendantByName(
                probe.ScrollViewer,
                "PART_ScrollViewer") is ScrollViewer innerScrollViewer)
        {
            innerScrollViewer.ChangeView(
                horizontalOffset: 100,
                verticalOffset: null,
                zoomFactor: null,
                disableAnimation: true);
        }
    }

    private static void AssertTouchTargetProbe(TouchTargetProbe probe)
    {
        var minimum = probe.ExpectedTarget;
        AssertTarget(probe.ApplicationMenu, "PART_Button", minimum);
        AssertTarget(probe.Backstage, "PART_BackButton", minimum);
        AssertTarget(probe.Gallery, "PART_UpButton", minimum);
        AssertTarget(probe.Gallery, "PART_DownButton", minimum);
        AssertTarget(probe.Gallery, "PART_ExpandButton", minimum);
        AssertTarget(probe.QuickAccess, "PART_OverflowButton", minimum);
        AssertTarget(probe.QuickAccess, "PART_MenuButton", minimum);
        AssertTarget(probe.Group, "LauncherButton", minimum);
        AssertTarget(probe.CollapsedGroup, "PART_CollapsedButton", minimum);
        AssertSpinnerTargets(probe);
        AssertTarget(probe.SplitButton, "PART_Button", minimum);
        AssertTarget(probe.SplitButton, "PART_DropDownButton", minimum);
        AssertTarget(probe.ScrollViewer, "PART_LeftButton", minimum);
        AssertTarget(probe.ScrollViewer, "PART_RightButton", minimum);
        AssertTarget(probe.VerticalResize, "PART_ResizeVerticalThumb", minimum);
        AssertTarget(probe.BothResize, "PART_ResizeBothThumb", minimum);
        AssertTarget(probe.ColorGallery, "PART_AutomaticButton", minimum);
        AssertTarget(probe.ColorGallery, "PART_NoColorButton", minimum);
        AssertTarget(probe.ColorGallery, "PART_MoreColorsButton", minimum);

        var strip = FindDescendantByName(probe.Gallery, "PART_ActionStrip");
        Require(
            strip is not null && strip.ActualWidth >= minimum,
            $"{minimum:F0} DIP gallery action strip resolved to {strip?.ActualWidth ?? 0:F1} DIP");

        var swatch = EnumerateDescendants<Microsoft.UI.Xaml.Controls.Button>(probe.ColorGallery)
            .FirstOrDefault(
                button => AutomationProperties.GetAutomationId(button)
                    .StartsWith("ColorGallery", StringComparison.Ordinal)
                          && button.Content is Border);
        Require(swatch is not null, $"{minimum:F0} DIP color swatch was not realized");
        Require(
            swatch!.ActualWidth >= minimum && swatch.ActualHeight >= minimum,
            $"{minimum:F0} DIP color swatch target resolved to {swatch.ActualWidth:F1}x{swatch.ActualHeight:F1}");
        Require(
            swatch.Content is Border { ActualWidth: >= 12.5 and <= 13.5, ActualHeight: >= 12.5 and <= 13.5 },
            $"{minimum:F0} DIP color swatch visual chip did not remain 13x13");

        AssertVerticallyDisjoint(
            probe.Gallery,
            "PART_DownButton",
            "PART_ExpandButton",
            minimum);
    }

    private static void AssertTarget(
        UIElement owner,
        string partName,
        double minimum)
    {
        var part = FindDescendantByName(owner, partName);
        Require(part is not null, $"{partName} was not realized");
        Require(
            part!.ActualWidth >= minimum && part.ActualHeight >= minimum,
            $"{partName} resolved to {part.ActualWidth:F1}x{part.ActualHeight:F1}; expected >= {minimum:F0}x{minimum:F0}");
    }

    private static void AssertSpinnerTargets(TouchTargetProbe probe)
    {
        var minimum = probe.ExpectedTarget;
        var buttonWidth = minimum >= 44 ? minimum : 17;
        var buttonHeight = minimum >= 44 ? minimum / 2 : 11;

        AssertTarget(probe.Spinner, "InputRoot", minimum);
        foreach (var partName in new[] { "PART_UpButton", "PART_DownButton" })
        {
            var part = FindDescendantByName(probe.Spinner, partName);
            Require(part is not null, $"{partName} was not realized");
            Require(
                part!.ActualWidth >= buttonWidth && part.ActualHeight >= buttonHeight,
                $"{partName} resolved to {part.ActualWidth:F1}x{part.ActualHeight:F1}; "
                + $"expected >= {buttonWidth:F0}x{buttonHeight:F0}");
        }

        AssertVerticallyDisjoint(
            probe.Spinner,
            "PART_UpButton",
            "PART_DownButton",
            buttonHeight);
    }

    private static void AssertVerticallyDisjoint(
        UIElement owner,
        string firstPartName,
        string secondPartName,
        double minimum)
    {
        var first = FindDescendantByName(owner, firstPartName)!;
        var second = FindDescendantByName(owner, secondPartName)!;
        var firstTop = first.TransformToVisual(owner).TransformPoint(new Point()).Y;
        var secondTop = second.TransformToVisual(owner).TransformPoint(new Point()).Y;

        Require(
            first.ActualHeight >= minimum
            && second.ActualHeight >= minimum
            && firstTop + first.ActualHeight <= secondTop + 0.5,
            $"{firstPartName}/{secondPartName} targets overlap or are undersized");
    }

    private sealed record TouchTargetProbe(
        StackPanel Host,
        double ExpectedTarget,
        ApplicationMenu ApplicationMenu,
        BackstageTabControl Backstage,
        InRibbonGallery Gallery,
        QuickAccessToolBar QuickAccess,
        RibbonGroupBox Group,
        RibbonGroupBox CollapsedGroup,
        RibbonSpinner Spinner,
        RibbonSplitButton SplitButton,
        RibbonScrollViewer ScrollViewer,
        ResizeableContentControl VerticalResize,
        ResizeableContentControl BothResize,
        ColorGallery ColorGallery);
}
