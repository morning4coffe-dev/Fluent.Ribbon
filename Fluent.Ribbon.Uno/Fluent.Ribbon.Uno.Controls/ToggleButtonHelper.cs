namespace Fluent;

/// <summary>
/// Provides WPF-compatible toggle-button group callbacks.
/// </summary>
public static class ToggleButtonHelper
{
    /// <summary>
    /// Registers a platform toggle button in a named group.
    /// </summary>
    public static void Register(
        string groupName,
        Microsoft.UI.Xaml.Controls.Primitives.ToggleButton button)
    {
        Helpers.ToggleButtonHelper.Register(groupName, button);
    }

    /// <summary>
    /// Removes a platform toggle button from a named group.
    /// </summary>
    public static void Unregister(
        string groupName,
        Microsoft.UI.Xaml.Controls.Primitives.ToggleButton button)
    {
        Helpers.ToggleButtonHelper.Unregister(groupName, button);
    }

    /// <summary>
    /// Updates a named group around the checked platform button.
    /// </summary>
    public static void UpdateButtonGroup(
        string groupName,
        Microsoft.UI.Xaml.Controls.Primitives.ToggleButton checkedButton)
    {
        Helpers.ToggleButtonHelper.UpdateButtonGroup(groupName, checkedButton);
    }

    /// <summary>
    /// Updates group registration after the GroupName dependency property changes.
    /// </summary>
    public static void OnGroupNameChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is not IToggleButton toggleButton
            || d is not Microsoft.UI.Xaml.Controls.Primitives.ToggleButton platformButton)
        {
            return;
        }

        var oldValue = e.OldValue as string;
        var newValue = e.NewValue as string;
        if (string.Equals(oldValue, newValue, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.IsNullOrEmpty(oldValue))
        {
            Helpers.ToggleButtonHelper.Unregister(oldValue, platformButton);
        }

        if (!string.IsNullOrEmpty(newValue))
        {
            Helpers.ToggleButtonHelper.Register(newValue, platformButton);
            if (toggleButton.IsChecked == true)
            {
                Helpers.ToggleButtonHelper.UpdateButtonGroup(newValue, platformButton);
            }
        }
    }

    /// <summary>
    /// Unchecks the other toggle buttons in the same named group.
    /// </summary>
    public static void UpdateButtonGroup(IToggleButton toggleButton)
    {
        ArgumentNullException.ThrowIfNull(toggleButton);

        if (!string.IsNullOrEmpty(toggleButton.GroupName)
            && toggleButton is Microsoft.UI.Xaml.Controls.Primitives.ToggleButton platformButton)
        {
            Helpers.ToggleButtonHelper.UpdateButtonGroup(toggleButton.GroupName, platformButton);
        }
    }

    /// <summary>
    /// Updates the named group after a toggle becomes checked.
    /// </summary>
    public static void OnIsCheckedChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true && d is IToggleButton toggleButton)
        {
            UpdateButtonGroup(toggleButton);
        }
    }
}
