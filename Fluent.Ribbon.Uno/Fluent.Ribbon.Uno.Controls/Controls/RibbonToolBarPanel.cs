namespace Fluent;

using Fluent.Internal;
using Windows.Foundation;

/// <summary>
/// The layout panel used by <see cref="RibbonToolBar"/>.
/// </summary>
/// <remarks>
/// Reproduces the WPF Fluent.Ribbon toolbar layout: every row is packed
/// independently (a wide control in one row never widens another row), rows are
/// grouped into side-by-side columns separated by vertical separators, and a wrap
/// layout is used when no layout definition matches. It is a pure layout engine:
/// <see cref="RibbonToolBar"/> realizes and sizes the children, then hands the row
/// structure to this panel through <see cref="ConfigureCustomLayout"/> or
/// <see cref="ConfigureWrapLayout"/>.
/// </remarks>
public partial class RibbonToolBarPanel : Panel
{
    private IReadOnlyList<IReadOnlyList<FrameworkElement>>? _rows;
    private IReadOnlyDictionary<int, FrameworkElement>? _separators;
    private int _rowCount;
    private double _measuredRowHeight;
    private bool _useSingleRow;
    private bool _centerCustomRowItems;

    /// <summary>
    /// Switches the panel to the wrap layout used when no layout definition matches.
    /// The panel then arranges its own children directly.
    /// </summary>
    internal void ConfigureWrapLayout(bool useSingleRow = false)
    {
        _rows = null;
        _separators = null;
        _rowCount = 0;
        _useSingleRow = useSingleRow;
        _centerCustomRowItems = false;
        InvalidateMeasure();
        InvalidateArrange();
    }

    /// <summary>
    /// Switches the panel to the row/column layout described by the given rows.
    /// </summary>
    /// <param name="rows">The realized rows; each element is already sized.</param>
    /// <param name="rowCount">The layout definition row count used for column wrapping.</param>
    /// <param name="separators">The vertical separators keyed by the row index that starts a new column.</param>
    internal void ConfigureCustomLayout(
        IReadOnlyList<IReadOnlyList<FrameworkElement>> rows,
        int rowCount,
        IReadOnlyDictionary<int, FrameworkElement> separators,
        bool centerRowItems = false)
    {
        _rows = rows;
        _rowCount = rowCount;
        _separators = separators;
        _useSingleRow = false;
        _centerCustomRowItems = centerRowItems;
        InvalidateMeasure();
        InvalidateArrange();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
        => _rows is null
            ? WrapPanelLayout(availableSize, measure: true)
            : CustomLayout(availableSize, measure: true);

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
        => _rows is null
            ? WrapPanelLayout(finalSize, measure: false)
            : CustomLayout(finalSize, measure: false);

    private Size WrapPanelLayout(Size availableSize, bool measure)
    {
        if (_useSingleRow)
        {
            return SingleRowLayout(availableSize, measure);
        }

        var arrange = !measure;
        var availableHeight = double.IsPositiveInfinity(availableSize.Height)
            ? 0
            : availableSize.Height;

        double currentHeight = 0;
        double columnWidth = 0;
        double resultWidth = 0;
        double resultHeight = 0;

        foreach (var child in Children)
        {
            if (measure)
            {
                child.Measure(SizeConstants.Infinite);
            }

            if (currentHeight + child.DesiredSize.Height > availableHeight)
            {
                resultHeight = Math.Max(resultHeight, currentHeight);
                resultWidth += columnWidth;
                currentHeight = 0;
                columnWidth = 0;
            }

            if (arrange)
            {
                child.Arrange(new Rect(resultWidth, currentHeight, child.DesiredSize.Width, child.DesiredSize.Height));
            }

            columnWidth = Math.Max(columnWidth, child.DesiredSize.Width);
            currentHeight += child.DesiredSize.Height;
            resultHeight = Math.Max(resultHeight, currentHeight);
        }

        if (arrange)
        {
            return availableSize;
        }

        return new Size(resultWidth + columnWidth, resultHeight);
    }

    private Size SingleRowLayout(Size availableSize, bool measure)
    {
        var arrange = !measure;
        var availableHeight = double.IsPositiveInfinity(availableSize.Height)
            ? 0
            : availableSize.Height;
        var desiredSizes = new List<Size>(Children.Count);
        var maxHeight = 0.0;

        foreach (var child in Children)
        {
            if (measure)
            {
                child.Measure(SizeConstants.Infinite);
            }

            desiredSizes.Add(child.DesiredSize);
            maxHeight = Math.Max(maxHeight, child.DesiredSize.Height);
        }

        var rects = BuildSingleRowRects(desiredSizes, availableHeight);
        if (arrange)
        {
            for (var index = 0; index < Children.Count; index++)
            {
                Children[index].Arrange(rects[index]);
            }
        }

        var width = rects.Count > 0
            ? rects[^1].Right
            : 0;
        return arrange
            ? availableSize
            : new Size(width, maxHeight);
    }

    internal static IReadOnlyList<Rect> BuildSingleRowRects(
        IReadOnlyList<Size> desiredSizes,
        double availableHeight)
    {
        var rects = new List<Rect>(desiredSizes.Count);
        var x = 0.0;

        foreach (var desiredSize in desiredSizes)
        {
            var childHeight = availableHeight > 0
                ? Math.Min(desiredSize.Height, availableHeight)
                : desiredSize.Height;
            var y = Math.Max(0, (availableHeight - childHeight) / 2);
            rects.Add(new Rect(x, y, desiredSize.Width, childHeight));
            x += desiredSize.Width;
        }

        return rects;
    }

    private Size CustomLayout(Size availableSize, bool measure)
    {
        var arrange = !measure;
        var rows = _rows;
        if (rows is null || rows.Count == 0)
        {
            return arrange ? availableSize : new Size(0, 0);
        }

        var availableHeight = double.IsPositiveInfinity(availableSize.Height)
            ? 0
            : availableSize.Height;

        // Row height is the natural height of the first control and is intrinsic to the
        // content, so it is computed once during measure and reused during arrange.
        if (measure)
        {
            _measuredRowHeight = GetRowHeight(rows);
        }

        var rowHeight = _measuredRowHeight;
        var rowCountInColumn = Math.Max(1, Math.Min(_rowCount, rows.Count));
        var whitespace = (availableHeight - (rowCountInColumn * rowHeight)) / (rowCountInColumn + 1);

        double y = 0;
        double currentRowBegin = 0;
        double currentMaxX = 0;
        double maxY = 0;

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var x = currentRowBegin;

            if (rowIndex % rowCountInColumn == 0)
            {
                // Start of a new column: reset the packing origin and drop in the separator.
                x = currentRowBegin = currentMaxX;
                y = 0;

                if (rowIndex != 0
                    && _separators is not null
                    && _separators.TryGetValue(rowIndex, out var separator))
                {
                    if (measure)
                    {
                        separator.Height = Math.Max(0, availableHeight - separator.Margin.Bottom - separator.Margin.Top);
                        separator.Measure(availableSize);
                    }

                    if (arrange)
                    {
                        separator.Arrange(new Rect(x, y, separator.DesiredSize.Width, separator.DesiredSize.Height));
                    }

                    x += separator.DesiredSize.Width;
                }
            }

            if (rowIndex > 0)
            {
                y += whitespace;
            }

            foreach (var child in row)
            {
                if (measure)
                {
                    // Measure with an unbounded width so each control reports its natural
                    // width. Controls that carry an explicit RibbonToolBarControlDefinition.Width
                    // still report that fixed width, while input-bearing controls (whose templates
                    // stretch their input to fill a star column) collapse to their InputWidth
                    // instead of stretching to fill the row.
                    child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
                }

                if (arrange)
                {
                    var childY = y + (_centerCustomRowItems
                        ? GetCenteredRowOffset(rowHeight, child.DesiredSize.Height)
                        : 0);
                    child.Arrange(
                        new Rect(
                            x,
                            childY,
                            child.DesiredSize.Width,
                            child.DesiredSize.Height));
                }

                x += child.DesiredSize.Width;
            }

            y += rowHeight;

            if (currentMaxX < x)
            {
                currentMaxX = x;
            }

            if (maxY < y)
            {
                maxY = y;
            }
        }

        if (arrange)
        {
            return availableSize;
        }

        return new Size(currentMaxX, Math.Max(maxY + whitespace, 0));
    }

    internal static double GetCenteredRowOffset(double rowHeight, double childHeight)
        => Math.Max(0, (rowHeight - childHeight) / 2);

    private static double GetRowHeight(IReadOnlyList<IReadOnlyList<FrameworkElement>> rows)
    {
        foreach (var row in rows)
        {
            foreach (var child in row)
            {
                child.Measure(SizeConstants.Infinite);
                return child.DesiredSize.Height;
            }
        }

        return 0;
    }
}
