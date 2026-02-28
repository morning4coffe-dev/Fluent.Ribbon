namespace Fluent.Extensions;

/// <summary>
/// Extension methods for <see cref="UIElement"/> visual tree traversal.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// Uses <see cref="Microsoft.UI.Xaml.Media.VisualTreeHelper"/> instead of WPF's LogicalTreeHelper.
/// </remarks>
public static class UIElementExtensions
{
    /// <summary>
    /// Finds the first visual child of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of child to find.</typeparam>
    /// <param name="element">The parent element.</param>
    /// <returns>The first child of type <typeparamref name="T"/>, or <c>null</c>.</returns>
    public static T? FindVisualChild<T>(this DependencyObject element)
        where T : DependencyObject
    {
        var childrenCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);

        for (var i = 0; i < childrenCount; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);

            if (child is T typedChild)
            {
                return typedChild;
            }

            var result = FindVisualChild<T>(child);
            if (result is not null)
            {
                return result;
            }
        }

        return default;
    }

    /// <summary>
    /// Finds all visual children of a specific type.
    /// </summary>
    /// <typeparam name="T">The type of children to find.</typeparam>
    /// <param name="element">The parent element.</param>
    /// <returns>An enumerable of all children of type <typeparamref name="T"/>.</returns>
    public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject element)
        where T : DependencyObject
    {
        var childrenCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);

        for (var i = 0; i < childrenCount; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);

            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets the first visual child of the element.
    /// </summary>
    /// <param name="element">The parent element.</param>
    /// <returns>The first visual child, or <c>null</c>.</returns>
    public static DependencyObject? GetFirstVisualChild(this DependencyObject element)
    {
        var childrenCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
        return childrenCount > 0
            ? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, 0)
            : null;
    }

    /// <summary>
    /// Finds the nearest ancestor of a specific type by walking the visual tree.
    /// </summary>
    /// <typeparam name="T">The type of ancestor to find.</typeparam>
    /// <param name="element">The starting element.</param>
    /// <returns>The nearest ancestor of type <typeparamref name="T"/>, or <c>null</c>.</returns>
    public static T? FindVisualAncestor<T>(this DependencyObject element)
        where T : DependencyObject
    {
        var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);

        while (parent is not null)
        {
            if (parent is T typedParent)
            {
                return typedParent;
            }

            parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent);
        }

        return default;
    }

    /// <summary>
    /// Gets the visual parent of the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The visual parent, or <c>null</c>.</returns>
    public static DependencyObject? GetVisualParent(this DependencyObject element)
    {
        return Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(element);
    }
}
