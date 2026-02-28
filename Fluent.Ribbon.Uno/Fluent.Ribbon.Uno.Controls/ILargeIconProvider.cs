namespace Fluent;

/// <summary>
/// Defines a control that provides a large icon.
/// </summary>
public interface ILargeIconProvider
{
    /// <summary>
    /// Gets or sets the large icon (32x32).
    /// </summary>
    ImageSource? LargeIcon { get; set; }
}
