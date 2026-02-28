namespace Fluent.Converters;

/// <summary>
/// Converter that constructs a <see cref="Thickness"/> from individual side values.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. WPF uses IMultiValueConverter (not available in WinUI).
/// Adapted as a single-value converter that takes a string "left,top,right,bottom" or a Thickness object
/// and applies optional transformations through the parameter.
/// Parameter format: "L", "T", "R", "B", "LR" (left+right), "TB" (top+bottom), or "negate".
/// </remarks>
public class ThicknessConverter : IValueConverter
{
    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        var thickness = value switch
        {
            Thickness t => t,
            double d => new Thickness(d),
            int i => new Thickness(i),
            string s => ParseThickness(s),
            _ => default
        };

        if (parameter is string param)
        {
            return param.ToUpperInvariant() switch
            {
                "L" => new Thickness(thickness.Left, 0, 0, 0),
                "T" => new Thickness(0, thickness.Top, 0, 0),
                "R" => new Thickness(0, 0, thickness.Right, 0),
                "B" => new Thickness(0, 0, 0, thickness.Bottom),
                "LR" => new Thickness(thickness.Left, 0, thickness.Right, 0),
                "TB" => new Thickness(0, thickness.Top, 0, thickness.Bottom),
                "NEGATE" => new Thickness(-thickness.Left, -thickness.Top, -thickness.Right, -thickness.Bottom),
                _ => thickness
            };
        }

        return thickness;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        return value;
    }

    private static Thickness ParseThickness(string value)
    {
        var parts = value.Split(',');

        if (parts.Length == 1 && double.TryParse(parts[0].Trim(), out var uniform))
        {
            return new Thickness(uniform);
        }

        if (parts.Length == 2
            && double.TryParse(parts[0].Trim(), out var lr)
            && double.TryParse(parts[1].Trim(), out var tb))
        {
            return new Thickness(lr, tb, lr, tb);
        }

        if (parts.Length == 4
            && double.TryParse(parts[0].Trim(), out var l)
            && double.TryParse(parts[1].Trim(), out var t)
            && double.TryParse(parts[2].Trim(), out var r)
            && double.TryParse(parts[3].Trim(), out var b))
        {
            return new Thickness(l, t, r, b);
        }

        return default;
    }
}
