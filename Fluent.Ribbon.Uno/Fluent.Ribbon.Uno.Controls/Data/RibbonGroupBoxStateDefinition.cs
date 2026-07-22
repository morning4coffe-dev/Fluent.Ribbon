namespace Fluent;

/// <summary>
/// Holds transitionable states when the ribbon automatically resizes the <see cref="RibbonGroupBox"/>.
/// </summary>
[System.ComponentModel.TypeConverter(typeof(Converters.RibbonGroupBoxStateDefinitionConverter))]
public readonly struct RibbonGroupBoxStateDefinition : IEquatable<RibbonGroupBoxStateDefinition>
{
    private const int MaxStateDefinitionParts = 4;

    private static readonly RibbonGroupBoxState[] defaultStates =
    [
        RibbonGroupBoxState.Large,
        RibbonGroupBoxState.Medium,
        RibbonGroupBoxState.Small,
        RibbonGroupBoxState.Collapsed
    ];

    private static readonly char[] stateDefinitionSeparators = [' ', ',', ';', '-', '>'];

    /// <summary>
    /// Creates a new instance from a string definition (e.g. "Large Medium Small Collapsed").
    /// </summary>
    public RibbonGroupBoxStateDefinition(string? stateDefinition)
        : this()
    {
        states = defaultStates;

        if (string.IsNullOrEmpty(stateDefinition))
        {
            return;
        }

        var parts = stateDefinition!.Split(stateDefinitionSeparators, MaxStateDefinitionParts, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return;
        }

        var newStates = new List<RibbonGroupBoxState>();
        foreach (var item in parts)
        {
            var state = ToRibbonGroupBoxState(item);
            if (!newStates.Contains(state) && state != RibbonGroupBoxState.QuickAccess)
            {
                newStates.Add(state);
            }

            if (newStates.Count >= MaxStateDefinitionParts)
            {
                break;
            }
        }

        if (newStates.Count > 0)
        {
            newStates.Sort();
            states = newStates.ToArray();
        }
    }

    /// <summary>
    /// Gets the transitionable states.
    /// </summary>
    public IReadOnlyList<RibbonGroupBoxState> States => GetStates();

    private readonly RibbonGroupBoxState[]? states;

    /// <summary>
    /// Converts from <see cref="string"/> to <see cref="RibbonGroupBoxStateDefinition"/>.
    /// </summary>
    public static RibbonGroupBoxStateDefinition FromString(string stateDefinition)
    {
        return new RibbonGroupBoxStateDefinition(stateDefinition);
    }

    /// <summary>
    /// Converts a string to a group-box state definition.
    /// </summary>
    public static implicit operator RibbonGroupBoxStateDefinition(string stateDefinition)
    {
        return FromString(stateDefinition);
    }

    /// <summary>
    /// Converts a group-box state definition to its string representation.
    /// </summary>
    public static implicit operator string(RibbonGroupBoxStateDefinition stateDefinition)
    {
        return string.Join(",", stateDefinition.States.Select(ToWpfName));
    }

    /// <summary>
    /// Converts from <see cref="string"/> to <see cref="RibbonGroupBoxState"/>.
    /// </summary>
    public static RibbonGroupBoxState ToRibbonGroupBoxState(string ribbonControlState)
    {
        // Support both "Middle" (WPF) and "Medium" (Uno) naming
        if (string.Equals(ribbonControlState, "Middle", StringComparison.OrdinalIgnoreCase))
        {
            return RibbonGroupBoxState.Medium;
        }

        return Enum.TryParse(ribbonControlState, true, out RibbonGroupBoxState result)
            ? result
            : RibbonGroupBoxState.Large;
    }

    /// <summary>
    /// Gets the appropriate enlarged <see cref="RibbonGroupBoxState"/>.
    /// </summary>
    public RibbonGroupBoxState EnlargeState(RibbonGroupBoxState ribbonGroupBoxState)
    {
        var currentStates = GetStates();
        var index = Array.IndexOf(currentStates, ribbonGroupBoxState);
        if (index >= 0)
        {
            return index > 0 ? States[index - 1] : States[0];
        }

        // If not found, find the closest state
        while (--ribbonGroupBoxState >= RibbonGroupBoxState.Large)
        {
            index = Array.IndexOf(currentStates, ribbonGroupBoxState);
            if (index >= 0)
            {
                return States[index];
            }
        }

        return States[0];
    }

    /// <summary>
    /// Gets the appropriate reduced <see cref="RibbonGroupBoxState"/>.
    /// </summary>
    public RibbonGroupBoxState ReduceState(RibbonGroupBoxState ribbonGroupBoxState)
    {
        var currentStates = GetStates();
        var index = Array.IndexOf(currentStates, ribbonGroupBoxState);
        if (index >= 0)
        {
            return index < currentStates.Length - 1
                ? currentStates[index + 1]
                : currentStates[currentStates.Length - 1];
        }

        // If not found, find the closest state
        while (++ribbonGroupBoxState <= RibbonGroupBoxState.Collapsed)
        {
            index = Array.IndexOf(currentStates, ribbonGroupBoxState);
            if (index >= 0)
            {
                return currentStates[index];
            }
        }

        return currentStates[currentStates.Length - 1];
    }

    private RibbonGroupBoxState[] GetStates()
    {
        return states ?? defaultStates;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RibbonGroupBoxStateDefinition definition && Equals(definition);
    }

    /// <inheritdoc />
    public bool Equals(RibbonGroupBoxStateDefinition other)
    {
        var currentStates = GetStates();
        if (currentStates.Length != other.States.Count)
        {
            return false;
        }

        for (var i = 0; i < currentStates.Length; i++)
        {
            if (currentStates[i] != other.States[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var currentStates = GetStates();
            var hashCode = currentStates.Length;
            foreach (var state in currentStates)
            {
                hashCode = (hashCode * 397) ^ (int)state;
            }

            return hashCode;
        }
    }

    /// <summary>Determines whether the specified object instances are considered equal.</summary>
    public static bool operator ==(RibbonGroupBoxStateDefinition left, RibbonGroupBoxStateDefinition right)
    {
        return left.Equals(right);
    }

    /// <summary>Determines whether the specified object instances are not considered equal.</summary>
    public static bool operator !=(RibbonGroupBoxStateDefinition left, RibbonGroupBoxStateDefinition right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Join(",", States);
    }

    private static string ToWpfName(RibbonGroupBoxState state)
    {
        return state == RibbonGroupBoxState.Medium ? "Middle" : state.ToString();
    }
}
