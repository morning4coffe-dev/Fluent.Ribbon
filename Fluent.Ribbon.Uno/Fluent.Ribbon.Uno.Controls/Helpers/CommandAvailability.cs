namespace Fluent;

internal sealed class CommandAvailability
{
    private readonly Control owner;
    private readonly DependencyProperty commandProperty;
    private readonly DependencyProperty parameterProperty;
    private readonly Action<bool>? changed;
    private readonly EnabledStateConstraint? enabledConstraint;
    private CommandSubscription? subscription;
    private bool suspended;
    private bool available = true;

    internal CommandAvailability(
        Control owner,
        DependencyProperty commandProperty,
        DependencyProperty parameterProperty,
        Action<bool>? changed = null,
        bool constrainOwner = true)
    {
        this.owner = owner;
        this.commandProperty = commandProperty;
        this.parameterProperty = parameterProperty;
        this.changed = changed;
        enabledConstraint = constrainOwner ? new EnabledStateConstraint(owner) : null;
        owner.RegisterPropertyChangedCallback(commandProperty, OnCommandChanged);
        owner.RegisterPropertyChangedCallback(parameterProperty, (_, _) => Refresh());
        owner.Loaded += OnLoaded;
        owner.Unloaded += OnUnloaded;
        ObserveCommand();
        Refresh();
    }

    internal bool CanExecute
    {
        get
        {
            Refresh();
            return available;
        }
    }

    internal void Refresh()
    {
        var command = owner.GetValue(commandProperty) as ICommand;
        var canExecute = command is null
                         || Internal.CommandHelper.CanExecute(command, owner.GetValue(parameterProperty));
        var wasAvailable = available;
        available = canExecute;
        if (!suspended)
        {
            enabledConstraint?.SetAllowed(canExecute);
        }

        if (wasAvailable != canExecute)
        {
            changed?.Invoke(canExecute);
        }
    }

    private void OnCommandChanged(DependencyObject sender, DependencyProperty property)
    {
        ObserveCommand();
        Refresh();
    }

    private void ObserveCommand()
    {
        subscription?.Dispose();
        subscription = !suspended && owner.GetValue(commandProperty) is ICommand command
            ? new CommandSubscription(this, command)
            : null;
    }

    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        suspended = false;
        ObserveCommand();
        Refresh();
    }

    private void OnUnloaded(object sender, RoutedEventArgs args)
    {
        // Native panel replacement can deliver an old Unloaded after the same
        // container has already entered its new panel.
        if (owner.IsLoaded)
        {
            return;
        }

        suspended = true;
        subscription?.Dispose();
        subscription = null;
        enabledConstraint?.Release();
    }

    private void RequestRefresh()
    {
        if (owner.DispatcherQueue is not { HasThreadAccess: false } queue)
        {
            Refresh();
            return;
        }

        var weak = new WeakReference<CommandAvailability>(this);
        queue.TryEnqueue(() =>
        {
            if (weak.TryGetTarget(out var target) && !target.suspended)
            {
                target.Refresh();
            }
        });
    }

    private sealed class CommandSubscription : IDisposable
    {
        private readonly WeakReference<CommandAvailability> target;
        private readonly ICommand command;
        private bool disposed;

        internal CommandSubscription(CommandAvailability target, ICommand command)
        {
            this.target = new WeakReference<CommandAvailability>(target);
            this.command = command;
            command.CanExecuteChanged += OnChanged;
        }

        private void OnChanged(object? sender, EventArgs args)
        {
            if (target.TryGetTarget(out var observer))
            {
                if (ReferenceEquals(observer.subscription, this))
                {
                    observer.RequestRefresh();
                }
            }
            else
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                command.CanExecuteChanged -= OnChanged;
            }
        }
    }
}

/// <summary>Keeps command coercion separate from the user's local or bound IsEnabled value.</summary>
internal sealed class EnabledStateConstraint(Control target)
{
    private readonly EffectiveValueConstraint constraint = new(target, Control.IsEnabledProperty, nameof(Control.IsEnabled));

    internal void SetAllowed(bool allowed)
    {
        if (allowed)
        {
            Release();
            return;
        }

        constraint.Hold(false);
    }

    internal void Release()
    {
        constraint.Release();
    }
}
