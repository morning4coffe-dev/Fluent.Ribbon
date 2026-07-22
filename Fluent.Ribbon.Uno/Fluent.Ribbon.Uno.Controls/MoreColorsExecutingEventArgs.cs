namespace Fluent;

/// <summary>
/// Provides the color selected by a synchronous custom-color picker.
/// </summary>
public class MoreColorsExecutingEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the selected color.
    /// </summary>
    public Windows.UI.Color Color { get; set; }

    /// <summary>
    /// Gets or sets whether applying the selected color is canceled.
    /// </summary>
    public bool Canceled { get; set; }
}
