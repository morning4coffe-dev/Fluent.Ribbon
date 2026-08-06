namespace Fluent;

using Fluent.Automation.Peers;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Windows.System;
using Windows.UI.Core;

internal sealed partial class ResizeHandle : Control
{
    internal ResizeHandle()
    {
        PointerEntered += (_, _) => IsPointerOverHandle = true;
        PointerExited += (_, _) => IsPointerOverHandle = false;
    }

    internal ResizeableContentControl? ResizeOwner { get; set; }

    internal bool ResizesBothDirections { get; set; }

    internal bool IsPointerOverHandle { get; private set; }

    internal string AutomationHelpText { get; set; } = string.Empty;

    internal bool TryHandleKey(VirtualKey key, bool shiftDown)
    {
        return ResizeOwner?.TryResizeFromKey(
            key,
            ResizesBothDirections,
            shiftDown) == true;
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && TryHandleKey(e.Key, IsShiftDown()))
        {
            e.Handled = true;
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer()
        => new ResizeHandleAutomationPeer(this);

    private static bool IsShiftDown()
        => (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
            & CoreVirtualKeyStates.Down) != 0;
}

internal sealed partial class ResizeHandleAutomationPeer : FrameworkElementAutomationPeer, ITransformProvider
{
    internal ResizeHandleAutomationPeer(ResizeHandle owner)
        : base(owner)
    {
    }

    private ResizeHandle OwnerHandle => (ResizeHandle)Owner;

    protected override string GetClassNameCore() => nameof(ResizeHandle);

    protected override AutomationControlType GetAutomationControlTypeCore()
        => AutomationControlType.Thumb;

    protected override string GetHelpTextCore()
        => string.IsNullOrWhiteSpace(OwnerHandle.AutomationHelpText)
            ? base.GetHelpTextCore()
            : OwnerHandle.AutomationHelpText;

    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.Transform
            ? this
            : base.GetPatternCore(patternInterface);

    public new object? GetPattern(PatternInterface patternInterface)
        => GetPatternCore(patternInterface);

    public bool CanMove => false;

    public bool CanResize => RunOnOwnerThread(
        () => OwnerHandle.ResizeOwner?.IsResizeHandleAvailable(
            OwnerHandle.ResizesBothDirections) == true);

    public bool CanRotate => false;

    public void Move(double x, double y)
    {
        RunOnOwnerThread(
            () =>
            {
                AutomationProviderGuard.Validate(
                    this,
                    isAvailable: false,
                    "Resize handles cannot move their owner.");
            });
    }

    public void Resize(double width, double height)
    {
        RunOnOwnerThread(
            () =>
            {
                AutomationProviderGuard.Validate(
                    this,
                    OwnerHandle.ResizeOwner?.IsResizeHandleAvailable(
                        OwnerHandle.ResizesBothDirections) == true,
                    "This resize handle is not currently available.");

                if (!double.IsFinite(width)
                    || !double.IsFinite(height)
                    || width < 0
                    || height < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(width),
                        "Resize dimensions must be finite and non-negative.");
                }

                var rasterizationScale =
                    OwnerHandle.XamlRoot?.RasterizationScale ?? 1D;
                OwnerHandle.ResizeOwner!.ResizeTo(
                    PhysicalPixelsToDips(width, rasterizationScale),
                    PhysicalPixelsToDips(height, rasterizationScale),
                    OwnerHandle.ResizesBothDirections);
            });
    }

    public void Rotate(double degrees)
    {
        RunOnOwnerThread(
            () =>
            {
                AutomationProviderGuard.Validate(
                    this,
                    isAvailable: false,
                    "Resize handles cannot rotate their owner.");
            });
    }

    internal static double PhysicalPixelsToDips(
        double physicalPixels,
        double rasterizationScale)
        => physicalPixels
           / (double.IsFinite(rasterizationScale) && rasterizationScale > 0
               ? rasterizationScale
               : 1D);

    private void RunOnOwnerThread(Action action)
    {
        if (OwnerHandle.DispatcherQueue.HasThreadAccess)
        {
            action();
            return;
        }

        using var completion = new System.Threading.ManualResetEventSlim();
        Exception? dispatchException = null;
        if (!OwnerHandle.DispatcherQueue.TryEnqueue(
                () =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        dispatchException = ex;
                    }
                    finally
                    {
                        completion.Set();
                    }
                }))
        {
            throw new InvalidOperationException(
                "Could not dispatch the resize automation action.");
        }

        if (!completion.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("The resize automation action timed out.");
        }

        if (dispatchException is not null)
        {
            throw dispatchException;
        }
    }

    private T RunOnOwnerThread<T>(Func<T> action)
    {
        T result = default!;
        RunOnOwnerThread(() => result = action());
        return result;
    }
}
