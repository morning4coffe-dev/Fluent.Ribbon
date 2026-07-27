namespace Fluent.Helpers;

/// <summary>
/// Provides helper methods for managing items in dropdown controls.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, simplified for Uno/WinUI.
/// </remarks>
public static class ItemsControlHelper
{
    /// <summary>
    /// Moves all children from one panel to another.
    /// </summary>
    /// <param name="source">The source panel.</param>
    /// <param name="target">The target panel.</param>
    public static void MoveChildren(Panel source, Panel target)
    {
        var children = source.Children.ToList();
        source.Children.Clear();

        foreach (var child in children)
        {
            target.Children.Add(child);
        }
    }

    /// <summary>
    /// Removes a child from its current parent panel, if any.
    /// </summary>
    /// <param name="child">The child element to detach.</param>
    public static void DetachFromParent(UIElement child)
    {
        // On the native WinUI head the logical Parent reads null for elements hosted directly in a
        // Panel's Children, so the host must also be resolved through the visual tree. Missing it
        // makes this a silent no-op and the subsequent re-add throws COMException 0x800F1000.
        if (VisualTreeHelper.GetParent(child) is Panel visualParent)
        {
            visualParent.Children.Remove(child);
        }

        if (child is FrameworkElement { Parent: Panel logicalParent })
        {
            logicalParent.Children.Remove(child);
        }
    }
}
