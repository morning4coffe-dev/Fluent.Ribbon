namespace Fluent.Converters;

using System.Globalization;
using Windows.Foundation;

/// <summary>
/// WPF-compatible constructors and conversion overloads.
/// </summary>
public partial class ObjectToImageConverter
{
    /// <summary>Initializes an empty converter.</summary>
    public ObjectToImageConverter()
    {
    }

    /// <summary>Initializes a converter for the supplied input.</summary>
    public ObjectToImageConverter(object input)
        : this(input, new Size(0, 0), null)
    {
    }

    /// <summary>Initializes a converter with the desired image size.</summary>
    public ObjectToImageConverter(object input, Size desiredSize)
        : this(input, desiredSize, null)
    {
    }

    /// <summary>Initializes a converter with a value or binding for the desired size.</summary>
    public ObjectToImageConverter(object input, object desiredSize)
        : this(input, desiredSize, null)
    {
    }

    /// <summary>Initializes a converter with input, desired-size, and target bindings.</summary>
    public ObjectToImageConverter(
        object input,
        object desiredSize,
        Binding? targetVisualBinding)
    {
        if (desiredSize is Size size
            && (size.Width < 0 || size.Height < 0))
        {
            throw new ArgumentException(
                "DesiredSize width and height must not be negative.",
                nameof(desiredSize));
        }

        IconBinding = input as Binding ?? new Binding { Source = input };
        DesiredSizeBinding = desiredSize as Binding ?? new Binding { Source = desiredSize };
        TargetVisualBinding = targetVisualBinding;
    }

    /// <summary>Gets or sets the target visual binding.</summary>
    public Binding? TargetVisualBinding { get; set; }

    /// <summary>Gets or sets the icon binding.</summary>
    public Binding? IconBinding { get; set; }

    /// <summary>Gets or sets the desired-size binding.</summary>
    public Binding? DesiredSizeBinding { get; set; }

    /// <summary>WPF-compatible culture-based conversion overload.</summary>
    public virtual object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo? culture)
    {
        return Convert(value, targetType, parameter, culture?.Name ?? string.Empty);
    }

    /// <summary>WPF-compatible culture-based reverse conversion overload.</summary>
    public virtual object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }

    /// <summary>Converts a multi-value input.</summary>
    public virtual object? Convert(
        object[] values,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (values is null || values.Length == 0)
        {
            return null;
        }

        var desiredSize = values.Length > 1
            ? ParseCompatibilitySize(values[1])
            : ParseCompatibilitySize(parameter);
        var targetVisual = values.OfType<UIElement>().FirstOrDefault();
        var value = GetValueToConvert(values[0], desiredSize, targetVisual);

        return Convert(value, targetType, desiredSize, culture);
    }

    /// <summary>Multi-value reverse conversion is not supported.</summary>
    public virtual object[] ConvertBack(
        object value,
        Type[] targetTypes,
        object parameter,
        CultureInfo culture)
    {
        return targetTypes.Select(_ => DependencyProperty.UnsetValue).ToArray();
    }

    /// <summary>Returns the value to convert.</summary>
    protected virtual object? GetValueToConvert(
        object? value,
        Size desiredSize,
        UIElement? targetVisual)
    {
        return value;
    }

    /// <summary>Returns the converter for markup-extension compatibility.</summary>
    public virtual object ProvideValue(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        return this;
    }

    /// <inheritdoc />
    protected override object ProvideValue()
    {
        return this;
    }

    /// <inheritdoc />
    protected override object ProvideValue(IXamlServiceProvider serviceProvider)
    {
        return this;
    }

    /// <summary>Creates an image source without WPF freezing semantics.</summary>
    public static ImageSource? CreateFrozenImageSource(object? value, Size desiredSize)
    {
        return CreateImageSource(value, desiredSize);
    }

    /// <summary>Creates an image source without WPF freezing semantics.</summary>
    public static ImageSource? CreateFrozenImageSource(
        object? value,
        UIElement? targetVisual,
        Size desiredSize)
    {
        return CreateImageSource(value, targetVisual, desiredSize);
    }

    /// <summary>Creates an image source from a supported value.</summary>
    public static ImageSource? CreateImageSource(object? value, Size desiredSize)
    {
        return ConvertToImageSource(value);
    }

    /// <summary>Creates an image source from a supported value.</summary>
    public static ImageSource? CreateImageSource(
        object? value,
        UIElement? targetVisual,
        Size desiredSize)
    {
        return ConvertToImageSource(value);
    }

    /// <summary>Returns the desired size adjusted for the target visual scale.</summary>
    protected static Size GetScaledDesiredSize(
        Size desiredSize,
        UIElement? targetVisual)
    {
        var scale = targetVisual?.XamlRoot?.RasterizationScale ?? 1.0;
        return new Size(desiredSize.Width * scale, desiredSize.Height * scale);
    }

    private static Size ParseCompatibilitySize(object? value)
    {
        return value switch
        {
            Size size => size,
            double number => new Size(number, number),
            int number => new Size(number, number),
            string text when double.TryParse(
                text,
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var number) => new Size(number, number),
            _ => new Size(0, 0),
        };
    }
}
