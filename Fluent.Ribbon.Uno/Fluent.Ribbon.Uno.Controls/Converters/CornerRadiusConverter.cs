namespace Fluent.Converters;

using System.Globalization;

/// <summary>
/// Defines parts of a <see cref="CornerRadius"/>.
/// </summary>
[Flags]
public enum CornerRadiusPart
{
    /// <summary>None.</summary>
    None = 0,

    /// <summary>Top left.</summary>
    TopLeft = 1 << 1,

    /// <summary>Top right.</summary>
    TopRight = 1 << 2,

    /// <summary>Bottom right.</summary>
    BottomRight = 1 << 3,

    /// <summary>Bottom left.</summary>
    BottomLeft = 1 << 4,

    /// <summary>All parts.</summary>
    All = TopLeft | TopRight | BottomRight | BottomLeft
}

/// <summary>
/// Extracts specific parts of a <see cref="CornerRadius"/>.
/// </summary>
public class CornerRadiusConverter : IValueConverter, global::Fluent.IMultiValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        var valuesToExtract = parameter is CornerRadiusPart cornerRadiusValue
            ? cornerRadiusValue
            : CornerRadiusPart.All;

        if (value is not CornerRadius source)
        {
            return new CornerRadius(0);
        }

        var topLeft = valuesToExtract.HasFlag(CornerRadiusPart.TopLeft) ? source.TopLeft : 0;
        var topRight = valuesToExtract.HasFlag(CornerRadiusPart.TopRight) ? source.TopRight : 0;
        var bottomRight = valuesToExtract.HasFlag(CornerRadiusPart.BottomRight) ? source.BottomRight : 0;
        var bottomLeft = valuesToExtract.HasFlag(CornerRadiusPart.BottomLeft) ? source.BottomLeft : 0;

        return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
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

    /// <summary>Converts four values to a corner radius.</summary>
    public virtual object Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length < 4)
        {
            throw new ArgumentException("Four corner values are required.", nameof(values));
        }

        var parts = parameter is CornerRadiusPart requestedParts
            ? requestedParts
            : CornerRadiusPart.All;
        return new CornerRadius(
            parts.HasFlag(CornerRadiusPart.TopLeft) ? TryConvertSingleValue(values[0]) : 0,
            parts.HasFlag(CornerRadiusPart.TopRight) ? TryConvertSingleValue(values[1]) : 0,
            parts.HasFlag(CornerRadiusPart.BottomRight) ? TryConvertSingleValue(values[2]) : 0,
            parts.HasFlag(CornerRadiusPart.BottomLeft) ? TryConvertSingleValue(values[3]) : 0);
    }

    /// <summary>Multi-value reverse conversion is unsupported.</summary>
    public virtual object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static double TryConvertSingleValue(object value)
    {
        try
        {
            return (value as IConvertible)?.ToDouble(CultureInfo.InvariantCulture) ?? 0;
        }
        catch (Exception exception) when (exception is FormatException
                                          or InvalidCastException
                                          or OverflowException)
        {
            return 0;
        }
    }
}
