namespace FluentRibbon.Uno.Showcase;

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

public sealed partial class MainPage
{
    private async Task VerifyCompactLayoutAsync()
    {
        AutoLog("COMPACT-LAYOUT BEGIN");
        try
        {
            MainRibbon.IsSimplified = true;
            MainRibbon.SelectedTabIndex = 0;
            await SettleAsync(4, 100);

            var comboCenter = CenterY(fontNameCombo, FontToolBar);
            var boldCenter = CenterY(btnBold, FontToolBar);
            var italicCenter = CenterY(btnItalic, FontToolBar);
            var underlineCenter = CenterY(btnUnderline, FontToolBar);
            AutoLog(
                $"  font centers combo={comboCenter:F1} bold={boldCenter:F1} "
                + $"italic={italicCenter:F1} underline={underlineCenter:F1}");
            Require(
                Math.Abs(comboCenter - boldCenter) <= 1
                && Math.Abs(comboCenter - italicCenter) <= 1
                && Math.Abs(comboCenter - underlineCenter) <= 1,
                "simplified font toolbar controls are not vertically centered");

            MainRibbon.SelectedTabIndex = 1;
            await SettleAsync(4, 100);

            var collapsedPanel = FindDescendantByName(
                InsertSplitGallery,
                "CollapsedButtonPanel") as StackPanel;
            var collapsedGlyph = FindDescendantByName(
                InsertSplitGallery,
                "CollapsedButtonGlyph") as FontIcon;
            AutoLog(
                $"  split simplified={InsertSplitGallery.IsSimplified} "
                + $"collapsed={InsertSplitGallery.IsCollapsed} "
                + $"orientation={collapsedPanel?.Orientation} "
                + $"glyph={collapsedGlyph?.FontSize:F1}");
            Require(InsertSplitGallery.IsSimplified, "insert gallery did not enter simplified mode");
            Require(InsertSplitGallery.IsCollapsed, "insert gallery did not enter collapsed presentation");
            Require(
                collapsedPanel?.Orientation == Orientation.Horizontal,
                "insert gallery did not use the horizontal simplified presentation");
            Require(
                collapsedGlyph is not null && collapsedGlyph.FontSize <= 16.5,
                "insert gallery simplified glyph remained oversized");
        }
        catch (Exception ex)
        {
            AutoLog($"  COMPACT-LAYOUT THREW {ex.GetType().Name}: {ex.Message}");
        }

        AutoLog("COMPACT-LAYOUT END");
    }

    private static double CenterY(FrameworkElement element, FrameworkElement relativeTo)
    {
        var top = element.TransformToVisual(relativeTo).TransformPoint(new Point()).Y;
        return top + (element.ActualHeight / 2);
    }
}
