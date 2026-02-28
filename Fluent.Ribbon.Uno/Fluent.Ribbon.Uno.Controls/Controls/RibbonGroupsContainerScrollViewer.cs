namespace Fluent;

/// <summary>
/// A specialized scroll viewer for the <see cref="RibbonGroupsContainer"/>
/// that handles horizontal mouse wheel scrolling and prevents scrolling when dropdowns are open.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WPF version overrides OnMouseWheel; Uno version handles PointerWheelChanged.
/// </remarks>
public partial class RibbonGroupsContainerScrollViewer : ScrollViewer
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonGroupsContainerScrollViewer"/> class.
    /// </summary>
    public RibbonGroupsContainerScrollViewer()
    {
        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        HorizontalScrollMode = ScrollMode.Enabled;
        VerticalScrollMode = ScrollMode.Disabled;

        PointerWheelChanged += OnPointerWheelChanged;
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        var delta = properties.MouseWheelDelta;

        if (delta == 0)
        {
            return;
        }

        // Check if any dropdown is open — if so, don't scroll
        if (IsAnyDropDownOpen())
        {
            return;
        }

        // Scroll horizontally
        if (delta > 0)
        {
            ChangeView(Math.Max(0, HorizontalOffset - 48), null, null);
        }
        else
        {
            ChangeView(HorizontalOffset + 48, null, null);
        }

        e.Handled = true;
    }

    private bool IsAnyDropDownOpen()
    {
        // Check children for open dropdowns
        foreach (var child in Fluent.Extensions.UIElementExtensions.FindVisualChildren<FrameworkElement>(this))
        {
            if (child is IDropDownControl dropDown && dropDown.IsDropDownOpen)
            {
                return true;
            }
        }

        return false;
    }
}
