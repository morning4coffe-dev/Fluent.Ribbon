namespace Fluent.Converters;

using System.Globalization;
using System.Text;

/// <summary>
/// Converter class which converts from <see cref="string"/> to <see cref="double"/> and back.
/// </summary>
public class SpinnerTextToValueConverter : IValueConverter
{
    /// <summary>
    /// Gets a default instance of <see cref="SpinnerTextToValueConverter"/>.
    /// </summary>
    public static readonly SpinnerTextToValueConverter DefaultInstance = new();

    /// <inheritdoc />
    public virtual object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string text && parameter is Tuple<string, double> converterParam)
        {
            return TextToDouble(
                text,
                converterParam.Item1,
                converterParam.Item2,
                GetCulture(language));
        }

        return 0.0;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is double d && parameter is string format)
        {
            return DoubleToText(d, format, GetCulture(language));
        }

        return string.Empty;
    }

    /// <summary>WPF-compatible culture-based conversion overload.</summary>
    public virtual object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is string text && parameter is Tuple<string, double> converterParam)
        {
            return TextToDouble(
                text,
                converterParam.Item1,
                converterParam.Item2,
                culture);
        }

        return 0.0;
    }

    /// <summary>WPF-compatible culture-based reverse conversion overload.</summary>
    public virtual object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is double number && parameter is string format
            ? DoubleToText(number, format, culture)
            : string.Empty;
    }

    /// <summary>
    /// Converts the given <paramref name="text"/> to a <see cref="double"/>.
    /// </summary>
    /// <returns>The converted value, or <paramref name="previousValue"/> if conversion fails.</returns>
    public virtual double TextToDouble(string text, string format, double previousValue, CultureInfo culture)
    {
        var sb = new StringBuilder();

        foreach (var symbol in text)
        {
            if (char.IsDigit(symbol)
                || symbol == ','
                || symbol == '.'
                || (symbol == '-' && sb.Length == 0))
            {
                sb.Append(symbol);
            }
        }

        text = sb.ToString();

        if (!double.TryParse(text, NumberStyles.Any, culture, out var doubleValue))
        {
            doubleValue = previousValue;
        }

        return doubleValue;
    }

    /// <summary>
    /// Converts <paramref name="value"/> to a formatted text using <paramref name="format"/>.
    /// </summary>
    public virtual string DoubleToText(double value, string format, CultureInfo culture)
    {
        return value.ToString(format, culture);
    }

    private static CultureInfo GetCulture(string language)
    {
        return string.IsNullOrWhiteSpace(language)
            ? CultureInfo.CurrentCulture
            : CultureInfo.GetCultureInfo(language);
    }
}
