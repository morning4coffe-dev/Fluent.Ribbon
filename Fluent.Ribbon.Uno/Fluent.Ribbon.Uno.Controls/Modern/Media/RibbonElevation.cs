namespace Fluent.Modern.Media;

using System.Numerics;

/// <summary>
/// <para><b>Modern extension</b> — attaches Fluent ThemeShadow elevation to modern ribbon surfaces.</para>
/// </summary>
[ModernExtension]
public static class RibbonElevation
{
    private static readonly ThemeShadow SharedShadow = new();

    #region Dependency Properties

    /// <summary>Identifies the Depth attached dependency property.</summary>
    public static readonly DependencyProperty DepthProperty =
        DependencyProperty.RegisterAttached(
            "Depth",
            typeof(double),
            typeof(RibbonElevation),
            new PropertyMetadata(0d, OnDepthChanged));

    #endregion

    #region Attached Property Accessors

    /// <summary>
    /// Gets the Fluent elevation depth for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The elevation depth in effective pixels.</returns>
    public static double GetDepth(DependencyObject element)
    {
        if (element is null)
        {
            return 0d;
        }

        return (double)element.GetValue(DepthProperty);
    }

    /// <summary>
    /// Sets the Fluent elevation depth for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The elevation depth in effective pixels.</param>
    public static void SetDepth(DependencyObject element, double value)
    {
        if (element is null)
        {
            return;
        }

        element.SetValue(DepthProperty, value);
    }

    #endregion

    #region Property Changed

    private static void OnDepthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        try
        {
            var depth = e.NewValue is double value && !double.IsNaN(value) && value > 0d ? value : 0d;
            if (depth > 0d)
            {
                element.Shadow = SharedShadow;
                element.Translation = new Vector3(0f, 0f, (float)depth);
            }
            else
            {
                element.Shadow = null;
                element.Translation = new Vector3(element.Translation.X, element.Translation.Y, 0f);
            }
        }
        catch
        {
            // Modern helpers must never throw into app code.
        }
    }

    #endregion
}
