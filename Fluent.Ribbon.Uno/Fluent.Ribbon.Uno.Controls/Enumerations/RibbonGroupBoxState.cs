namespace Fluent;

/// <summary>
/// Represents the different states of a <see cref="RibbonGroupBox"/>.
/// </summary>
public enum RibbonGroupBoxState
{
    /// <summary>
    /// Large state - all controls are at their largest size.
    /// </summary>
    Large = 0,

    /// <summary>
    /// Medium state - controls use medium-sized icons.
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Small state - controls use small icons without labels.
    /// </summary>
    Small = 2,

    /// <summary>
    /// Collapsed state - the group is collapsed into a popup.
    /// </summary>
    Collapsed = 3,

    /// <summary>
    /// QuickAccess state - the group is displayed in the quick access toolbar.
    /// </summary>
    QuickAccess = 4
}
