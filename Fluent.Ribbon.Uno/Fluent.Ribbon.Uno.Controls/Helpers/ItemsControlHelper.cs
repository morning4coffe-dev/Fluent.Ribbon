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
        if (child is FrameworkElement fe && fe.Parent is Panel parentPanel)
        {
            parentPanel.Children.Remove(child);
        }
    }
}
