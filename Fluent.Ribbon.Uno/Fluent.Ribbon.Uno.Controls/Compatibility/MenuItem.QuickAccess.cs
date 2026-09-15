namespace Fluent;

public partial class MenuItem
{
    private WeakReference<RibbonDropDownButton>? quickAccessBorrower;
    private WeakReference<RibbonDropDownButton>? pendingQuickAccessBorrower;

    internal DependencyObject QuickAccessSubmenuOwner =>
        quickAccessBorrower is { } borrower && borrower.TryGetTarget(out var clone) ? clone : this;

    private void BindMenuQuickAccessItem(FrameworkElement clone)
    {
        var bindings = QuickAccessBindingSession.For(this, clone);
        bindings.BindCommon();
        ToolTipService.SetToolTip(clone, ToolTipService.GetToolTip(this) ?? Description);

        if (clone is DropDownButton dropdown)
        {
            bindings.BindPresentation(HeaderProperty, RibbonDropDownButton.HeaderProperty);
            bindings.BindPresentation(IconProperty, DropDownButton.IconProperty);
            bindings.Bind(HeaderTemplateProperty, RibbonDropDownButton.HeaderTemplateProperty);
            bindings.Bind(HeaderTemplateSelectorProperty, DropDownButton.HeaderTemplateSelectorProperty);
            bindings.Bind(IconGlyphProperty, RibbonDropDownButton.IconGlyphProperty);
            bindings.Bind(ResizeModeProperty, RibbonDropDownButton.ResizeModeProperty);
            bindings.Bind(MaxDropDownHeightProperty, RibbonDropDownButton.MaxDropDownHeightProperty);
#if WINDOWS
            dropdown.BindNativeMenuPopup(this);
#else
            dropdown.BindQuickAccessContent(this, static (source, target) => ((MenuItem)source).GetQuickAccessPopupContent(target));
#endif
        }
        else if (clone is Button button)
        {
            bindings.BindPresentation(HeaderProperty, RibbonButton.HeaderProperty);
            bindings.BindPresentation(IconProperty, Button.IconProperty);
            bindings.Bind(HeaderTemplateProperty, Button.HeaderTemplateProperty);
            bindings.Bind(HeaderTemplateSelectorProperty, Button.HeaderTemplateSelectorProperty);
            bindings.Bind(IconGlyphProperty, RibbonButton.IconGlyphProperty);
            bindings.Bind(IsDefinitiveProperty, Button.IsDefinitiveProperty);
        }
        else if (clone is RibbonToggleButton)
        {
            bindings.BindPresentation(HeaderProperty, RibbonToggleButton.HeaderProperty);
            bindings.BindPresentation(IconProperty, RibbonToggleButton.IconProperty);
            bindings.Bind(IconGlyphProperty, RibbonToggleButton.IconGlyphProperty);
            bindings.Bind(GroupNameProperty, RibbonToggleButton.GroupNameProperty);
            bindings.Bind(IsCheckedProperty, Microsoft.UI.Xaml.Controls.Primitives.ToggleButton.IsCheckedProperty, twoWay: true);
        }

        if (clone is RibbonSplitButton split)
        {
            bindings.Bind(IsCheckableProperty, RibbonSplitButton.IsCheckableProperty);
            bindings.Bind(IsCheckedProperty, RibbonSplitButton.IsCheckedProperty, twoWay: true,
                convertBack: static (source, value) =>
                    source is MenuItem { GroupName.Length: > 0, IsChecked: true } && value is false ? true : value);
            bindings.Bind(IsDefinitiveProperty, RibbonSplitButton.IsDefinitiveProperty);
            bindings.Bind(CommandParameterProperty, RibbonSplitButton.CommandParameterProperty);
            split.Command = new QuickAccessMenuCommand(this, clone, bindings);
        }
        else if (clone is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase buttonBase)
        {
            bindings.Bind(CommandParameterProperty, Microsoft.UI.Xaml.Controls.Primitives.ButtonBase.CommandParameterProperty);
            buttonBase.Command = new QuickAccessMenuCommand(this, clone, bindings);
        }
    }

#if !WINDOWS
    private QuickAccessPopupContent? GetQuickAccessPopupContent(RibbonDropDownButton clone)
    {
        if (quickAccessBorrower is { } reference && reference.TryGetTarget(out var current))
        {
            pendingQuickAccessBorrower = new(clone);
            current.CloseDropDown();
            if (quickAccessBorrower is not null)
            {
                return null;
            }
        }

        ApplyTemplate();
        if (submenuItemsHost is null || submenuResizeHost is null)
        {
            CreateCompatibilitySubmenu();
        }

        if (submenuResizeHost?.Content is not FrameworkElement content || submenuItemsHost is null)
        {
            throw new InvalidOperationException("The menu template has no transferable submenu content.");
        }

        var host = submenuResizeHost;
        var observation = itemsBinding.AcquirePresentationLease();
        ValidateSubmenuGenerator();
        quickAccessBorrower = new(clone);

        return new QuickAccessPopupContent(
            content,
            () =>
            {
                if (ReferenceEquals(host.Content, content))
                {
                    host.Content = null;
                    IsDropDownOpen = false;
                }
            },
            () =>
            {
                if (!ReferenceEquals(host.Content, content))
                {
                    host.Content = content;
                }
                SetQuickAccessSubmenuOwner(this);
                quickAccessBorrower = null;
                ResumeOwnQuickAccessContent();
            },
            SetQuickAccessSubmenuOwner,
            observation);
    }
#endif

    private void SetQuickAccessSubmenuOwner(DependencyObject owner)
    {
        foreach (var item in Items.OfType<IDropDownItemOwner>())
        {
            item.SetDropDownOwner(owner);
        }
    }

    private bool PrepareOwnQuickAccessContent()
    {
        if (quickAccessBorrower is not { } reference || !reference.TryGetTarget(out var clone))
        {
            return true;
        }

        clone.CloseDropDown();
        return quickAccessBorrower is null;
    }

    private void ResumeOwnQuickAccessContent()
    {
        if (IsDropDownOpen && IsLoaded)
        {
            ShowCompatibilitySubmenu();
        }
        else if (pendingQuickAccessBorrower is { } pending && pending.TryGetTarget(out var clone)
                 && clone.IsDropDownOpen && clone.IsLoaded)
        {
            pendingQuickAccessBorrower = null;
            clone.OpenDropDownForAutomation();
        }
    }

    private void InvokeQuickAccessAction(DependencyObject clone)
    {
        if (!CanInvoke)
        {
            return;
        }

        InvokeItem();
        RaiseInvokedAutomationEvent();
        if (IsDefinitive)
        {
            PopupService.RaiseDismissPopupEvent(clone, DismissPopupMode.Always);
        }
    }

    private sealed class QuickAccessMenuCommand : ICommand
    {
        private readonly WeakReference<MenuItem> source;
        private readonly WeakReference<FrameworkElement> clone;
        private readonly List<(DependencyProperty Property, long Token)> tokens = [];
        private ICommand? observedCommand;
        private EventHandler? commandChanged;
        private EventHandler? handlers;
        private bool active = true;

        internal QuickAccessMenuCommand(MenuItem source, FrameworkElement clone, QuickAccessBindingSession bindings)
        {
            this.source = new(source);
            this.clone = new(clone);
            bindings.Activated += () =>
            {
                active = true;
                Connect();
                handlers?.Invoke(this, EventArgs.Empty);
            };
            bindings.Deactivated += () =>
            {
                active = false;
                Disconnect();
            };
        }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                handlers += value;
                Connect();
            }
            remove
            {
                handlers -= value;
                if (handlers is null)
                {
                    Disconnect();
                }
            }
        }

        public bool CanExecute(object? parameter) =>
            source.TryGetTarget(out var owner) && owner.CanInvoke;

        public void Execute(object? parameter)
        {
            if (source.TryGetTarget(out var owner) && clone.TryGetTarget(out var copy))
            {
                owner.InvokeQuickAccessAction(copy);
            }
        }

        private void Connect()
        {
            if (!active || handlers is null || tokens.Count != 0 || !source.TryGetTarget(out var owner))
            {
                return;
            }

            var weak = new WeakReference<QuickAccessMenuCommand>(this);
            foreach (var property in new[] { CommandProperty, CommandParameterProperty, IsEnabledProperty })
            {
                long token = 0;
                token = owner.RegisterPropertyChangedCallback(property, (sender, changedProperty) =>
                {
                    if (weak.TryGetTarget(out var command))
                    {
                        command.ObserveCommand();
                        command.handlers?.Invoke(command, EventArgs.Empty);
                    }
                    else
                    {
                        sender.UnregisterPropertyChangedCallback(changedProperty, token);
                    }
                });
                tokens.Add((property, token));
            }

            ObserveCommand();
        }

        private void ObserveCommand()
        {
            var command = source.TryGetTarget(out var owner) ? owner.Command : null;
            if (ReferenceEquals(command, observedCommand))
            {
                return;
            }

            if (observedCommand is not null && commandChanged is not null)
            {
                observedCommand.CanExecuteChanged -= commandChanged;
            }

            observedCommand = command;
            if (command is null)
            {
                commandChanged = null;
                return;
            }

            var weak = new WeakReference<QuickAccessMenuCommand>(this);
            var weakCommand = new WeakReference<ICommand>(command);
            EventHandler? handler = null;
            handler = (_, _) =>
            {
                if (weak.TryGetTarget(out var proxy))
                {
                    proxy.handlers?.Invoke(proxy, EventArgs.Empty);
                }
                else if (weakCommand.TryGetTarget(out var retired))
                {
                    retired.CanExecuteChanged -= handler;
                }
            };
            commandChanged = handler;
            command.CanExecuteChanged += handler;
        }

        private void Disconnect()
        {
            if (source.TryGetTarget(out var owner))
            {
                foreach (var (property, token) in tokens)
                {
                    owner.UnregisterPropertyChangedCallback(property, token);
                }
            }

            tokens.Clear();
            if (observedCommand is not null && commandChanged is not null)
            {
                observedCommand.CanExecuteChanged -= commandChanged;
            }

            observedCommand = null;
            commandChanged = null;
        }
    }
}
