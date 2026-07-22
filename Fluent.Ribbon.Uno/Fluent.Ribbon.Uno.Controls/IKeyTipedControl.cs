namespace Fluent;

/// <summary>
/// Base interface for controls that support key tips.
/// </summary>
public interface IKeyTipedControl
{
    /// <summary>
    /// Gets or sets the key tip for keyboard navigation.
    /// </summary>
    string? KeyTip { get; set; }

    /// <summary>
    /// Handles key tip pressed.
    /// </summary>
    KeyTipPressedResult OnKeyTipPressed();

    /// <summary>
    /// Handles back navigation with key tips.
    /// </summary>
    void OnKeyTipBack();
}
