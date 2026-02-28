namespace Fluent.Converters;

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
}
