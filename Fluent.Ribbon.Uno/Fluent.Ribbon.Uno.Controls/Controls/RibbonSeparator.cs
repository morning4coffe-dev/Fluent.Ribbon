namespace Fluent;

/// <summary>
/// Represents a visual separator between ribbon controls or groups.
/// </summary>
public partial class RibbonSeparator : Control
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(RibbonSeparator),
            new PropertyMetadata(Orientation.Vertical));

    /// <summary>
    /// Gets or sets the orientation of the separator.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonSeparator"/> class.
    /// </summary>
    public RibbonSeparator()
    {
        DefaultStyleKey = typeof(RibbonSeparator);
    }

    #endregion
}
