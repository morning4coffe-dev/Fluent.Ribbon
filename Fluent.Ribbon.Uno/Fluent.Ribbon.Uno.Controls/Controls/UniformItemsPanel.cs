using Windows.Foundation;

namespace Fluent;

/// <summary>
/// A lightweight, non-virtualizing panel that arranges its children in a uniform grid.
/// Each child occupies a cell of <see cref="ItemWidth"/> x <see cref="ItemHeight"/>. The number
/// of columns is capped by <see cref="MaxColumns"/> (0 means "fit as many as the available width
/// allows").
/// </summary>
/// <remarks>
/// The ribbon galleries host pre-built <see cref="UIElement"/> instances directly (not data items
/// with a template). Hosting live elements in an <c>ItemsRepeater</c> + <c>UniformGridLayout</c>
/// makes Uno's virtualization bookkeeping (<c>ElementManager</c>) desync and throw
/// <see cref="System.ArgumentOutOfRangeException"/> from <c>GetLayoutBoundsForDataIndex</c> during a
/// layout pass. Because galleries only ever contain a handful of items, virtualization brings no
/// benefit, so this simple measure/arrange panel replaces it and removes that crash class entirely.
/// </remarks>
public partial class UniformItemsPanel : Panel
{
    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(UniformItemsPanel),
            new PropertyMetadata(60.0, OnLayoutPropertyChanged));

    /// <summary>Gets or sets the width of each cell. 0 uses each child's desired width.</summary>
    public double ItemWidth
    {
        get => (double)GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemHeight"/> dependency property.</summary>
    public static readonly DependencyProperty ItemHeightProperty =
        DependencyProperty.Register(
            nameof(ItemHeight),
            typeof(double),
            typeof(UniformItemsPanel),
            new PropertyMetadata(24.0, OnLayoutPropertyChanged));

    /// <summary>Gets or sets the height of each cell. 0 uses each child's desired height.</summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxColumns"/> dependency property.</summary>
    public static readonly DependencyProperty MaxColumnsProperty =
        DependencyProperty.Register(
            nameof(MaxColumns),
            typeof(int),
            typeof(UniformItemsPanel),
            new PropertyMetadata(0, OnLayoutPropertyChanged));

    /// <summary>Gets or sets the maximum number of columns. 0 means "fit by available width".</summary>
    public int MaxColumns
    {
        get => (int)GetValue(MaxColumnsProperty);
        set => SetValue(MaxColumnsProperty, value);
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((UniformItemsPanel)d).InvalidateMeasure();
    }

    private int ComputeColumns(double availableWidth, double effectiveCellWidth, int itemCount)
    {
        if (itemCount == 0)
        {
            return 1;
        }

        int fit;
        if (double.IsInfinity(availableWidth) || availableWidth <= 0 || effectiveCellWidth <= 0)
        {
            // No usable width constraint: place everything on one row unless MaxColumns caps it.
            fit = itemCount;
        }
        else
        {
            fit = Math.Max(1, (int)(availableWidth / effectiveCellWidth));
        }

        if (MaxColumns > 0)
        {
            fit = Math.Min(fit, MaxColumns);
        }

        return Math.Max(1, Math.Min(fit, itemCount));
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var cellW = ItemWidth > 0 ? ItemWidth : 0;
        var cellH = ItemHeight > 0 ? ItemHeight : 0;

        var childConstraint = new Size(
            cellW > 0 ? cellW : double.PositiveInfinity,
            cellH > 0 ? cellH : double.PositiveInfinity);

        double maxChildW = 0;
        double maxChildH = 0;
        var visible = 0;
        foreach (var child in Children)
        {
            child.Measure(childConstraint);

            // Collapsed children are filtered out (e.g. gallery group filtering) and take no cell.
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            visible++;
            if (cellW <= 0 && child.DesiredSize.Width > maxChildW)
            {
                maxChildW = child.DesiredSize.Width;
            }

            if (cellH <= 0 && child.DesiredSize.Height > maxChildH)
            {
                maxChildH = child.DesiredSize.Height;
            }
        }

        var effW = cellW > 0 ? cellW : maxChildW;
        var effH = cellH > 0 ? cellH : maxChildH;

        var columns = ComputeColumns(availableSize.Width, effW, visible);
        var rows = visible == 0 ? 0 : (int)Math.Ceiling((double)visible / columns);

        return new Size(columns * effW, rows * effH);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var cellW = ItemWidth > 0 ? ItemWidth : 0;
        var cellH = ItemHeight > 0 ? ItemHeight : 0;

        double maxChildW = 0;
        double maxChildH = 0;
        var visible = 0;
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            visible++;
            if (cellW <= 0 && child.DesiredSize.Width > maxChildW)
            {
                maxChildW = child.DesiredSize.Width;
            }

            if (cellH <= 0 && child.DesiredSize.Height > maxChildH)
            {
                maxChildH = child.DesiredSize.Height;
            }
        }

        if (visible == 0)
        {
            foreach (var child in Children)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
            }

            return finalSize;
        }

        var effW = cellW > 0 ? cellW : maxChildW;
        var effH = cellH > 0 ? cellH : maxChildH;

        var columns = ComputeColumns(finalSize.Width, effW, visible);
        var column = 0;
        var row = 0;
        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new Rect(0, 0, 0, 0));
                continue;
            }

            child.Arrange(new Rect(column * effW, row * effH, effW, effH));
            column++;
            if (column >= columns)
            {
                column = 0;
                row++;
            }
        }

        return finalSize;
    }
}
