using Windows.System;

namespace Fluent;

/// <summary>
/// Base class for the lightweight menu-item controls that derive directly from
/// <see cref="Control"/> instead of <c>ButtonBase</c>, yet are expected to behave
/// like a button. It drives the <c>CommonStates</c> visual state group
/// (<c>Normal</c>/<c>PointerOver</c>/<c>Pressed</c>/<c>Disabled</c>), invokes on
/// pointer-release and keyboard (Enter/Space), is focusable, and shows the system
/// focus visual — so every consumer gets consistent, accessible click affordance.
/// Concrete items implement <see cref="OnInvoke"/>.
/// </summary>
public abstract partial class InteractiveMenuItemBase : Control
{
    private bool _isPointerOver;
    private bool _isPressed;

    /// <summary>
    /// Initializes a new instance of the <see cref="InteractiveMenuItemBase"/> class.
    /// </summary>
    protected InteractiveMenuItemBase()
    {
        IsTabStop = true;
        UseSystemFocusVisuals = true;
        IsEnabledChanged += OnIsEnabledChanged;
    }

    /// <summary>
    /// Called when the item is activated by a pointer release or a keyboard
    /// Enter/Space press. Implementations perform the item's action.
    /// </summary>
    protected abstract void OnInvoke();

    /// <summary>
    /// Invokes the item from the keyboard. Menu items with submenus override this
    /// to transfer focus without changing pointer activation.
    /// </summary>
    protected virtual void OnKeyboardInvoke(VirtualKey key) => OnInvoke();

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualState(false);
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        _isPointerOver = true;
        UpdateVisualState(true);
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        _isPointerOver = false;
        _isPressed = false;
        UpdateVisualState(true);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);

        if (!IsEnabled)
        {
            return;
        }

        _isPressed = true;
        Focus(FocusState.Pointer);
        UpdateVisualState(true);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);

        var shouldInvoke = _isPressed && IsEnabled;
        _isPressed = false;
        UpdateVisualState(true);

        if (shouldInvoke)
        {
            e.Handled = true;
            OnInvoke();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isPressed = false;
        UpdateVisualState(true);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        if (!IsEnabled)
        {
            return;
        }

        switch (e.Key)
        {
            case VirtualKey.Enter:
                e.Handled = true;
                OnKeyboardInvoke(e.Key);
                break;
            case VirtualKey.Space:
                _isPressed = true;
                UpdateVisualState(true);
                e.Handled = true;
                break;
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyUp(KeyRoutedEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.Key == VirtualKey.Space)
        {
            var shouldInvoke = _isPressed && IsEnabled;
            _isPressed = false;
            UpdateVisualState(true);

            if (shouldInvoke)
            {
                e.Handled = true;
                OnKeyboardInvoke(e.Key);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        UpdateVisualState(true);
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        _isPressed = false;
        UpdateVisualState(true);
    }

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        => UpdateVisualState(true);

    /// <summary>
    /// Transitions the control to the visual state that reflects the current
    /// pointer/pressed/enabled condition.
    /// </summary>
    /// <param name="useTransitions">Whether to use visual transitions.</param>
    protected void UpdateVisualState(bool useTransitions)
    {
        string state;
        if (!IsEnabled)
        {
            state = "Disabled";
        }
        else if (_isPressed)
        {
            state = "Pressed";
        }
        else if (_isPointerOver)
        {
            state = "PointerOver";
        }
        else
        {
            state = "Normal";
        }

        VisualStateManager.GoToState(this, state, useTransitions);
    }
}
