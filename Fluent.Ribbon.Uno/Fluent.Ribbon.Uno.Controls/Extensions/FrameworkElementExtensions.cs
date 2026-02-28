namespace Fluent.Extensions;

/// <summary>
/// Extension methods for <see cref="FrameworkElement"/> layout management.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
public static class FrameworkElementExtensions
{
    /// <summary>
    /// Forces an immediate measure pass on the element.
    /// </summary>
    /// <param name="element">The element to measure.</param>
    public static void ForceMeasureImmediate(this FrameworkElement element)
    {
        element.InvalidateMeasure();
        element.UpdateLayout();
    }

    /// <summary>
    /// Invalidates both the measure and arrange passes.
    /// </summary>
    /// <param name="element">The element to invalidate.</param>
    public static void InvalidateMeasureAndArrange(this FrameworkElement element)
    {
        element.InvalidateMeasure();
        element.InvalidateArrange();
    }

    /// <summary>
    /// Forces an immediate measure and arrange pass on the element.
    /// </summary>
    /// <param name="element">The element to update.</param>
    public static void ForceMeasureAndArrangeImmediate(this FrameworkElement element)
    {
        element.InvalidateMeasure();
        element.InvalidateArrange();
        element.UpdateLayout();
    }
}
