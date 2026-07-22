namespace Fluent.Internal;

/// <summary>
/// Provides well-known boxed values for common types to reduce boxing allocations.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. In WinUI boxing is less of a concern than WPF
/// since most property system paths handle boxing internally.
/// Still useful for frequently used callback values.
/// </remarks>
public static class KnownBoxValues
{
    /// <summary>Boxed <c>true</c> value.</summary>
    public static readonly object TrueBox = true;

    /// <summary>Boxed <c>false</c> value.</summary>
    public static readonly object FalseBox = false;

    /// <summary>Boxed <see cref="Visibility.Visible"/>.</summary>
    public static readonly object VisibleBox = Visibility.Visible;

    /// <summary>Boxed <see cref="Visibility.Collapsed"/>.</summary>
    public static readonly object CollapsedBox = Visibility.Collapsed;

    /// <summary>Boxed zero (double).</summary>
    public static readonly object DoubleZeroBox = 0.0;

    /// <summary>Boxed zero (int).</summary>
    public static readonly object IntZeroBox = 0;

    /// <summary>
    /// Returns the boxed bool for the given value.
    /// </summary>
    public static object Box(bool value)
    {
        return value ? TrueBox : FalseBox;
    }

    /// <summary>
    /// Returns the boxed visibility for the given value.
    /// </summary>
    public static object Box(Visibility value)
    {
        return value == Visibility.Visible ? VisibleBox : CollapsedBox;
    }
}
