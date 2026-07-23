using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Fluent;

/// <summary>Contains the result of a compatibility-facade runtime smoke run.</summary>
/// <param name="WrapperCount">The number of facade wrappers instantiated.</param>
/// <param name="AppliedTemplateCount">The number of wrappers or clones whose template applied successfully.</param>
/// <param name="QuickAccessCloneCount">The number of non-null quick-access clones created.</param>
/// <param name="CommandExecutionCount">The number of test command executions observed.</param>
/// <param name="Failures">Failures collected while running independent smoke operations.</param>
public sealed record CompatibilityRuntimeSmokeResult(
    int WrapperCount,
    int AppliedTemplateCount,
    int QuickAccessCloneCount,
    int CommandExecutionCount,
    IReadOnlyList<string> Failures)
{
    /// <summary>Gets whether every smoke operation completed without a failure.</summary>
    public bool Succeeded => Failures.Count == 0;
}

/// <summary>
/// Provides a consumer-callable runtime check for facade construction, templates, commands, and QAT clones.
/// </summary>
/// <remarks>
/// Call <see cref="Run()"/> from the application's UI thread after WinUI resources have initialized.
/// The helper does not attach controls to the visual tree and does not retain created controls.
/// </remarks>
public static class CompatibilityRuntimeSmoke
{
    /// <summary>Runs the compatibility facade smoke checks on the current UI thread.</summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when called without a WinUI dispatcher on the current thread.
    /// </exception>
    public static CompatibilityRuntimeSmokeResult Run()
    {
        return RunCore(null);
    }

    /// <summary>
    /// Runs the compatibility facade smoke checks while attaching controls to a live panel.
    /// </summary>
    public static CompatibilityRuntimeSmokeResult Run(Panel host)
    {
        ArgumentNullException.ThrowIfNull(host);
        return RunCore(host);
    }

    private static CompatibilityRuntimeSmokeResult RunCore(Panel? host)
    {
        DispatcherQueue? dispatcherQueue;
        try
        {
            dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        }
        catch (NotSupportedException exception)
        {
            throw new InvalidOperationException(
                "CompatibilityRuntimeSmoke.Run requires a concrete WinUI runtime, not a reference assembly.",
                exception);
        }

        if (dispatcherQueue is null)
        {
            throw new InvalidOperationException(
                "CompatibilityRuntimeSmoke.Run must be called from an initialized WinUI UI thread.");
        }

        var failures = new List<string>();
        var wrappers = new List<Control>();
        var appliedTemplates = 0;
        var quickAccessClones = 0;
        var commandExecutions = 0;
        StackPanel? attachmentHost = null;

        if (host is not null)
        {
            attachmentHost = new StackPanel
            {
                Opacity = 0,
                IsHitTestVisible = false,
            };
            host.Children.Add(attachmentHost);
        }

        try
        {
            Create<Button>(wrappers, failures);
            Create<ToggleButton>(wrappers, failures);
            Create<CheckBox>(wrappers, failures);
            Create<ComboBox>(wrappers, failures);
            Create<DropDownButton>(wrappers, failures);
            Create<Gallery>(wrappers, failures);
            Create<GalleryItem>(wrappers, failures);
            Create<MenuItem>(wrappers, failures);
            Create<RadioButton>(wrappers, failures);
            Create<RibbonTabItem>(wrappers, failures);
            Create<Spinner>(wrappers, failures);
            Create<SplitButton>(wrappers, failures);
            Create<StatusBar>(wrappers, failures);
            Create<StatusBarItem>(wrappers, failures);
            Create<TextBox>(wrappers, failures);

            foreach (var wrapper in wrappers)
            {
                attachmentHost?.Children.Add(wrapper);
            }

            attachmentHost?.UpdateLayout();

            foreach (var wrapper in wrappers)
            {
                Try(
                    $"{wrapper.GetType().Name}.ApplyTemplate",
                    () =>
                    {
                        if (wrapper.ApplyTemplate() || wrapper.Template is not null)
                        {
                            appliedTemplates++;
                        }
                        else
                        {
                            failures.Add(
                                $"{wrapper.GetType().Name}.ApplyTemplate returned false; verify Fluent.Ribbon.Uno resources are loaded.");
                        }
                    },
                    failures);

                if (wrapper is IQuickAccessItemProvider quickAccessProvider)
                {
                    Try(
                        $"{wrapper.GetType().Name}.CreateQuickAccessItem",
                        () =>
                        {
                            var clone = quickAccessProvider.CreateQuickAccessItem();
                            if (clone is null)
                            {
                                failures.Add(
                                    $"{wrapper.GetType().Name}.CreateQuickAccessItem returned null.");
                                return;
                            }

                            quickAccessClones++;
                            attachmentHost?.Children.Add(clone);
                            attachmentHost?.UpdateLayout();
                            if (clone is Control cloneControl
                                && (cloneControl.ApplyTemplate() || cloneControl.Template is not null))
                            {
                                appliedTemplates++;
                            }
                            else
                            {
                                failures.Add(
                                    $"{wrapper.GetType().Name} QAT clone did not apply a control template.");
                            }
                        },
                        failures);
                }
            }

            Try(
                "Button command execution",
                () =>
                {
                    var command = new SmokeCommand(() => commandExecutions++);
                    var button = new Button
                    {
                        Command = command,
                        CommandParameter = CompatibilityRuntimeSmokeResultMarker.Instance
                    };
                    button.OnKeyTipPressed();
                    if (commandExecutions != 1)
                    {
                        failures.Add("Button.OnKeyTipPressed did not execute its assigned command exactly once.");
                    }
                },
                failures);
        }
        finally
        {
            if (attachmentHost is not null)
            {
                host!.Children.Remove(attachmentHost);
            }
        }

        return new(
            wrappers.Count,
            appliedTemplates,
            quickAccessClones,
            commandExecutions,
            failures);
    }

    private static void Create<T>(
        ICollection<Control> wrappers,
        ICollection<string> failures)
        where T : Control, new()
    {
        Try(typeof(T).Name, () => wrappers.Add(new T()), failures);
    }

    private static void Try(string operation, Action action, ICollection<string> failures)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            failures.Add($"{operation}: {exception.GetType().Name}: {exception.Message}");
        }
    }

    private sealed class SmokeCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add
            {
            }

            remove
            {
            }
        }

        public bool CanExecute(object? parameter) =>
            ReferenceEquals(parameter, CompatibilityRuntimeSmokeResultMarker.Instance);

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                execute();
            }
        }
    }

    private sealed class CompatibilityRuntimeSmokeResultMarker
    {
        internal static CompatibilityRuntimeSmokeResultMarker Instance { get; } = new();
    }
}
