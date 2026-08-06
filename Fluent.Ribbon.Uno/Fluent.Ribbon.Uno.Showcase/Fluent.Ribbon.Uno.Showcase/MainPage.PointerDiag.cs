using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace FluentRibbon.Uno.Showcase;

/// <summary>
/// Opt-in pointer/focus routing tracer. Enabled with SHOWCASE_POINTER_DIAG=1 and written to
/// SHOWCASE_AUTOTEST_LOG. Exists because pointer defects that reproduce with a physical mouse are
/// invisible to UIA-driven automation, which bypasses the pointer pipeline entirely.
/// </summary>
public sealed partial class MainPage
{
    private static bool PointerDiagEnabled =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SHOWCASE_POINTER_DIAG"));

    private void InitializePointerDiagnostics()
    {
        if (!PointerDiagEnabled)
        {
            return;
        }

        Loaded += (_, _) =>
        {
            if (XamlRoot?.Content is not UIElement root)
            {
                return;
            }

            root.AddHandler(
                UIElement.PointerPressedEvent,
                new PointerEventHandler(OnDiagPointerPressed),
                handledEventsToo: true);
            root.AddHandler(
                UIElement.PointerReleasedEvent,
                new PointerEventHandler(OnDiagPointerReleased),
                handledEventsToo: true);
            root.AddHandler(
                UIElement.KeyDownEvent,
                new KeyEventHandler(OnDiagKeyDown),
                handledEventsToo: true);
            root.AddHandler(
                UIElement.PointerCaptureLostEvent,
                new PointerEventHandler(OnDiagCaptureLost),
                handledEventsToo: true);
            root.AddHandler(
                UIElement.PointerCanceledEvent,
                new PointerEventHandler(OnDiagPointerCanceled),
                handledEventsToo: true);
            root.AddHandler(
                UIElement.PointerExitedEvent,
                new PointerEventHandler(OnDiagPointerExited),
                handledEventsToo: true);

            LogDiag("POINTER DIAG ATTACHED");
        };
    }

    private void OnDiagPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        LogDiag($"PRESSED handled={e.Handled} src={ShortDescribe(source)} | {DescribeControlState(source)}");
    }

    private string DescribeControlState(DependencyObject? source)
    {
        var control = source;
        while (control is not null && control is not Control)
        {
            control = VisualTreeHelper.GetParent(control);
        }

        if (control is not Control c)
        {
            return "<no Control ancestor>";
        }

        var name = string.IsNullOrEmpty(c.Name) ? c.GetType().Name : $"{c.GetType().Name}#{c.Name}";
        return $"{name} enabled={c.IsEnabled} tabStop={c.IsTabStop} hitTest={c.IsHitTestVisible} " +
               $"allowFocus={c.AllowFocusOnInteraction} focusState={c.FocusState} " +
               $"captures={c.PointerCaptures?.Count ?? -1}";
    }

    private void OnDiagPointerReleased(object sender, PointerRoutedEventArgs e)
        => LogDiag($"RELEASED handled={e.Handled} src={ShortDescribe(e.OriginalSource as DependencyObject)} | {DescribeControlState(e.OriginalSource as DependencyObject)}");

    private void OnDiagCaptureLost(object sender, PointerRoutedEventArgs e)
        => LogDiag($"CAPTURELOST src={ShortDescribe(e.OriginalSource as DependencyObject)}");

    private void OnDiagPointerCanceled(object sender, PointerRoutedEventArgs e)
        => LogDiag($"CANCELED src={ShortDescribe(e.OriginalSource as DependencyObject)}");

    private void OnDiagPointerExited(object sender, PointerRoutedEventArgs e)
        => LogDiag($"EXITED src={ShortDescribe(e.OriginalSource as DependencyObject)}");

    private static string ShortDescribe(DependencyObject? element)
    {
        if (element is null)
        {
            return "<null>";
        }

        var name = element is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name) ? "#" + fe.Name : string.Empty;
        return element.GetType().Name + name;
    }

    private void OnDiagKeyDown(object sender, KeyRoutedEventArgs e)
        => LogDiag($"KEYDOWN key={e.Key} handled={e.Handled} keyTipsVisible={SafeKeyTipsVisible()} focus={DescribeFocus()}");

    private string SafeKeyTipsVisible()
    {
        try
        {
            return MainRibbon.AreAnyKeyTipsVisible.ToString();
        }
        catch
        {
            return "<error>";
        }
    }

    private string DescribeFocus()
    {
        try
        {
            var focused = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot!);
            return Describe(focused as DependencyObject);
        }
        catch
        {
            return "<focus error>";
        }
    }

    private static string Describe(DependencyObject? element)
    {
        if (element is null)
        {
            return "<null>";
        }

        var builder = new StringBuilder();
        var current = element;
        for (var depth = 0; depth < 40 && current is not null; depth++)
        {
            if (builder.Length > 0)
            {
                builder.Append(" < ");
            }

            var name = current is FrameworkElement fe && !string.IsNullOrEmpty(fe.Name)
                ? fe.Name
                : string.Empty;
            builder.Append(current.GetType().Name);
            if (name.Length > 0)
            {
                builder.Append('#').Append(name);
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return builder.ToString();
    }

    private static void LogDiag(string message)
    {
        var path = Environment.GetEnvironmentVariable("SHOWCASE_AUTOTEST_LOG");
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        try
        {
            File.AppendAllText(path, $"[PDIAG] {DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // Diagnostics must never break the showcase.
        }
    }
}
