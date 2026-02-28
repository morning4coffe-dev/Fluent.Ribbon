namespace Fluent.Extensions;

/// <summary>
/// Extension methods for <see cref="System.Collections.IList"/>.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public static class IListExtensions
{
    /// <summary>
    /// Returns the list itself if it is not <c>null</c>, or an empty array.
    /// </summary>
    /// <param name="list">The list to check.</param>
    /// <returns>The original list, or an empty <see cref="System.Collections.ArrayList"/> if <c>null</c>.</returns>
    public static System.Collections.IList NullSafe(this System.Collections.IList? list)
    {
        return list ?? new System.Collections.ArrayList(0);
    }
}
