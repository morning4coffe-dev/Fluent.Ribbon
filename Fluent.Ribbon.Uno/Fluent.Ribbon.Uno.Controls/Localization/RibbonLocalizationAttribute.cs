namespace Fluent.Localization;

/// <summary>
/// Attribute class providing information about a localization.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RibbonLocalizationAttribute : Attribute
{
    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="displayName">Specifies the display name.</param>
    /// <param name="cultureName">Specifies the culture name.</param>
    public RibbonLocalizationAttribute(string displayName, string cultureName)
    {
        DisplayName = displayName;
        CultureName = cultureName;
    }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// Gets the culture name.
    /// </summary>
    public string CultureName { get; }
}
