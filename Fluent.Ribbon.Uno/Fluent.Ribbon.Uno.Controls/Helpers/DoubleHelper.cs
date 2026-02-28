namespace Fluent.Helpers;

/// <summary>
/// Helper methods for <see cref="double"/> comparison and validation.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. Complements <see cref="Internal.DoubleUtil"/> and
/// <see cref="Extensions.DoubleExtensions"/> with additional helper patterns.
/// </remarks>
public static class DoubleHelper
{
    /// <summary>
    /// Returns the value clamped between min and max.
    /// </summary>
    public static double Clamp(double value, double min, double max)
    {
        if (value < min)
        {
            return min;
        }

        if (value > max)
        {
            return max;
        }

        return value;
    }

    /// <summary>
    /// Returns true if the value is a valid finite number (not NaN, not infinity).
    /// </summary>
    public static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    /// <summary>
    /// Returns the value if it's finite, otherwise returns the fallback.
    /// </summary>
    public static double GetFiniteOrDefault(double value, double fallback = 0.0)
    {
        return IsFinite(value) ? value : fallback;
    }
}
