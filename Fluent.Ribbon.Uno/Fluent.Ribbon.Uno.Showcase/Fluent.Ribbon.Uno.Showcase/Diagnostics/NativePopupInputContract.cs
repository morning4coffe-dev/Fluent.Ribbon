#if WINDOWS
namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

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
        uint? pressedPointerId = null;
        PointerEventHandler pressed = (_, args) =>
        {
            pressedPointerId ??= args.Pointer.PointerId;
            App.LogAutoTestStartup(
                $"NATIVE EXTERNAL POINTER PRESSED stage={stage} pointer={args.Pointer.PointerId}");
        };
        PointerEventHandler released = (_, args) =>
        {
            if (pressedPointerId == args.Pointer.PointerId)
            {
                App.LogAutoTestStartup(
                    $"NATIVE EXTERNAL POINTER RELEASED stage={stage} pointer={args.Pointer.PointerId}");
                completed.TrySetResult();
            }
        };
        PointerEventHandler canceled = (_, args) =>
        {
            if (pressedPointerId == args.Pointer.PointerId)
            {
                completed.TrySetException(
                    new InvalidOperationException("The external pointer interaction was canceled before release."));
            }
        };
        Microsoft.UI.Input.InputPointerSource? pointerSource = null;
        var outsidePointerPressed = false;
        var popupClosed = false;
        TypedEventHandler<Microsoft.UI.Input.InputPointerSource, Microsoft.UI.Input.PointerEventArgs> islandPressed =
            (_, args) =>
            {
                var point = args.CurrentPoint;
                var bounds = target.TransformToVisual(target.XamlRoot.Content)
                    .TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
                if (!bounds.Contains(point.Position))
                {
                    return;
                }

                outsidePointerPressed = true;
                App.LogAutoTestStartup(
                    $"NATIVE EXTERNAL POINTER PRESSED stage={stage} pointer={point.PointerId} source=content-island");
                if (popupClosed)
                {
                    completed.TrySetResult();
                }
            };
        EventHandler<object> closed = (_, _) =>
        {
            popupClosed = true;
            if (outsidePointerPressed)
            {
                completed.TrySetResult();
            }
            else if (!target.DispatcherQueue.TryEnqueue(() =>
                     {
                         if (!outsidePointerPressed)
                         {
                             completed.TrySetException(new InvalidOperationException(
                                 "The native popup closed without a physical press on the outside target."));
                         }
                     }))
            {
                completed.TrySetException(
                    new InvalidOperationException("The native pointer verification could not be dispatched."));
            }
        };
        if (expectedClosure is null)
        {
            target.AddHandler(UIElement.PointerPressedEvent, pressed, true);
            target.AddHandler(UIElement.PointerReleasedEvent, released, true);
            target.AddHandler(UIElement.PointerCanceledEvent, canceled, true);
        }
        else
        {
            var environment = target.XamlRoot.ContentIslandEnvironment;
            var islands = ContentIsland.FindAllForCurrentThread().ToArray();
            var island = islands.SingleOrDefault(candidate => ReferenceEquals(candidate.Environment, environment))
                         ?? throw new InvalidOperationException(
                             $"The outside target has no matching native content island among {islands.Length} islands.");
            pointerSource = Microsoft.UI.Input.InputPointerSource.GetForIsland(island)
                            ?? throw new InvalidOperationException("The native content island has no pointer source.");
            pointerSource.PointerPressed += islandPressed;
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
                target.RemoveHandler(UIElement.PointerReleasedEvent, released);
                target.RemoveHandler(UIElement.PointerCanceledEvent, canceled);
            }
            else
            {
                expectedClosure.Closed -= closed;
                pointerSource!.PointerPressed -= islandPressed;
            }
        }
    }

    internal static async Task WaitForLightDismissArmAsync(Button target)
    {
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        RoutedEventHandler arm = (_, _) => completed.TrySetResult();
        target.Click += arm;
        try
        {
            target.Content = "Prepare light-dismiss";
            App.LogAutoTestStartup(
                $"NATIVE EXTERNAL POINTER ARM READY pid={Environment.ProcessId} stage=light-dismiss-outside-click "
                + $"target={AutomationProperties.GetAutomationId(target)} action=invoke-to-arm-next-stage");
            var responseTimeout = ShowcaseDiagnosticOptions.GetExternalInputTimeout(
                Environment.GetEnvironmentVariable("SHOWCASE_NATIVE_POPUP_INPUT_TIMEOUT_SECONDS"));
            await completed.Task.WaitAsync(responseTimeout);
        }
        finally
        {
            target.Click -= arm;
        }
    }
}

internal sealed class NativePopupInputUnavailableException(string message) : InvalidOperationException(message);
#endif
