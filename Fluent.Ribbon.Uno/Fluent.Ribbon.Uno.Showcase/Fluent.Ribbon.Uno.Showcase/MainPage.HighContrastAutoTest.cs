using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fluent;
using Fluent.Modern.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace FluentRibbon.Uno.Showcase;

public sealed partial class MainPage
{
    private static readonly (string Background, string Foreground)[] HighContrastResourcePairs =
    {
        ("RibbonAccentBrush", "RibbonAccentForegroundBrush"),
        ("RibbonHoverBrush", "RibbonHoverForegroundBrush"),
        ("RibbonPressedBrush", "RibbonPressedForegroundBrush"),
        ("RibbonCheckedBrush", "RibbonCheckedForegroundBrush"),
        ("RibbonSelectedBrush", "RibbonSelectedForegroundBrush"),
        ("RibbonBackstageBackgroundBrush", "RibbonBackstageForegroundBrush"),
        ("RibbonBackstageBackgroundHoverBrush", "RibbonBackstageForegroundHoverBrush"),
        ("RibbonBackstageBackgroundPressedBrush", "RibbonBackstageForegroundPressedBrush"),
        ("RibbonBackstageBackgroundSelectedBrush", "RibbonBackstageForegroundSelectedBrush"),
        ("RibbonStatusBarBrush", "RibbonStatusBarTextBrush"),
    };

    private async Task RunHighContrastOnlyAutoTestAsync()
    {
        autoTestFailed = false;
        AutoLog("HIGH-CONTRAST START");
        try
        {
            await VerifyHighContrastResourcesAndStatesAsync();
            AutoLog("HIGH-CONTRAST COMPLETE");
        }
        catch (Exception ex)
        {
            AutoLog($"HIGH-CONTRAST FATAL {ex.GetType().Name}: {ex.Message}");
        }

        await FinishAutoTestAsync();
    }

    private async Task VerifyHighContrastResourcesAndStatesAsync()
    {
        AutoLog("HIGH-CONTRAST-RESOURCES BEGIN");
        var common = new ResourceDictionary
        {
            Source = new Uri("ms-appx:///Fluent.Ribbon.Uno/Themes/Common.xaml"),
        };
        var highContrast = common.ThemeDictionaries["HighContrast"] as ResourceDictionary;
        Require(highContrast is not null, "Common.xaml did not expose its HighContrast dictionary");

        foreach (var (background, foreground) in HighContrastResourcePairs)
        {
            Require(
                highContrast!.TryGetValue(background, out var backgroundValue)
                && backgroundValue is SolidColorBrush,
                $"HighContrast resource '{background}' did not resolve to a brush");
            Require(
                highContrast.TryGetValue(foreground, out var foregroundValue)
                && foregroundValue is SolidColorBrush,
                $"HighContrast resource '{foreground}' did not resolve to a brush");
        }

        Require(
            highContrast!.TryGetValue("RibbonDisabledForegroundBrush", out var disabledValue)
            && disabledValue is SolidColorBrush,
            "HighContrast disabled foreground did not resolve to a brush");

        if (Content is not Panel root)
        {
            throw new InvalidOperationException("Showcase content is not a Panel.");
        }

        var host = new StackPanel
        {
            Opacity = 0,
            IsHitTestVisible = false,
        };
        root.Children.Add(host);
        try
        {
            await VerifyStatePairAsync(
                host,
                new RibbonButton { Header = "Button" },
                "PointerOver",
                "RootBorder",
                "PART_Label",
                "RibbonHoverBrush",
                "RibbonHoverForegroundBrush");
            await VerifyStatePairAsync(
                host,
                new ModernRibbonButton { Header = "Modern" },
                "Pressed",
                "RootBorder",
                "PART_Label",
                "RibbonPressedBrush",
                "RibbonPressedForegroundBrush");
            await VerifyStatePairAsync(
                host,
                new RibbonToggleButton { Header = "Toggle" },
                "Checked",
                "RootBorder",
                "PART_Label",
                "RibbonCheckedBrush",
                "RibbonCheckedForegroundBrush");
            await VerifyStatePairAsync(
                host,
                new RibbonGalleryItem { Content = "Gallery item" },
                "Selected",
                "RootBorder",
                "ContentPresenter",
                "RibbonSelectedBrush",
                "RibbonSelectedForegroundBrush");
            await VerifyStatePairAsync(
                host,
                new BackstageButton { Header = "Backstage" },
                "PointerOver",
                "RootBorder",
                "HeaderPresenter",
                "RibbonBackstageBackgroundHoverBrush",
                "RibbonBackstageForegroundHoverBrush");
            await VerifyStatePairAsync(
                host,
                new ApplicationMenu { Header = "File" },
                "Pressed",
                "RootBorder",
                "ApplicationMenuContent",
                "RibbonBackstageBackgroundPressedBrush",
                "RibbonBackstageForegroundPressedBrush",
                "PART_Button");
        }
        finally
        {
            root.Children.Remove(host);
        }

        AutoLog("HIGH-CONTRAST-RESOURCES PASS");
    }

    private async Task VerifyStatePairAsync(
        Panel host,
        Control control,
        string state,
        string backgroundPartName,
        string foregroundPartName,
        string backgroundKey,
        string foregroundKey,
        string? stateOwnerPartName = null)
    {
        host.Children.Add(control);
        try
        {
            control.ApplyTemplate();
            await SettleAsync(1, 25);
            var stateOwner = stateOwnerPartName is null
                ? control
                : FindDescendantByName(control, stateOwnerPartName) as Control;
            if (stateOwner is null)
            {
                throw new InvalidOperationException(
                    $"{control.GetType().Name} did not realize {stateOwnerPartName}");
            }

            Require(
                VisualStateManager.GoToState(stateOwner, state, false),
                $"{control.GetType().Name} did not expose state '{state}'");
            await SettleAsync(1, 25);

            var backgroundPart = FindDescendantByName(control, backgroundPartName) as Border;
            var foregroundPart = FindDescendantByName(control, foregroundPartName);
            if (backgroundPart is null)
            {
                throw new InvalidOperationException(
                    $"{control.GetType().Name} did not realize {backgroundPartName}");
            }

            if (foregroundPart is null)
            {
                throw new InvalidOperationException(
                    $"{control.GetType().Name} did not realize {foregroundPartName}");
            }

            if (!TryFindResource(Application.Current.Resources, backgroundKey, out var backgroundValue)
                || backgroundValue is not SolidColorBrush expectedBackground)
            {
                throw new InvalidOperationException($"{backgroundKey} did not resolve");
            }

            if (!TryFindResource(Application.Current.Resources, foregroundKey, out var foregroundValue)
                || foregroundValue is not SolidColorBrush expectedForeground)
            {
                throw new InvalidOperationException($"{foregroundKey} did not resolve");
            }

            Require(
                backgroundPart.Background is SolidColorBrush actualBackground
                && actualBackground.Color == expectedBackground.Color,
                $"{control.GetType().Name}.{state} did not apply {backgroundKey}: "
                + $"actual={((backgroundPart.Background as SolidColorBrush)?.Color.ToString() ?? "null")}, "
                + $"expected={expectedBackground.Color}");
            Require(
                GetForeground(foregroundPart) is SolidColorBrush actualForeground
                && actualForeground.Color == expectedForeground.Color,
                $"{control.GetType().Name}.{state} did not apply {foregroundKey}: "
                + $"actual={((GetForeground(foregroundPart) as SolidColorBrush)?.Color.ToString() ?? "null")}, "
                + $"expected={expectedForeground.Color}");
        }
        finally
        {
            host.Children.Remove(control);
        }
    }

    private static Brush? GetForeground(FrameworkElement element)
        => element switch
        {
            Control control => control.Foreground,
            ContentPresenter presenter => presenter.Foreground,
            TextBlock textBlock => textBlock.Foreground,
            FontIcon fontIcon => fontIcon.Foreground,
            _ => null,
        };
}
