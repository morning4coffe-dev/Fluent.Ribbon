namespace Fluent;

/// <summary>
/// Represents a control that has a drop down popup/flyout.
/// </summary>
public interface IDropDownControl
{
    /// <summary>
    /// Gets the popup used by controls that expose a WinUI <see cref="Popup"/>.
    /// </summary>
    Popup? DropDownPopup => this is InRibbonGallery gallery ? gallery.DropDownPopup : null;

    /// <summary>
    /// Gets or sets whether the control's context menu is open.
    /// </summary>
    bool IsContextMenuOpened
    {
        get => ContextMenuStates.GetOrCreateValue(this).IsOpen;
        set => ContextMenuStates.GetOrCreateValue(this).IsOpen = value;
    }

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

    private static System.Runtime.CompilerServices.ConditionalWeakTable<IDropDownControl, ContextMenuState>
        ContextMenuStates { get; } = new();

    private sealed class ContextMenuState
    {
        public ContextMenuState()
        {
        }

        public bool IsOpen { get; set; }
    }
}
