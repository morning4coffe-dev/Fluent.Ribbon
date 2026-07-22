namespace Fluent.Converters;

using System.ComponentModel;
using System.Globalization;

/// <summary>
/// Converts a string representation to a <see cref="RibbonGroupBoxStateDefinition"/>.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public class RibbonGroupBoxStateDefinitionConverter : TypeConverter, IValueConverter
{
    /// <summary>
    /// Default instance.
    /// </summary>
    public static readonly RibbonGroupBoxStateDefinitionConverter Default = new();

    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc />
    public override object ConvertFrom(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object value)
    {
        return value is string text
            ? RibbonGroupBoxStateDefinition.FromString(text)
            : base.ConvertFrom(context, culture, value)
              ?? throw new NotSupportedException(
                  $"Cannot convert {value.GetType().FullName} to {nameof(RibbonGroupBoxStateDefinition)}.");
    }

    /// <inheritdoc />
    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType)
    {
        if (destinationType == typeof(string)
            && value is RibbonGroupBoxStateDefinition definition)
        {
            return (string)definition;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

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
