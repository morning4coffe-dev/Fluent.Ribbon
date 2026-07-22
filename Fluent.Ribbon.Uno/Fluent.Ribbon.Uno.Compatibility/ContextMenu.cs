using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Fluent;

/// <summary>
/// WinUI context-menu compatibility surface backed by <see cref="MenuFlyout"/>.
/// </summary>
/// <remarks>
/// WPF's template thumbs and live resize behavior have no MenuFlyout equivalent.
/// <see cref="ResizeMode"/> is retained as capability metadata but is not applied.
/// </remarks>
public class ContextMenu : MenuFlyout
{
    internal bool IsQuickAccessCompatibilityMenu { get; set; }

    /// <summary>Identifies the <see cref="ResizeMode"/> dependency property.</summary>
    public static readonly DependencyProperty ResizeModeProperty =
        DependencyProperty.Register(
            nameof(ResizeMode),
            typeof(ContextMenuResizeMode),
            typeof(ContextMenu),
            new PropertyMetadata(ContextMenuResizeMode.None));

    /// <summary>
    /// Gets or sets the requested resize mode.
    /// </summary>
    /// <remarks>MenuFlyout does not support user resizing; this value is informational.</remarks>
    public ContextMenuResizeMode ResizeMode
    {
        get => (ContextMenuResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    /// <summary>Creates the default menu-item container.</summary>
    protected virtual DependencyObject GetContainerForItemOverride()
    {
        return new MenuFlyoutItem();
    }

    /// <summary>Gets whether an item is already its own menu container.</summary>
    protected virtual bool IsItemItsOwnContainerOverride(object item)
    {
        return item is MenuFlyoutItemBase or MenuFlyoutSeparator;
    }

    /// <summary>Applies compatibility template state.</summary>
    public virtual void OnApplyTemplate()
    {
    }
}
