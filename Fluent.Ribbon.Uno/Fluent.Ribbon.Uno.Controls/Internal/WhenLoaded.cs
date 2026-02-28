namespace Fluent.Internal;

/// <summary>
/// Provides helpers for executing actions when a <see cref="FrameworkElement"/> is loaded.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
public static class WhenLoadedHelper
{
    /// <summary>
    /// Executes the action when the element is loaded, or immediately if already loaded.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="action">The action to execute.</param>
    public static void WhenLoaded(this FrameworkElement element, Action<FrameworkElement> action)
    {
        if (element.IsLoaded)
        {
            action(element);
            return;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            element.Loaded -= OnLoaded;
            action(element);
        }

        element.Loaded += OnLoaded;
    }

    /// <summary>
    /// Executes the action when the element is loaded, or immediately if already loaded.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="action">The action to execute (no parameters).</param>
    public static void WhenLoaded(this FrameworkElement element, Action action)
    {
        element.WhenLoaded(_ => action());
    }
}
