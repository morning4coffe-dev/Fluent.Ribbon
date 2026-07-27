namespace Fluent.Modern;

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fluent.Modern.Model;
using Microsoft.UI.Xaml.Automation;
using Windows.Storage;

/// <summary>
/// <para><b>Modern extension</b> — captures, applies, and persists ribbon customization layout as JSON.</para>
/// </summary>
[ModernExtension]
public static class RibbonCustomizationService
{
    private const string StorageContainerName = "Fluent.Modern.RibbonCustomizationService";
    private const string DefaultStorageName = "ribbon-layout";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    private static readonly DependencyProperty HiddenQuickAccessItemsProperty =
        DependencyProperty.RegisterAttached(
            "HiddenQuickAccessItems",
            typeof(List<UIElement>),
            typeof(RibbonCustomizationService),
            new PropertyMetadata(null));

    #region Dependency Properties

    /// <summary>Identifies the ItemKey attached dependency property.</summary>
    public static readonly DependencyProperty ItemKeyProperty =
        DependencyProperty.RegisterAttached(
            "ItemKey",
            typeof(string),
            typeof(RibbonCustomizationService),
            new PropertyMetadata(string.Empty));

    #endregion

    #region Attached Property Accessors

    /// <summary>
    /// Gets the explicit stable customization key for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The explicit item key, or an empty string when not set.</returns>
    public static string GetItemKey(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (string)element.GetValue(ItemKeyProperty);
    }

    /// <summary>
    /// Sets the explicit stable customization key for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The stable item key.</param>
    public static void SetItemKey(DependencyObject element, string value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ItemKeyProperty, value ?? string.Empty);
    }

    #endregion

    #region Observable APIs

    /// <summary>
    /// Resolves the deterministic key used for an element without using its localized header.
    /// </summary>
    /// <param name="element">The element to identify.</param>
    /// <returns>The resolved key and any derivation warning.</returns>
    public static RibbonCustomizationResult<string> ResolveStableKey(DependencyObject element)
    {
        if (element is null)
        {
            return Failure<string>("NullElement", "A customization key cannot be resolved for a null element.");
        }

        var issues = new List<RibbonCustomizationIssue>();
        var resolution = ResolveKey(element);
        if (resolution.IsStructural)
        {
            issues.Add(Warning(
                "DerivedStructuralKey",
                $"'{element.GetType().Name}' has no explicit ItemKey, x:Name, or AutomationId. "
                + $"The derived key '{resolution.Key}' is deterministic, but an explicit ItemKey is required when structurally identical siblings exist."));
        }

        return new RibbonCustomizationResult<string>(resolution.Key, issues);
    }

    /// <summary>
    /// Captures the current ribbon tab and Quick Access Toolbar layout with duplicate-key validation.
    /// </summary>
    /// <param name="ribbon">The ribbon to capture.</param>
    /// <returns>The captured layout and all warnings or errors.</returns>
    public static RibbonCustomizationResult<RibbonLayout> CaptureResult(Fluent.Ribbon ribbon)
    {
        if (ribbon is null)
        {
            return Failure<RibbonLayout>("NullRibbon", "A ribbon is required to capture customization.");
        }

        var issues = new List<RibbonCustomizationIssue>();
        var layout = new RibbonLayout();

        try
        {
            var tabKeys = ResolveUniqueKeys(ribbon.Tabs, "Ribbon tabs", issues);
            var quickAccessKeys = ResolveUniqueKeys(ribbon.QuickAccessToolBarItems, "Quick Access Toolbar items", issues);
            if (HasErrors(issues))
            {
                return new RibbonCustomizationResult<RibbonLayout>(layout, issues);
            }

            for (var index = 0; index < ribbon.Tabs.Count; index++)
            {
                var tab = ribbon.Tabs[index];
                layout.Tabs.Add(new RibbonTabLayout
                {
                    Key = tabKeys[tab],
                    IsVisible = tab.Visibility == Visibility.Visible,
                    Order = index,
                });
            }

            foreach (var item in ribbon.QuickAccessToolBarItems)
            {
                layout.QuickAccessItemKeys.Add(quickAccessKeys[item]);
            }
        }
        catch (Exception exception)
        {
            issues.Add(Error("CaptureFailed", "Ribbon customization capture failed.", exception));
        }

        return new RibbonCustomizationResult<RibbonLayout>(layout, issues);
    }

    /// <summary>
    /// Applies a captured ribbon layout after validating both persisted and live keys.
    /// </summary>
    /// <param name="ribbon">The ribbon to customize.</param>
    /// <param name="layout">The layout to apply.</param>
    /// <returns>An observable apply result.</returns>
    public static RibbonCustomizationResult<bool> ApplyResult(Fluent.Ribbon ribbon, RibbonLayout layout)
    {
        if (ribbon is null)
        {
            return Failure<bool>("NullRibbon", "A ribbon is required to apply customization.");
        }

        if (layout is null)
        {
            return Failure<bool>("NullLayout", "A layout is required to apply customization.");
        }

        var issues = new List<RibbonCustomizationIssue>();
        try
        {
            ValidateLayoutKeys(layout, issues);

            var originalTabs = ribbon.Tabs.ToList();
            var tabKeys = ResolveUniqueKeys(originalTabs, "Ribbon tabs", issues);
            var quickAccessPool = ribbon.QuickAccessToolBarItems
                .Concat(GetHiddenQuickAccessItems(ribbon))
                .Distinct()
                .ToList();
            var quickAccessKeys = ResolveUniqueKeys(quickAccessPool, "Quick Access Toolbar items", issues);
            if (HasErrors(issues))
            {
                return new RibbonCustomizationResult<bool>(false, issues);
            }

            ApplyTabs(ribbon, layout, originalTabs, tabKeys, issues);
            ApplyQuickAccessItems(ribbon, layout, quickAccessPool, quickAccessKeys, issues);
        }
        catch (Exception exception)
        {
            issues.Add(Error("ApplyFailed", "Ribbon customization apply failed.", exception));
        }

        return new RibbonCustomizationResult<bool>(!HasErrors(issues), issues);
    }

    /// <summary>
    /// Serializes a ribbon layout to JSON.
    /// </summary>
    /// <param name="layout">The layout.</param>
    /// <returns>The JSON value or a serialization error.</returns>
    public static RibbonCustomizationResult<string> SerializeResult(RibbonLayout layout)
    {
        if (layout is null)
        {
            return Failure<string>("NullLayout", "A layout is required for serialization.");
        }

        try
        {
            return new RibbonCustomizationResult<string>(JsonSerializer.Serialize(layout, JsonOptions));
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or ArgumentException)
        {
            return Failure<string>("SerializationFailed", "Ribbon customization serialization failed.", exception);
        }
    }

    /// <summary>
    /// Deserializes a ribbon layout from JSON.
    /// </summary>
    /// <param name="json">The JSON string.</param>
    /// <returns>The layout or a deserialization error.</returns>
    public static RibbonCustomizationResult<RibbonLayout> DeserializeResult(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Failure<RibbonLayout>("EmptyJson", "Ribbon customization JSON cannot be empty.");
        }

        try
        {
            var layout = JsonSerializer.Deserialize<RibbonLayout>(json, JsonOptions);
            return layout is null
                ? Failure<RibbonLayout>("DeserializationReturnedNull", "Ribbon customization JSON did not contain a layout.")
                : new RibbonCustomizationResult<RibbonLayout>(layout);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or ArgumentException)
        {
            return Failure<RibbonLayout>("DeserializationFailed", "Ribbon customization JSON could not be read.", exception);
        }
    }

    /// <summary>
    /// Saves a ribbon layout to durable application storage.
    /// </summary>
    /// <param name="layout">The layout to save.</param>
    /// <param name="name">The storage name.</param>
    /// <returns>The successful backend name together with any fallback warnings, or an error.</returns>
    public static async Task<RibbonCustomizationResult<string>> SaveResultAsync(
        RibbonLayout layout,
        string name = DefaultStorageName)
    {
        var serialized = SerializeResult(layout);
        if (!serialized.Succeeded || serialized.Value is null)
        {
            return new RibbonCustomizationResult<string>(null, serialized.Issues);
        }

        var key = GetStorageKey(name);
        var issues = new List<RibbonCustomizationIssue>();
        try
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                GetFileName(key),
                CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, serialized.Value);
            return new RibbonCustomizationResult<string>("LocalFolder", issues);
        }
        catch (Exception exception)
        {
            issues.Add(Warning("LocalFolderSaveFailed", "Local-folder persistence failed; LocalSettings will be attempted.", exception));
        }

        try
        {
            ApplicationData.Current.LocalSettings.Values[$"{StorageContainerName}.{key}"] = serialized.Value;
            return new RibbonCustomizationResult<string>("LocalSettings", issues);
        }
        catch (Exception exception)
        {
            issues.Add(Warning("LocalSettingsSaveFailed", "LocalSettings persistence failed; LocalApplicationData will be attempted.", exception));
        }

        try
        {
            var path = GetFallbackStoragePath(key);
            var directory = System.IO.Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("Ribbon customization storage directory is unavailable.");
            }

            System.IO.Directory.CreateDirectory(directory);
            await System.IO.File.WriteAllTextAsync(path, serialized.Value);
            return new RibbonCustomizationResult<string>("LocalApplicationData", issues);
        }
        catch (Exception exception)
        {
            issues.Add(Error("StorageSaveFailed", "No durable ribbon customization storage backend succeeded.", exception));
            return new RibbonCustomizationResult<string>(null, issues);
        }
    }

    /// <summary>
    /// Loads a ribbon layout from durable application storage.
    /// </summary>
    /// <param name="name">The storage name.</param>
    /// <returns>The loaded layout, or a successful null value when no layout exists.</returns>
    public static async Task<RibbonCustomizationResult<RibbonLayout?>> LoadResultAsync(
        string name = DefaultStorageName)
    {
        var key = GetStorageKey(name);
        var issues = new List<RibbonCustomizationIssue>();

        try
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(GetFileName(key));
            var json = await FileIO.ReadTextAsync(file);
            var deserialized = DeserializeResult(json);
            return new RibbonCustomizationResult<RibbonLayout?>(deserialized.Value, issues.Concat(deserialized.Issues));
        }
        catch (Exception exception)
        {
            issues.Add(Warning("LocalFolderLoadFailed", "Local-folder load failed; LocalSettings will be attempted.", exception));
        }

        try
        {
            var settings = ApplicationData.Current.LocalSettings;
            if (!settings.Values.TryGetValue($"{StorageContainerName}.{key}", out var value))
            {
                issues.Add(Warning("LocalSettingsValueMissing", "LocalSettings did not contain the requested layout; LocalApplicationData will be attempted."));
            }
            else if (value is not string json)
            {
                issues.Add(Error("InvalidStoredValue", "The stored ribbon customization value is not JSON text."));
                return new RibbonCustomizationResult<RibbonLayout?>(null, issues);
            }
            else
            {
                var deserialized = DeserializeResult(json);
                return new RibbonCustomizationResult<RibbonLayout?>(deserialized.Value, issues.Concat(deserialized.Issues));
            }
        }
        catch (Exception exception)
        {
            issues.Add(Warning("LocalSettingsLoadFailed", "LocalSettings load failed; LocalApplicationData will be attempted.", exception));
        }

        try
        {
            var path = GetFallbackStoragePath(key);
            if (!System.IO.File.Exists(path))
            {
                return new RibbonCustomizationResult<RibbonLayout?>(null, issues);
            }

            var json = await System.IO.File.ReadAllTextAsync(path);
            var deserialized = DeserializeResult(json);
            return new RibbonCustomizationResult<RibbonLayout?>(deserialized.Value, issues.Concat(deserialized.Issues));
        }
        catch (Exception exception)
        {
            issues.Add(Error("StorageLoadFailed", "Ribbon customization could not be loaded from durable storage.", exception));
            return new RibbonCustomizationResult<RibbonLayout?>(null, issues);
        }
    }

    /// <summary>
    /// Clears a stored ribbon layout from durable application storage.
    /// </summary>
    /// <param name="name">The storage name.</param>
    /// <returns>An observable clear result.</returns>
    public static async Task<RibbonCustomizationResult<bool>> ClearResultAsync(string name)
    {
        var key = GetStorageKey(name);
        var issues = new List<RibbonCustomizationIssue>();
        var clearedBackend = false;

        try
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(GetFileName(key));
            await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
            clearedBackend = true;
        }
        catch (Exception exception)
        {
            issues.Add(Warning("LocalFolderClearFailed", "Local-folder clear failed; LocalSettings will still be cleared.", exception));
        }

        try
        {
            ApplicationData.Current.LocalSettings.Values.Remove($"{StorageContainerName}.{key}");
            clearedBackend = true;
        }
        catch (Exception exception)
        {
            issues.Add(Warning("LocalSettingsClearFailed", "LocalSettings clear failed; LocalApplicationData will still be cleared.", exception));
        }

        try
        {
            var path = GetFallbackStoragePath(key);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                clearedBackend = true;
            }
            else if (!clearedBackend)
            {
                issues.Add(Error(
                    "StorageClearFailed",
                    "Primary storage backends could not be cleared and no fallback file existed."));
            }
        }
        catch (Exception exception)
        {
            issues.Add(clearedBackend
                ? Warning("LocalApplicationDataClearFailed", "LocalApplicationData clear failed after another backend was cleared.", exception)
                : Error("StorageClearFailed", "No durable ribbon customization storage backend could be cleared.", exception));
        }

        return new RibbonCustomizationResult<bool>(clearedBackend && !HasErrors(issues), issues);
    }

    #endregion

    #region Compatibility Wrappers

    /// <summary>
    /// Captures the current ribbon layout without throwing. Use <see cref="CaptureResult"/> to observe failures.
    /// </summary>
    public static RibbonLayout Capture(Fluent.Ribbon ribbon)
    {
        var result = CaptureResult(ribbon);
        return result.Succeeded && result.Value is not null ? result.Value : new RibbonLayout();
    }

    /// <summary>
    /// Applies a ribbon layout without throwing. Use <see cref="ApplyResult"/> to observe failures.
    /// </summary>
    public static void Apply(Fluent.Ribbon ribbon, RibbonLayout layout)
    {
        _ = ApplyResult(ribbon, layout);
    }

    /// <summary>
    /// Serializes a layout without throwing. Use <see cref="SerializeResult"/> to observe failures.
    /// </summary>
    public static string Serialize(RibbonLayout layout)
    {
        var result = SerializeResult(layout);
        return result.Succeeded ? result.Value ?? "{}" : "{}";
    }

    /// <summary>
    /// Deserializes a layout without throwing. Use <see cref="DeserializeResult"/> to observe failures.
    /// </summary>
    public static RibbonLayout? Deserialize(string json)
    {
        var result = DeserializeResult(json);
        return result.Succeeded ? result.Value : null;
    }

    /// <summary>
    /// Saves a layout without throwing. Use <see cref="SaveResultAsync"/> to observe failures.
    /// </summary>
    public static async Task SaveAsync(RibbonLayout layout, string name = DefaultStorageName)
    {
        _ = await SaveResultAsync(layout, name);
    }

    /// <summary>
    /// Loads a layout without throwing. Use <see cref="LoadResultAsync"/> to observe failures.
    /// </summary>
    public static async Task<RibbonLayout?> LoadAsync(string name = DefaultStorageName)
    {
        var result = await LoadResultAsync(name);
        return result.Succeeded ? result.Value : null;
    }

    /// <summary>
    /// Clears a stored layout without throwing. Use <see cref="ClearResultAsync"/> to observe failures.
    /// </summary>
    public static async Task ClearAsync(string name)
    {
        _ = await ClearResultAsync(name);
    }

    #endregion

    #region Apply Helpers

    private static void ApplyTabs(
        Fluent.Ribbon ribbon,
        RibbonLayout layout,
        IReadOnlyList<Fluent.RibbonTabItem> originalTabs,
        IReadOnlyDictionary<Fluent.RibbonTabItem, string> tabKeys,
        ICollection<RibbonCustomizationIssue> issues)
    {
        if (originalTabs.Count == 0)
        {
            return;
        }

        var selectedTab = ribbon.SelectedTab;
        var keyedTabs = tabKeys.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);
        var layoutTabs = layout.Tabs.OrderBy(tab => tab.Order).ToList();

        foreach (var tabLayout in layoutTabs)
        {
            if (keyedTabs.TryGetValue(tabLayout.Key, out var tab))
            {
                tab.Visibility = tabLayout.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                issues.Add(Warning("UnknownTabKey", $"The persisted tab key '{tabLayout.Key}' does not exist in the current ribbon."));
            }
        }

        var orderedTabs = new List<Fluent.RibbonTabItem>();
        foreach (var tabLayout in layoutTabs)
        {
            if (keyedTabs.TryGetValue(tabLayout.Key, out var tab) && !orderedTabs.Contains(tab))
            {
                orderedTabs.Add(tab);
            }
        }

        foreach (var tab in originalTabs)
        {
            if (!orderedTabs.Contains(tab))
            {
                orderedTabs.Add(tab);
            }
        }

        if (!orderedTabs.SequenceEqual(originalTabs))
        {
            ribbon.Tabs.Clear();
            foreach (var tab in orderedTabs)
            {
                ribbon.Tabs.Add(tab);
            }
        }

        RestoreSelectedTab(ribbon, selectedTab);
    }

    private static void ApplyQuickAccessItems(
        Fluent.Ribbon ribbon,
        RibbonLayout layout,
        IReadOnlyList<UIElement> pool,
        IReadOnlyDictionary<UIElement, string> itemKeys,
        ICollection<RibbonCustomizationIssue> issues)
    {
        var keyedItems = itemKeys.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);
        var selected = new List<UIElement>();
        foreach (var key in layout.QuickAccessItemKeys)
        {
            if (keyedItems.TryGetValue(key, out var item))
            {
                selected.Add(item);
            }
            else
            {
                issues.Add(Warning("UnknownQuickAccessKey", $"The persisted Quick Access Toolbar key '{key}' does not exist in the current ribbon."));
            }
        }

        ribbon.QuickAccessToolBarItems.Clear();
        foreach (var item in selected)
        {
            ribbon.QuickAccessToolBarItems.Add(item);
        }

        SetHiddenQuickAccessItems(ribbon, pool.Where(item => !selected.Contains(item)).ToList());
    }

    private static void RestoreSelectedTab(Fluent.Ribbon ribbon, Fluent.RibbonTabItem? selectedTab)
    {
        if (selectedTab is not null && ribbon.Tabs.Contains(selectedTab) && selectedTab.Visibility == Visibility.Visible)
        {
            ribbon.SelectedTab = selectedTab;
            ribbon.SelectedTabIndex = ribbon.Tabs.IndexOf(selectedTab);
            return;
        }

        var firstVisible = ribbon.Tabs.FirstOrDefault(tab => tab.Visibility == Visibility.Visible);
        if (firstVisible is not null)
        {
            ribbon.SelectedTab = firstVisible;
            ribbon.SelectedTabIndex = ribbon.Tabs.IndexOf(firstVisible);
        }
    }

    #endregion

    #region Key Helpers

    private static Dictionary<T, string> ResolveUniqueKeys<T>(
        IEnumerable<T> elements,
        string scope,
        ICollection<RibbonCustomizationIssue> issues)
        where T : DependencyObject
    {
        var keys = new Dictionary<T, string>();
        var owners = new Dictionary<string, T>(StringComparer.Ordinal);

        foreach (var element in elements)
        {
            var resolution = ResolveKey(element);
            keys[element] = resolution.Key;
            if (resolution.IsStructural)
            {
                issues.Add(Warning(
                    "DerivedStructuralKey",
                    $"{scope}: '{element.GetType().Name}' uses derived key '{resolution.Key}'. "
                    + "Set RibbonCustomizationService.ItemKey when the structure is not guaranteed to remain unique."));
            }

            if (owners.TryGetValue(resolution.Key, out var duplicate))
            {
                issues.Add(Error(
                    "DuplicateKey",
                    $"{scope} contain duplicate key '{resolution.Key}' on "
                    + $"'{duplicate.GetType().Name}' and '{element.GetType().Name}'. Assign unique ItemKey values."));
            }
            else
            {
                owners.Add(resolution.Key, element);
            }
        }

        return keys;
    }

    private static KeyResolution ResolveKey(DependencyObject element)
    {
        var explicitKey = GetItemKey(element).Trim();
        if (!string.IsNullOrWhiteSpace(explicitKey))
        {
            return new KeyResolution(explicitKey, false);
        }

        if (element is FrameworkElement frameworkElement && !string.IsNullOrWhiteSpace(frameworkElement.Name))
        {
            return new KeyResolution($"name:{frameworkElement.Name.Trim()}", false);
        }

        var automationId = AutomationProperties.GetAutomationId(element);
        if (!string.IsNullOrWhiteSpace(automationId))
        {
            return new KeyResolution($"automation:{automationId.Trim()}", false);
        }

        var keyTip = GetPropertyValue<string>(element, "KeyTip");
        if (!string.IsNullOrWhiteSpace(keyTip))
        {
            return new KeyResolution($"keytip:{element.GetType().Name}:{keyTip.Trim()}", true);
        }

        var signature = BuildStructuralSignature(element);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(signature));
        return new KeyResolution($"struct:{element.GetType().Name}:{Convert.ToHexString(hash.AsSpan(0, 12))}", true);
    }

    private static string BuildStructuralSignature(DependencyObject element)
    {
        var parts = new List<string>
        {
            element.GetType().FullName ?? element.GetType().Name,
        };

        var explicitKey = GetItemKey(element);
        if (!string.IsNullOrWhiteSpace(explicitKey))
        {
            parts.Add($"ItemKey={explicitKey.Trim()}");
        }

        if (element is FrameworkElement frameworkElement && !string.IsNullOrWhiteSpace(frameworkElement.Name))
        {
            parts.Add($"Name={frameworkElement.Name.Trim()}");
        }

        var automationId = AutomationProperties.GetAutomationId(element);
        if (!string.IsNullOrWhiteSpace(automationId))
        {
            parts.Add($"AutomationId={automationId.Trim()}");
        }

        AddStableProperty(parts, element, "KeyTip");
        AddStableProperty(parts, element, "ContextualTabGroupName");
        AddStableProperty(parts, element, "IconGlyph");
        AddStableProperty(parts, element, "Symbol");

        var command = GetPropertyValue<object>(element, "Command");
        if (command is not null)
        {
            parts.Add($"CommandType={command.GetType().FullName}");
        }

        var childSignatures = GetStructuralChildren(element)
            .Select(BuildStructuralSignature)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        if (childSignatures.Length > 0)
        {
            parts.Add($"Children=[{string.Join("|", childSignatures)}]");
        }

        return string.Join(";", parts);
    }

    private static IEnumerable<DependencyObject> GetStructuralChildren(DependencyObject element)
    {
        return element switch
        {
            Fluent.RibbonTabItem tab => tab.Groups,
            Fluent.RibbonGroupBox group => group.Items,
            Fluent.RibbonSplitButton splitButton =>
                splitButton.Items.OfType<DependencyObject>(),
            Fluent.RibbonDropDownButton dropDownButton =>
                dropDownButton.Items.OfType<DependencyObject>(),
            Fluent.RibbonMenuItem menuItem => menuItem.Items,
            _ => Array.Empty<DependencyObject>(),
        };
    }

    private static void AddStableProperty(ICollection<string> parts, object source, string propertyName)
    {
        var value = GetPropertyValue<object>(source, propertyName);
        switch (value)
        {
            case string text when !string.IsNullOrWhiteSpace(text):
                parts.Add($"{propertyName}={text.Trim()}");
                break;
            case Enum enumValue:
                parts.Add($"{propertyName}={enumValue.GetType().FullName}.{enumValue}");
                break;
        }
    }

    private static T? GetPropertyValue<T>(object source, string propertyName)
    {
        var property = ReflectionPropertyHelper.GetReadableProperty(
            source.GetType(),
            propertyName);
        return property?.GetMethod is not null && property.GetValue(source) is T value ? value : default;
    }

    private static void ValidateLayoutKeys(RibbonLayout layout, ICollection<RibbonCustomizationIssue> issues)
    {
        ValidateUniqueStrings(layout.Tabs.Select(tab => tab.Key), "Persisted tabs", issues);
        ValidateUniqueStrings(layout.QuickAccessItemKeys, "Persisted Quick Access Toolbar items", issues);
    }

    private static void ValidateUniqueStrings(
        IEnumerable<string> keys,
        string scope,
        ICollection<RibbonCustomizationIssue> issues)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                issues.Add(Error("EmptyKey", $"{scope} contain an empty key."));
            }
            else if (!seen.Add(key))
            {
                issues.Add(Error("DuplicateKey", $"{scope} contain duplicate key '{key}'."));
            }
        }
    }

    #endregion

    #region Storage and Result Helpers

    private static List<UIElement> GetHiddenQuickAccessItems(DependencyObject element)
    {
        return element.GetValue(HiddenQuickAccessItemsProperty) as List<UIElement> ?? new List<UIElement>();
    }

    private static void SetHiddenQuickAccessItems(DependencyObject element, List<UIElement> items)
    {
        element.SetValue(HiddenQuickAccessItemsProperty, items);
    }

    private static string GetStorageKey(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? DefaultStorageName : name.Trim();
    }

    private static string GetFileName(string key)
    {
        return $"{SanitizePathSegment(key)}.json";
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        return new string(
            value.Select(character => invalid.Contains(character) ? '_' : character)
                .ToArray());
    }

    private static string GetFallbackStoragePath(string key)
    {
        var localApplicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("Local application data storage is unavailable.");
        }

        var applicationName =
            System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name
            ?? AppDomain.CurrentDomain.FriendlyName;
        if (string.IsNullOrWhiteSpace(applicationName))
        {
            applicationName = "Application";
        }

        return System.IO.Path.Combine(
            localApplicationData,
            "Fluent.Ribbon.Uno",
            "RibbonCustomization",
            SanitizePathSegment(applicationName),
            GetFileName(key));
    }

    private static bool HasErrors(IEnumerable<RibbonCustomizationIssue> issues)
    {
        return issues.Any(issue => issue.IsError);
    }

    private static RibbonCustomizationIssue Warning(string code, string message, Exception? exception = null)
    {
        return new RibbonCustomizationIssue(code, message, false, exception);
    }

    private static RibbonCustomizationIssue Error(string code, string message, Exception? exception = null)
    {
        return new RibbonCustomizationIssue(code, message, true, exception);
    }

    private static RibbonCustomizationResult<T> Failure<T>(string code, string message, Exception? exception = null)
    {
        return new RibbonCustomizationResult<T>(default, [Error(code, message, exception)]);
    }

    private sealed record KeyResolution(string Key, bool IsStructural);

    #endregion
}
