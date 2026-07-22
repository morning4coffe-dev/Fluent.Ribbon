namespace Fluent.Converters;

using System.Globalization;

/// <summary>
/// Converts <c>null</c> to <c>true</c> and not <c>null</c> to <c>false</c>.
/// </summary>
public class IsNullConverter : IValueConverter
{
    /// <summary>
    /// A singleton instance for <see cref="IsNullConverter"/>.
    /// </summary>
    public static readonly IsNullConverter Instance = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is null;
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
