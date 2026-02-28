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
}
