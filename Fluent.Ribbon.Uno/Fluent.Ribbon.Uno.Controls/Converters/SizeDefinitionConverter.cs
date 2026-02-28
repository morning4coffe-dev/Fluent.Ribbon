namespace Fluent.Converters;

using Fluent.Data;

/// <summary>
/// Converts a string representation to a <see cref="RibbonControlSizeDefinition"/>.
/// Used for XAML attribute parsing.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// WPF uses TypeConverter; Uno uses IValueConverter for XAML binding support.
/// </remarks>
public class SizeDefinitionConverter : IValueConverter
{
    /// <summary>
    /// Default instance.
    /// </summary>
    public static readonly SizeDefinitionConverter Default = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        // Pass through if already the right type
        if (value is RibbonControlSizeDefinition)
        {
            return value;
        }

        if (value is string s)
        {
            return RibbonControlSizeDefinition.FromString(s);
        }

        return null;
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        return value?.ToString();
    }
}
