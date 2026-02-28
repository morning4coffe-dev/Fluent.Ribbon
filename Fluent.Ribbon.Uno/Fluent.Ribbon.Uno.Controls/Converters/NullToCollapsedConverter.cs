namespace Fluent.Converters;

/// <summary>
/// Converts a null value to <see cref="Microsoft.UI.Xaml.Visibility.Collapsed"/>
/// and a non-null value to <see cref="Microsoft.UI.Xaml.Visibility.Visible"/>.
/// </summary>
public class NullToCollapsedConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return value is null ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
