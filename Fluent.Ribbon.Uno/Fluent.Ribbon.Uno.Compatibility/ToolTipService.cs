using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Fluent;

/// <summary>
/// Maps Fluent tooltip compatibility APIs to WinUI's tooltip service.
/// </summary>
/// <remarks>
/// WPF timing metadata overrides such as initial delay and show duration are not
/// portable in WinUI. <see cref="Attach"/> therefore validates intent only.
/// </remarks>
public static class ToolTipService
{
    /// <summary>Aliases the WinUI tooltip dependency property.</summary>
    public static DependencyProperty ToolTipProperty =>
        Microsoft.UI.Xaml.Controls.ToolTipService.ToolTipProperty;

    /// <summary>Aliases the WinUI tooltip placement dependency property.</summary>
    public static DependencyProperty PlacementProperty =>
        Microsoft.UI.Xaml.Controls.ToolTipService.PlacementProperty;

    /// <summary>Aliases the WinUI tooltip placement-target dependency property.</summary>
    public static DependencyProperty PlacementTargetProperty =>
        Microsoft.UI.Xaml.Controls.ToolTipService.PlacementTargetProperty;

    /// <summary>Registers compatibility intent for a control type.</summary>
    public static void Attach(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
    }

    /// <summary>Gets an attached tooltip.</summary>
    public static object? GetToolTip(DependencyObject element) =>
        Microsoft.UI.Xaml.Controls.ToolTipService.GetToolTip(element);

    /// <summary>Sets an attached tooltip.</summary>
    public static void SetToolTip(DependencyObject element, object? value) =>
        Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(element, value);

    /// <summary>Gets tooltip placement.</summary>
    public static PlacementMode GetPlacement(DependencyObject element) =>
        Microsoft.UI.Xaml.Controls.ToolTipService.GetPlacement(element);

    /// <summary>Sets tooltip placement.</summary>
    public static void SetPlacement(DependencyObject element, PlacementMode value) =>
        Microsoft.UI.Xaml.Controls.ToolTipService.SetPlacement(element, value);

    /// <summary>Gets the tooltip placement target.</summary>
    public static UIElement? GetPlacementTarget(DependencyObject element) =>
        Microsoft.UI.Xaml.Controls.ToolTipService.GetPlacementTarget(element);

    /// <summary>Sets the tooltip placement target.</summary>
    public static void SetPlacementTarget(DependencyObject element, UIElement? value) =>
        Microsoft.UI.Xaml.Controls.ToolTipService.SetPlacementTarget(element, value);
}
