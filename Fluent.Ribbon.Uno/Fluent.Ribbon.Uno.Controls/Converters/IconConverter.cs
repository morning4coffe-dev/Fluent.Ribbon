namespace Fluent.Converters;

/// <summary>
/// Converts an object to an <see cref="Microsoft.UI.Xaml.Controls.Image"/>,
/// falling back to the application icon if the source is null.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, simplified for Uno/WinUI.
/// The WPF version uses Win32 P/Invoke to retrieve window/class icons.
/// This Uno version simply delegates to <see cref="ObjectToImageConverter"/>.
/// </remarks>
public class IconConverter : ObjectToImageConverter
{
    /// <summary>
    /// New default instance.
    /// </summary>
    public static new readonly IconConverter Default = new();

    /// <summary>Initializes an empty converter.</summary>
    public IconConverter()
    {
    }

    /// <summary>Initializes a converter for an icon binding.</summary>
    public IconConverter(Binding iconBinding)
        : base(iconBinding, new Windows.Foundation.Size(16, 16))
    {
    }

    /// <summary>Initializes a converter for an icon and target-visual binding.</summary>
    public IconConverter(Binding iconBinding, Binding targetVisualBinding)
        : base(
            iconBinding,
            new Windows.Foundation.Size(16, 16),
            targetVisualBinding)
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
}
