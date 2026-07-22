namespace Fluent;

/// <summary>
/// Represents the size of a ribbon control.
/// </summary>
public enum RibbonControlSize
{
    /// <summary>
    /// Large size with icon and text stacked vertically.
    /// </summary>
    Large = 0,

    /// <summary>
    /// Medium size with icon and text side by side.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// WPF-compatible name for <see cref="Medium"/>.
    /// </summary>
    Middle = Medium,

    /// <summary>
    /// Small size with icon only.
    /// </summary>
    Small = 2
}
