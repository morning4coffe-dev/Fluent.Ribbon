namespace Fluent;

/// <summary>
/// Struct to map from <see cref="RibbonGroupBoxState"/> to <see cref="RibbonControlSize"/>.
/// Defines what size a ribbon control should be at each group box state.
/// </summary>
public struct RibbonControlSizeDefinition : IEquatable<RibbonControlSizeDefinition>
{
    private const int MaxSizeDefinitionParts = 3;

    private static readonly char[] sizeDefinitionSeparators = [' ', ',', ';', '-', '>'];

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    public RibbonControlSizeDefinition(RibbonControlSize large, RibbonControlSize medium, RibbonControlSize small)
        : this()
    {
        Large = large;
        Medium = medium;
        Small = small;
    }

    /// <summary>
    /// Creates a new instance from a string definition (e.g. "Large Medium Small").
    /// </summary>
    public RibbonControlSizeDefinition(string? sizeDefinition)
        : this()
    {
        if (string.IsNullOrEmpty(sizeDefinition))
        {
            Large = RibbonControlSize.Large;
            Medium = RibbonControlSize.Large;
            Small = RibbonControlSize.Large;
            return;
        }

        var parts = sizeDefinition!.Split(sizeDefinitionSeparators, MaxSizeDefinitionParts, StringSplitOptions.RemoveEmptyEntries).ToList();

        if (parts.Count == 0)
        {
            Large = RibbonControlSize.Large;
            Medium = RibbonControlSize.Large;
            Small = RibbonControlSize.Large;
            return;
        }

        // Ensure three sizes
        for (var i = parts.Count; i < MaxSizeDefinitionParts; i++)
        {
            parts.Add(parts[parts.Count - 1]);
        }

        Large = ToRibbonControlSize(parts[0]);
        Medium = ToRibbonControlSize(parts[1]);
        Small = ToRibbonControlSize(parts[2]);
    }

    /// <summary>
    /// Gets or sets the value for large group sizes.
    /// </summary>
    public RibbonControlSize Large { get; set; }

    /// <summary>
    /// Gets or sets the value for medium group sizes.
    /// </summary>
    public RibbonControlSize Medium { get; set; }

    /// <summary>
    /// Gets or sets the value for small group sizes.
    /// </summary>
    public RibbonControlSize Small { get; set; }

    /// <summary>
    /// Converts from <see cref="string"/> to <see cref="RibbonControlSizeDefinition"/>.
    /// </summary>
    public static RibbonControlSizeDefinition FromString(string sizeDefinition)
    {
        return new RibbonControlSizeDefinition(sizeDefinition);
    }

    /// <summary>
    /// Converts from <see cref="string"/> to <see cref="RibbonControlSize"/>.
    /// </summary>
    public static RibbonControlSize ToRibbonControlSize(string ribbonControlSize)
    {
        // Support both "Middle" (WPF) and "Medium" (Uno) naming
        if (string.Equals(ribbonControlSize, "Middle", StringComparison.OrdinalIgnoreCase))
        {
            return RibbonControlSize.Medium;
        }

        return Enum.TryParse(ribbonControlSize, true, out RibbonControlSize result)
            ? result
            : RibbonControlSize.Large;
    }

    /// <summary>
    /// Gets the appropriate <see cref="RibbonControlSize"/> depending on the group box state.
    /// </summary>
    public RibbonControlSize GetSize(RibbonGroupBoxState ribbonGroupBoxState)
    {
        return ribbonGroupBoxState switch
        {
            RibbonGroupBoxState.Large => Large,
            RibbonGroupBoxState.Medium => Medium,
            RibbonGroupBoxState.Small => Small,
            RibbonGroupBoxState.Collapsed or RibbonGroupBoxState.QuickAccess => Large,
            _ => RibbonControlSize.Large
        };
    }

    /// <summary>
    /// Gets the appropriate <see cref="RibbonControlSize"/> depending on the control size.
    /// </summary>
    public RibbonControlSize GetSize(RibbonControlSize ribbonControlSize)
    {
        return ribbonControlSize switch
        {
            RibbonControlSize.Large => Large,
            RibbonControlSize.Medium => Medium,
            RibbonControlSize.Small => Small,
            _ => RibbonControlSize.Large
        };
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is RibbonControlSizeDefinition definition && Equals(definition);
    }

    /// <inheritdoc />
    public bool Equals(RibbonControlSizeDefinition other)
    {
        return Large == other.Large && Medium == other.Medium && Small == other.Small;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (int)Large;
            hashCode = (hashCode * 397) ^ (int)Medium;
            hashCode = (hashCode * 397) ^ (int)Small;
            return hashCode;
        }
    }

    /// <summary>Determines whether the specified object instances are considered equal.</summary>
    public static bool operator ==(RibbonControlSizeDefinition left, RibbonControlSizeDefinition right)
    {
        return left.Equals(right);
    }

    /// <summary>Determines whether the specified object instances are not considered equal.</summary>
    public static bool operator !=(RibbonControlSizeDefinition left, RibbonControlSizeDefinition right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Large} {Medium} {Small}";
    }
}
