namespace FluentRibbon.Uno.Showcase;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

/// <summary>
/// Regression coverage for the ribbon input-header alignment fix. WPF aligns the header/label of
/// every Spinner/TextBox/ComboBox in a group via <c>Grid.IsSharedSizeScope</c>; WinUI/Uno has no
/// such feature, so <see cref="RibbonGroupBox"/> sizes every input header to the widest one. This
/// test verifies the resulting header right-edges (hence the input start positions) line up in the
/// "Spinners" group (Toolbars tab) and the dedicated "SharedSizeScope" group (Tests tab).
/// </summary>
public sealed partial class MainPage
{
    private async Task VerifySpinnerHeaderAlignmentAsync()
    {
        AutoLog("SPINALIGN BEGIN");
        try
        {
            MainRibbon.IsSimplified = false;

            MainRibbon.SelectedTabIndex = 0; // Toolbars
            await SettleAsync(4, 120);
            MeasureGroupInputAlignment("Spinners", SpinnersGroup);
            VerifySpinnerInputHeights("Spinners", SpinnersGroup);

            var testsIndex = IndexOfTab("Tests");
            if (testsIndex >= 0)
            {
                MainRibbon.SelectedTabIndex = testsIndex;
                await SettleAsync(4, 120);

                var shared = FindGroupByHeader("SharedSizeScope");
                if (shared is not null)
                {
                    MeasureGroupInputAlignment("SharedSizeScope", shared);
                }
                else
                {
                    AutoLog("  FAIL SPINALIGN SharedSizeScope group not found");
                }
            }
            else
            {
                AutoLog("  FAIL SPINALIGN Tests tab not found");
            }

            await VerifySpinnerStackLayoutAsync();
        }
        catch (Exception ex)
        {
            AutoLog($"  SPINALIGN THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("SPINALIGN END");
    }

    private async Task VerifySpinnerStackLayoutAsync()
    {
        if (Content is not Panel root)
        {
            AutoLog("  FAIL SPINLAYOUT Showcase content is not a Panel");
            return;
        }

        var panel = new RibbonGroupItemsPanel
        {
            Width = 240,
            Height = 72,
            Opacity = 0.01,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var spinners = new[]
        {
            new RibbonSpinner { Header = "Left:", InputWidth = 75 },
            new RibbonSpinner { Header = "Right:", InputWidth = 75 },
            new RibbonSpinner { Header = "Top:", InputWidth = 75 },
        };
        foreach (var spinner in spinners)
        {
            panel.Children.Add(spinner);
        }

        Grid.SetRowSpan(panel, 3);
        Canvas.SetZIndex(panel, 40);
        root.Children.Add(panel);

        try
        {
            await SettleAsync(2, 75);
            foreach (var spinner in spinners)
            {
                spinner.ApplyTemplate();
            }

            await SettleAsync(2, 75);
            var positions = spinners
                .Select(
                    spinner => spinner.TransformToVisual(panel)
                        .TransformPoint(new Windows.Foundation.Point()))
                .ToArray();
            var sameColumn = positions.Max(point => point.X) - positions.Min(point => point.X) <= 1;
            var verticallyOrdered = positions[0].Y < positions[1].Y
                                    && positions[1].Y < positions[2].Y
                                    && positions[0].Y + spinners[0].ActualHeight <= positions[1].Y + 0.5
                                    && positions[1].Y + spinners[1].ActualHeight <= positions[2].Y + 0.5;

            AutoLog(sameColumn && verticallyOrdered
                ? $"  SPINLAYOUT vertical ({string.Join(",", positions.Select(point => $"({point.X:F1},{point.Y:F1})"))})"
                : $"  FAIL SPINLAYOUT expected one vertical column, actual ({string.Join(",", positions.Select(point => $"({point.X:F1},{point.Y:F1})"))})");
        }
        finally
        {
            root.Children.Remove(panel);
        }
    }

    private void VerifySpinnerInputHeights(string label, RibbonGroupBox group)
    {
        var heights = new List<double>();
        foreach (var spinner in group.Items.OfType<RibbonSpinner>())
        {
            if (FindByName(spinner, "InputRoot") is FrameworkElement inputRoot)
            {
                heights.Add(inputRoot.ActualHeight);
            }
        }

        const double expectedHeight = 24;
        var valid = heights.Count > 0
                    && heights.All(height => Math.Abs(height - expectedHeight) <= 1.5);
        AutoLog(valid
            ? $"  SPINHEIGHT {label} compact ({string.Join(",", heights.Select(height => height.ToString("F1")))})"
            : $"  FAIL SPINHEIGHT {label} expected {expectedHeight:F1}, actual ({string.Join(",", heights.Select(height => height.ToString("F1")))})");
    }

    private void MeasureGroupInputAlignment(string label, RibbonGroupBox group)
    {
        // The user-visible symptom is that the input boxes (number boxes / text boxes / combo
        // boxes) of a group start at different x. RibbonGroupBox emulates WPF's shared header
        // column by imposing the widest natural header width on every input header, which pushes
        // every input to the same start x. We therefore assert on the input's start offset within
        // its own control (inputX - controlX) — that offset equals the shared header-column width
        // and is the exact quantity the fix aligns. (We deliberately do NOT measure the header
        // TextBlock's ActualWidth: a TextBlock left-aligns its text inside the imposed slot, so its
        // ActualWidth reports the text extent, not the slot — see imposedW/desW in the log below.)
        var inputOffsets = new List<double>();
        var builder = new StringBuilder();
        builder.Append($"  {label}:");

        foreach (var item in group.Items)
        {
            if (item is not FrameworkElement fe)
            {
                continue;
            }

            var header = FindByName(fe, "HeaderText");
            if (header is null || header.Visibility != Visibility.Visible)
            {
                continue;
            }

            // The input area (Spinner/TextBox "PART_TextBox", ComboBox "InputRoot") begins right
            // after the header slot within each control.
            var input = FindByName(fe, "PART_TextBox") ?? FindByName(fe, "InputRoot");
            if (input is null)
            {
                continue;
            }

            var ctrlX = fe.TransformToVisual(MainRibbon)
                .TransformPoint(new Windows.Foundation.Point(0, 0)).X;
            var inputX = input.TransformToVisual(MainRibbon)
                .TransformPoint(new Windows.Foundation.Point(0, 0)).X;
            var inputOffset = inputX - ctrlX;
            inputOffsets.Add(inputOffset);

            var headerText = (header as TextBlock)?.Text ?? "?";
            builder.Append(
                $" [{headerText}: hdrTextW={header.ActualWidth:F1} imposedW={header.Width:F1} desW={header.DesiredSize.Width:F1} ctrlX={ctrlX:F1} inputX={inputX:F1} inputOffset={inputOffset:F1}]");
        }

        AutoLog(builder.ToString());

        if (inputOffsets.Count >= 2)
        {
            var min = inputOffsets[0];
            var max = inputOffsets[0];
            foreach (var value in inputOffsets)
            {
                min = Math.Min(min, value);
                max = Math.Max(max, value);
            }

            var spread = max - min;
            AutoLog(spread <= 1.5
                ? $"  SPINALIGN {label} aligned (inputStart spread={spread:F2}px, n={inputOffsets.Count})"
                : $"  FAIL SPINALIGN {label} inputs not aligned (inputStart spread={spread:F2}px, n={inputOffsets.Count})");
        }
        else
        {
            AutoLog($"  SPINALIGN {label} skipped (n={inputOffsets.Count})");
        }
    }

    private int IndexOfTab(string header)
    {
        for (var i = 0; i < MainRibbon.Tabs.Count; i++)
        {
            if (string.Equals(MainRibbon.Tabs[i].Header?.ToString(), header, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private RibbonGroupBox? FindGroupByHeader(string header)
    {
        foreach (var group in EnumerateDescendants<RibbonGroupBox>(MainRibbon))
        {
            if (string.Equals(group.Header?.ToString(), header, StringComparison.Ordinal))
            {
                return group;
            }
        }

        return null;
    }

    private static IEnumerable<T> EnumerateDescendants<T>(DependencyObject root)
        where T : class
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var deeper in EnumerateDescendants<T>(child))
            {
                yield return deeper;
            }
        }
    }

    private static FrameworkElement? FindByName(DependencyObject root, string name)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is FrameworkElement fe && string.Equals(fe.Name, name, StringComparison.Ordinal))
            {
                return fe;
            }

            if (FindByName(child, name) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
