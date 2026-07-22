namespace Fluent;

/// <summary>
/// Attached properties for controls with a text-input part.
/// </summary>
public partial class InputControlProperties : DependencyObject
{
    /// <summary>
    /// Identifies the input-width attached property.
    /// </summary>
    public static readonly DependencyProperty InputWidthProperty =
        DependencyProperty.RegisterAttached(
            "InputWidth",
            typeof(double),
            typeof(InputControlProperties),
            new PropertyMetadata(double.NaN));

    /// <summary>
    /// Sets the input width.
    /// </summary>
    public static void SetInputWidth(DependencyObject element, double value)
    {
        element.SetValue(InputWidthProperty, value);
    }

    /// <summary>
    /// Gets the input width.
    /// </summary>
    public static double GetInputWidth(DependencyObject element)
    {
        return (double)element.GetValue(InputWidthProperty);
    }

    /// <summary>
    /// Identifies the minimum input-width attached property.
    /// </summary>
    public static readonly DependencyProperty InputMinWidthProperty =
        DependencyProperty.RegisterAttached(
            "InputMinWidth",
            typeof(double),
            typeof(InputControlProperties),
            new PropertyMetadata(0D));

    /// <summary>
    /// Sets the minimum input width.
    /// </summary>
    public static void SetInputMinWidth(DependencyObject element, double value)
    {
        element.SetValue(InputMinWidthProperty, value);
    }

    /// <summary>
    /// Gets the minimum input width.
    /// </summary>
    public static double GetInputMinWidth(DependencyObject element)
    {
        return (double)element.GetValue(InputMinWidthProperty);
    }

    /// <summary>
    /// Identifies the input-height attached property.
    /// </summary>
    public static readonly DependencyProperty InputHeightProperty =
        DependencyProperty.RegisterAttached(
            "InputHeight",
            typeof(double),
            typeof(InputControlProperties),
            new PropertyMetadata(22D));

    /// <summary>
    /// Sets the input height.
    /// </summary>
    public static void SetInputHeight(DependencyObject element, double value)
    {
        element.SetValue(InputHeightProperty, value);
    }

    /// <summary>
    /// Gets the input height.
    /// </summary>
    public static double GetInputHeight(DependencyObject element)
    {
        return (double)element.GetValue(InputHeightProperty);
    }
}
