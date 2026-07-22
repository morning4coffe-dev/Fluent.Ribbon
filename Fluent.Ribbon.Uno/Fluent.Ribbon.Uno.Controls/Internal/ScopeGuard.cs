namespace Fluent.Internal;

/// <summary>
/// A disposable reentrancy guard. Prevents recursive calls from re-entering critical sections.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. Platform-agnostic utility.
/// </remarks>
public class ScopeGuard : IDisposable
{
    private readonly Action? onEntry;
    private readonly Action? onDispose;

    /// <summary>Initializes an empty scope guard.</summary>
    public ScopeGuard()
    {
    }

    /// <summary>Initializes a scope guard with entry and disposal actions.</summary>
    public ScopeGuard(Action onEntry, Action onDispose)
    {
        this.onEntry = onEntry ?? throw new ArgumentNullException(nameof(onEntry));
        this.onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));
    }

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
        if (IsActive)
        {
            return this;
        }

        IsActive = true;
        onEntry?.Invoke();
        return this;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        var wasActive = IsActive;
        IsActive = false;
        if (wasActive)
        {
            onDispose?.Invoke();
        }
    }
}
