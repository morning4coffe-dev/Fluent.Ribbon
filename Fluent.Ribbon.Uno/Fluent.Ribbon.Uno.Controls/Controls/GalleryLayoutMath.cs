namespace Fluent;

/// <summary>
/// Pure layout and scaling calculations shared by gallery controls and tests.
/// </summary>
internal static class GalleryLayoutMath
{
    internal static int NormalizeCount(int value) => Math.Max(0, value);

    internal static int ClampCurrentItemsInRow(int current, int minimum, int maximum)
    {
        var normalizedMaximum = NormalizeCount(maximum);
        var normalizedMinimum = Math.Min(NormalizeCount(minimum), normalizedMaximum);
        return Math.Clamp(NormalizeCount(current), normalizedMinimum, normalizedMaximum);
    }

    internal static int ReduceItemsInRow(int current, int minimum)
    {
        var normalizedCurrent = NormalizeCount(current);
        return normalizedCurrent > NormalizeCount(minimum)
            ? normalizedCurrent - 1
            : normalizedCurrent;
    }

    internal static int EnlargeItemsInRow(int current, int maximum)
    {
        var normalizedMaximum = NormalizeCount(maximum);
        return Math.Min(NormalizeCount(current) + 1, normalizedMaximum);
    }

    internal static int ComputeColumns(
        double availableWidth,
        double effectiveCellWidth,
        int itemCount,
        int minimumColumns,
        int maximumColumns,
        Orientation orientation)
    {
        if (itemCount <= 0)
        {
            return 1;
        }

        int columns;
        if (!double.IsInfinity(availableWidth) && availableWidth > 0 && effectiveCellWidth > 0)
        {
            columns = (int)(availableWidth / effectiveCellWidth);
        }

        else
        {
            columns = 0;
        }

        if (columns <= 0 && maximumColumns > 0)
        {
            columns = maximumColumns;
        }
        else if (columns <= 0)
        {
            // WPF's UniformGridWithItemSize uses a square-ish layout when neither the
            // available width nor MaxColumns provides a column count.
            columns = (int)Math.Ceiling(Math.Sqrt(itemCount));
        }

        var normalizedMinimum = orientation == Orientation.Horizontal
            ? NormalizeCount(minimumColumns)
            : 1;
        var normalizedMaximum = orientation == Orientation.Horizontal
            ? NormalizeCount(maximumColumns)
            : 1;

        if (normalizedMinimum > 0 && columns < normalizedMinimum)
        {
            columns = normalizedMinimum;
        }

        if (normalizedMaximum > 0 && columns > normalizedMaximum)
        {
            columns = normalizedMaximum;
        }

        return Math.Max(1, Math.Min(columns, itemCount));
    }
}
