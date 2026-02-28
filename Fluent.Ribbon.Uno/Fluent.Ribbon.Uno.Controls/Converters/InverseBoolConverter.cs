namespace Fluent.Converters;

/// <summary>
/// Converter used to invert a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is bool b ? !b : value;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is bool b ? !b : value;
    }
}
