namespace Fluent.Converters;

using System.Globalization;

/// <summary>
/// Checks equality of value and the converter parameter.
/// Returns <see cref="Visibility.Visible"/> if they are equal.
/// Returns <see cref="Visibility.Collapsed"/> if they are NOT equal.
/// </summary>
public class EqualsToVisibilityConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value == parameter
            || (value is not null && value.Equals(parameter)))
        {
            return Visibility.Visible;
        }

        return Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }

    /// <summary>WPF-compatible culture-based conversion overload.</summary>
    public virtual object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture.Name);
    }

    /// <summary>WPF-compatible culture-based reverse conversion overload.</summary>
    public virtual object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ConvertBack(value, targetType, parameter, culture.Name);
    }
}
