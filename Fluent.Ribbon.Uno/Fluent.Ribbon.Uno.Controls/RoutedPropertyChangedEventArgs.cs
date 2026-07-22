namespace Fluent;

/// <summary>Provides old and new values for a routed property change.</summary>
public class RoutedPropertyChangedEventArgs<T> : RoutedEventArgs
{
    /// <summary>Initializes a new instance.</summary>
    public RoutedPropertyChangedEventArgs(T oldValue, T newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>Gets the old value.</summary>
    public T OldValue { get; }

    /// <summary>Gets the new value.</summary>
    public T NewValue { get; }
}

/// <summary>Handles a routed property change.</summary>
public delegate void RoutedPropertyChangedEventHandler<T>(
    object sender,
    RoutedPropertyChangedEventArgs<T> e);
