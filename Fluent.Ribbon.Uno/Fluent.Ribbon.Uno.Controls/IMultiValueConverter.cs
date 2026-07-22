namespace Fluent;

using System.Globalization;

/// <summary>
/// Provides the portable multi-value conversion contract used by compatibility converters.
/// </summary>
public interface IMultiValueConverter
{
    /// <summary>Converts multiple source values to a target value.</summary>
    object? Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture);

    /// <summary>Converts a target value back to multiple source values.</summary>
    object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture);
}
