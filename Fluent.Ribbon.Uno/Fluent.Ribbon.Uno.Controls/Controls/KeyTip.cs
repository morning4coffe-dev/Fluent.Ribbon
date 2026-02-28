namespace Fluent;

/// <summary>
/// Defines the attached properties for key tip functionality.
/// Key tips are Office-style keyboard accelerators that appear when Alt is pressed.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WPF uses a Label subclass; Uno uses a simple Control with attached properties.
/// </remarks>
public partial class KeyTip : Control
{
    #region Attached Properties

    /// <summary>Identifies the Keys attached property.</summary>
    public static readonly DependencyProperty KeysProperty =
        DependencyProperty.RegisterAttached(
            "Keys",
            typeof(string),
            typeof(KeyTip),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the key tip string for the specified element.
    /// </summary>
    public static string? GetKeys(DependencyObject element)
    {
        return (string?)element.GetValue(KeysProperty);
    }

    /// <summary>
    /// Sets the key tip string for the specified element.
    /// </summary>
    public static void SetKeys(DependencyObject element, string? value)
    {
        element.SetValue(KeysProperty, value);
    }

    /// <summary>Identifies the AutoPlacement attached property.</summary>
    public static readonly DependencyProperty AutoPlacementProperty =
        DependencyProperty.RegisterAttached(
            "AutoPlacement",
            typeof(bool),
            typeof(KeyTip),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets whether the key tip should be automatically placed.
    /// </summary>
    public static bool GetAutoPlacement(DependencyObject element)
    {
        return (bool)element.GetValue(AutoPlacementProperty);
    }

    /// <summary>
    /// Sets whether the key tip should be automatically placed.
    /// </summary>
    public static void SetAutoPlacement(DependencyObject element, bool value)
    {
        element.SetValue(AutoPlacementProperty, value);
    }

    /// <summary>Identifies the HorizontalAlignment attached property.</summary>
    public static new readonly DependencyProperty HorizontalAlignmentProperty =
        DependencyProperty.RegisterAttached(
            "HorizontalAlignment",
            typeof(HorizontalAlignment),
            typeof(KeyTip),
            new PropertyMetadata(HorizontalAlignment.Center));

    /// <summary>
    /// Gets the horizontal alignment of the key tip popup relative to its target.
    /// </summary>
    public static HorizontalAlignment GetHorizontalAlignment(DependencyObject element)
    {
        return (HorizontalAlignment)element.GetValue(HorizontalAlignmentProperty);
    }

    /// <summary>
    /// Sets the horizontal alignment of the key tip popup relative to its target.
    /// </summary>
    public static void SetHorizontalAlignment(DependencyObject element, HorizontalAlignment value)
    {
        element.SetValue(HorizontalAlignmentProperty, value);
    }

    /// <summary>Identifies the VerticalAlignment attached property.</summary>
    public static new readonly DependencyProperty VerticalAlignmentProperty =
        DependencyProperty.RegisterAttached(
            "VerticalAlignment",
            typeof(VerticalAlignment),
            typeof(KeyTip),
            new PropertyMetadata(VerticalAlignment.Bottom));

    /// <summary>
    /// Gets the vertical alignment of the key tip popup relative to its target.
    /// </summary>
    public static VerticalAlignment GetVerticalAlignment(DependencyObject element)
    {
        return (VerticalAlignment)element.GetValue(VerticalAlignmentProperty);
    }

    /// <summary>
    /// Sets the vertical alignment of the key tip popup relative to its target.
    /// </summary>
    public static void SetVerticalAlignment(DependencyObject element, VerticalAlignment value)
    {
        element.SetValue(VerticalAlignmentProperty, value);
    }

    /// <summary>Identifies the Margin attached property.</summary>
    public static new readonly DependencyProperty MarginProperty =
        DependencyProperty.RegisterAttached(
            "Margin",
            typeof(Thickness),
            typeof(KeyTip),
            new PropertyMetadata(new Thickness(0)));

    /// <summary>
    /// Gets the margin offset for the key tip popup.
    /// </summary>
    public static Thickness GetMargin(DependencyObject element)
    {
        return (Thickness)element.GetValue(MarginProperty);
    }

    /// <summary>
    /// Sets the margin offset for the key tip popup.
    /// </summary>
    public static void SetMargin(DependencyObject element, Thickness value)
    {
        element.SetValue(MarginProperty, value);
    }

    #endregion

    #region Instance Properties

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(KeyTip),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the key tip text displayed in the popup.
    /// </summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Identifies the <see cref="IsEnabled"/> dependency property.</summary>
    public static new readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(KeyTip),
            new PropertyMetadata(true));

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyTip"/> class.
    /// </summary>
    public KeyTip()
    {
        DefaultStyleKey = typeof(KeyTip);
    }

    #endregion
}
