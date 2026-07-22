namespace Fluent.Converters;

/// <summary>
/// Returns the input <see cref="double"/> unless it is <see cref="double.NaN"/>,
/// in which case the converter parameter (parsed as a double) is returned, or
/// <see cref="Microsoft.UI.Xaml.DependencyProperty.UnsetValue"/> when no valid
/// parameter is supplied.
/// <para>
/// This prevents a NaN value from reaching layout properties such as
/// <c>MaxDropDownHeight</c>/<c>MaxHeight</c>. WPF treats NaN as "auto", but some
/// Uno Platform targets assign it directly to <c>MaxHeight</c> and throw
/// <see cref="System.ArgumentException"/> ("Property 'MaxHeight' cannot be set to NaN").
/// </para>
/// </summary>
public class NaNFallbackConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double d && !double.IsNaN(d))
        {
            return d;
        }

        if (parameter is double p)
        {
            return p;
        }

        if (parameter is string s && double.TryParse(s, out var parsed))
        {
            return parsed;
        }

        return Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => Microsoft.UI.Xaml.DependencyProperty.UnsetValue;
}
