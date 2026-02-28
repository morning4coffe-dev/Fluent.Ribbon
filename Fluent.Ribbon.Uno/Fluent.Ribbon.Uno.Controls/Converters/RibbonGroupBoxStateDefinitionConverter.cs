namespace Fluent.Converters;

using Fluent.Data;

/// <summary>
/// Converts a string representation to a <see cref="RibbonGroupBoxStateDefinition"/>.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public class RibbonGroupBoxStateDefinitionConverter : IValueConverter
{
    /// <summary>
    /// Default instance.
    /// </summary>
    public static readonly RibbonGroupBoxStateDefinitionConverter Default = new();

    /// <inheritdoc/>
    public object? Convert(object? value, Type targetType, object? parameter, string language)
    {
        if (value is RibbonGroupBoxStateDefinition)
        {
            return value;
        }

        if (value is string s)
        {
            return RibbonGroupBoxStateDefinition.FromString(s);
        }

        return new RibbonGroupBoxStateDefinition();
    }

    /// <inheritdoc/>
    public object? ConvertBack(object? value, Type targetType, object? parameter, string language)
    {
        return value?.ToString();
    }
}
