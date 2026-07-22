namespace Fluent;

/// <summary>
/// Specifies when a popup should be dismissed.
/// </summary>
public enum DismissPopupMode
{
    /// <summary>
    /// Always dismiss the popup.
    /// </summary>
    Always,

    /// <summary>
    /// Dismiss only when the pointer is not over the popup.
    /// </summary>
    MouseNotOver,
}

/// <summary>
/// Describes why a popup is being dismissed.
/// </summary>
public enum DismissPopupReason
{
    /// <summary>
    /// No reason was supplied.
    /// </summary>
    Undefined,

    /// <summary>
    /// The application lost focus.
    /// </summary>
    ApplicationLostFocus,

    /// <summary>
    /// KeyTips are being shown.
    /// </summary>
    ShowingKeyTips,
}

/// <summary>
/// Provides popup-dismissal details.
/// </summary>
public class DismissPopupEventArgs : RoutedEventArgs
{
    /// <summary>
    /// Initializes a new instance with <see cref="DismissPopupMode.Always"/>.
    /// </summary>
    public DismissPopupEventArgs()
        : this(DismissPopupMode.Always)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified dismissal mode.
    /// </summary>
    public DismissPopupEventArgs(DismissPopupMode dismissMode)
        : this(dismissMode, DismissPopupReason.Undefined)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified mode and reason.
    /// </summary>
    public DismissPopupEventArgs(
        DismissPopupMode dismissMode,
        DismissPopupReason reason)
    {
        DismissMode = dismissMode;
        DismissReason = reason;
    }

    /// <summary>
    /// Gets the dismissal mode.
    /// </summary>
    public DismissPopupMode DismissMode { get; }

    /// <summary>
    /// Gets or sets the dismissal reason.
    /// </summary>
    public DismissPopupReason DismissReason { get; set; }

    /// <summary>
    /// Invokes a WPF-compatible popup-dismissal handler.
    /// </summary>
    protected virtual void InvokeEventHandler(
        Delegate genericHandler,
        object genericTarget)
    {
        ((EventHandler<DismissPopupEventArgs>)genericHandler)(genericTarget, this);
    }
}
