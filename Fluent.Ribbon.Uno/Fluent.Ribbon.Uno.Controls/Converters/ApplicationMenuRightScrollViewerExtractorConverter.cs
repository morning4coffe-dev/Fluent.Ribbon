namespace Fluent.Converters;

using System.Globalization;

/// <summary>
/// Extracts the right-side scroll viewer from an application menu.
/// </summary>
public class ApplicationMenuRightScrollViewerExtractorConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        string language)
    {
        return value is ApplicationMenu menu
            ? FindDescendant<ScrollViewer>(menu) ?? value
            : value;
    }

    /// <inheritdoc />
    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        string language)
    {
        return value;
    }

    /// <summary>
    /// WPF-compatible conversion overload.
    /// </summary>
    public virtual object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture.Name);
    }

    /// <summary>
    /// WPF-compatible reverse-conversion overload.
    /// </summary>
    public virtual object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return ConvertBack(value, targetType, parameter, culture.Name);
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : class
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is { } descendant)
            {
                return descendant;
            }
        }

        return null;
    }
}
