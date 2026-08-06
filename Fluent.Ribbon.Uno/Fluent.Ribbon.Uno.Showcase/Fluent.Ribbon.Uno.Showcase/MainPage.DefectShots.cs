namespace FluentRibbon.Uno.Showcase;

using System;
using System.IO;
using System.Threading.Tasks;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

/// <summary>
/// Env-gated visual capture harness for the reported title-bar / panel-clip defects. Set
/// <c>SHOWCASE_DEFECT_SHOTS=&lt;dir&gt;</c> to drive the ribbon into each defect state, save a PNG
/// crop, and log measured coordinates. No-op unless the variable is set.
/// </summary>
public sealed partial class MainPage
{
    private async Task CaptureDefectShotsAsync()
    {
        var dir = Environment.GetEnvironmentVariable("SHOWCASE_DEFECT_SHOTS");
        if (string.IsNullOrEmpty(dir))
        {
            return;
        }

        AutoLog("DEFECTSHOTS BEGIN");
        try
        {
            Directory.CreateDirectory(dir);

            // ---- Defect A: contextual "Table Tools" header ----
            MainRibbon.SelectedTabIndex = 0;
            await SettleAsync(6, 150);
            await CaptureElementPngAsync(MainRibbon, Path.Combine(dir, "A0-before.png"));

            TableToolsGroup.Visibility = Visibility.Visible;
            await SettleAsync(6, 150);

            RibbonTabItem? design = null;
            foreach (var tab in MainRibbon.Tabs)
            {
                if (string.Equals(tab.Header?.ToString(), "Design", StringComparison.Ordinal))
                {
                    design = tab;
                    break;
                }
            }

            if (design is not null)
            {
                MainRibbon.SelectedTab = design;
                await SettleAsync(6, 150);
            }

            await CaptureElementPngAsync(MainRibbon, Path.Combine(dir, "A1-tabletools-on.png"));
            MeasureContextual(design);

            TableToolsGroup.Visibility = Visibility.Collapsed;
            await SettleAsync(6, 150);
            await CaptureElementPngAsync(MainRibbon, Path.Combine(dir, "A2-tabletools-off.png"));

            // ---- Defect B: "Ribbon controls in panel" tab ----
            var panelIndex = IndexOfTab("Ribbon controls in panel");
            if (panelIndex >= 0)
            {
                MainRibbon.SelectedTabIndex = panelIndex;
                await SettleAsync(6, 150);
                await CaptureElementPngAsync(MainRibbon, Path.Combine(dir, "B-panel.png"));
                MeasurePanelGroups();
            }
            else
            {
                AutoLog("  DEFECTSHOTS panel tab not found");
            }

            // ---- Defect C: disabled Backstage item contrast on the dark surface ----
            var originalTheme = RequestedTheme;
            ApplyShowcaseTheme(ElementTheme.Dark);
            BackstageView.IsOpen = true;
            await SettleAsync(4, 120);
            await CaptureElementPngAsync(BackstageView, Path.Combine(dir, "C-backstage-dark.png"));
            MeasureDisabledBackstageItem();
            AutoLog(SaveAsSeparator.Visibility == Visibility.Collapsed
                ? "  FILEMENU Save As separator collapsed"
                : $"  FAIL FILEMENU Save As separator visibility={SaveAsSeparator.Visibility}");
            BackstageView.IsOpen = false;
            ApplyShowcaseTheme(originalTheme);

            MainRibbon.SelectedTabIndex = 0;
            await SettleAsync(4, 120);
        }
        catch (Exception ex)
        {
            AutoLog($"  DEFECTSHOTS THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("DEFECTSHOTS END");
    }

    private void MeasureContextual(RibbonTabItem? designTab)
    {
        AutoLog($"  MainRibbon size={MainRibbon.ActualWidth:F1}x{MainRibbon.ActualHeight:F1}");
        var toolbarHost = FindByName(MainRibbon, "PART_ToolBarItemsHost");
        var toolbarLeft = toolbarHost?.TransformToVisual(MainRibbon)
            .TransformPoint(new Point()).X;

        foreach (var container in EnumerateDescendants<RibbonContextualGroupsContainer>(MainRibbon))
        {
            var cpos = container.TransformToVisual(MainRibbon).TransformPoint(new Point(0, 0));
            AutoLog(
                $"  ctxContainer at ({cpos.X:F1},{cpos.Y:F1}) size={container.ActualWidth:F1}x{container.ActualHeight:F1} children={container.Children.Count}");

            foreach (var child in container.Children)
            {
                if (child is RibbonContextualTabGroup group)
                {
                    var gpos = group.TransformToVisual(MainRibbon).TransformPoint(new Point(0, 0));
                    AutoLog(
                        $"    group '{group.Header}' at ({gpos.X:F1},{gpos.Y:F1}) size={group.ActualWidth:F1}x{group.ActualHeight:F1} innerVis={group.InnerVisibility} vis={group.Visibility}");
                    if (toolbarLeft is { } left && group.InnerVisibility == Visibility.Visible)
                    {
                        var groupRight = gpos.X + group.ActualWidth;
                        AutoLog(groupRight <= left + 0.5
                            ? $"    CTXHEADER clear of toolbar (right={groupRight:F1}, toolbarLeft={left:F1})"
                            : $"    FAIL CTXHEADER overlaps toolbar (right={groupRight:F1}, toolbarLeft={left:F1})");
                    }
                }
            }
        }

        if (designTab is not null)
        {
            var tpos = designTab.TransformToVisual(MainRibbon).TransformPoint(new Point(0, 0));
            AutoLog(
                $"  designTab at ({tpos.X:F1},{tpos.Y:F1}) size={designTab.ActualWidth:F1}x{designTab.ActualHeight:F1} vis={designTab.Visibility}");
        }
    }

    private void MeasureDisabledBackstageItem()
    {
        var disabledItem = EnumerateDescendants<BackstageTabItem>(BackstageView)
            .FirstOrDefault(item => string.Equals(item.Header?.ToString(), "Disabled", StringComparison.Ordinal));
        var header = disabledItem is null
            ? null
            : FindByName(disabledItem, "PART_Header") as ContentPresenter;
        var color = (header?.Foreground as Microsoft.UI.Xaml.Media.SolidColorBrush)?.Color;
        var colorText = color is { } value
            ? $"#{value.R:X2}{value.G:X2}{value.B:X2}"
            : "<none>";

        AutoLog(colorText == "#C7DDF0"
            ? $"  BACKSTAGE-DISABLED foreground={colorText}"
            : $"  FAIL BACKSTAGE-DISABLED expected #C7DDF0, actual {colorText}");
    }

    private void MeasurePanelGroups()
    {
        foreach (var name in new[] { "PanelGroup11", "PanelGroup12", "PanelGroup13" })
        {
            var group = FindByName(MainRibbon, name) as RibbonGroupBox;
            if (group is null)
            {
                AutoLog($"  {name}: not found");
                continue;
            }

            var gpos = group.TransformToVisual(MainRibbon).TransformPoint(new Point(0, 0));
            AutoLog(
                $"  {name} '{group.Header}' at ({gpos.X:F1},{gpos.Y:F1}) size={group.ActualWidth:F1}x{group.ActualHeight:F1}");

            var index = 0;
            foreach (var button in EnumerateDescendants<RibbonButton>(group))
            {
                var bpos = button.TransformToVisual(MainRibbon).TransformPoint(new Point(0, 0));
                var clipped = button.DesiredSize.Height > button.ActualHeight + 0.5
                              || bpos.Y + button.ActualHeight > gpos.Y + group.ActualHeight + 0.5;

                var contentPanel = FindByName(button, "ContentPanel") as StackPanel;
                var glyph = FindByName(button, "PART_GlyphIcon") as Microsoft.UI.Xaml.Controls.FontIcon;
                var label = FindByName(button, "PART_Label") as Microsoft.UI.Xaml.Controls.TextBlock;
                var orient = contentPanel?.Orientation.ToString() ?? "?";
                var glyphFont = glyph is null ? "?" : glyph.FontSize.ToString("F0");
                var labelVis = label?.Visibility.ToString() ?? "?";

                AutoLog(
                    $"    [{index}] '{button.Header}' size={button.Size} orient={orient} glyphFont={glyphFont} labelVis={labelVis} actual={button.ActualWidth:F1}x{button.ActualHeight:F1} desired={button.DesiredSize.Width:F1}x{button.DesiredSize.Height:F1}{(clipped ? " CLIPPED" : string.Empty)}");
                index++;
            }
        }
    }
}
