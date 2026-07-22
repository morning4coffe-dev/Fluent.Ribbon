namespace Fluent;

/// <summary>
/// Represents a visual separator between tab items in the ribbon tab strip.
/// </summary>
public partial class SeparatorTabItem : TabViewItem
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SeparatorTabItem"/> class.
    /// </summary>
    public SeparatorTabItem()
    {
        DefaultStyleKey = typeof(SeparatorTabItem);
        IsTabStop = false;
        Width = 1;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    #endregion
}
