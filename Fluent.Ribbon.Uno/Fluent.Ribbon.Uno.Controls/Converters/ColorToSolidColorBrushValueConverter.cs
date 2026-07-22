namespace Fluent.Converters;

using System.Globalization;

/// <summary>
/// Provides the WPF-compatible converter type name and overloads.
/// </summary>
public class ColorToSolidColorBrushValueConverter : ColorToSolidColorBrushConverter
{
    /// <summary>
    /// Converts a color to a brush.
    /// </summary>
    public virtual object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture.Name);
    }

    /// <summary>
    /// Converts a brush back to a color.
    /// </summary>
    public virtual object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ConvertBack(value, targetType, parameter, culture.Name);
    }
}
