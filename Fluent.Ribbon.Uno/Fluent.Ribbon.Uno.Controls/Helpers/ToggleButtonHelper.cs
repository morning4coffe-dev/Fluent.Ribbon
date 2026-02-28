namespace Fluent.Helpers;

/// <summary>
/// Helper for managing toggle button group behavior (radio-button-like exclusive selection).
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, simplified for Uno/WinUI.
/// The WPF version uses reflection to access KeyboardNavigation.GetVisualRoot;
/// this version uses a simpler dictionary-based approach.
/// </remarks>
public static class ToggleButtonHelper
{
    [ThreadStatic]
    private static Dictionary<string, List<WeakReference<Microsoft.UI.Xaml.Controls.Primitives.ToggleButton>>>? _groupsByName;

    private static Dictionary<string, List<WeakReference<Microsoft.UI.Xaml.Controls.Primitives.ToggleButton>>> Groups
        => _groupsByName ??= new Dictionary<string, List<WeakReference<Microsoft.UI.Xaml.Controls.Primitives.ToggleButton>>>();

    /// <summary>
    /// Registers a toggle button in a named group.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="button">The toggle button to register.</param>
    public static void Register(string groupName, Microsoft.UI.Xaml.Controls.Primitives.ToggleButton button)
    {
        if (string.IsNullOrEmpty(groupName))
        {
            return;
        }

        if (!Groups.TryGetValue(groupName, out var group))
        {
            group = new List<WeakReference<Microsoft.UI.Xaml.Controls.Primitives.ToggleButton>>();
            Groups[groupName] = group;
        }

        PurgeDeadReferences(group);
        group.Add(new WeakReference<Microsoft.UI.Xaml.Controls.Primitives.ToggleButton>(button));
    }

    /// <summary>
    /// Unregisters a toggle button from a named group.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="button">The toggle button to unregister.</param>
    public static void Unregister(string groupName, Microsoft.UI.Xaml.Controls.Primitives.ToggleButton button)
    {
        if (string.IsNullOrEmpty(groupName) || !Groups.TryGetValue(groupName, out var group))
        {
            return;
        }

        group.RemoveAll(wr => !wr.TryGetTarget(out var target) || ReferenceEquals(target, button));

        if (group.Count == 0)
        {
            Groups.Remove(groupName);
        }
    }

    /// <summary>
    /// Updates the group so that only the specified button is checked.
    /// Unchecks all other buttons in the group.
    /// </summary>
    /// <param name="groupName">The group name.</param>
    /// <param name="checkedButton">The button that should remain checked.</param>
    public static void UpdateButtonGroup(string groupName, Microsoft.UI.Xaml.Controls.Primitives.ToggleButton checkedButton)
    {
        if (string.IsNullOrEmpty(groupName) || !Groups.TryGetValue(groupName, out var group))
        {
            return;
        }

        foreach (var wr in group)
        {
            if (wr.TryGetTarget(out var button) && !ReferenceEquals(button, checkedButton))
            {
                button.IsChecked = false;
            }
        }
    }

    private static void PurgeDeadReferences(List<WeakReference<Microsoft.UI.Xaml.Controls.Primitives.ToggleButton>> group)
    {
        group.RemoveAll(wr => !wr.TryGetTarget(out _));
    }
}
