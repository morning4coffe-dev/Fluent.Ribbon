namespace Fluent.Converters;

using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

/// <summary>
/// Converts <see cref="Color"/> to a <see cref="SolidColorBrush"/> and back.
/// </summary>
public class ColorToSolidColorBrushConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is Color color)
        {
            return new SolidColorBrush(color);
        }

        return null;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        if (value is SolidColorBrush brush)
        {
            return brush.Color;
        }

        return null;
    }
}
