using Fluent;
using Fluent.Modern;
using Fluent.Modern.Commands;
using Fluent.Modern.Controls;
using Fluent.Modern.Helpers;
using Fluent.Modern.Media;
using Fluent.Modern.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Reflection;
using System.Windows.Input;
using Windows.Foundation;
using RibbonInputModeHelper = Fluent.Modern.Helpers.RibbonInputMode;
using RibbonInputDensity = Fluent.Modern.RibbonInputMode;

namespace FluentRibbon.Uno.Showcase;

public sealed partial class MainPage
{
    // The Modern tab carries ten groups — more than fit at any sane window width — so it needs a
    // reduce order like every other tab, otherwise its groups simply overflow the window and get
    // clipped with no way to reach them. Least-essential groups collapse first; three passes take
    // each group Large -> Medium -> Small -> Collapsed. Entries match a group's Name or Header.
    private const string ModernReduceOrder =
        "Onboarding,Customization,Data-driven,Fluent Visuals,Accessibility,"
        + "RTL & Accent,Adaptive & Touch,Keyboard Accelerators,Command Search,Icons,"
        + "Onboarding,Customization,Data-driven,Fluent Visuals,Accessibility,"
        + "RTL & Accent,Adaptive & Touch,Keyboard Accelerators,Command Search,Icons,"
        + "Onboarding,Customization,Data-driven,Fluent Visuals,Accessibility,"
        + "RTL & Accent,Adaptive & Touch,Keyboard Accelerators,Command Search,Icons";

    private Ribbon? modernGeneratedRibbonPreview;

    private void InitializeModernShowcase()
    {
        var tab = new RibbonTab
        {
            Header = "Modern ✨ (beyond WPF)",
            ReduceOrder = ModernReduceOrder,
        };
        IdentifyModernElement(tab, "ModernTab", "Modern extensions");
        AutomationProperties.SetAccessibilityView(tab, AccessibilityView.Control);
        RibbonCustomizationService.SetItemKey(tab, "modern-showcase");

        var iconsGroup = new RibbonGroupBox
        {
            Header = "Icons",
        };

        iconsGroup.Items.Add(IdentifyModernElement(new ModernRibbonButton
        {
            Header = "Font",
            Size = RibbonControlSize.Large,
            LargeIconSource = new FontIconSource { Glyph = "\uE734" },
            SmallIconSource = new FontIconSource { Glyph = "\uE734" },
            ScreenTipTitle = "FontIconSource",
            ScreenTipText = "Modern ribbon button icon rendered from a WinUI FontIconSource.",
        }, "ModernIconFontButton"));

        iconsGroup.Items.Add(IdentifyModernElement(new ModernRibbonButton
        {
            Header = "Path",
            Size = RibbonControlSize.Large,
            LargeIconSource = new PathIconSource { Data = CreateModernStarGeometry(32) },
            SmallIconSource = new PathIconSource { Data = CreateModernStarGeometry(16) },
            ScreenTipTitle = "PathIconSource",
            ScreenTipText = "Modern ribbon button icon rendered from vector path geometry.",
        }, "ModernIconPathButton"));

        iconsGroup.Items.Add(IdentifyModernElement(new ModernRibbonButton
        {
            Header = "SVG",
            Size = RibbonControlSize.Large,
            LargeIconSource = CreateModernSvgIconSource(),
            SmallIconSource = CreateModernSvgIconSource(),
            ScreenTipTitle = "BitmapIconSource + SVG asset",
            ScreenTipText = "Modern ribbon button icon rendered from an SVG source asset processed by Uno Resizetizer.",
        }, "ModernIconSvgButton"));

        tab.Groups.Add(iconsGroup);

        var searchGroup = new RibbonGroupBox
        {
            Header = "Command Search",
        };
        AutomationProperties.SetAutomationId(searchGroup, "ModernSearchGroup");

        var ribbonSearchBox = new RibbonSearchBox
        {
            Ribbon = MainRibbon,
            Width = 280,
        };
        AutomationProperties.SetAutomationId(ribbonSearchBox, "ModernRibbonSearchBox");
        AutomationProperties.SetName(ribbonSearchBox, "Search ribbon commands");
        searchGroup.Items.Add(ribbonSearchBox);

        tab.Groups.Add(searchGroup);

        var acceleratorGroup = new RibbonGroupBox
        {
            Header = "Keyboard Accelerators",
        };
        AutomationProperties.SetAutomationId(acceleratorGroup, "ModernAcceleratorGroup");

        var acceleratorLog = new TextBlock
        {
            Text = "Accelerator log",
            TextWrapping = TextWrapping.Wrap,
        };
        IdentifyModernElement(acceleratorLog, "ModernAcceleratorLog");

        var saveButton = CreateAcceleratorButton("Save", "Ctrl+S", "Save command", acceleratorLog, "ModernAcceleratorSaveButton");
        var copyButton = CreateAcceleratorButton("Copy", "Ctrl+C", "Copy command", acceleratorLog, "ModernAcceleratorCopyButton");
        var paletteButton = CreateAcceleratorButton("Palette", "Ctrl+Shift+P", "Command palette", acceleratorLog, "ModernAcceleratorPaletteButton");
        RibbonAccelerator.SetShowInScreenTip(paletteButton, false);

        var acceleratorPanel = CreateModernFlyoutPanel(
            "ModernAcceleratorPanel",
            "Keyboard accelerator examples",
            340);
        var acceleratorButtons = CreateModernFlyoutRow();
        acceleratorButtons.Children.Add(saveButton);
        acceleratorButtons.Children.Add(copyButton);
        acceleratorButtons.Children.Add(paletteButton);
        acceleratorPanel.Children.Add(acceleratorButtons);
        acceleratorPanel.Children.Add(acceleratorLog);
        acceleratorGroup.Items.Add(acceleratorPanel);

        tab.Groups.Add(acceleratorGroup);

        var adaptiveGroup = new RibbonGroupBox
        {
            Header = "Adaptive & Touch",
        };

        var adaptiveToggle = new ToggleButton
        {
            Content = "Adaptive layout",
        };
        IdentifyModernElement(adaptiveToggle, "ModernAdaptiveToggle");
        adaptiveToggle.Checked += (_, _) => RibbonAdaptiveBehavior.SetIsEnabled(MainRibbon, true);
        adaptiveToggle.Unchecked += (_, _) => RibbonAdaptiveBehavior.SetIsEnabled(MainRibbon, false);

        var touchToggle = new ToggleButton
        {
            Content = "Touch input",
        };
        IdentifyModernElement(touchToggle, "ModernTouchDensityToggle");
        touchToggle.Checked += (_, _) => RibbonInputModeHelper.SetInputMode(MainRibbon, RibbonInputDensity.Touch);
        touchToggle.Unchecked += (_, _) => RibbonInputModeHelper.SetInputMode(MainRibbon, RibbonInputDensity.Mouse);

        var adaptivePanel = CreateModernFlyoutPanel(
            "ModernAdaptivePanel",
            "Adaptive layout and touch input",
            360);
        var adaptiveButtons = CreateModernFlyoutRow();
        adaptiveButtons.Children.Add(adaptiveToggle);
        adaptiveButtons.Children.Add(touchToggle);
        adaptivePanel.Children.Add(adaptiveButtons);
        adaptivePanel.Children.Add(new TextBlock
        {
            Text = "Adaptive reacts to width. Touch declares input preference; merge the supplied density dictionary before realization for touch metrics.",
            TextWrapping = TextWrapping.Wrap,
        });
        adaptiveGroup.Items.Add(adaptivePanel);

        tab.Groups.Add(adaptiveGroup);

        var rtlAccentGroup = new RibbonGroupBox
        {
            Header = "RTL & Accent",
        };

        var rtlToggle = new ToggleButton
        {
            Content = "Right-to-left",
        };
        IdentifyModernElement(rtlToggle, "ModernRtlToggle");
        rtlToggle.Checked += (_, _) => RibbonFlow.SetIsRightToLeft(tab, true);
        rtlToggle.Unchecked += (_, _) => RibbonFlow.SetIsRightToLeft(tab, false);

        var accentSwatch = new Border
        {
            Width = 160,
            Height = 42,
            CornerRadius = new CornerRadius(8),
            Background = ResolveModernBrush("ModernRibbonAccentBrush", new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212))),
            Child = new TextBlock
            {
                Text = "System accent",
                Foreground = ResolveModernBrush("ModernRibbonAccentTextBrush", new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 4, 10, 4),
            },
        };
        IdentifyModernElement(accentSwatch, "ModernAccentSwatch");

        var rtlAccentPanel = CreateModernFlyoutPanel(
            "ModernRtlAccentPanel",
            "Right-to-left layout and system accent",
            360);
        var rtlAccentControls = CreateModernFlyoutRow();
        rtlAccentControls.Children.Add(rtlToggle);
        rtlAccentControls.Children.Add(accentSwatch);
        rtlAccentPanel.Children.Add(rtlAccentControls);
        rtlAccentPanel.Children.Add(new TextBlock
        {
            Text = "FlowDirection and accent brushes are opt-in modern resources.",
            TextWrapping = TextWrapping.Wrap,
        });
        rtlAccentGroup.Items.Add(rtlAccentPanel);

        tab.Groups.Add(rtlAccentGroup);

        var accessibilityGroup = new RibbonGroupBox
        {
            Header = "Accessibility",
        };

        var accessibilityPanel = CreateModernFlyoutPanel(
            "ModernAccessibilityPanel",
            "Accessibility and directional focus",
            240);
        RibbonFocus.SetEnableXYFocus(accessibilityPanel, true);
        accessibilityPanel.Children.Add(IdentifyModernElement(
            new Button { Content = "Navigate up" },
            "ModernNavigateUpButton"));
        accessibilityPanel.Children.Add(IdentifyModernElement(
            new Button { Content = "Navigate down" },
            "ModernNavigateDownButton"));
        accessibilityPanel.Children.Add(IdentifyModernElement(
            new Button { Content = "Navigate right" },
            "ModernNavigateRightButton"));
        accessibilityPanel.Children.Add(new TextBlock
        {
            Text = "XYFocus enables gamepad/arrow navigation. Modern controls expose Narrator-friendly automation peers.",
            TextWrapping = TextWrapping.Wrap,
        });

        accessibilityGroup.Items.Add(accessibilityPanel);
        tab.Groups.Add(accessibilityGroup);

        var visualsGroup = new RibbonGroupBox
        {
            Header = "Fluent Visuals",
        };

        var backdropStatus = new TextBlock
        {
            Text = "Backdrop: choose a material",
            TextWrapping = TextWrapping.Wrap,
        };
        IdentifyModernElement(backdropStatus, "ModernBackdropStatus");

        var visualsPanel = CreateModernFlyoutPanel(
            "ModernVisualsPanel",
            "Fluent visual effects",
            420);
        var backdropButtons = CreateModernFlyoutRow();
        backdropButtons.Children.Add(CreateBackdropButton("Mica", RibbonBackdropKind.Mica, backdropStatus, "ModernBackdropMicaButton"));
        backdropButtons.Children.Add(CreateBackdropButton("Acrylic", RibbonBackdropKind.Acrylic, backdropStatus, "ModernBackdropAcrylicButton"));
        backdropButtons.Children.Add(CreateBackdropButton("None", RibbonBackdropKind.None, backdropStatus, "ModernBackdropNoneButton"));
        visualsPanel.Children.Add(backdropButtons);
        visualsPanel.Children.Add(backdropStatus);

        var elevatedCard = new Border
        {
            Width = 190,
            Height = 64,
            CornerRadius = new CornerRadius(8),
            Background = ResolveModernBrush(
                "CardBackgroundFillColorDefaultBrush",
                ResolveModernBrush(
                    "RibbonContentBrush",
                    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 247, 250)))),
            Child = new TextBlock
            {
                Text = "ThemeShadow depth 16",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
                Foreground = ResolveModernBrush(
                    "RibbonTextBrush",
                    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 31, 31, 31))),
            },
        };
        IdentifyModernElement(elevatedCard, "ModernElevationCard");
        RibbonElevation.SetDepth(elevatedCard, 16);

        var animationPanel = new StackPanel
        {
            Spacing = 4,
            Width = 200,
        };
        var animatedChip = new Border
        {
            Height = 28,
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 212)),
            Child = new TextBlock
            {
                Text = "Implicit transitions",
                Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                Margin = new Thickness(10, 4, 10, 4),
            },
        };
        IdentifyModernElement(animatedChip, "ModernAnimatedChip");
        var animationToggle = new ToggleButton
        {
            Content = "Animate panel",
        };
        IdentifyModernElement(animationToggle, "ModernAnimationToggle");
        animationToggle.Checked += (_, _) =>
        {
            RibbonAnimations.SetEnableImplicitTransitions(animatedChip, true);
            var visual = ElementCompositionPreview.GetElementVisual(animatedChip);
            visual.Opacity = 0.72f;
            visual.Offset = new System.Numerics.Vector3(12f, visual.Offset.Y, visual.Offset.Z);
        };
        animationToggle.Unchecked += (_, _) =>
        {
            var visual = ElementCompositionPreview.GetElementVisual(animatedChip);
            visual.Opacity = 1f;
            visual.Offset = new System.Numerics.Vector3(0f, visual.Offset.Y, visual.Offset.Z);
            RibbonAnimations.SetEnableImplicitTransitions(animatedChip, false);
        };
        animationPanel.Children.Add(animationToggle);
        animationPanel.Children.Add(animatedChip);
        var visualDetails = CreateModernFlyoutRow();
        visualDetails.Children.Add(elevatedCard);
        visualDetails.Children.Add(animationPanel);
        visualsPanel.Children.Add(visualDetails);
        visualsGroup.Items.Add(visualsPanel);

        tab.Groups.Add(visualsGroup);

        var builderGroup = new RibbonGroupBox
        {
            Header = "Data-driven",
        };

        var builderLog = new TextBlock
        {
            Text = "Build and inspect a generated ribbon without mutating the live TabView.",
            TextWrapping = TextWrapping.Wrap,
        };
        IdentifyModernElement(builderLog, "ModernBuilderLog");

        var builderPanel = CreateModernFlyoutPanel(
            "ModernBuilderPanel",
            "Data-driven ribbon builder",
            300);
        builderPanel.Children.Add(IdentifyModernElement(new ModernRibbonButton
        {
            Header = "Build model",
            Size = RibbonControlSize.Large,
            LargeIconSource = new FontIconSource { Glyph = "\uE8A5" },
            SmallIconSource = new FontIconSource { Glyph = "\uE8A5" },
            ScreenTipTitle = "Data-driven RibbonBuilder",
            ScreenTipText = "Builds and validates a real Fluent ribbon control tree from a lightweight command model.",
            Command = new ModernShowcaseCommand(() =>
            {
                modernGeneratedRibbonPreview ??= Fluent.Modern.RibbonBuilder.Build(
                    CreateBuilderShowcaseModel(builderLog));
                var generatedTab = modernGeneratedRibbonPreview.Tabs.FirstOrDefault();
                if (generatedTab is null)
                {
                    builderLog.Text = "RibbonBuilder returned no tabs.";
                    return;
                }

                IdentifyGeneratedRibbon(generatedTab);
                var itemCount = generatedTab.Groups.Sum(group => group.Items.Count);
                builderLog.Text =
                    $"Built '{generatedTab.Header}' with {generatedTab.Groups.Count} groups "
                    + $"and {itemCount} controls.";
            }),
        }, "ModernBuildModelButton"));
        builderPanel.Children.Add(builderLog);
        builderGroup.Items.Add(builderPanel);

        tab.Groups.Add(builderGroup);

        var customizationGroup = new RibbonGroupBox
        {
            Header = "Customization",
        };

        var customizationLog = new TextBlock
        {
            Text = "Save, load, or preview JSON layout customization.",
            TextWrapping = TextWrapping.Wrap,
        };
        IdentifyModernElement(customizationLog, "ModernCustomizationLog");

        var customizationPanel = CreateModernFlyoutPanel(
            "ModernCustomizationPanel",
            "Ribbon layout customization",
            420);
        var customizationButtons = CreateModernFlyoutRow();
        customizationButtons.Children.Add(IdentifyModernElement(new RibbonButton
        {
            Header = "Save layout",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(async () =>
            {
                var capture = RibbonCustomizationService.CaptureResult(MainRibbon);
                if (!capture.Succeeded || capture.Value is null)
                {
                    customizationLog.Text = DescribeCustomizationIssues("Save failed", capture.Issues);
                    return;
                }

                var saved = await RibbonCustomizationService.SaveResultAsync(capture.Value);
                customizationLog.Text = saved.Succeeded
                    ? $"Ribbon layout saved to {saved.Value}."
                    : DescribeCustomizationIssues("Save failed", saved.Issues);
            }),
        }, "ModernSaveLayoutButton"));
        customizationButtons.Children.Add(IdentifyModernElement(new RibbonButton
        {
            Header = "Load layout",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(async () =>
            {
                var loaded = await RibbonCustomizationService.LoadResultAsync();
                if (!loaded.Succeeded)
                {
                    customizationLog.Text = DescribeCustomizationIssues("Load failed", loaded.Issues);
                    return;
                }

                if (loaded.Value is null)
                {
                    customizationLog.Text = "No saved ribbon layout found.";
                    return;
                }

                var applied = RibbonCustomizationService.ApplyResult(MainRibbon, loaded.Value);
                customizationLog.Text = applied.Succeeded
                    ? "Ribbon layout loaded and applied."
                    : DescribeCustomizationIssues("Apply failed", applied.Issues);
            }),
        }, "ModernLoadLayoutButton"));
        customizationButtons.Children.Add(IdentifyModernElement(new RibbonButton
        {
            Header = "Preview JSON",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(() => PreviewCustomizationLayout(customizationLog)),
        }, "ModernPreviewJsonButton"));
        customizationPanel.Children.Add(customizationButtons);
        customizationPanel.Children.Add(customizationLog);
        customizationGroup.Items.Add(customizationPanel);

        tab.Groups.Add(customizationGroup);

        var onboardingGroup = new RibbonGroupBox
        {
            Header = "Onboarding",
        };

        var coachTarget = new ModernRibbonButton
        {
            Header = "Modern tip target",
            Size = RibbonControlSize.Large,
            LargeIconSource = new FontIconSource { Glyph = "\uE946" },
            SmallIconSource = new FontIconSource { Glyph = "\uE946" },
            ScreenTipTitle = "Modern coach mark target",
            ScreenTipText = "The coach mark points at this modern ribbon button.",
        };
        IdentifyModernElement(coachTarget, "ModernCoachTarget");

        var infoBarHost = new RibbonInfoBarHost
        {
            Width = 400,
            IsClosable = true,
        };
        IdentifyModernElement(infoBarHost, "ModernInfoBarHost");

        var onboardingPanel = CreateModernFlyoutPanel(
            "ModernOnboardingPanel",
            "Onboarding tips and notifications",
            420);
        var onboardingButtons = CreateModernFlyoutRow();
        onboardingButtons.Children.Add(coachTarget);
        onboardingButtons.Children.Add(IdentifyModernElement(new RibbonButton
        {
            Header = "Show coach mark",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(() => RibbonCoachMark.Show(
                coachTarget,
                "Tip",
                "This button is a modern extension.")),
        }, "ModernShowCoachMarkButton"));
        onboardingButtons.Children.Add(IdentifyModernElement(new RibbonButton
        {
            Header = "Notify",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(() => infoBarHost.Show(
                InfoBarSeverity.Success,
                "Saved",
                "Your document was saved.")),
        }, "ModernNotifyButton"));
        onboardingPanel.Children.Add(onboardingButtons);
        onboardingPanel.Children.Add(infoBarHost);
        onboardingGroup.Items.Add(onboardingPanel);

        tab.Groups.Add(onboardingGroup);
        var modernTabIndex = Math.Min(5, MainRibbon.Tabs.Count);
        MainRibbon.Tabs.Insert(modernTabIndex, tab);
    }

    private void PreviewCustomizationLayout(TextBlock log)
    {
        var capture = RibbonCustomizationService.CaptureResult(MainRibbon);
        if (!capture.Succeeded || capture.Value is null)
        {
            log.Text = DescribeCustomizationIssues("Preview failed", capture.Issues);
            return;
        }

        var serialized = RibbonCustomizationService.SerializeResult(capture.Value);
        if (!serialized.Succeeded || string.IsNullOrWhiteSpace(serialized.Value))
        {
            log.Text = DescribeCustomizationIssues("Preview failed", serialized.Issues);
            return;
        }

        log.Text =
            $"JSON preview: {capture.Value.Tabs.Count} tabs, {serialized.Value.Length} characters.";
        ToolTipService.SetToolTip(log, serialized.Value);
    }

    private static RibbonModel CreateBuilderShowcaseModel(TextBlock log)
    {
        var model = new RibbonModel();
        var tab = new RibbonTabModel { Header = "Generated" };
        var fileGroup = new RibbonGroupModel { Header = "Generated File" };
        fileGroup.Items.Add(new RibbonButtonModel
        {
            Header = "Save",
            Size = RibbonControlSize.Large,
            Gesture = "Ctrl+S",
            IconSource = new FontIconSource { Glyph = "\uE74E" },
            ScreenTipTitle = "Generated save",
            ScreenTipText = "Built by RibbonBuilder with IconSource and a keyboard accelerator.",
            Command = new ModernShowcaseCommand(() => log.Text = "Generated Save invoked."),
        });
        fileGroup.Items.Add(new RibbonSeparatorModel());
        fileGroup.Items.Add(new RibbonToggleButtonModel
        {
            Header = "Pinned",
            Size = RibbonControlSize.Medium,
            IconGlyph = "\uE840",
            Command = new ModernShowcaseCommand(() => log.Text = "Generated toggle invoked."),
        });

        var pluginsGroup = new RibbonGroupModel { Header = "Plugins" };
        pluginsGroup.Items.Add(new RibbonButtonModel
        {
            Header = "Run",
            Size = RibbonControlSize.Medium,
            IconGlyph = "\uE768",
            Command = new ModernShowcaseCommand(() => log.Text = "Generated plugin command invoked."),
        });
        pluginsGroup.Items.Add(new RibbonButtonModel
        {
            Header = "Info",
            Size = RibbonControlSize.Small,
            IconGlyph = "\uE946",
            Command = new ModernShowcaseCommand(() => log.Text = "Generated info command invoked."),
        });

        tab.Groups.Add(fileGroup);
        tab.Groups.Add(pluginsGroup);
        model.Tabs.Add(tab);
        return model;
    }

    private static RibbonButton CreateBackdropButton(
        string header,
        RibbonBackdropKind kind,
        TextBlock status,
        string automationId)
    {
        return IdentifyModernElement(new RibbonButton
        {
            Header = header,
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(() =>
            {
                var window = GetShowcaseMainWindow();
                var applied = window is not null && RibbonBackdrop.TryApply(window, kind);
                status.Text = applied
                    ? $"Backdrop: {kind} applied"
                    : $"Backdrop: {kind} not supported on this platform";
            }),
        }, automationId);
    }

    private static Window? GetShowcaseMainWindow()
    {
        try
        {
            var app = Application.Current;
            var property = app?.GetType().GetProperty(
                "MainWindow",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetValue(app) as Window;
        }
        catch
        {
            return null;
        }
    }

    private static RibbonButton CreateAcceleratorButton(
        string header,
        string gesture,
        string description,
        TextBlock log,
        string automationId)
    {
        var button = IdentifyModernElement(new RibbonButton
        {
            Header = header,
            Size = RibbonControlSize.Large,
            ScreenTipTitle = header,
            ScreenTipText = description,
            Command = new ModernShowcaseCommand(() =>
            {
                log.Text = $"{gesture} pressed";
            }),
        }, automationId);

        RibbonAccelerator.SetGesture(button, gesture);
        return button;
    }

    private static T IdentifyModernElement<T>(
        T element,
        string automationId,
        string? automationName = null)
        where T : FrameworkElement
    {
        AutomationProperties.SetAutomationId(element, automationId);
        if (!string.IsNullOrWhiteSpace(automationName))
        {
            AutomationProperties.SetName(element, automationName);
        }

        return element;
    }

    private static StackPanel CreateModernFlyoutPanel(
        string automationId,
        string automationName,
        double width)
    {
        var panel = IdentifyModernElement(
            new StackPanel
            {
                Spacing = 8,
                Width = width,
            },
            automationId,
            automationName);
        AutomationProperties.SetAccessibilityView(panel, AccessibilityView.Control);
        return panel;
    }

    private static StackPanel CreateModernFlyoutRow()
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
        };
    }

    private static void IdentifyGeneratedRibbon(RibbonTabItem tab)
    {
        IdentifyModernElement(tab, "ModernGeneratedTab", "Generated ribbon tab");
        RibbonCustomizationService.SetItemKey(tab, "modern-generated-tab");
        for (var groupIndex = 0; groupIndex < tab.Groups.Count; groupIndex++)
        {
            var group = tab.Groups[groupIndex];
            IdentifyModernElement(group, $"ModernGeneratedGroup{groupIndex}");
            RibbonCustomizationService.SetItemKey(
                group,
                $"modern-generated-group-{groupIndex}");
            for (var itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
            {
                if (group.Items[itemIndex] is FrameworkElement element)
                {
                    IdentifyModernElement(
                        element,
                        $"ModernGeneratedItem{groupIndex}_{itemIndex}");
                    RibbonCustomizationService.SetItemKey(
                        element,
                        $"modern-generated-item-{groupIndex}-{itemIndex}");
                }
            }
        }
    }

    private static string DescribeCustomizationIssues(
        string prefix,
        IEnumerable<RibbonCustomizationIssue> issues)
    {
        var details = issues
            .Select(issue => $"{issue.Code}: {issue.Message}")
            .ToArray();
        return details.Length == 0
            ? prefix
            : $"{prefix}: {string.Join(" | ", details)}";
    }

    private static Brush ResolveModernBrush(string key, Brush fallback)
    {
        try
        {
            return Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush
                ? brush
                : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static BitmapIconSource CreateModernSvgIconSource()
    {
        return new BitmapIconSource
        {
            UriSource = new Uri("ms-appx:///Assets/Modern/star.png"),
            ShowAsMonochrome = true,
        };
    }

    private static Geometry CreateModernStarGeometry(double dimension = 32)
    {
        var scale = dimension / 32;
        var figure = new PathFigure
        {
            StartPoint = ScalePoint(16, 3, scale),
            IsClosed = true,
            IsFilled = true,
        };

        foreach (var point in new (double X, double Y)[]
        {
            (20, 11),
            (29, 12),
            (22.5, 18.5),
            (24, 28),
            (16, 23.5),
            (8, 28),
            (9.5, 18.5),
            (3, 12),
            (12, 11),
        })
        {
            figure.Segments.Add(new LineSegment
            {
                Point = ScalePoint(point.X, point.Y, scale),
            });
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static Point ScalePoint(double x, double y, double scale)
        => new(x * scale, y * scale);

    private sealed class ModernShowcaseCommand : ICommand
    {
        private readonly Action _execute;

        public ModernShowcaseCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }
}
