namespace Fluent.Converters;

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
public class CornerRadiusConverter : IValueConverter
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
}
