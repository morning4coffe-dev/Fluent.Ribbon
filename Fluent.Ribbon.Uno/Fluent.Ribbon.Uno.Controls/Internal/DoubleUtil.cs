namespace Fluent.Internal;

/// <summary>
/// Utility methods for floating-point comparisons.
/// </summary>
internal static class DoubleUtil
{
    // Smallest value such that 1.0 + DBL_EPSILON != 1.0
    private const double DBL_EPSILON = 2.2204460492503131e-016;

    /// <summary>
    /// Returns whether or not two doubles are "close" (within epsilon proportional to their magnitudes).
    /// </summary>
    /// <param name="value1">The first double to compare.</param>
    /// <param name="value2">The second double to compare.</param>
    /// <returns>True if the values are close; otherwise, false.</returns>
    public static bool AreClose(double value1, double value2)
    {
        if (value1 == value2)
        {
            return true;
        }

        var eps = (Math.Abs(value1) + Math.Abs(value2) + 10.0) * DBL_EPSILON;
        var delta = value1 - value2;
        return (-eps < delta) && (eps > delta);
    }

    /// <summary>
    /// Returns whether or not the first double is strictly greater than (and not within epsilon of) the second.
    /// </summary>
    /// <param name="value1">The first double to compare.</param>
    /// <param name="value2">The second double to compare.</param>
    /// <returns>True if the first value is greater than the second; otherwise, false.</returns>
    public static bool GreaterThan(double value1, double value2)
    {
        return (value1 > value2) && !AreClose(value1, value2);
    }
}
