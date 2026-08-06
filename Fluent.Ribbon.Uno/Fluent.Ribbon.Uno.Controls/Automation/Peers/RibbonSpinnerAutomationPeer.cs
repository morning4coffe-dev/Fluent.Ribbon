namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

/// <summary>
/// Automation peer for <see cref="RibbonSpinner"/>.
/// </summary>
public partial class RibbonSpinnerAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonSpinnerAutomationPeer"/> class.
    /// </summary>
    public RibbonSpinnerAutomationPeer(RibbonSpinner owner)
        : base(owner)
    {
    }

    private RibbonSpinner OwnerSpinner => (RibbonSpinner)Owner;

    /// <inheritdoc/>
    protected override string GetClassNameCore() => Owner.GetType().Name;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore()
#if __WASM__
        // Uno 6.6 only emits range attributes and keyboard value callbacks through its
        // slider semantic-element factory. Other heads retain the native Spinner role.
        => AutomationControlType.Slider;
#else
        => AutomationControlType.Spinner;
#endif

    /// <inheritdoc/>
    protected override string GetNameCore()
    {
        var name = AutomationProperties.GetName(OwnerSpinner);
        if (!string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        name = AutomationPeerHelpers.GetHeaderOrPlaceholderName(OwnerSpinner);
        return string.IsNullOrWhiteSpace(name) ? base.GetNameCore() : name;
    }

    /// <inheritdoc/>
    protected override string GetAccessKeyCore()
    {
        var accessKey = base.GetAccessKeyCore();
        return string.IsNullOrWhiteSpace(accessKey)
            ? OwnerSpinner.KeyTip ?? string.Empty
            : accessKey;
    }

    /// <inheritdoc/>
    protected override object? GetPatternCore(PatternInterface patternInterface)
        => patternInterface == PatternInterface.RangeValue
            ? this
            : base.GetPatternCore(patternInterface);

    /// <inheritdoc cref="AutomationPeer.GetPattern"/>
    public new virtual object? GetPattern(PatternInterface patternInterface)
        => GetPatternCore(patternInterface);

    /// <inheritdoc/>
    protected override void SetFocusCore()
    {
        AutomationProviderGuard.EnsureEnabled(this);
        AutomationProviderGuard.EnsureAvailable(
            OwnerSpinner.FocusEditorForAutomation(),
            "The ribbon spinner editor could not receive focus.");
    }

    /// <inheritdoc/>
    protected override List<AutomationPeer>? GetChildrenCore() => [];

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public double LargeChange => RunOnOwnerThread(
        () => NormalizeAutomationChange(OwnerSpinner.Increment));

    /// <inheritdoc/>
    public double Maximum => RunOnOwnerThread(
        () => Math.Max(OwnerSpinner.Minimum, OwnerSpinner.Maximum));

    /// <inheritdoc/>
    public double Minimum => RunOnOwnerThread(
        () => Math.Min(OwnerSpinner.Minimum, OwnerSpinner.Maximum));

    /// <inheritdoc/>
    public double SmallChange => RunOnOwnerThread(
        () => NormalizeAutomationChange(OwnerSpinner.Increment));

    /// <inheritdoc/>
    public double Value => RunOnOwnerThread(() => OwnerSpinner.Value);

    /// <inheritdoc/>
    public void SetValue(double value)
    {
        RunOnOwnerThread(
            () =>
            {
                AutomationProviderGuard.Validate(
                    this,
                    !IsReadOnly,
                    "The ribbon spinner is read-only.");

                var minimum = Math.Min(OwnerSpinner.Minimum, OwnerSpinner.Maximum);
                var maximum = Math.Max(OwnerSpinner.Minimum, OwnerSpinner.Maximum);
                if (!double.IsFinite(value) || value < minimum || value > maximum)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        $"The value must be finite and between {minimum} and {maximum}.");
                }

                OwnerSpinner.Value = value;
            });
    }

    internal void RaiseValueChanged(double oldValue, double newValue)
        => RaiseIfChanged(
            RangeValuePatternIdentifiers.ValueProperty,
            oldValue,
            newValue);

    internal void RaiseRangeChanged(
        double oldMinimum,
        double newMinimum,
        double oldMaximum,
        double newMaximum)
    {
        RaiseIfChanged(
            RangeValuePatternIdentifiers.MinimumProperty,
            oldMinimum,
            newMinimum);
        RaiseIfChanged(
            RangeValuePatternIdentifiers.MaximumProperty,
            oldMaximum,
            newMaximum);
    }

    internal void RaiseIncrementChanged(double oldValue, double newValue)
    {
        oldValue = NormalizeAutomationChange(oldValue);
        newValue = NormalizeAutomationChange(newValue);
        RaiseIfChanged(
            RangeValuePatternIdentifiers.SmallChangeProperty,
            oldValue,
            newValue);
        RaiseIfChanged(
            RangeValuePatternIdentifiers.LargeChangeProperty,
            oldValue,
            newValue);
    }

    internal static double NormalizeAutomationChange(double increment)
        => double.IsFinite(increment)
            ? Math.Abs(increment)
            : double.NaN;

    private void RaiseIfChanged(
        AutomationProperty property,
        double oldValue,
        double newValue)
    {
        if (!oldValue.Equals(newValue))
        {
            RaisePropertyChangedEvent(property, oldValue, newValue);
        }
    }

    private void RunOnOwnerThread(Action action)
    {
        RunOnOwnerThread(
            () =>
            {
                action();
                return true;
            });
    }

    private T RunOnOwnerThread<T>(Func<T> action)
    {
        if (OwnerSpinner.DispatcherQueue.HasThreadAccess)
        {
            return action();
        }

        using var completion = new System.Threading.ManualResetEventSlim();
        Exception? dispatchException = null;
        T result = default!;
        if (!OwnerSpinner.DispatcherQueue.TryEnqueue(
                () =>
                {
                    try
                    {
                        result = action();
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
            throw new InvalidOperationException("Could not dispatch the spinner automation action.");
        }

        if (!completion.Wait(TimeSpan.FromSeconds(5)))
        {
            throw new TimeoutException("The spinner automation action timed out.");
        }

        if (dispatchException is not null)
        {
            throw dispatchException;
        }

        return result;
    }
}
