namespace Fluent.Automation.Peers;

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

internal static class AutomationPeerHelpers
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        DependencyObject,
        GeneratedValueOwnership> GeneratedAutomationNames = new();
    private static readonly ConditionalWeakTable<DependencyObject, GeneratedValues> GeneratedPropertyValues = new();

    [ThreadStatic]
    private static HashSet<object>? nameExtractionPath;

    internal static string GetHeaderName(FrameworkElement owner)
    {
        if (owner is IHeaderedControl headeredControl)
        {
            return GetObjectName(headeredControl.Header);
        }

        return owner is ContentControl contentControl
            ? GetObjectName(contentControl.Content)
            : string.Empty;
    }

    internal static string GetHeaderOrPlaceholderName(FrameworkElement owner)
    {
        var header = GetHeaderName(owner);
        if (!string.IsNullOrWhiteSpace(header))
        {
            return header;
        }

        var placeholder = owner switch
        {
            ComboBox comboBox => comboBox.PlaceholderText ?? string.Empty,
            TextBox textBox => textBox.PlaceholderText ?? string.Empty,
            _ => string.Empty,
        };
        if (!string.IsNullOrWhiteSpace(placeholder))
        {
            return placeholder;
        }

        return GetObjectName(ToolTipService.GetToolTip(owner));
    }

    internal static string GetAccessKey(
        FrameworkElement owner,
        string? frameworkAccessKey)
        => !string.IsNullOrWhiteSpace(frameworkAccessKey)
            ? frameworkAccessKey
            : owner is IKeyTipedControl keyTipControl
                ? keyTipControl.KeyTip ?? string.Empty
                : string.Empty;

    internal static string GetObjectName(object? value)
    {
        var ownsPath = nameExtractionPath is null;
        nameExtractionPath ??= new HashSet<object>(ReferenceEqualityComparer.Instance);
        try
        {
            return GetObjectNameCore(value, nameExtractionPath, 0);
        }
        finally
        {
            if (ownsPath)
            {
                nameExtractionPath = null;
            }
        }
    }

    private static string GetObjectNameCore(object? value, HashSet<object> path, int depth)
    {
        if (value is null || depth > 32)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return NormalizeMeaningfulText(text);
        }

        if (!value.GetType().IsValueType && !path.Add(value))
        {
            return string.Empty;
        }

        try
        {
            if (value is DependencyObject dependencyObject)
            {
                var explicitName = NormalizeMeaningfulText(AutomationProperties.GetName(dependencyObject));
                if (!string.IsNullOrEmpty(explicitName))
                {
                    return explicitName;
                }
            }

            var extracted = value switch
            {
                TextBlock textBlock => NormalizeMeaningfulText(textBlock.Text),
                ContentPresenter presenter => GetObjectNameCore(presenter.Content, path, depth + 1),
                ContentControl contentControl => GetObjectNameCore(contentControl.Content, path, depth + 1),
                Panel panel => JoinNames(panel.Children, path, depth + 1),
                System.Collections.IList list => JoinNames(list, path, depth + 1),
                _ => string.Empty,
            };
            if (!string.IsNullOrEmpty(extracted))
            {
                return extracted;
            }

            if (value is FrameworkElement element)
            {
                var peerName = NormalizeMeaningfulText(
                    FrameworkElementAutomationPeer.FromElement(element)?.GetName());
                if (!string.IsNullOrEmpty(peerName))
                {
                    return peerName;
                }

                var childNames = new List<string>();
                var childCount = Math.Min(
                    VisualTreeHelper.GetChildrenCount(element),
                    64);
                for (var index = 0; index < childCount; index++)
                {
                    var childName = GetObjectNameCore(
                        VisualTreeHelper.GetChild(element, index),
                        path,
                        depth + 1);
                    if (!string.IsNullOrEmpty(childName))
                    {
                        childNames.Add(childName);
                    }
                }

                if (childNames.Count > 0)
                {
                    return string.Join(" ", childNames);
                }
            }

            return GetMeaningfulCustomString(value);
        }
        finally
        {
            if (!value.GetType().IsValueType)
            {
                path.Remove(value);
            }
        }
    }

    private static string JoinNames(
        IEnumerable<UIElement> children,
        HashSet<object> path,
        int depth)
    {
        return string.Join(
            " ",
            children
                .Select(child => GetObjectNameCore(child, path, depth))
                .Where(name => !string.IsNullOrEmpty(name)));
    }

    private static string JoinNames(
        System.Collections.IList values,
        HashSet<object> path,
        int depth)
    {
        var names = new List<string>();
        var count = Math.Min(values.Count, 64);
        for (var index = 0; index < count; index++)
        {
            var value = values[index];
            var name = GetObjectNameCore(value, path, depth);
            if (!string.IsNullOrEmpty(name))
            {
                names.Add(name);
            }
        }

        return string.Join(" ", names);
    }

    private static string GetMeaningfulCustomString(object value)
    {
        if (value is Type)
        {
            return string.Empty;
        }

        var toString = value.GetType().GetMethod(
            nameof(ToString),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            Type.EmptyTypes,
            modifiers: null);
        if (toString?.DeclaringType == typeof(object))
        {
            return string.Empty;
        }

        var text = NormalizeMeaningfulText(value.ToString());
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var type = value.GetType();
        return text == type.Name
               || text == type.FullName
               || text == type.ToString()
            ? string.Empty
            : text;
    }

    private static string NormalizeMeaningfulText(string? text)
    {
        var normalized = text?.Trim() ?? string.Empty;
        if (normalized.Length == 0
            || normalized.All(
                character => char.IsWhiteSpace(character)
                             || character is >= '\uE000' and <= '\uF8FF'))
        {
            return string.Empty;
        }

        return normalized.Any(char.IsLetterOrDigit) ? normalized : string.Empty;
    }

    internal static void SetNameIfUnsetOrGenerated(
        DependencyObject element,
        string localizedName)
    {
        var currentName = AutomationProperties.GetName(element);
        var localValue = element.ReadLocalValue(AutomationProperties.NameProperty);
        var generatedValue = CloneGeneratedValue(localizedName);
        if (GeneratedAutomationNames.TryGetValue(element, out var ownership))
        {
            if (!ownership.TryGenerate(currentName, localValue, generatedValue))
            {
                GeneratedAutomationNames.Remove(element);
                return;
            }

            AutomationProperties.SetName(element, (string)generatedValue!);
            ownership.CaptureLocalValue(
                element.ReadLocalValue(AutomationProperties.NameProperty));
            RaiseNameChanged(element, currentName, (string?)generatedValue);
            return;
        }

        ownership = new GeneratedValueOwnership();
        if (!ownership.TryGenerate(currentName, localValue, generatedValue))
        {
            return;
        }

        AutomationProperties.SetName(element, (string)generatedValue!);
        ownership.CaptureLocalValue(
            element.ReadLocalValue(AutomationProperties.NameProperty));
        GeneratedAutomationNames.Add(element, ownership);
        RaiseNameChanged(element, currentName, (string?)generatedValue);
    }

    internal static void SetToolTipIfUnsetOrGenerated(
        DependencyObject element,
        object? localizedToolTip)
        => SetValueIfUnsetOrGenerated(
            element,
            ToolTipService.ToolTipProperty,
            localizedToolTip);

    internal static void SetValueIfUnsetOrGenerated(
        DependencyObject element,
        DependencyProperty property,
        object? localizedValue)
    {
        var generatedValues = GeneratedPropertyValues.GetOrCreateValue(element);
        var currentValue = element.GetValue(property);
        var localValue = element.ReadLocalValue(property);
        var generatedValue = CloneGeneratedValue(localizedValue);
        if (generatedValues.Values.TryGetValue(property, out var ownership))
        {
            if (!ownership.TryGenerate(currentValue, localValue, generatedValue))
            {
                generatedValues.Values.Remove(property);
                return;
            }

            element.SetValue(property, generatedValue);
            ownership.CaptureLocalValue(element.ReadLocalValue(property));
            return;
        }

        ownership = new GeneratedValueOwnership();
        if (!ownership.TryGenerate(currentValue, localValue, generatedValue))
        {
            return;
        }

        element.SetValue(property, generatedValue);
        ownership.CaptureLocalValue(element.ReadLocalValue(property));
        generatedValues.Values.Add(property, ownership);
    }

    private static object? CloneGeneratedValue(object? value)
        => value is string text
            ? new string(text.ToCharArray())
            : value;

    internal static string GetColorDescription(
        Windows.UI.Color color,
        global::Fluent.Localization.RibbonLocalizationBase localization)
    {
        return color.A == byte.MaxValue
            ? string.Format(
                CultureInfo.CurrentCulture,
                localization.ColorDescriptionFormat,
                color.R,
                color.G,
                color.B)
            : string.Format(
                CultureInfo.CurrentCulture,
                localization.ColorDescriptionWithAlphaFormat,
                color.A,
                color.R,
                color.G,
                color.B);
    }

    private static void RaiseNameChanged(
        DependencyObject element,
        string? oldName,
        string? newName)
    {
        if (string.Equals(oldName, newName, StringComparison.Ordinal)
            || element is not FrameworkElement frameworkElement
            || FrameworkElementAutomationPeer.FromElement(frameworkElement) is not { } peer)
        {
            return;
        }

        peer.RaisePropertyChangedEvent(
            AutomationElementIdentifiers.NameProperty,
            oldName ?? string.Empty,
            newName ?? string.Empty);
    }

    internal static void UpdatePeerName(
        FrameworkElement element,
        ref string? cachedName,
        string newName)
    {
        var oldName = cachedName;
        cachedName = newName;
        RaiseNameChanged(element, oldName, newName);
    }

    internal sealed class GeneratedValueOwnership
    {
        internal bool IsOwned { get; private set; }

        internal object? Value { get; private set; }

        internal object? LocalValue { get; private set; }

        internal bool TryGenerate(
            object? currentValue,
            object? localValue,
            object? generatedValue)
        {
            if (IsOwned)
            {
                if (!IsSameOwnedValue(currentValue, Value)
                    || !IsSameOwnedValue(localValue, LocalValue))
                {
                    IsOwned = false;
                    Value = null;
                    LocalValue = null;
                    return false;
                }

                Value = generatedValue;
                return true;
            }

            if (localValue != DependencyProperty.UnsetValue)
            {
                return false;
            }

            if (currentValue is string currentText
                    ? !string.IsNullOrEmpty(currentText)
                    : currentValue is not null)
            {
                return false;
            }

            IsOwned = true;
            Value = generatedValue;
            return true;
        }

        internal void CaptureLocalValue(object? localValue)
            => LocalValue = localValue;

        private static bool IsSameOwnedValue(object? current, object? owned)
            => current?.GetType().IsValueType == true
                ? Equals(current, owned)
                : ReferenceEquals(current, owned);
    }

    private sealed class GeneratedValues
    {
        internal Dictionary<DependencyProperty, GeneratedValueOwnership> Values { get; } = new();
    }

    internal static T? FindAncestor<T>(DependencyObject child)
        where T : DependencyObject
    {
        for (var current = VisualTreeHelper.GetParent(child);
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is T match)
            {
                return match;
            }
        }

        return default;
    }

    internal static bool IsEffectivelyVisible(UIElement element)
    {
        for (DependencyObject? current = element;
             current is UIElement uiElement;
             current = VisualTreeHelper.GetParent(current))
        {
            if (uiElement.Visibility != Visibility.Visible)
            {
                return false;
            }
        }

        return true;
    }
}

internal static class AutomationProviderGuard
{
    internal static void EnsureEnabled(AutomationPeer peer)
    {
        ArgumentNullException.ThrowIfNull(peer);
        EnsureEnabled(peer.IsEnabled());
    }

    internal static void EnsureEnabled(bool isEnabled)
    {
        if (!isEnabled)
        {
            throw new UiaElementNotEnabledException();
        }
    }

    internal static void EnsureAvailable(bool isAvailable, string message)
    {
        if (!isAvailable)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static void Validate(
        AutomationPeer peer,
        bool isAvailable,
        string unavailableMessage)
    {
        EnsureEnabled(peer);
        EnsureAvailable(isAvailable, unavailableMessage);
    }

    internal static void Validate(
        bool isEnabled,
        bool isAvailable,
        string unavailableMessage)
    {
        EnsureEnabled(isEnabled);
        EnsureAvailable(isAvailable, unavailableMessage);
    }

    private sealed class UiaElementNotEnabledException : ElementNotEnabledException
    {
        internal UiaElementNotEnabledException()
        {
            HResult = unchecked((int)0x80040200);
        }
    }
}
