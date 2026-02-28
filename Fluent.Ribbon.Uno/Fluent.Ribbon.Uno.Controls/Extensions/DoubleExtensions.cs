namespace Fluent.Extensions;

using Fluent.Internal;

/// <summary>
/// Extension methods for <see cref="double"/>.
/// </summary>
internal static class DoubleExtensions
{
    /// <summary>
    /// Returns whether or not the two values are "close" (within floating-point epsilon).
    /// </summary>
    public static bool AlmostEquals(this double x, double y)
    {
        return DoubleUtil.AreClose(x, y);
    }

    /// <summary>
    /// Returns 0 if the value is infinity or NaN, otherwise returns the value.
    /// </summary>
    public static double GetZeroIfInfinityOrNaN(this double doubleValue)
    {
        if (double.IsInfinity(doubleValue)
            || double.IsNaN(doubleValue))
        {
            return 0;
        }

        return doubleValue;
    }
}
