namespace Fluent;

using System.Collections;
using Microsoft.UI.Xaml.Input;

public partial class RibbonTabItem
{
    /// <summary>Handles the WPF-compatible keyboard-focus hook.</summary>
    protected virtual void OnGotKeyboardFocus(RoutedEventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        OnGotKeyboardFocus(e);
    }

    /// <summary>Handles the WPF-compatible primary-pointer hook.</summary>
    protected virtual void OnMouseLeftButtonDown(PointerRoutedEventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        OnMouseLeftButtonDown(e);
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
    {
        get
        {
            foreach (var group in Groups)
            {
                yield return group;
            }

            if (Header is not null)
            {
                yield return Header;
            }
        }
    }

    /// <inheritdoc />
    void ILogicalChildSupport.AddLogicalChild(object child)
    {
    }

    /// <inheritdoc />
    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
    }
}
