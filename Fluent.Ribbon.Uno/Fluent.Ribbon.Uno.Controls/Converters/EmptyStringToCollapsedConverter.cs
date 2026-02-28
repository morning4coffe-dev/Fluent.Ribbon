namespace Fluent.Converters;

/// <summary>
/// Converts an empty or null string to <see cref="Microsoft.UI.Xaml.Visibility.Collapsed"/>
/// and a non-empty string to <see cref="Microsoft.UI.Xaml.Visibility.Visible"/>.
/// </summary>
public class EmptyStringToCollapsedConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object? value, Type targetType, object? parameter, string language)
    {
        return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <inheritdoc/>
    public object ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        throw new NotImplementedException();
    }
}
