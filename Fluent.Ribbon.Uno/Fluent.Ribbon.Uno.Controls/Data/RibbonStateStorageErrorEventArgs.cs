namespace Fluent;

/// <summary>Describes a ribbon state storage operation that could not be completed.</summary>
public sealed class RibbonStateStorageErrorEventArgs : EventArgs
{
    /// <summary>Creates event data for a failed storage operation.</summary>
    public RibbonStateStorageErrorEventArgs(
        RibbonStateStorageOperation operation,
        Exception exception)
    {
        Operation = operation;
        Exception = exception ?? throw new ArgumentNullException(nameof(exception));
    }

    /// <summary>Gets the operation that could not be completed.</summary>
    public RibbonStateStorageOperation Operation { get; }

    /// <summary>Gets the platform capability exception.</summary>
    public Exception Exception { get; }
}

/// <summary>Identifies a persistent ribbon state operation.</summary>
public enum RibbonStateStorageOperation
{
    /// <summary>Saving state.</summary>
    Save,

    /// <summary>Loading state.</summary>
    Load,

    /// <summary>Resetting state.</summary>
    Reset,
}
