namespace Fluent;

/// <summary>
/// Defines a single control in a <see cref="RibbonToolBarLayoutDefinition"/>.
/// Specifies what control to target and at what size to display it.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public partial class RibbonToolBarControlDefinition : DependencyObject
{
    /// <summary>Identifies the <see cref="Target"/> dependency property.</summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(string),
            typeof(RibbonToolBarControlDefinition),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the name of the target control.
    /// </summary>
    public string? Target
    {
        get => (string?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonToolBarControlDefinition),
            new PropertyMetadata(RibbonControlSize.Small));

    /// <summary>
    /// Gets or sets the size at which to display the control.
    /// </summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="Width"/> dependency property.</summary>
    public static readonly DependencyProperty WidthProperty =
        DependencyProperty.Register(
            nameof(Width),
            typeof(double),
            typeof(RibbonToolBarControlDefinition),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Gets or sets an optional specific width for the control.
    /// </summary>
    public double Width
    {
        get => (double)GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }
}
