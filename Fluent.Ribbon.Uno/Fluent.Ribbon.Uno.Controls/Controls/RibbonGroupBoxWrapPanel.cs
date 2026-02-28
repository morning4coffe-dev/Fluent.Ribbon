namespace Fluent;

using Fluent.Internal;

/// <summary>
/// A wrap panel specialized for <see cref="RibbonGroupBox"/>.
/// In normal mode it wraps children vertically (like WrapPanel with Orientation=Vertical).
/// In simplified mode it acts as a horizontal StackPanel.
/// </summary>
public partial class RibbonGroupBoxWrapPanel : Panel
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(RibbonGroupBoxWrapPanel),
            new PropertyMetadata(Orientation.Vertical, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the orientation. Wrapping occurs in the orthogonal direction.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(RibbonGroupBoxWrapPanel),
            new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets a uniform width for all items. NaN means use each child's DesiredSize.
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
            typeof(RibbonGroupBoxWrapPanel),
            new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets a uniform height for all items. NaN means use each child's DesiredSize.
    /// </summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonGroupBoxWrapPanel),
            new PropertyMetadata(false, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets whether the ribbon is in simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    #endregion

    #region Layout

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonGroupBoxWrapPanel panel)
        {
            panel.InvalidateMeasure();
            panel.InvalidateArrange();
        }
    }

    /// <inheritdoc />
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        if (IsSimplified)
        {
            return MeasureStackMode(availableSize);
        }

        return MeasureWrapMode(availableSize);
    }

    /// <inheritdoc />
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        if (IsSimplified)
        {
            return ArrangeStackMode(finalSize);
        }

        return ArrangeWrapMode(finalSize);
    }

    #endregion

    #region Wrap Mode

    private Windows.Foundation.Size MeasureWrapMode(Windows.Foundation.Size constraint)
    {
        var isHorizontal = Orientation == Orientation.Horizontal;
        var itemWidth = ItemWidth;
        var itemHeight = ItemHeight;
        var itemWidthSet = !double.IsNaN(itemWidth);
        var itemHeightSet = !double.IsNaN(itemHeight);

        var childConstraint = new Windows.Foundation.Size(
            itemWidthSet ? itemWidth : constraint.Width,
            itemHeightSet ? itemHeight : constraint.Height);

        double curLineU = 0, curLineV = 0;
        double panelU = 0, panelV = 0;
        var constraintU = isHorizontal ? constraint.Width : constraint.Height;

        foreach (var child in Children)
        {
            child.Measure(childConstraint);

            var childU = itemWidthSet && isHorizontal ? itemWidth
                : itemHeightSet && !isHorizontal ? itemHeight
                : isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            var childV = itemHeightSet && isHorizontal ? itemHeight
                : itemWidthSet && !isHorizontal ? itemWidth
                : isHorizontal ? child.DesiredSize.Height : child.DesiredSize.Width;

            if (DoubleUtil.GreaterThan(curLineU + childU, constraintU) && curLineU > 0)
            {
                panelU = Math.Max(curLineU, panelU);
                panelV += curLineV;
                curLineU = childU;
                curLineV = childV;
            }
            else
            {
                curLineU += childU;
                curLineV = Math.Max(childV, curLineV);
            }
        }

        panelU = Math.Max(curLineU, panelU);
        panelV += curLineV;

        return isHorizontal
            ? new Windows.Foundation.Size(panelU, panelV)
            : new Windows.Foundation.Size(panelV, panelU);
    }

    private Windows.Foundation.Size ArrangeWrapMode(Windows.Foundation.Size finalSize)
    {
        var isHorizontal = Orientation == Orientation.Horizontal;
        var itemWidth = ItemWidth;
        var itemHeight = ItemHeight;
        var itemWidthSet = !double.IsNaN(itemWidth);
        var itemHeightSet = !double.IsNaN(itemHeight);

        var constraintU = isHorizontal ? finalSize.Width : finalSize.Height;
        double curLineU = 0, curLineV = 0, accumulatedV = 0;
        var firstInLine = 0;

        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var childU = itemWidthSet && isHorizontal ? itemWidth
                : itemHeightSet && !isHorizontal ? itemHeight
                : isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            var childV = itemHeightSet && isHorizontal ? itemHeight
                : itemWidthSet && !isHorizontal ? itemWidth
                : isHorizontal ? child.DesiredSize.Height : child.DesiredSize.Width;

            if (DoubleUtil.GreaterThan(curLineU + childU, constraintU) && curLineU > 0)
            {
                ArrangeLine(firstInLine, i, accumulatedV, curLineV, isHorizontal, itemWidth, itemHeight);
                accumulatedV += curLineV;
                firstInLine = i;
                curLineU = childU;
                curLineV = childV;
            }
            else
            {
                curLineU += childU;
                curLineV = Math.Max(childV, curLineV);
            }
        }

        ArrangeLine(firstInLine, Children.Count, accumulatedV, curLineV, isHorizontal, itemWidth, itemHeight);

        return finalSize;
    }

    private void ArrangeLine(int start, int end, double v, double lineV, bool isHorizontal, double itemWidth, double itemHeight)
    {
        var itemWidthSet = !double.IsNaN(itemWidth);
        var itemHeightSet = !double.IsNaN(itemHeight);
        double u = 0;

        for (var i = start; i < end; i++)
        {
            var child = Children[i];
            var childU = itemWidthSet && isHorizontal ? itemWidth
                : itemHeightSet && !isHorizontal ? itemHeight
                : isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height;

            var rect = isHorizontal
                ? new Windows.Foundation.Rect(u, v, childU, lineV)
                : new Windows.Foundation.Rect(v, u, lineV, childU);

            child.Arrange(rect);
            u += childU;
        }
    }

    #endregion

    #region Stack Mode

    private Windows.Foundation.Size MeasureStackMode(Windows.Foundation.Size constraint)
    {
        var isHorizontal = Orientation == Orientation.Horizontal;
        var layoutSlotSize = isHorizontal
            ? new Windows.Foundation.Size(double.PositiveInfinity, constraint.Height)
            : new Windows.Foundation.Size(constraint.Width, double.PositiveInfinity);

        double totalU = 0, maxV = 0;

        foreach (var child in Children)
        {
            child.Measure(layoutSlotSize);
            var childSize = child.DesiredSize;

            if (isHorizontal)
            {
                totalU += childSize.Width;
                maxV = Math.Max(maxV, childSize.Height);
            }
            else
            {
                totalU += childSize.Height;
                maxV = Math.Max(maxV, childSize.Width);
            }
        }

        return isHorizontal
            ? new Windows.Foundation.Size(totalU, maxV)
            : new Windows.Foundation.Size(maxV, totalU);
    }

    private Windows.Foundation.Size ArrangeStackMode(Windows.Foundation.Size finalSize)
    {
        var isHorizontal = Orientation == Orientation.Horizontal;
        double offset = 0;

        foreach (var child in Children)
        {
            var rect = isHorizontal
                ? new Windows.Foundation.Rect(offset, 0, child.DesiredSize.Width, finalSize.Height)
                : new Windows.Foundation.Rect(0, offset, finalSize.Width, child.DesiredSize.Height);

            child.Arrange(rect);
            offset += isHorizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
        }

        return finalSize;
    }

    #endregion
}
