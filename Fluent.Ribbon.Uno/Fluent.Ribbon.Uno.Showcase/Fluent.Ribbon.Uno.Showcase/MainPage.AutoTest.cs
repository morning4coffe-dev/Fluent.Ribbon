// Opt-in runtime diagnostics / auto-test harness for the Showcase.
//
// This file adds NO behavior to normal runs: everything below is gated on
// environment variables and is a no-op unless they are set.
//
//   SHOWCASE_TAB=<index>        Select a single ribbon tab at startup (handy for screenshots).
//   SHOWCASE_AUTOTEST=1         Walk every tab, open every dropdown/gallery, exercise
//                               Enlarge/Reduce + Simplified/Minimized toggles, logging each step.
//   SHOWCASE_AUTOTEST_LOG=path  Append the [AUTOTEST] log lines to this file (also written to stdout).
//   SHOWCASE_AUTOTEST_EXIT=1    Exit the process once the walk completes (so a runner can detect "done").
//
// The walker deliberately drives controls programmatically because synthetic
// mouse/keyboard input does not reach the Uno Skia window. Any layout crash that
// fires on a dispatcher tick is surfaced either as a "THREW" line here or as an
// Uno "NativeDispatcher unhandled exception" on stdout, which the runner greps for.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace FluentRibbon.Uno.Showcase;

public sealed partial class MainPage
{
    private static bool AutoTestEnabled =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHOWCASE_AUTOTEST"));

    private static void AutoLog(string message)
    {
        var line = $"[AUTOTEST] {DateTime.Now:HH:mm:ss.fff} {message}";
        Console.WriteLine(line);
        try
        {
            var path = Environment.GetEnvironmentVariable("SHOWCASE_AUTOTEST_LOG");
            if (!string.IsNullOrEmpty(path))
            {
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch
        {
            // logging must never throw
        }
    }

    // Called from the constructor. Wires opt-in diagnostics only when the relevant
    // environment variables are present; otherwise returns immediately.
    private void InitializeDiagnostics()
    {
        var tabVar = Environment.GetEnvironmentVariable("SHOWCASE_TAB");
        var autotest = AutoTestEnabled;
        if (string.IsNullOrEmpty(tabVar) && !autotest)
        {
            return;
        }

        this.Loaded += async (_, _) =>
        {
            if (int.TryParse(tabVar, out var idx) && idx >= 0 && idx < MainRibbon.Tabs.Count)
            {
                MainRibbon.SelectedTabIndex = idx;
            }

            if (autotest)
            {
                await RunAutoTestAsync();
            }
        };
    }

    private async Task SettleAsync(int cycles = 3, int delayMs = 150)
    {
        for (var i = 0; i < cycles; i++)
        {
            try
            {
                this.UpdateLayout();
            }
            catch (Exception ex)
            {
                AutoLog($"  THREW during UpdateLayout: {ex.GetType().Name}: {ex.Message}");
            }

            await Task.Delay(delayMs);
        }
    }

    private async Task RunAutoTestAsync()
    {
        AutoLog("START");
        try
        {
            var tabCount = MainRibbon.Tabs.Count;
            AutoLog($"Ribbon has {tabCount} tabs");

            for (var i = 0; i < tabCount; i++)
            {
                var header = MainRibbon.Tabs[i].Header?.ToString() ?? "?";
                AutoLog($"TAB {i} '{header}' BEGIN");
                MainRibbon.SelectedTabIndex = i;
                await SettleAsync();

                var targets = new List<Control>();
                CollectDropdownControls(this, targets);
                AutoLog($"TAB {i} '{header}' found {targets.Count} dropdown-bearing controls");

                foreach (var control in targets)
                {
                    await ExerciseControlAsync(control);
                }

                AutoLog($"TAB {i} '{header}' END");
            }

            await ExerciseTogglesAsync();

            // RibbonGallery is not instantiated anywhere in the Showcase XAML, so exercise it here to keep
            // regression coverage over the non-virtualizing UniformItemsPanel migration (measure + grouping
            // + filter rebuild + item mutation), which is where the ItemsRepeater ElementManager crash lived.
            await VerifyRibbonGalleryAsync();

            AutoLog("COMPLETE");
        }
        catch (Exception ex)
        {
            AutoLog($"FATAL {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHOWCASE_AUTOTEST_EXIT")))
        {
            await Task.Delay(500);
            AutoLog("EXIT");
            try
            {
                Application.Current.Exit();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static void CollectDropdownControls(DependencyObject root, List<Control> sink)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            switch (child)
            {
                case InRibbonGallery:
                case RibbonDropDownButton:
                case RibbonSplitButton:
                case RibbonComboBox:
                case ColorGallery:
                    sink.Add((Control)child);
                    break;
            }

            CollectDropdownControls(child, sink);
        }
    }

    private async Task ExerciseControlAsync(Control control)
    {
        var name = (control as FrameworkElement)?.Name ?? string.Empty;
        var kind = control.GetType().Name;
        AutoLog($"  OPEN {kind} '{name}'");
        try
        {
            switch (control)
            {
                case InRibbonGallery gallery:
                    // The popup (expandedRepeater) is the real crash suspect; open it the
                    // same way a click would, then hide it.
                    InvokePrivate(gallery, "ShowPopup");
                    await SettleAsync(3);
                    HidePrivateFlyout(gallery, "_popup");
                    await SettleAsync(1);
                    AutoLog($"  ENLARGE/REDUCE {kind} '{name}'");
                    gallery.Enlarge();
                    await SettleAsync(1);
                    gallery.Enlarge();
                    await SettleAsync(1);
                    gallery.Reduce();
                    await SettleAsync(1);
                    gallery.Reduce();
                    await SettleAsync(1);
                    break;

                case RibbonDropDownButton dropDown:
                    InvokePrivate(dropDown, "ShowDropDown");
                    await SettleAsync(3);
                    dropDown.CloseDropDown();
                    await SettleAsync(1);
                    break;

                case RibbonSplitButton split:
                    InvokePrivate(split, "ShowDropDown");
                    await SettleAsync(3);
                    split.CloseDropDown();
                    await SettleAsync(1);
                    break;

                case RibbonComboBox combo:
                    if (FindDescendant<ComboBox>(combo) is ComboBox inner)
                    {
                        inner.IsDropDownOpen = true;
                        await SettleAsync(2);
                        inner.IsDropDownOpen = false;
                        await SettleAsync(1);
                    }

                    break;

                case ColorGallery:
                    await SettleAsync(1);
                    break;
            }

            AutoLog($"  OK {kind} '{name}'");
        }
        catch (Exception ex)
        {
            AutoLog($"  THREW {kind} '{name}': {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static void InvokePrivate(object target, string methodName)
    {
        var method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        method?.Invoke(target, null);
    }

    private static void HidePrivateFlyout(object target, string fieldName)
    {
        var field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        var value = field?.GetValue(target);
        if (value is FlyoutBase flyout)
        {
            flyout.Hide();
        }
        else if (value is Popup popup)
        {
            popup.IsOpen = false;
        }
    }

    private async Task ExerciseTogglesAsync()
    {
        AutoLog("TOGGLES BEGIN");
        try
        {
            MainRibbon.SelectedTabIndex = 0;
            await SettleAsync();

            AutoLog("  Simplified ON");
            MainRibbon.IsSimplified = true;
            await SettleAsync();
            AutoLog("  Simplified OFF");
            MainRibbon.IsSimplified = false;
            await SettleAsync();

            AutoLog("  Minimized ON");
            MainRibbon.IsMinimized = true;
            await SettleAsync();
            AutoLog("  Minimized OFF");
            MainRibbon.IsMinimized = false;
            await SettleAsync();
        }
        catch (Exception ex)
        {
            AutoLog($"  TOGGLES THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("TOGGLES END");
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : class
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is T deeper)
            {
                return deeper;
            }
        }

        return null;
    }

    // Standalone runtime exercise of RibbonGallery (which the Showcase XAML never instantiates).
    private async Task VerifyRibbonGalleryAsync()
    {
        AutoLog("RGTEST BEGIN");
        Popup? popup = null;
        try
        {
            var flat = new RibbonGallery { ItemWidth = 32, ItemHeight = 32, Width = 240, Height = 120 };
            for (var i = 0; i < 10; i++)
            {
                flat.Items.Add(MakeRgItem(i, null));
            }

            var grouped = new RibbonGallery
            {
                ItemWidth = 32,
                ItemHeight = 32,
                Width = 240,
                Height = 160,
                IsGrouped = true,
            };
            var groupNames = new[] { "Alpha", "Beta", "Gamma" };
            for (var i = 0; i < 15; i++)
            {
                grouped.Items.Add(MakeRgItem(i, groupNames[i % groupNames.Length]));
            }

            var filterAB = new GalleryGroupFilter { Title = "A+B", Groups = "Alpha,Beta" };
            var filterG = new GalleryGroupFilter { Title = "G", Groups = "Gamma" };
            grouped.Filters.Add(filterAB);
            grouped.Filters.Add(filterG);

            var host = new StackPanel { Spacing = 8 };
            host.Children.Add(flat);
            host.Children.Add(grouped);

            popup = new Popup { Child = host };
            if (this.Content is Panel rootPanel)
            {
                rootPanel.Children.Add(popup);
            }

            popup.IsOpen = true;
            await SettleAsync(4, 150);

            AutoLog("  RGTEST apply filter A+B");
            grouped.SelectedFilter = filterAB;
            await SettleAsync(3, 150);

            AutoLog("  RGTEST apply filter G");
            grouped.SelectedFilter = filterG;
            await SettleAsync(3, 150);

            AutoLog("  RGTEST clear filter");
            grouped.SelectedFilter = null;
            await SettleAsync(3, 150);

            AutoLog("  RGTEST mutate items");
            grouped.Items.Add(MakeRgItem(99, "Alpha"));
            grouped.Items.RemoveAt(0);
            await SettleAsync(3, 150);

            AutoLog("  RGTEST OK");
        }
        catch (Exception ex)
        {
            AutoLog($"  RGTEST THREW {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            try
            {
                if (popup is not null)
                {
                    popup.IsOpen = false;
                    if (this.Content is Panel rootPanel)
                    {
                        rootPanel.Children.Remove(popup);
                    }
                }
            }
            catch
            {
                // ignore teardown errors
            }
        }

        AutoLog("RGTEST END");
    }

    private static RibbonGalleryItem MakeRgItem(int i, string? group)
    {
        var hue = (i * 36) % 360;
        var color = HsvToColor(hue);
        return new RibbonGalleryItem
        {
            Group = group ?? string.Empty,
            Content = new Border
            {
                Width = 28,
                Height = 28,
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(3),
            },
        };
    }

    private static Windows.UI.Color HsvToColor(double hue)
    {
        var h = hue / 60.0;
        var x = 1 - Math.Abs((h % 2) - 1);
        double r = 0, g = 0, b = 0;
        switch ((int)h)
        {
            case 0: r = 1; g = x; break;
            case 1: r = x; g = 1; break;
            case 2: g = 1; b = x; break;
            case 3: g = x; b = 1; break;
            case 4: r = x; b = 1; break;
            default: r = 1; b = x; break;
        }

        return Windows.UI.Color.FromArgb(255, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }
}
