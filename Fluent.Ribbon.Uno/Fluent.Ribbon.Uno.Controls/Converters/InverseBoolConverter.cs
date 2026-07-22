namespace Fluent.Converters;

using System.Globalization;

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

    /// <summary>WPF-compatible culture-based conversion overload.</summary>
    public virtual object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture.Name);
    }

    /// <summary>WPF-compatible culture-based reverse conversion overload.</summary>
    public virtual object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return ConvertBack(value, targetType, parameter, culture.Name);
    }
}
