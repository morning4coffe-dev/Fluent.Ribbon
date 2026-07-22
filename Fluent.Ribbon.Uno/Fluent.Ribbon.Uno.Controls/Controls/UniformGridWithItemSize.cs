namespace Fluent;

/// <summary>
/// A panel that lays out its children in a uniform grid where each cell has the
/// same specified size. Used primarily in galleries.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public partial class UniformGridWithItemSize : Panel
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(UniformGridWithItemSize),
            new PropertyMetadata(0.0, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the width of each item cell.
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
            typeof(UniformGridWithItemSize),
            new PropertyMetadata(0.0, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the height of each item cell.
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
            typeof(UniformGridWithItemSize),
            new PropertyMetadata(0, OnLayoutPropertyChanged));

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
            typeof(UniformGridWithItemSize),
            new PropertyMetadata(int.MaxValue, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the maximum number of items per row.
    /// </summary>
    public int MaxItemsInRow
    {
        get => (int)GetValue(MaxItemsInRowProperty);
        set => SetValue(MaxItemsInRowProperty, value);
    }

    /// <summary>Identifies the WPF-compatible minimum-column property.</summary>
    public static readonly DependencyProperty MinColumnsProperty = MinItemsInRowProperty;

    /// <summary>Gets or sets the minimum number of columns.</summary>
    public int MinColumns
    {
        get => Orientation == Orientation.Horizontal ? MinItemsInRow : 1;
        set => MinItemsInRow = value;
    }

    /// <summary>Identifies the WPF-compatible maximum-column property.</summary>
    public static readonly DependencyProperty MaxColumnsProperty = MaxItemsInRowProperty;

    /// <summary>Gets or sets the maximum number of columns.</summary>
    public int MaxColumns
    {
        get => Orientation == Orientation.Horizontal ? MaxItemsInRow : 1;
        set => MaxItemsInRow = value;
    }

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(UniformGridWithItemSize),
            new PropertyMetadata(Orientation.Horizontal, OnLayoutPropertyChanged));

    /// <summary>Gets or sets the panel orientation.</summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    #endregion

    #region Fields

    private int _columns;
    private int _rows;

    #endregion

    #region Layout

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UniformGridWithItemSize panel)
        {
            panel.InvalidateMeasure();
        }
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        UpdateComputedValues(availableSize);

        var childSize = new Windows.Foundation.Size(ItemWidth, ItemHeight);

        foreach (var child in Children)
        {
            child.Measure(childSize);
        }

        var width = _columns * ItemWidth;
        var height = _rows * ItemHeight;

        return new Windows.Foundation.Size(width, height);
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        var childIndex = 0;

        foreach (var child in Children)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                continue;
            }

            var col = childIndex % _columns;
            var row = childIndex / _columns;

            var rect = new Windows.Foundation.Rect(
                col * ItemWidth,
                row * ItemHeight,
                ItemWidth,
                ItemHeight);

            child.Arrange(rect);
            childIndex++;
        }

        return finalSize;
    }

    private void UpdateComputedValues(Windows.Foundation.Size availableSize)
    {
        var visibleCount = 0;

        foreach (var child in Children)
        {
            if (child.Visibility != Visibility.Collapsed)
            {
                visibleCount++;
            }
        }

        if (visibleCount == 0 || ItemWidth <= 0 || ItemHeight <= 0)
        {
            _columns = 0;
            _rows = 0;
            return;
        }

        var maxColumns = double.IsPositiveInfinity(availableSize.Width)
            ? visibleCount
            : Math.Max(1, (int)(availableSize.Width / ItemWidth));

        _columns = Math.Max(MinColumns, Math.Min(maxColumns, MaxColumns));
        _columns = Math.Min(_columns, visibleCount);

        _rows = _columns > 0 ? (int)Math.Ceiling((double)visibleCount / _columns) : 0;
    }

    #endregion
}
