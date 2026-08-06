namespace Fluent;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Fluent.Localization;

/// <summary>
/// Contains localizable Ribbon properties.
/// Set <see cref="Culture"/> property to change current Ribbon localization
/// or set <see cref="Localization"/> directly.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public class RibbonLocalization : INotifyPropertyChanged
{
    private CultureInfo _culture = null!;
    private RibbonLocalizationBase _localization = null!;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the <see cref="PropertyChanged"/> event.
    /// </summary>
    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Static instance of <see cref="RibbonLocalization"/> for easy use in XAML.
    /// </summary>
    public static RibbonLocalization Current { get; } = new();

    /// <summary>
    /// Gets a map of all registered localization classes.
    /// The key is the culture name.
    /// </summary>
    public Dictionary<string, Type> LocalizationMap { get; }

    /// <summary>
    /// Gets or sets current culture used for localization.
    /// </summary>
    public CultureInfo Culture
    {
        get => _culture;
        set
        {
            if (!Equals(_culture, value))
            {
                _culture = value;
                LoadCulture(_culture);
                RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the current localization.
    /// </summary>
    public RibbonLocalizationBase Localization
    {
        get => _localization;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!ReferenceEquals(_localization, value))
            {
                _localization = value;
                RaisePropertyChanged();
            }
        }
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public RibbonLocalization()
    {
        var localizationClasses = GetTypesInNamespace(
            Assembly.GetExecutingAssembly(),
            "Fluent.Localization.Languages");

        LocalizationMap = localizationClasses
            .Where(t => t.GetCustomAttribute<RibbonLocalizationAttribute>() is not null)
            .ToDictionary(
                x => x.GetCustomAttribute<RibbonLocalizationAttribute>()!.CultureName,
                x => x);

        Culture = CultureInfo.CurrentUICulture;
    }

    private void LoadCulture(CultureInfo requestedCulture)
    {
        if (LocalizationMap.TryGetValue(requestedCulture.Name, out var localizationClass))
        {
            Localization = (RibbonLocalizationBase)Activator.CreateInstance(localizationClass)!;
            return;
        }

        if (LocalizationMap.TryGetValue(requestedCulture.TwoLetterISOLanguageName, out localizationClass))
        {
            Localization = (RibbonLocalizationBase)Activator.CreateInstance(localizationClass)!;
            return;
        }

        Debug.WriteLine($"Localization for culture \"{requestedCulture.DisplayName}\" not found. Falling back to English.");
        Localization = RibbonLocalizationBase.FallbackLocalization;
    }

    private static IList<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
    {
        return assembly.GetTypes()
            .Where(t => string.Equals(t.Namespace, nameSpace, StringComparison.Ordinal))
            .ToList();
    }
}
