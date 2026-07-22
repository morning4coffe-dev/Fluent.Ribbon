namespace Fluent;

/// <summary>
/// Represents the result of <see cref="IKeyTipedControl.OnKeyTipPressed"/>.
/// </summary>
public class KeyTipPressedResult : EventArgs
{
    /// <summary>
    /// Gets an empty result for controls that do not acquire focus or open a popup.
    /// </summary>
    public static new readonly KeyTipPressedResult Empty = new();

    private KeyTipPressedResult()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyTipPressedResult"/> class.
    /// </summary>
    public KeyTipPressedResult(bool pressedElementAquiredFocus, bool pressedElementOpenedPopup)
    {
        PressedElementAquiredFocus = pressedElementAquiredFocus;
        PressedElementOpenedPopup = pressedElementOpenedPopup;
    }

    /// <summary>
    /// Gets whether the activated element acquired focus.
    /// </summary>
    public bool PressedElementAquiredFocus { get; }

    /// <summary>
    /// Gets whether the activated element opened a popup.
    /// </summary>
    public bool PressedElementOpenedPopup { get; }
}
