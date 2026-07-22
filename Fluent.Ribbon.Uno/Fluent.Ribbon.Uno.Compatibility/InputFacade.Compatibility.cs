namespace Fluent;

using System.Collections;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

public partial class ComboBox
{
    /// <summary>Gets the template drop-down popup when available.</summary>
    public Popup? DropDownPopup { get; private set; }

    /// <summary>Handles preview keyboard input.</summary>
    protected override void OnPreviewKeyDown(KeyRoutedEventArgs e)
    {
        if (IsEditable
            && e.Key is VirtualKey.Down or VirtualKey.Up
            && IsDropDownOpen is false)
        {
            IsDropDownOpen = true;
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (!e.Handled && e.Key == VirtualKey.Escape && IsDropDownOpen)
        {
            IsDropDownOpen = false;
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
    {
        get
        {
            foreach (var item in Items)
            {
                yield return item;
            }

            if (Menu is not null)
            {
                yield return Menu;
            }

            if (Icon is not null)
            {
                yield return Icon;
            }

            if (Header is not null)
            {
                yield return Header;
            }

            if (TopPopupContent is not null)
            {
                yield return TopPopupContent;
            }
        }
    }
}

public partial class TextBox
{
    /// <summary>Handles context-menu opening.</summary>
    protected virtual void OnContextMenuOpening(ContextMenuEventArgs e)
    {
    }

    /// <summary>Handles context-menu closing.</summary>
    protected virtual void OnContextMenuClosing(ContextMenuEventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Enter or VirtualKey.Space)
        {
            return;
        }

        base.OnKeyUp(e);
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
    {
        get
        {
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
