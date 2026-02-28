namespace Fluent.Internal;

/// <summary>
/// Utility methods for keyboard event handling.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WPF version uses P/Invoke (GetKeyboardState, MapVirtualKey, ToUnicode);
/// Uno version uses VirtualKey mapping.
/// </remarks>
public static class KeyEventUtility
{
    /// <summary>
    /// Gets a string representation of the specified virtual key.
    /// </summary>
    /// <param name="key">The virtual key.</param>
    /// <returns>A string representing the key, or an empty string if not representable.</returns>
    public static string GetStringFromKey(Windows.System.VirtualKey key)
    {
        // Map common alphanumeric keys
        if (key >= Windows.System.VirtualKey.A && key <= Windows.System.VirtualKey.Z)
        {
            return ((char)('A' + (key - Windows.System.VirtualKey.A))).ToString();
        }

        if (key >= Windows.System.VirtualKey.Number0 && key <= Windows.System.VirtualKey.Number9)
        {
            return ((char)('0' + (key - Windows.System.VirtualKey.Number0))).ToString();
        }

        if (key >= Windows.System.VirtualKey.NumberPad0 && key <= Windows.System.VirtualKey.NumberPad9)
        {
            return ((char)('0' + (key - Windows.System.VirtualKey.NumberPad0))).ToString();
        }

        return key switch
        {
            Windows.System.VirtualKey.Space => " ",
            Windows.System.VirtualKey.Enter => "\r",
            Windows.System.VirtualKey.Tab => "\t",
            _ => string.Empty,
        };
    }
}
