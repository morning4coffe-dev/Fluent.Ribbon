namespace Fluent;

/// <summary>
/// Defines a control that provides a medium icon.
/// </summary>
public interface IMediumIconProvider
{
    /// <summary>
    /// Gets or sets the medium icon (16x16).
    /// </summary>
    ImageSource? MediumIcon { get; set; }
}
