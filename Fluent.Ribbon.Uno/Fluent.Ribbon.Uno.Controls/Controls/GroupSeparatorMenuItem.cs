namespace Fluent;

/// <summary>
/// Represents a separator menu item that displays a group header text.
/// Used in dropdown menus and galleries to group items visually.
/// </summary>
public partial class GroupSeparatorMenuItem : MenuItem
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(GroupSeparatorMenuItem),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the group header text.
    /// </summary>
    public new string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupSeparatorMenuItem"/> class.
    /// </summary>
    public GroupSeparatorMenuItem()
    {
        DefaultStyleKey = typeof(GroupSeparatorMenuItem);
        IsTabStop = false;
    }

    #endregion
}
