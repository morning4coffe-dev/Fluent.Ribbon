namespace Fluent.Converters;

/// <summary>
/// Converts an Orientation to a width value for separators.
/// </summary>
public class OrientationToWidthConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Orientation orientation)
        {
            return orientation == Orientation.Vertical ? 1.0 : double.NaN;
        }

        return 1.0;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts an Orientation to a height value for separators.
/// </summary>
public class OrientationToHeightConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Orientation orientation)
        {
            return orientation == Orientation.Horizontal ? 1.0 : double.NaN;
        }

        return double.NaN;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
