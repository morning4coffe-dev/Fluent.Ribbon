namespace Fluent.Data;

/// <summary>
/// Stores the result of a key tip press.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public enum KeyTipPressedResult
{
    /// <summary>
    /// The key tip was not handled.
    /// </summary>
    None,

    /// <summary>
    /// The key tip was handled and the control was activated.
    /// </summary>
    Activated,

    /// <summary>
    /// The key tip was handled and a submenu was opened.
    /// </summary>
    SubmenuOpened,
}
