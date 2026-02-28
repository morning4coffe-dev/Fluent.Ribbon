namespace Fluent;

/// <summary>
/// Represents a control that has a drop down popup/flyout.
/// </summary>
public interface IDropDownControl
{
    /// <summary>
    /// Gets or sets a value indicating whether the drop down is open.
    /// </summary>
    bool IsDropDownOpen { get; set; }

    /// <summary>
    /// Occurs when the drop down is opened.
    /// </summary>
    event EventHandler? DropDownOpened;

    /// <summary>
    /// Occurs when the drop down is closed.
    /// </summary>
    event EventHandler? DropDownClosed;
}
