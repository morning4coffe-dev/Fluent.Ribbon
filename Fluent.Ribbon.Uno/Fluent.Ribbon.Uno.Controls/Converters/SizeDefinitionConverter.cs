namespace Fluent.Converters;

using System.ComponentModel;
using System.Globalization;

/// <summary>
/// Converts a string representation to a <see cref="RibbonControlSizeDefinition"/>.
/// Used for XAML attribute parsing.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// WPF uses TypeConverter; Uno uses IValueConverter for XAML binding support.
/// </remarks>
public class SizeDefinitionConverter : TypeConverter, IValueConverter
{
    /// <summary>
    /// Default instance.
    /// </summary>
    public static readonly SizeDefinitionConverter Default = new();

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
            ? RibbonControlSizeDefinition.FromString(text)
            : base.ConvertFrom(context, culture, value)
              ?? throw new NotSupportedException(
                  $"Cannot convert {value.GetType().FullName} to {nameof(RibbonControlSizeDefinition)}.");
    }

    /// <inheritdoc />
    public override object? ConvertTo(
        ITypeDescriptorContext? context,
        CultureInfo? culture,
        object? value,
        Type destinationType)
    {
        if (destinationType == typeof(string)
            && value is RibbonControlSizeDefinition definition)
        {
            return (string)definition;
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }

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
