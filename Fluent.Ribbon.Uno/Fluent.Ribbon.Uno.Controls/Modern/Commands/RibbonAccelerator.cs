namespace Fluent.Modern.Commands;

using System.Diagnostics;
using System.Reflection;
using Fluent;
using Windows.System;

/// <summary>
/// <para><b>Modern extension</b> — attaches Office-style keyboard accelerators to ribbon controls.</para>
/// </summary>
[ModernExtension]
public static class RibbonAccelerator
{
    private static readonly DependencyProperty AppliedAcceleratorProperty =
        DependencyProperty.RegisterAttached(
            "AppliedAccelerator",
            typeof(KeyboardAccelerator),
            typeof(RibbonAccelerator),
            new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalToolTipProperty =
        DependencyProperty.RegisterAttached(
            "OriginalToolTip",
            typeof(object),
            typeof(RibbonAccelerator),
            new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalToolTipCapturedProperty =
        DependencyProperty.RegisterAttached(
            "OriginalToolTipCaptured",
            typeof(bool),
            typeof(RibbonAccelerator),
            new PropertyMetadata(false));

    #region Dependency Properties

    /// <summary>Identifies the Gesture attached dependency property.</summary>
    public static readonly DependencyProperty GestureProperty =
        DependencyProperty.RegisterAttached(
            "Gesture",
            typeof(string),
            typeof(RibbonAccelerator),
            new PropertyMetadata(string.Empty, OnGestureChanged));

    /// <summary>Identifies the ShowInScreenTip attached dependency property.</summary>
    public static readonly DependencyProperty ShowInScreenTipProperty =
        DependencyProperty.RegisterAttached(
            "ShowInScreenTip",
            typeof(bool),
            typeof(RibbonAccelerator),
            new PropertyMetadata(true, OnShowInScreenTipChanged));

    /// <summary>Identifies the AcceleratorText attached dependency property.</summary>
    public static readonly DependencyProperty AcceleratorTextProperty =
        DependencyProperty.RegisterAttached(
            "AcceleratorText",
            typeof(string),
            typeof(RibbonAccelerator),
            new PropertyMetadata(string.Empty));

    #endregion

    #region Attached Property Accessors

    /// <summary>
    /// Gets the gesture string used to create a keyboard accelerator.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The gesture string.</returns>
    public static string GetGesture(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (string)element.GetValue(GestureProperty);
    }

    /// <summary>
    /// Sets the gesture string used to create a keyboard accelerator.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The gesture string.</param>
    public static void SetGesture(DependencyObject element, string value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(GestureProperty, value);
    }

    /// <summary>
    /// Gets whether the formatted gesture should be surfaced in the element ScreenTip.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns><c>true</c> when the ScreenTip should include the gesture; otherwise <c>false</c>.</returns>
    public static bool GetShowInScreenTip(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(ShowInScreenTipProperty);
    }

    /// <summary>
    /// Sets whether the formatted gesture should be surfaced in the element ScreenTip.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value"><c>true</c> to include the gesture in the ScreenTip; otherwise <c>false</c>.</param>
    public static void SetShowInScreenTip(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(ShowInScreenTipProperty, value);
    }

    /// <summary>
    /// Gets the formatted accelerator text, such as <c>Ctrl+Shift+P</c>.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The formatted accelerator text.</returns>
    public static string GetAcceleratorText(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (string)element.GetValue(AcceleratorTextProperty);
    }

    private static void SetAcceleratorText(DependencyObject element, string value)
    {
        element.SetValue(AcceleratorTextProperty, value);
    }

    #endregion

    #region Property Changed

    private static void OnGestureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not UIElement element)
        {
            return;
        }

        RemoveAppliedAccelerator(element);
        SetAcceleratorText(element, string.Empty);

        var gesture = e.NewValue as string;
        if (!TryParseGesture(gesture, out var key, out var modifiers, out var displayText))
        {
            Debug.WriteLine($"RibbonAccelerator ignored invalid gesture '{gesture}'.");
            UpdateScreenTip(element);
            return;
        }

        var accelerator = new KeyboardAccelerator
        {
            Key = key,
            Modifiers = modifiers,
        };

        accelerator.Invoked += (_, args) =>
        {
            RibbonInvoker.Invoke(element);
            args.Handled = true;
        };

        element.KeyboardAccelerators.Add(accelerator);
        element.SetValue(AppliedAcceleratorProperty, accelerator);
        SetAcceleratorText(element, displayText);
        UpdateScreenTip(element);
    }

    private static void OnShowInScreenTipChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is UIElement element)
        {
            UpdateScreenTip(element);
        }
    }

    #endregion

    #region Gesture Parsing

    private static bool TryParseGesture(string? gesture, out VirtualKey key, out VirtualKeyModifiers modifiers, out string displayText)
    {
        key = VirtualKey.None;
        modifiers = VirtualKeyModifiers.None;
        displayText = string.Empty;

        if (string.IsNullOrWhiteSpace(gesture))
        {
            return false;
        }

        var tokens = gesture.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        for (var i = 0; i < tokens.Length - 1; i++)
        {
            if (!TryParseModifier(tokens[i], out var modifier))
            {
                return false;
            }

            modifiers |= modifier;
        }

        if (!TryParseKey(tokens[^1], out key))
        {
            return false;
        }

        displayText = FormatGesture(key, modifiers);
        return true;
    }

    private static bool TryParseModifier(string token, out VirtualKeyModifiers modifier)
    {
        modifier = token.Trim().ToUpperInvariant() switch
        {
            "CTRL" or "CONTROL" => VirtualKeyModifiers.Control,
            "SHIFT" => VirtualKeyModifiers.Shift,
            "ALT" or "MENU" => VirtualKeyModifiers.Menu,
            "WIN" or "WINDOWS" => VirtualKeyModifiers.Windows,
            _ => VirtualKeyModifiers.None
        };

        return modifier != VirtualKeyModifiers.None;
    }

    private static bool TryParseKey(string token, out VirtualKey key)
    {
        key = VirtualKey.None;
        var normalized = token.Trim();
        if (normalized.Length == 1)
        {
            var c = char.ToUpperInvariant(normalized[0]);
            if (c is >= 'A' and <= 'Z')
            {
                key = (VirtualKey)c;
                return true;
            }

            if (c is >= '0' and <= '9')
            {
                key = (VirtualKey)c;
                return true;
            }
        }

        var upper = normalized.ToUpperInvariant();
        if (upper.Length is >= 2 and <= 3 && upper[0] == 'F' && int.TryParse(upper[1..], out var functionKey) && functionKey is >= 1 and <= 24)
        {
            key = (VirtualKey)((int)VirtualKey.F1 + functionKey - 1);
            return true;
        }

        key = upper switch
        {
            "ENTER" => VirtualKey.Enter,
            "ESC" or "ESCAPE" => VirtualKey.Escape,
            "SPACE" => VirtualKey.Space,
            "DELETE" or "DEL" => VirtualKey.Delete,
            "INSERT" or "INS" => VirtualKey.Insert,
            "HOME" => VirtualKey.Home,
            "END" => VirtualKey.End,
            "PAGEUP" => VirtualKey.PageUp,
            "PAGEDOWN" => VirtualKey.PageDown,
            "TAB" => VirtualKey.Tab,
            "LEFT" or "LEFTARROW" => VirtualKey.Left,
            "UP" or "UPARROW" => VirtualKey.Up,
            "RIGHT" or "RIGHTARROW" => VirtualKey.Right,
            "DOWN" or "DOWNARROW" => VirtualKey.Down,
            _ => VirtualKey.None
        };

        return key != VirtualKey.None;
    }

    private static string FormatGesture(VirtualKey key, VirtualKeyModifiers modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(VirtualKeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(FormatKey(key));
        return string.Join("+", parts);
    }

    private static string FormatKey(VirtualKey key)
    {
        var keyValue = (int)key;
        if (keyValue is >= 'A' and <= 'Z' or >= '0' and <= '9')
        {
            return ((char)keyValue).ToString();
        }

        if (key is >= VirtualKey.F1 and <= VirtualKey.F24)
        {
            return $"F{(int)key - (int)VirtualKey.F1 + 1}";
        }

        return key switch
        {
            VirtualKey.Escape => "Esc",
            VirtualKey.PageUp => "PageUp",
            VirtualKey.PageDown => "PageDown",
            VirtualKey.Left => "Left",
            VirtualKey.Up => "Up",
            VirtualKey.Right => "Right",
            VirtualKey.Down => "Down",
            _ => key.ToString()
        };
    }

    #endregion

    #region ScreenTip Integration

    private static void UpdateScreenTip(UIElement element)
    {
        if (element is not FrameworkElement frameworkElement)
        {
            return;
        }

        var acceleratorText = GetAcceleratorText(element);
        if (!GetShowInScreenTip(element) || string.IsNullOrWhiteSpace(acceleratorText))
        {
            RestoreOriginalToolTip(frameworkElement);
            return;
        }

        CaptureOriginalToolTip(frameworkElement);

        var baseScreenTip = GetBaseScreenTip(frameworkElement);
        var title = GetStringProperty(frameworkElement, "ScreenTipTitle")
            ?? baseScreenTip?.Title
            ?? GetHeaderText(frameworkElement);
        var text = GetStringProperty(frameworkElement, "ScreenTipText")
            ?? Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(baseScreenTip?.Text)
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text))
        {
            ToolTipService.SetToolTip(frameworkElement, acceleratorText);
            return;
        }

        ScreenTip.Attach(frameworkElement, title ?? string.Empty, AppendGesture(text, acceleratorText));
    }

    private static void CaptureOriginalToolTip(FrameworkElement element)
    {
        if ((bool)element.GetValue(OriginalToolTipCapturedProperty))
        {
            return;
        }

        element.SetValue(OriginalToolTipProperty, ToolTipService.GetToolTip(element));
        element.SetValue(OriginalToolTipCapturedProperty, true);
    }

    private static void RestoreOriginalToolTip(FrameworkElement element)
    {
        if (!(bool)element.GetValue(OriginalToolTipCapturedProperty))
        {
            return;
        }

        ToolTipService.SetToolTip(element, element.GetValue(OriginalToolTipProperty));
        element.ClearValue(OriginalToolTipProperty);
        element.ClearValue(OriginalToolTipCapturedProperty);
    }

    private static ScreenTip? GetBaseScreenTip(FrameworkElement element)
    {
        if ((bool)element.GetValue(OriginalToolTipCapturedProperty)
            && element.GetValue(OriginalToolTipProperty) is ScreenTip originalScreenTip)
        {
            return originalScreenTip;
        }

        return ToolTipService.GetToolTip(element) as ScreenTip;
    }

    private static string AppendGesture(string? text, string acceleratorText)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return acceleratorText;
        }

        var trimmed = text.Trim();
        return trimmed.Contains(acceleratorText, StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed} ({acceleratorText})";
    }

    private static string? GetStringProperty(object item, string propertyName)
    {
        var value = GetPropertyValue<object>(item, propertyName);
        return value is string text && !string.IsNullOrWhiteSpace(text) ? text : null;
    }

    private static string? GetHeaderText(object item)
    {
        var value = GetPropertyValue<object>(item, "Header") ?? GetPropertyValue<object>(item, "Content");
        var name = Fluent.Automation.Peers.AutomationPeerHelpers.GetObjectName(value);
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static T? GetPropertyValue<T>(object source, string propertyName)
    {
        var property = global::Fluent.Modern.ReflectionPropertyHelper.GetReadableProperty(
            source.GetType(),
            propertyName);
        if (property?.GetMethod is null)
        {
            return default;
        }

        return property.GetValue(source) is T value ? value : default;
    }

    #endregion

    #region Helpers

    private static void RemoveAppliedAccelerator(UIElement element)
    {
        if (element.GetValue(AppliedAcceleratorProperty) is not KeyboardAccelerator accelerator)
        {
            return;
        }

        element.KeyboardAccelerators.Remove(accelerator);
        element.ClearValue(AppliedAcceleratorProperty);
    }

    #endregion
}
