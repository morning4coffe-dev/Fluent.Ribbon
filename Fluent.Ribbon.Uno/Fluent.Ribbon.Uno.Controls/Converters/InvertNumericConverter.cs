namespace Fluent.Converters;

/// <summary>
/// Used to negate numeric values.
/// </summary>
public class InvertNumericConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is float f)
        {
            return f * -1;
        }

        if (value is double d)
        {
            return d * -1;
        }

        if (value is int i)
        {
            return i * -1;
        }

        if (value is long l)
        {
            return l * -1;
        }

        return value;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return Convert(value, targetType, parameter, language);
    }
}
