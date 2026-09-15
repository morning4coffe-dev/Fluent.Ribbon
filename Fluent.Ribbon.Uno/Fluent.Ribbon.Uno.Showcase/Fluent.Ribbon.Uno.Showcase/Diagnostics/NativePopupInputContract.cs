#if WINDOWS
namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

internal static class NativePopupInputContract
{
    internal static async Task WaitForExternalClickAsync(FrameworkElement target, Popup? expectedClosure = null)
    {
        if (target is not { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0, XamlRoot: not null })
        {
            throw new InvalidOperationException("The external pointer target is not a real measured UI element.");
        }
        var stage = expectedClosure is null ? "persistent-popup-outside-click" : "light-dismiss-outside-click";
        if (Environment.GetEnvironmentVariable("SHOWCASE_NATIVE_POPUP_EXTERNAL_INPUT") != "1")
        {
            throw new NativePopupInputUnavailableException(
                $"External pointer verification is not enabled. Case=inert-popup-properties, Stage={stage}, "
                + $"AutomationId={AutomationProperties.GetAutomationId(target)}. The full native gate remains blocked.");
        }
        var responseTimeout = ShowcaseDiagnosticOptions.GetExternalInputTimeout(
            Environment.GetEnvironmentVariable("SHOWCASE_NATIVE_POPUP_INPUT_TIMEOUT_SECONDS"));

        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        PointerEventHandler pressed = (_, _) => completed.TrySetResult();
        EventHandler<object> closed = (_, _) => completed.TrySetResult();
        if (expectedClosure is null)
        {
            target.AddHandler(UIElement.PointerPressedEvent, pressed, true);
        }
        else
        {
            expectedClosure.Closed += closed;
        }
        try
        {
            if (target is Button button)
            {
                button.Content = expectedClosure is null ? "Click here - 1 of 2" : "Click here - 2 of 2";
            }
            App.LogAutoTestStartup(
                $"NATIVE EXTERNAL POINTER READY pid={Environment.ProcessId} case=inert-popup-properties "
                + $"stage={stage} target={AutomationProperties.GetAutomationId(target)} "
                + "action=physical-click-target-with-approved-computer-use");
            await completed.Task.WaitAsync(responseTimeout);
            App.LogAutoTestStartup($"NATIVE EXTERNAL POINTER COMPLETED stage={stage}");
        }
        finally
        {
            if (expectedClosure is null)
            {
                target.RemoveHandler(UIElement.PointerPressedEvent, pressed);
            }
            else
            {
                expectedClosure.Closed -= closed;
            }
        }
    }
}

internal sealed class NativePopupInputUnavailableException(string message) : InvalidOperationException(message);
#endif
