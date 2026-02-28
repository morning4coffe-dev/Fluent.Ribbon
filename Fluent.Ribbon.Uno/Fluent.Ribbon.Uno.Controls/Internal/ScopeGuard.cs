namespace Fluent.Internal;

/// <summary>
/// A disposable reentrancy guard. Prevents recursive calls from re-entering critical sections.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. Platform-agnostic utility.
/// </remarks>
public sealed class ScopeGuard : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the guard is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Starts the scope guard. Returns itself so it can be used in a <c>using</c> statement.
    /// </summary>
    /// <returns>This <see cref="ScopeGuard"/> instance.</returns>
    public ScopeGuard Start()
    {
        IsActive = true;
        return this;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        IsActive = false;
    }
}
