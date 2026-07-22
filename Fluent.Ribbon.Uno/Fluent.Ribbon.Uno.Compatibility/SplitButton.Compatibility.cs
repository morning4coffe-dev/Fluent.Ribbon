namespace Fluent;

using System.Collections;
using Microsoft.UI.Xaml.Input;
using Windows.System;

public partial class SplitButton
{
    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (!e.Handled)
        {
            if (e.Key == VirtualKey.Escape && IsDropDownOpen)
            {
                CloseDropDown();
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Down)
            {
                OnKeyTipPressed();
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Enter)
            {
                InvokePrimaryAction();
                e.Handled = true;
            }
        }

        base.OnKeyDown(e);
    }

    /// <summary>Handles the WPF-compatible preview pointer hook.</summary>
    protected virtual void OnPreviewMouseLeftButtonDown(PointerRoutedEventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        OnPreviewMouseLeftButtonDown(e);
        base.OnPointerPressed(e);
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected override IEnumerator LogicalChildren
    {
        get
        {
            foreach (var item in Items)
            {
                yield return item;
            }

            if (Icon is not null)
            {
                yield return Icon;
            }

            if (Header is not null)
            {
                yield return Header;
            }
        }
    }
}
