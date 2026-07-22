namespace Fluent.Modern.Commands;

using System.Reflection;
using Fluent;
using Fluent.Extensions;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

/// <summary>
/// <para><b>Modern extension</b> — shared command invocation helper for modern ribbon features.</para>
/// </summary>
[ModernExtension]
public static class RibbonInvoker
{
    #region Methods

    /// <summary>
    /// Invokes a ribbon control using UI Automation first, then command execution as a fallback.
    /// </summary>
    /// <param name="control">The ribbon control to invoke.</param>
    /// <returns><c>true</c> when an invoke provider or executable command handled the request; otherwise <c>false</c>.</returns>
    public static bool Invoke(DependencyObject control)
    {
        ArgumentNullException.ThrowIfNull(control);

        if (TryInvokeAutomation(control))
        {
            return true;
        }

        return TryExecuteCommand(control);
    }

    /// <summary>
    /// Invokes a ribbon control using UI Automation first, then command execution as a fallback.
    /// </summary>
    /// <param name="control">The ribbon control to invoke.</param>
    /// <returns><c>true</c> when an invoke provider or executable command handled the request; otherwise <c>false</c>.</returns>
    public static bool Invoke(Control control)
    {
        return Invoke((DependencyObject)control);
    }

    private static bool TryInvokeAutomation(DependencyObject control)
    {
        if (control is not UIElement element)
        {
            return false;
        }

        var peer = FrameworkElementAutomationPeer.FromElement(element) ?? FrameworkElementAutomationPeer.CreatePeerForElement(element);
        if (peer?.GetPattern(PatternInterface.Invoke) is not IInvokeProvider invokeProvider)
        {
            return false;
        }

        invokeProvider.Invoke();
        return true;
    }

    private static bool TryExecuteCommand(DependencyObject control)
    {
        var command = GetPropertyValue<ICommand>(control, "Command");
        var commandParameter = GetPropertyValue<object>(control, "CommandParameter");
        if (!ICommandSourceExtensions.CanExecuteCommandSource(command, commandParameter))
        {
            return false;
        }

        ICommandSourceExtensions.ExecuteCommandSource(command, commandParameter);
        return true;
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
}
