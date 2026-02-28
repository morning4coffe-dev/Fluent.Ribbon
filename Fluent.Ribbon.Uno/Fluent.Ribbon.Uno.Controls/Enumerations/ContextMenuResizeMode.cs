namespace Fluent;

/// <summary>
/// Represents context menu resize mode.
/// </summary>
public enum ContextMenuResizeMode
{
    /// <summary>
    /// Context menu can not be resized.
    /// </summary>
    None = 0,

    /// <summary>
    /// Context menu can be only resized vertically.
    /// </summary>
    Vertical,

    /// <summary>
    /// Context menu can be resized vertically and horizontally.
    /// </summary>
    Both,
}
