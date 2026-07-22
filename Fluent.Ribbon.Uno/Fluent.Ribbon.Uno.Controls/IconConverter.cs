namespace Fluent;

/// <summary>
/// Provides the WPF-compatible namespace for the icon converter.
/// </summary>
public class IconConverter : Converters.IconConverter
{
    /// <summary>
    /// Gets a reusable converter instance.
    /// </summary>
    public static new readonly IconConverter Default = new();

    /// <summary>Initializes a converter.</summary>
    public IconConverter()
    {
    }

    /// <summary>Initializes a converter for an icon binding.</summary>
    public IconConverter(Binding iconBinding)
        : base(iconBinding)
    {
    }

    /// <summary>Initializes a converter for an icon and target-visual binding.</summary>
    public IconConverter(Binding iconBinding, Binding targetVisualBinding)
        : base(iconBinding, targetVisualBinding)
    {
    }

    /// <summary>Initializes a converter with explicit desired size.</summary>
    public IconConverter(
        Binding iconBinding,
        object desiredSize,
        Binding targetVisualBinding)
        : base(iconBinding, desiredSize, targetVisualBinding)
    {
    }

    /// <inheritdoc />
    protected override object? GetValueToConvert(
        object? value,
        Windows.Foundation.Size desiredSize,
        UIElement? targetVisual)
    {
        return base.GetValueToConvert(value, desiredSize, targetVisual);
    }
}
