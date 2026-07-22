namespace Fluent;

using Fluent.Internal;

/// <summary>
/// A panel that arranges ribbon tab headers horizontally, dynamically adjusting
/// tab header padding to fit within the available width.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WinUI does not have IScrollInfo; horizontal scrolling is handled by a parent ScrollViewer.
/// </remarks>
public partial class RibbonTabsContainer : Panel, IScrollInfo
{
    private const double MinimumLeftRightHeaderPadding = 5;
    private const double DefaultLeftRightHeaderPadding = 9;

    #region Layout

    /// <inheritdoc/>
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        if (Children.Count == 0)
        {
            return new Windows.Foundation.Size(0, 0);
        }

        var desiredSize = MeasureChildrenDesiredSize(availableSize);

        var overflowWidth = desiredSize.Width - availableSize.Width;
        var reduce = overflowWidth > 0;
        var stepSize = reduce ? -1.0 : 1.0;
        var absoluteOverflowWidth = Math.Abs(overflowWidth);

        var visibleTabs = Children
            .OfType<RibbonTab>()
            .Where(x => x.Visibility != Visibility.Collapsed && !x.IsContextual)
            .OrderByDescending(x => x.HeaderPadding.Right)
            .ToList();

        var sizeChanges = 0;

        if (reduce || DoubleUtil.GreaterThan(absoluteOverflowWidth, 2.0))
        {
            while (DoubleUtil.GreaterThan(absoluteOverflowWidth, 0))
            {
                var anyTabChanged = false;

                foreach (var tab in visibleTabs)
                {
                    if (reduce)
                    {
                        if (!DoubleUtil.GreaterThan(tab.HeaderPadding.Right, MinimumLeftRightHeaderPadding))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (DoubleUtil.AreClose(tab.HeaderPadding.Right, DefaultLeftRightHeaderPadding)
                            || DoubleUtil.GreaterThan(tab.HeaderPadding.Right, DefaultLeftRightHeaderPadding))
                        {
                            continue;
                        }
                    }

                    var newPadding = tab.HeaderPadding;

                    if ((reduce && DoubleUtil.GreaterThan(newPadding.Right, newPadding.Left))
                        || DoubleUtil.GreaterThan(newPadding.Left, newPadding.Right))
                    {
                        newPadding.Right += stepSize;
                    }
                    else
                    {
                        newPadding.Left += stepSize;
                    }

                    tab.HeaderPadding = newPadding;
                    sizeChanges++;
                    anyTabChanged = true;

                    absoluteOverflowWidth -= Math.Abs(stepSize);

                    if (!DoubleUtil.GreaterThan(absoluteOverflowWidth, 0))
                    {
                        break;
                    }
                }

                if (!anyTabChanged)
                {
                    break;
                }
            }
        }

        if (sizeChanges != 0)
        {
            desiredSize = MeasureChildrenDesiredSize(new Windows.Foundation.Size(
                desiredSize.Width + (sizeChanges * stepSize),
                desiredSize.Height));
        }

        // Gradually make separators visible between tabs when compressed
        var separatorOpacity = 0.0;
        if (visibleTabs.Any())
        {
            var averageHeaderPadding = visibleTabs.Average(x => x.HeaderPadding.Left + x.HeaderPadding.Right);
            var paddingDiff = averageHeaderPadding - (MinimumLeftRightHeaderPadding * 2);
            if (!DoubleUtil.GreaterThan(paddingDiff, 7))
            {
                separatorOpacity = 1.0 - (paddingDiff / 8);
            }
        }

        UpdateSeparators(separatorOpacity);

        return desiredSize;
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        double x = 0;

        // Arrange non-contextual tabs first, then contextual tabs
        var orderedChildren = Children
            .OfType<RibbonTab>()
            .OrderBy(x => x.IsContextual);

        foreach (var tab in orderedChildren)
        {
            var width = tab.DesiredSize.Width;
            var height = Math.Max(finalSize.Height, tab.DesiredSize.Height);
            tab.Arrange(new Windows.Foundation.Rect(x, 0, width, height));
            x += width;
        }

        return finalSize;
    }

    #endregion

    #region Helpers

    private Windows.Foundation.Size MeasureChildrenDesiredSize(Windows.Foundation.Size availableSize)
    {
        double width = 0;
        double height = 0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Windows.Foundation.Size(width, height);
    }

    private void UpdateSeparators(double opacity)
    {
        foreach (var tab in Children.OfType<RibbonTab>())
        {
            tab.SeparatorOpacity = opacity;
        }
    }

    #endregion
}
