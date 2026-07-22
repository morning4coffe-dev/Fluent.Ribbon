namespace Fluent;

/// <summary>
/// Represents the color layout mode of a <see cref="ColorGallery"/>.
/// </summary>
public enum ColorGalleryMode
{
    /// <summary>
    /// A compact palette of highlight colors (as used for text highlighting).
    /// </summary>
    HighlightColors = 0,

    /// <summary>
    /// A grid of standard colors.
    /// </summary>
    StandardColors,

    /// <summary>
    /// Theme colors (with tint/shade variants) plus standard colors.
    /// </summary>
    ThemeColors,
}
