namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

internal sealed class ButtonPointerClickFallback : IDisposable
{
    private readonly WinUIButton button;
    private readonly Action action;
    private readonly PointerEventHandler pointerPressedHandler;
    private readonly PointerEventHandler pointerReleasedHandler;
    private readonly PointerEventHandler pointerCanceledHandler;
    private readonly RoutedEventHandler clickHandler;
    private bool pending;
    private bool disposed;

    private ButtonPointerClickFallback(WinUIButton button, Action action)
    {
        this.button = button;
        this.action = action;
        pointerPressedHandler = OnPointerPressed;
        pointerReleasedHandler = OnPointerReleased;
        pointerCanceledHandler = OnPointerCanceled;
        clickHandler = OnClick;

        button.AddHandler(UIElement.PointerPressedEvent, pointerPressedHandler, handledEventsToo: true);
        button.AddHandler(UIElement.PointerReleasedEvent, pointerReleasedHandler, handledEventsToo: true);
        button.AddHandler(UIElement.PointerCanceledEvent, pointerCanceledHandler, handledEventsToo: true);
        button.Click += clickHandler;
    }

    public static ButtonPointerClickFallback Attach(WinUIButton button, Action action)
        => new(button, action);

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        button.RemoveHandler(UIElement.PointerPressedEvent, pointerPressedHandler);
        button.RemoveHandler(UIElement.PointerReleasedEvent, pointerReleasedHandler);
        button.RemoveHandler(UIElement.PointerCanceledEvent, pointerCanceledHandler);
        button.Click -= clickHandler;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        pending = e.GetCurrentPoint(button).Properties.IsLeftButtonPressed;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (!pending || !IsInsideButton(e))
        {
            pending = false;
            return;
        }

        if (button.DispatcherQueue?.TryEnqueue(RunIfStillPending) != true)
        {
            RunIfStillPending();
        }
    }

    private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
    {
        pending = false;
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        pending = false;
    }

    private void RunIfStillPending()
    {
        if (!pending)
        {
            return;
        }
        pending = false;
        pending = false;
        action();
    }

    private bool IsInsideButton(PointerRoutedEventArgs e)
    {
        var position = e.GetCurrentPoint(button).Position;
        return position.X >= 0
            && position.Y >= 0
            && position.X <= button.ActualWidth
            && position.Y <= button.ActualHeight;
    }
}
