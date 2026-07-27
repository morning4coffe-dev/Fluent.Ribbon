namespace Fluent;

using System.Collections.Generic;

/// <summary>
/// Arranges the items of a <see cref="RibbonGroupBox"/> the way Office and the original
/// WPF Fluent.Ribbon do: large controls occupy a full-height column of their own, while
/// consecutive small/medium controls stack vertically into columns of up to three rows.
/// Separators break the current column and draw a vertical divider.
/// </summary>
/// <remarks>
/// This replaces a plain horizontal <see cref="StackPanel"/> so that, for example, a large
/// Paste button sits next to a stacked Cut/Copy column, matching the classic ribbon look.
/// </remarks>
public partial class RibbonGroupItemsPanel : Panel
{
    private const int RowsPerColumn = 3;
    private const double FallbackHeight = 90;

    private List<Windows.Foundation.Rect> _arrangeRects = new();

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonGroupItemsPanel),
            new PropertyMetadata(false, OnIsSimplifiedChanged));

    /// <summary>Gets or sets whether items should use the single-row simplified layout.</summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    private enum ItemKind
    {
        Large,
        Stacked,
        Separator,
    }

    private static ItemKind Classify(UIElement element)
    {
        return element switch
        {
            RibbonSeparator => ItemKind.Separator,
            InRibbonGallery => ItemKind.Large,
            // A RibbonToolBar is a multi-row container; it must own a full-height column
            // instead of being squished into a single stacked row.
            RibbonToolBar => ItemKind.Large,
            IScalableRibbonControl { Size: RibbonControlSize.Large } => ItemKind.Large,
            // Medium/Small ribbon controls stack three-per-column, as in WPF.
            IScalableRibbonControl => ItemKind.Stacked,
            // Anything else is a raw container (StackPanel/Grid) or decorative element
            // hosted directly in the group. Stacking it would clamp it to a third of the
            // group height and clip its contents, so give it a full-height column.
            _ => ItemKind.Large,
        };
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        var contentHeight = double.IsInfinity(availableSize.Height) || availableSize.Height <= 0
            ? FallbackHeight
            : availableSize.Height;

        if (IsSimplified)
        {
            foreach (var child in Children)
            {
                child.Measure(new Windows.Foundation.Size(double.PositiveInfinity, contentHeight));
            }

            var simplifiedWidth = BuildSimplifiedLayout(contentHeight, out _arrangeRects);
            return new Windows.Foundation.Size(simplifiedWidth, contentHeight);
        }

        var rowHeight = contentHeight / RowsPerColumn;

        // Measure each child against the space it will actually receive.
        foreach (var child in Children)
        {
            var kind = Classify(child);
            var childHeight = kind == ItemKind.Stacked ? rowHeight : contentHeight;
            child.Measure(new Windows.Foundation.Size(double.PositiveInfinity, childHeight));
        }

        var totalWidth = BuildLayout(contentHeight, out _arrangeRects);
        return new Windows.Foundation.Size(totalWidth, contentHeight);
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        var height = finalSize.Height > 0 ? finalSize.Height : FallbackHeight;

        // Recompute against the final height so rows line up even if it differs from measure.
        if (IsSimplified)
        {
            BuildSimplifiedLayout(height, out var simplifiedRects);
            ArrangeChildren(simplifiedRects);
            return finalSize;
        }

        BuildLayout(height, out var rects);
        ArrangeChildren(rects);
        return finalSize;
    }

    private void ArrangeChildren(IReadOnlyList<Windows.Foundation.Rect> rects)
    {
        var index = 0;
        foreach (var child in Children)
        {
            if (index < rects.Count)
            {
                child.Arrange(rects[index]);
            }

            index++;
        }
    }

    private double BuildSimplifiedLayout(
        double contentHeight,
        out List<Windows.Foundation.Rect> rects)
    {
        var result = new List<Windows.Foundation.Rect>(Children.Count);
        var x = 0.0;

        foreach (var child in Children)
        {
            var width = child.DesiredSize.Width;
            var desiredHeight = child is RibbonSeparator
                ? contentHeight
                : child.DesiredSize.Height;
            var height = desiredHeight > 0
                ? System.Math.Min(contentHeight, desiredHeight)
                : contentHeight;
            var y = System.Math.Max(0, (contentHeight - height) / 2);

            result.Add(new Windows.Foundation.Rect(x, y, width, height));
            x += width;
        }

        rects = result;
        return x;
    }

    /// <summary>
    /// Computes the placement rectangle for every child using its already-measured
    /// <see cref="UIElement.DesiredSize"/>. Returns the total desired width.
    /// </summary>
    private double BuildLayout(double contentHeight, out List<Windows.Foundation.Rect> rects)
    {
        var result = new List<Windows.Foundation.Rect>(Children.Count);
        var rowHeight = contentHeight / RowsPerColumn;

        var x = 0.0;
        var columnStartX = 0.0;
        var columnWidth = 0.0;
        var rowInColumn = 0;
        var columnIndices = new List<int>();

        void FinalizeColumn()
        {
            // Give each stacked item in the column the same width so they left-align cleanly.
            foreach (var idx in columnIndices)
            {
                var r = result[idx];
                result[idx] = new Windows.Foundation.Rect(r.X, r.Y, columnWidth, r.Height);
            }

            x = columnStartX + columnWidth;
            columnIndices.Clear();
            columnWidth = 0;
            rowInColumn = 0;
        }

        foreach (var child in Children)
        {
            var kind = Classify(child);

            if (kind is ItemKind.Large or ItemKind.Separator)
            {
                if (rowInColumn > 0)
                {
                    FinalizeColumn();
                }

                var width = child.DesiredSize.Width;
                result.Add(new Windows.Foundation.Rect(x, 0, width, contentHeight));
                x += width;
                columnStartX = x;
            }
            else
            {
                if (rowInColumn == 0)
                {
                    columnStartX = x;
                }

                var width = child.DesiredSize.Width;
                result.Add(new Windows.Foundation.Rect(columnStartX, rowInColumn * rowHeight, width, rowHeight));
                columnIndices.Add(result.Count - 1);
                columnWidth = System.Math.Max(columnWidth, width);
                rowInColumn++;

                if (rowInColumn >= RowsPerColumn)
                {
                    FinalizeColumn();
                }
            }
        }

        if (rowInColumn > 0)
        {
            FinalizeColumn();
        }

        rects = result;
        return x;
    }

    private static void OnIsSimplifiedChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var panel = (RibbonGroupItemsPanel)sender;
        panel.InvalidateMeasure();
        panel.InvalidateArrange();
    }
}
