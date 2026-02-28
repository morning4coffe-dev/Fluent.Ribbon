namespace Fluent;

/// <summary>
/// A layout panel for gallery items with grouping and filtering support.
/// Arranges items in a wrap layout, optionally organized into <see cref="GalleryGroupContainer"/>s.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, simplified for Uno/WinUI.
/// WPF version uses VisualCollection and ItemContainerGenerator;
/// Uno version manages children directly.
/// </remarks>
public partial class GalleryPanel : Panel
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(GalleryPanel),
            new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the uniform width for items.
    /// </summary>
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
            typeof(GalleryPanel),
            new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the uniform height for items.
    /// </summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="MinItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MinItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MinItemsInRow),
            typeof(int),
            typeof(GalleryPanel),
            new PropertyMetadata(1, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the minimum number of items per row.
    /// </summary>
    public int MinItemsInRow
    {
        get => (int)GetValue(MinItemsInRowProperty);
        set => SetValue(MinItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="MaxItemsInRow"/> dependency property.</summary>
    public static readonly DependencyProperty MaxItemsInRowProperty =
        DependencyProperty.Register(
            nameof(MaxItemsInRow),
            typeof(int),
            typeof(GalleryPanel),
            new PropertyMetadata(int.MaxValue, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the maximum number of items per row.
    /// </summary>
    public int MaxItemsInRow
    {
        get => (int)GetValue(MaxItemsInRowProperty);
        set => SetValue(MaxItemsInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(GalleryPanel),
            new PropertyMetadata(Orientation.Horizontal, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the orientation for item layout.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    #endregion

    #region Layout

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GalleryPanel panel)
        {
            panel.InvalidateMeasure();
        }
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        var itemWidth = ItemWidth;
        var itemHeight = ItemHeight;
        var useItemWidth = !double.IsNaN(itemWidth) && itemWidth > 0;
        var useItemHeight = !double.IsNaN(itemHeight) && itemHeight > 0;

        if (!useItemWidth || !useItemHeight)
        {
            // Simple stack layout when no uniform sizes specified
            return MeasureStackLayout(availableSize);
        }

        return MeasureUniformLayout(availableSize, itemWidth, itemHeight);
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        var itemWidth = ItemWidth;
        var itemHeight = ItemHeight;
        var useItemWidth = !double.IsNaN(itemWidth) && itemWidth > 0;
        var useItemHeight = !double.IsNaN(itemHeight) && itemHeight > 0;

        if (!useItemWidth || !useItemHeight)
        {
            return ArrangeStackLayout(finalSize);
        }

        return ArrangeUniformLayout(finalSize, itemWidth, itemHeight);
    }

    #endregion

    #region Uniform Layout

    private Windows.Foundation.Size MeasureUniformLayout(Windows.Foundation.Size availableSize, double itemWidth, double itemHeight)
    {
        var visibleCount = 0;

        foreach (var child in Children)
        {
            if (child.Visibility != Visibility.Collapsed)
            {
                child.Measure(new Windows.Foundation.Size(itemWidth, itemHeight));
                visibleCount++;
            }
        }

        if (visibleCount == 0)
        {
            return new Windows.Foundation.Size(0, 0);
        }

        var maxColumns = double.IsPositiveInfinity(availableSize.Width)
            ? visibleCount
            : Math.Max(1, (int)(availableSize.Width / itemWidth));

        var columns = Math.Max(MinItemsInRow, Math.Min(maxColumns, MaxItemsInRow));
        columns = Math.Min(columns, visibleCount);

        var rows = columns > 0 ? (int)Math.Ceiling((double)visibleCount / columns) : 0;

        return new Windows.Foundation.Size(columns * itemWidth, rows * itemHeight);
    }

    private Windows.Foundation.Size ArrangeUniformLayout(Windows.Foundation.Size finalSize, double itemWidth, double itemHeight)
    {
        var columns = Math.Max(1, (int)(finalSize.Width / itemWidth));
        columns = Math.Max(MinItemsInRow, Math.Min(columns, MaxItemsInRow));

        var childIndex = 0;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            var col = childIndex % columns;
            var row = childIndex / columns;

            child.Arrange(new Windows.Foundation.Rect(col * itemWidth, row * itemHeight, itemWidth, itemHeight));
            childIndex++;
        }

        return finalSize;
    }

    #endregion

    #region Stack Layout

    private Windows.Foundation.Size MeasureStackLayout(Windows.Foundation.Size availableSize)
    {
        var isHorizontal = Orientation == Orientation.Horizontal;
        double totalU = 0, maxV = 0;
        double curLineU = 0, curLineV = 0;
        var constraintU = isHorizontal ? availableSize.Width : availableSize.Height;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            child.Measure(availableSize);

            var childU = isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            var childV = isHorizontal ? child.DesiredSize.Height : child.DesiredSize.Width;

            if (curLineU + childU > constraintU && curLineU > 0)
            {
                totalU = Math.Max(totalU, curLineU);
                maxV += curLineV;
                curLineU = childU;
                curLineV = childV;
            }
            else
            {
                curLineU += childU;
                curLineV = Math.Max(curLineV, childV);
            }
        }

        totalU = Math.Max(totalU, curLineU);
        maxV += curLineV;

        return isHorizontal
            ? new Windows.Foundation.Size(totalU, maxV)
            : new Windows.Foundation.Size(maxV, totalU);
    }

    private Windows.Foundation.Size ArrangeStackLayout(Windows.Foundation.Size finalSize)
    {
        var isHorizontal = Orientation == Orientation.Horizontal;
        var constraintU = isHorizontal ? finalSize.Width : finalSize.Height;
        double curU = 0, curV = 0, lineV = 0;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            var childU = isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            var childV = isHorizontal ? child.DesiredSize.Height : child.DesiredSize.Width;

            if (curU + childU > constraintU && curU > 0)
            {
                curV += lineV;
                curU = 0;
                lineV = 0;
            }

            var rect = isHorizontal
                ? new Windows.Foundation.Rect(curU, curV, childU, childV)
                : new Windows.Foundation.Rect(curV, curU, childV, childU);

            child.Arrange(rect);
            curU += childU;
            lineV = Math.Max(lineV, childV);
        }

        return finalSize;
    }

    #endregion
}
