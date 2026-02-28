namespace Fluent.Converters;

/// <summary>
/// Converts a boolean value to a Visibility value.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Gets or sets whether to invert the conversion.
    /// </summary>
    public bool Invert { get; set; }

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var boolValue = value is bool b && b;
        if (Invert)
        {
            boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        var visibility = value is Visibility v && v == Visibility.Visible;
        return Invert ? !visibility : visibility;
    }
}
