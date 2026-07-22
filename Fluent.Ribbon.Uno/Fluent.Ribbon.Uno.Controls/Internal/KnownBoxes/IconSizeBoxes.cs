namespace Fluent.Internal.KnownBoxes;

/// <summary>
/// Contains boxed values for <see cref="IconSize"/>.
/// </summary>
public static class IconSizeBoxes
{
    /// <summary>Boxed <see cref="IconSize.Small"/>.</summary>
    public static readonly object Small = IconSize.Small;

    /// <summary>Boxed <see cref="IconSize.Medium"/>.</summary>
    public static readonly object Medium = IconSize.Medium;

    /// <summary>Boxed <see cref="IconSize.Large"/>.</summary>
    public static readonly object Large = IconSize.Large;

    /// <summary>Boxed <see cref="IconSize.Custom"/>.</summary>
    public static readonly object Custom = IconSize.Custom;

    /// <summary>
    /// Returns the cached boxed value.
    /// </summary>
    public static object Box(IconSize iconSize)
    {
        return iconSize switch
        {
            IconSize.Small => Small,
            IconSize.Medium => Medium,
            IconSize.Large => Large,
            IconSize.Custom => Custom,
            _ => throw new ArgumentOutOfRangeException(nameof(iconSize), iconSize, null),
        };
    }
}
