namespace Fluent;

using System.Reflection;

/// <summary>
/// Provides WPF-compatible framework metadata and layout-rounding attached properties.
/// </summary>
public static class FrameworkHelper
{
    /// <summary>
    /// Gets the version of the XAML framework used by the Uno application.
    /// </summary>
    public static readonly Version PresentationFrameworkVersion =
        typeof(FrameworkElement).Assembly.GetName().Version ?? new Version();

    /// <summary>
    /// Gets whether layout rounding is enabled for an element.
    /// </summary>
    public static bool GetUseLayoutRounding(DependencyObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return (bool)obj.GetValue(UseLayoutRoundingProperty);
    }

    /// <summary>
    /// Sets whether layout rounding is enabled for an element.
    /// </summary>
    public static void SetUseLayoutRounding(DependencyObject obj, bool value)
    {
        ArgumentNullException.ThrowIfNull(obj);
        obj.SetValue(UseLayoutRoundingProperty, value);
    }

    /// <summary>
    /// Identifies the UseLayoutRounding attached property.
    /// </summary>
    public static readonly DependencyProperty UseLayoutRoundingProperty =
        DependencyProperty.RegisterAttached(
            "UseLayoutRounding",
            typeof(bool),
            typeof(FrameworkHelper),
            new PropertyMetadata(false, OnUseLayoutRoundingChanged));

    private static void OnUseLayoutRoundingChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element)
        {
            element.UseLayoutRounding = (bool)e.NewValue;
        }
    }
}
