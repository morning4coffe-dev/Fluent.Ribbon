namespace Fluent;

using Microsoft.UI.Xaml.Input;
using Windows.System;

public partial class GalleryItem
{
    /// <summary>Gets whether the item and its command are currently enabled.</summary>
    protected virtual bool IsEnabledCore =>
        IsEnabled && (Command?.CanExecute(CommandParameter) ?? true);

    /// <inheritdoc />
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        base.OnKeyUp(e);

        if (!e.Handled && e.Key == VirtualKey.Enter)
        {
            RaiseClick();
            e.Handled = true;
        }
    }

    /// <summary>Handles pointer-capture loss.</summary>
    protected virtual void OnLostMouseCapture(PointerRoutedEventArgs e)
    {
    }

    /// <summary>Handles pointer entry.</summary>
    protected virtual void OnMouseEnter(PointerRoutedEventArgs e)
    {
    }

    /// <summary>Handles pointer exit.</summary>
    protected virtual void OnMouseLeave(PointerRoutedEventArgs e)
    {
    }

    /// <summary>Handles primary-pointer press.</summary>
    protected virtual void OnMouseLeftButtonDown(PointerRoutedEventArgs e)
    {
    }

    /// <summary>Handles primary-pointer release.</summary>
    protected virtual void OnMouseLeftButtonUp(PointerRoutedEventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        OnLostMouseCapture(e);
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        OnMouseEnter(e);
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        OnMouseLeave(e);
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        OnMouseLeftButtonDown(e);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        OnMouseLeftButtonUp(e);
    }
}
