using Windows.Foundation;

namespace Fluent;

/// <summary>
/// A lightweight, non-virtualizing panel that arranges its children in a uniform grid.
/// Each child occupies a cell of <see cref="ItemWidth"/> x <see cref="ItemHeight"/>. The number
/// of columns is constrained by <see cref="MinColumns"/> and <see cref="MaxColumns"/>.
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
    private readonly Dictionary<string, TextBlock> groupHeaders =
        new(StringComparer.OrdinalIgnoreCase);
    private Func<UIElement, string>? groupSelector;

    /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
    public static readonly DependencyProperty ItemWidthProperty =
        DependencyProperty.Register(
            nameof(ItemWidth),
            typeof(double),
            typeof(UniformItemsPanel),
            new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

    /// <summary>Gets or sets the width of each cell. NaN or 0 uses each child's desired width.</summary>
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
            new PropertyMetadata(double.NaN, OnLayoutPropertyChanged));

    /// <summary>Gets or sets the height of each cell. NaN or 0 uses each child's desired height.</summary>
    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    /// <summary>Identifies the <see cref="MinColumns"/> dependency property.</summary>
    public static readonly DependencyProperty MinColumnsProperty =
        DependencyProperty.Register(
            nameof(MinColumns),
            typeof(int),
            typeof(UniformItemsPanel),
            new PropertyMetadata(0, OnLayoutPropertyChanged));

    /// <summary>Gets or sets the minimum number of columns for horizontal layout.</summary>
    public int MinColumns
    {
        get => (int)GetValue(MinColumnsProperty);
        set => SetValue(MinColumnsProperty, value);
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

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(UniformItemsPanel),
            new PropertyMetadata(Orientation.Horizontal, OnLayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the gallery orientation. Vertical galleries use one column, matching WPF.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    private static void OnLayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((UniformItemsPanel)d).InvalidateMeasure();
    }

    private int ComputeColumns(double availableWidth, double effectiveCellWidth, int itemCount)
        => GalleryLayoutMath.ComputeColumns(
            availableWidth,
            effectiveCellWidth,
            itemCount,
            MinColumns,
            MaxColumns,
            Orientation);

    internal void ConfigureGrouping(Func<UIElement, string>? selector)
    {
        foreach (var header in groupHeaders.Values)
        {
            Children.Remove(header);
        }

        groupHeaders.Clear();
        groupSelector = selector;
        if (selector is not null)
        {
            var groups = GetItemChildren()
                .Select(selector)
                .Where(static group => !string.IsNullOrEmpty(group))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            foreach (var group in groups)
            {
                var header = new TextBlock
                {
                    Text = group,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(6, 6, 6, 2),
                };
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(header, group);
                Microsoft.UI.Xaml.Automation.AutomationProperties.SetHeadingLevel(
                    header,
                    Microsoft.UI.Xaml.Automation.Peers.AutomationHeadingLevel.Level3);
                groupHeaders.Add(group, header);
                Children.Add(header);
            }
        }

        InvalidateMeasure();
    }

    private IEnumerable<UIElement> GetItemChildren() =>
        Children.Where(child => child is not TextBlock header || !groupHeaders.ContainsValue(header));

    private IEnumerable<IGrouping<string, UIElement>> GetVisibleGroups() =>
        GetItemChildren()
            .Where(static child => child.Visibility != Visibility.Collapsed)
            .GroupBy(
                child => groupSelector?.Invoke(child) ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

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
            if (child is TextBlock header && groupHeaders.ContainsValue(header))
            {
                continue;
            }

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

        if (groupSelector is not null)
        {
            double desiredWidth = 0;
            double desiredHeight = 0;
            foreach (var group in GetVisibleGroups())
            {
                if (groupHeaders.TryGetValue(group.Key, out var header))
                {
                    desiredWidth = Math.Max(desiredWidth, header.DesiredSize.Width);
                    desiredHeight += header.DesiredSize.Height;
                }

                var itemCount = group.Count();
                var groupColumns = ComputeColumns(availableSize.Width, effW, itemCount);
                var groupRows = (int)Math.Ceiling((double)itemCount / groupColumns);
                desiredWidth = Math.Max(desiredWidth, groupColumns * effW);
                desiredHeight += groupRows * effH;
            }

            return new Size(desiredWidth, desiredHeight);
        }

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
            if (child is TextBlock header && groupHeaders.ContainsValue(header))
            {
                continue;
            }

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

        if (groupSelector is not null)
        {
            var arrangedHeaders = new HashSet<TextBlock>();
            double y = 0;
            foreach (var group in GetVisibleGroups())
            {
                if (groupHeaders.TryGetValue(group.Key, out var header))
                {
                    header.Arrange(new Rect(0, y, finalSize.Width, header.DesiredSize.Height));
                    arrangedHeaders.Add(header);
                    y += header.DesiredSize.Height;
                }

                var groupItems = group.ToArray();
                var groupColumns = ComputeColumns(finalSize.Width, effW, groupItems.Length);
                for (var index = 0; index < groupItems.Length; index++)
                {
                    var itemColumn = index % groupColumns;
                    var itemRow = index / groupColumns;
                    groupItems[index].Arrange(
                        new Rect(itemColumn * effW, y + (itemRow * effH), effW, effH));
                }

                y += Math.Ceiling((double)groupItems.Length / groupColumns) * effH;
            }

            foreach (var header in groupHeaders.Values.Except(arrangedHeaders))
            {
                header.Arrange(new Rect(0, 0, 0, 0));
            }

            return finalSize;
        }

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
