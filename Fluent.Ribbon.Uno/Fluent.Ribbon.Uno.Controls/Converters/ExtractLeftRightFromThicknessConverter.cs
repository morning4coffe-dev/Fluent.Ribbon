namespace Fluent.Converters;

using System.Globalization;

/// <summary>
/// Extracts a <see cref="Thickness"/> with only horizontal (Left, Right) components from the input.
/// Vertical components are set to 0.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public class ExtractLeftRightFromThicknessConverter : IValueConverter
{
    /// <summary>
    /// Default instance.
    /// </summary>
    public static readonly ExtractLeftRightFromThicknessConverter Default = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is Thickness thickness)
        {
            return new Thickness(thickness.Left, 0, thickness.Right, 0);
        }

        return new Thickness(0);
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotSupportedException();
    }

    /// <summary>WPF-compatible culture-based conversion overload.</summary>
    public virtual object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture.Name);
    }

    /// <summary>WPF-compatible culture-based reverse conversion overload.</summary>
    public virtual object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ConvertBack(value, targetType, parameter, culture.Name);
    }
}
