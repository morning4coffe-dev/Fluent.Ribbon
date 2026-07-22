using Fluent;
using Fluent.Modern;
using Fluent.Modern.Commands;
using Fluent.Modern.Controls;
using Fluent.Modern.Helpers;
using Fluent.Modern.Media;
using Fluent.Modern.Model;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
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
    private void InitializeModernShowcase()
    {
        var tab = new RibbonTab
        {
            Header = "Modern ✨ (beyond WPF)",
        };
        RibbonCustomizationService.SetItemKey(tab, "modern-showcase");

        var group = new RibbonGroupBox
        {
            Header = "Modern",
        };

        group.Items.Add(new RibbonButton
        {
            Header = "Modern feature demos are added here by later agents.",
            Size = RibbonControlSize.Large,
        });

        tab.Groups.Add(group);

        var iconsGroup = new RibbonGroupBox
        {
            Header = "Icons",
        };

        iconsGroup.Items.Add(new ModernRibbonButton
        {
            Header = "Font",
            Size = RibbonControlSize.Large,
            LargeIconSource = new FontIconSource { Glyph = "\uE734" },
            SmallIconSource = new FontIconSource { Glyph = "\uE734" },
            ScreenTipTitle = "FontIconSource",
            ScreenTipText = "Modern ribbon button icon rendered from a WinUI FontIconSource.",
        });

        iconsGroup.Items.Add(new ModernRibbonButton
        {
            Header = "Path",
            Size = RibbonControlSize.Large,
            LargeIconSource = new PathIconSource { Data = CreateModernStarGeometry() },
            SmallIconSource = new PathIconSource { Data = CreateModernStarGeometry() },
            ScreenTipTitle = "PathIconSource",
            ScreenTipText = "Modern ribbon button icon rendered from vector path geometry.",
        });

        iconsGroup.Items.Add(new ModernRibbonButton
        {
            Header = "SVG",
            Size = RibbonControlSize.Large,
            LargeIconSource = CreateModernSvgIconSource(),
            SmallIconSource = CreateModernSvgIconSource(),
            ScreenTipTitle = "ImageIconSource + SVG",
            ScreenTipText = "Modern ribbon button icon rendered from an SvgImageSource asset.",
        });

        tab.Groups.Add(iconsGroup);

        var searchGroup = new RibbonGroupBox
        {
            Header = "Command Search",
        };

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

        var acceleratorLog = new TextBlock
        {
            Text = "Accelerator log",
            TextWrapping = TextWrapping.Wrap,
            Width = 180,
        };

        var saveButton = CreateAcceleratorButton("Save", "Ctrl+S", "Save command", acceleratorLog);
        var copyButton = CreateAcceleratorButton("Copy", "Ctrl+C", "Copy command", acceleratorLog);
        var paletteButton = CreateAcceleratorButton("Palette", "Ctrl+Shift+P", "Command palette", acceleratorLog);
        RibbonAccelerator.SetShowInScreenTip(paletteButton, false);

        acceleratorGroup.Items.Add(saveButton);
        acceleratorGroup.Items.Add(copyButton);
        acceleratorGroup.Items.Add(paletteButton);
        acceleratorGroup.Items.Add(acceleratorLog);

        tab.Groups.Add(acceleratorGroup);

        var adaptiveGroup = new RibbonGroupBox
        {
            Header = "Adaptive & Touch",
        };

        var adaptiveToggle = new ToggleButton
        {
            Content = "Adaptive layout",
        };
        adaptiveToggle.Checked += (_, _) => RibbonAdaptiveBehavior.SetIsEnabled(MainRibbon, true);
        adaptiveToggle.Unchecked += (_, _) => RibbonAdaptiveBehavior.SetIsEnabled(MainRibbon, false);

        var touchToggle = new ToggleButton
        {
            Content = "Touch density",
        };
        touchToggle.Checked += (_, _) => RibbonInputModeHelper.SetInputMode(MainRibbon, RibbonInputDensity.Touch);
        touchToggle.Unchecked += (_, _) => RibbonInputModeHelper.SetInputMode(MainRibbon, RibbonInputDensity.Mouse);

        adaptiveGroup.Items.Add(adaptiveToggle);
        adaptiveGroup.Items.Add(touchToggle);
        adaptiveGroup.Items.Add(new TextBlock
        {
            Text = "Adaptive reacts to window width; touch density is applied on demand.",
            TextWrapping = TextWrapping.Wrap,
            Width = 180,
        });

        tab.Groups.Add(adaptiveGroup);

        var rtlAccentGroup = new RibbonGroupBox
        {
            Header = "RTL & Accent",
        };

        var rtlToggle = new ToggleButton
        {
            Content = "Right-to-left",
        };
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

        rtlAccentGroup.Items.Add(rtlToggle);
        rtlAccentGroup.Items.Add(accentSwatch);
        rtlAccentGroup.Items.Add(new TextBlock
        {
            Text = "FlowDirection and accent brushes are opt-in modern resources.",
            TextWrapping = TextWrapping.Wrap,
            Width = 180,
        });

        tab.Groups.Add(rtlAccentGroup);

        var accessibilityGroup = new RibbonGroupBox
        {
            Header = "Accessibility",
        };

        var accessibilityPanel = new StackPanel
        {
            Spacing = 4,
            Width = 190,
        };
        RibbonFocus.SetEnableXYFocus(accessibilityPanel, true);
        accessibilityPanel.Children.Add(new Button { Content = "Navigate up" });
        accessibilityPanel.Children.Add(new Button { Content = "Navigate down" });
        accessibilityPanel.Children.Add(new Button { Content = "Navigate right" });
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
            Width = 180,
        };

        visualsGroup.Items.Add(CreateBackdropButton("Mica", RibbonBackdropKind.Mica, backdropStatus));
        visualsGroup.Items.Add(CreateBackdropButton("Acrylic", RibbonBackdropKind.Acrylic, backdropStatus));
        visualsGroup.Items.Add(CreateBackdropButton("None", RibbonBackdropKind.None, backdropStatus));
        visualsGroup.Items.Add(backdropStatus);

        var elevatedCard = new Border
        {
            Width = 160,
            Height = 54,
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 245, 247, 250)),
            Child = new TextBlock
            {
                Text = "ThemeShadow depth 16",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(10),
            },
        };
        RibbonElevation.SetDepth(elevatedCard, 16);
        visualsGroup.Items.Add(elevatedCard);

        var animationPanel = new StackPanel
        {
            Spacing = 4,
            Width = 170,
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
        var animationToggle = new ToggleButton
        {
            Content = "Animate panel",
        };
        animationToggle.Checked += (_, _) =>
        {
            RibbonAnimations.SetEnableImplicitTransitions(animationPanel, true);
            animatedChip.Opacity = 0.72;
            animatedChip.Translation = new System.Numerics.Vector3(12f, 0f, 0f);
        };
        animationToggle.Unchecked += (_, _) =>
        {
            RibbonAnimations.SetEnableImplicitTransitions(animationPanel, false);
            animatedChip.Opacity = 1d;
            animatedChip.Translation = default;
        };
        animationPanel.Children.Add(animationToggle);
        animationPanel.Children.Add(animatedChip);
        visualsGroup.Items.Add(animationPanel);

        tab.Groups.Add(visualsGroup);

        var builderGroup = new RibbonGroupBox
        {
            Header = "Data-driven",
        };

        var builderLog = new TextBlock
        {
            Text = "Build a generated ribbon tab from a RibbonModel.",
            TextWrapping = TextWrapping.Wrap,
            Width = 190,
        };

        builderGroup.Items.Add(new ModernRibbonButton
        {
            Header = "Build model",
            Size = RibbonControlSize.Large,
            LargeIconSource = new FontIconSource { Glyph = "\uE8A5" },
            SmallIconSource = new FontIconSource { Glyph = "\uE8A5" },
            ScreenTipTitle = "Data-driven RibbonBuilder",
            ScreenTipText = "Builds a real Fluent ribbon tab from a lightweight command model.",
            Command = new ModernShowcaseCommand(() =>
            {
                var generated = Fluent.Modern.RibbonBuilder.Build(CreateBuilderShowcaseModel(builderLog));
                var generatedTab = generated.Tabs.FirstOrDefault();
                if (generatedTab is null)
                {
                    builderLog.Text = "RibbonBuilder returned no tabs.";
                    return;
                }

                var existing = MainRibbon.Tabs.FirstOrDefault(item => string.Equals(item.Header?.ToString(), generatedTab.Header?.ToString(), StringComparison.Ordinal));
                if (existing is not null)
                {
                    MainRibbon.Tabs.Remove(existing);
                }

                MainRibbon.Tabs.Add(generatedTab);
                MainRibbon.SelectedTab = generatedTab;
                builderLog.Text = "Generated tab appended from RibbonModel.";
            }),
        });
        builderGroup.Items.Add(builderLog);

        tab.Groups.Add(builderGroup);

        var customizationGroup = new RibbonGroupBox
        {
            Header = "Customization",
        };

        var customizationLog = new TextBlock
        {
            Text = "Save, load, or preview JSON layout customization.",
            TextWrapping = TextWrapping.Wrap,
            Width = 230,
        };

        customizationGroup.Items.Add(new RibbonButton
        {
            Header = "Save layout",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(async () =>
            {
                await RibbonCustomizationService.SaveAsync(RibbonCustomizationService.Capture(MainRibbon));
                customizationLog.Text = "Ribbon layout saved to ApplicationData LocalSettings.";
            }),
        });
        customizationGroup.Items.Add(new RibbonButton
        {
            Header = "Load layout",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(async () =>
            {
                var layout = await RibbonCustomizationService.LoadAsync();
                if (layout is null)
                {
                    customizationLog.Text = "No saved ribbon layout found.";
                    return;
                }

                RibbonCustomizationService.Apply(MainRibbon, layout);
                customizationLog.Text = "Ribbon layout loaded and applied.";
            }),
        });
        customizationGroup.Items.Add(new RibbonButton
        {
            Header = "Preview JSON",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(() => PreviewCustomizationLayout(customizationLog)),
        });
        customizationGroup.Items.Add(customizationLog);

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

        var infoBarHost = new RibbonInfoBarHost
        {
            Width = 260,
            IsClosable = true,
        };

        onboardingGroup.Items.Add(coachTarget);
        onboardingGroup.Items.Add(new RibbonButton
        {
            Header = "Show coach mark",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(() => RibbonCoachMark.Show(
                coachTarget,
                "Tip",
                "This button is a modern extension.")),
        });
        onboardingGroup.Items.Add(new RibbonButton
        {
            Header = "Notify",
            Size = RibbonControlSize.Large,
            Command = new ModernShowcaseCommand(() => infoBarHost.Show(
                InfoBarSeverity.Success,
                "Saved",
                "Your document was saved.")),
        });
        onboardingGroup.Items.Add(infoBarHost);

        tab.Groups.Add(onboardingGroup);
        MainRibbon.Tabs.Add(tab);
    }

    private void PreviewCustomizationLayout(TextBlock log)
    {
        var layout = RibbonCustomizationService.Capture(MainRibbon);
        var modified = RibbonCustomizationService.Deserialize(RibbonCustomizationService.Serialize(layout));
        var firstTab = modified?.Tabs.OrderBy(item => item.Order).FirstOrDefault();
        if (modified is null || firstTab is null)
        {
            log.Text = "No ribbon tabs available to customize.";
            return;
        }

        firstTab.IsVisible = false;
        firstTab.Order = modified.Tabs.Count + 10;
        RibbonCustomizationService.Apply(MainRibbon, modified);
        log.Text = RibbonCustomizationService.Serialize(modified);
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

    private static RibbonButton CreateBackdropButton(string header, RibbonBackdropKind kind, TextBlock status)
    {
        return new RibbonButton
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
        };
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

    private static RibbonButton CreateAcceleratorButton(string header, string gesture, string description, TextBlock log)
    {
        var button = new RibbonButton
        {
            Header = header,
            Size = RibbonControlSize.Large,
            ScreenTipTitle = header,
            ScreenTipText = description,
            Command = new ModernShowcaseCommand(() =>
            {
                log.Text = $"{gesture} pressed";
            }),
        };

        RibbonAccelerator.SetGesture(button, gesture);
        return button;
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

    private static ImageIconSource CreateModernSvgIconSource()
    {
        return new ImageIconSource
        {
            ImageSource = new SvgImageSource(new Uri("ms-appx:///Fluent.Ribbon.Uno.Showcase/Assets/Modern/star.svg")),
        };
    }

    private static Geometry CreateModernStarGeometry()
    {
        var figure = new PathFigure
        {
            StartPoint = new Point(16, 3),
            IsClosed = true,
            IsFilled = true,
        };

        foreach (var point in new[]
        {
            new Point(20, 11),
            new Point(29, 12),
            new Point(22.5, 18.5),
            new Point(24, 28),
            new Point(16, 23.5),
            new Point(8, 28),
            new Point(9.5, 18.5),
            new Point(3, 12),
            new Point(12, 11),
        })
        {
            figure.Segments.Add(new LineSegment { Point = point });
        }

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }

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
