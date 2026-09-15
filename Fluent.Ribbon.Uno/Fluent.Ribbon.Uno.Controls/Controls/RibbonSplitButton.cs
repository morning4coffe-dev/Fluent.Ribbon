namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

/// <summary>
/// Represents a ribbon button with separate primary and drop-down actions.
/// </summary>
[ContentProperty(Name = nameof(Items))]
[TemplatePart(Name = PART_Button, Type = typeof(WinUIButton))]
[TemplatePart(Name = PART_DropDownButton, Type = typeof(WinUIButton))]
public partial class RibbonSplitButton : DropDownButton, IToggleButton
{
    private const string PART_Button = "PART_Button";
    private const string PART_DropDownButton = "PART_DropDownButton";

    private WinUIButton? button;
    private WinUIButton? dropDownButton;
    private ButtonPointerClickFallback? primaryClickFallback;
    private readonly CommandAvailability commandAvailability;
    private EnabledStateConstraint? primaryEnabledConstraint;
    private WeakReference<RibbonSplitButton>? quickAccessPrimarySource;

    /// <summary>Gets the template part used for the primary action.</summary>
    protected FrameworkElement? PrimaryActionTarget => button;

    /// <summary>Occurs when the primary button is clicked.</summary>
#if __ANDROID__
    public new event RoutedEventHandler? Click;
#else
    public event RoutedEventHandler? Click;
#endif

    /// <summary>Occurs when the button becomes checked.</summary>
    public event RoutedEventHandler? Checked;

    /// <summary>Occurs when the button becomes unchecked.</summary>
    public event RoutedEventHandler? Unchecked;

    /// <summary>Occurs when the three-state value becomes indeterminate.</summary>
    public event RoutedEventHandler? Indeterminate;

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the command for the primary action.</summary>
    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the primary command parameter.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool?),
            typeof(RibbonSplitButton),
            new PropertyMetadata(false, OnIsCheckedChanged));

    /// <summary>Gets or sets whether the primary action is checked.</summary>
    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Identifies the <see cref="IsCheckable"/> dependency property.</summary>
    public static readonly DependencyProperty IsCheckableProperty =
        DependencyProperty.Register(
            nameof(IsCheckable),
            typeof(bool),
            typeof(RibbonSplitButton),
            new PropertyMetadata(false, OnIsCheckableChanged));

    /// <summary>Gets or sets whether the primary action supports checked state.</summary>
    public bool IsCheckable
    {
        get => (bool)GetValue(IsCheckableProperty);
        set => SetValue(IsCheckableProperty, value);
    }

    /// <summary>Identifies the <see cref="GroupName"/> dependency property.</summary>
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(
            nameof(GroupName),
            typeof(string),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the mutually exclusive toggle group name.</summary>
    public string? GroupName
    {
        get => (string?)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    /// <summary>Identifies the <see cref="DropDownToolTip"/> dependency property.</summary>
    public static readonly DependencyProperty DropDownToolTipProperty =
        DependencyProperty.Register(
            nameof(DropDownToolTip),
            typeof(object),
            typeof(RibbonSplitButton),
            new PropertyMetadata(null, OnDropDownToolTipChanged));

    /// <summary>Gets or sets the tooltip for the drop-down action.</summary>
    public object? DropDownToolTip
    {
        get => GetValue(DropDownToolTipProperty);
        set => SetValue(DropDownToolTipProperty, value);
    }

    /// <summary>Identifies the <see cref="IsButtonEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsButtonEnabledProperty =
        DependencyProperty.Register(
            nameof(IsButtonEnabled),
            typeof(bool),
            typeof(RibbonSplitButton),
            new PropertyMetadata(true, OnIsButtonEnabledChanged));

    /// <summary>Gets or sets whether the primary action is enabled.</summary>
    public bool IsButtonEnabled
    {
        get => (bool)GetValue(IsButtonEnabledProperty);
        set => SetValue(IsButtonEnabledProperty, value);
    }

    /// <summary>Identifies whether the primary action is effectively enabled.</summary>
    public static readonly DependencyProperty IsPrimaryActionEnabledProperty =
        DependencyProperty.Register(nameof(IsPrimaryActionEnabled), typeof(bool), typeof(RibbonSplitButton),
            new PropertyMetadata(true));

    /// <summary>Gets the primary action's combined control, user, and command availability.</summary>
    public bool IsPrimaryActionEnabled => (bool)GetValue(IsPrimaryActionEnabledProperty);

    /// <summary>Identifies the <see cref="IsDefinitive"/> dependency property.</summary>
    public static readonly DependencyProperty IsDefinitiveProperty =
        DependencyProperty.Register(
            nameof(IsDefinitive),
            typeof(bool),
            typeof(RibbonSplitButton),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether the primary action dismisses an ancestor popup.</summary>
    public bool IsDefinitive
    {
        get => (bool)GetValue(IsDefinitiveProperty);
        set => SetValue(IsDefinitiveProperty, value);
    }

    /// <summary>Initializes a new instance of the <see cref="RibbonSplitButton"/> class.</summary>
    public RibbonSplitButton()
    {
        DefaultStyleKey = typeof(RibbonSplitButton);
        commandAvailability = new CommandAvailability(
            this, CommandProperty, CommandParameterProperty, UpdatePrimaryAvailability, constrainOwner: false);
        IsEnabledChanged += (_, _) => UpdatePrimaryAvailability(commandAvailability.CanExecute);
        Loaded += (_, _) => UpdatePrimaryAvailability(commandAvailability.CanExecute);
        Unloaded += (_, _) => primaryEnabledConstraint?.Release();
    }

    /// <inheritdoc />
    protected override string DropDownButtonTemplatePartName => PART_DropDownButton;

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        if (button is not null)
        {
            button.Click -= OnButtonClick;
            primaryClickFallback?.Dispose();
            primaryClickFallback = null;
        }
        primaryEnabledConstraint?.Release();
        primaryEnabledConstraint = null;

        base.OnApplyTemplate();

        button = GetTemplateChild(PART_Button) as WinUIButton;
        dropDownButton = GetTemplateChild(PART_DropDownButton) as WinUIButton;

        if (button is not null)
        {
            button.IsTabStop = false;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
                button,
                Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            button.Click += OnButtonClick;
            primaryClickFallback = ButtonPointerClickFallback.Attach(
                button,
                () =>
                {
                    Focus(FocusState.Pointer);
                    InvokePrimaryAction();
                });
            primaryEnabledConstraint = new EnabledStateConstraint(button);
        }

        if (dropDownButton is not null)
        {
            dropDownButton.IsTabStop = false;
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(
                dropDownButton,
                Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
            ToolTipService.SetToolTip(dropDownButton, DropDownToolTip);
        }

        UpdateSplitButtonVisualState();
        UpdatePrimaryAvailability(commandAvailability.CanExecute);
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        Focus(FocusState.Pointer);
        InvokePrimaryAction(e);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        // The outer split button is the only tab stop and UIA element. Enter/Space retain
        // the primary action while Down follows the base drop-down path.
        if (!e.Handled
            && e.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space)
        {
            InvokePrimaryAction();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected internal void InvokePrimaryAction()
    {
        InvokePrimaryAction(new RoutedEventArgs());
    }

    private void InvokePrimaryAction(RoutedEventArgs e)
    {
        if (!CanInvokePrimaryAction)
        {
            return;
        }

        if (IsCheckable)
        {
            IsChecked = !(IsChecked ?? false);
        }

        Click?.Invoke(this, e);

        ExecutePrimaryCommand();

        if (IsDefinitive)
        {
            PopupService.RaiseDismissPopupEvent(
                this,
                DismissPopupMode.Always,
                DismissPopupReason.Undefined);
        }
    }

    protected internal void ForwardQuickAccessPrimaryAction(RoutedEventArgs e)
    {
        if (!CanInvokePrimaryAction)
        {
            return;
        }

        Click?.Invoke(this, e);
        ExecutePrimaryCommand();
    }

    private void ExecutePrimaryCommand()
    {
        // A clone observes its own Command for availability, but its Click is
        // forwarded to the original source, which alone executes that command.
        if (quickAccessPrimarySource is null)
        {
            Internal.CommandHelper.Execute(Command, CommandParameter);
        }
    }

    protected internal bool CanInvokePrimaryAction
        => commandAvailability.CanExecute && IsEnabled && IsButtonEnabled
           && (quickAccessPrimarySource is null
               || (quickAccessPrimarySource.TryGetTarget(out var source) && source.CanInvokePrimaryAction));

    private void UpdatePrimaryAvailability(bool commandAvailable)
    {
        var sourceAvailable = quickAccessPrimarySource is null
                              || (quickAccessPrimarySource.TryGetTarget(out var source) && source.CanInvokePrimaryAction);
        var primaryAvailable = commandAvailable && IsButtonEnabled && sourceAvailable;
        SetValue(IsPrimaryActionEnabledProperty, IsEnabled && primaryAvailable);
        if (IsLoaded)
        {
            primaryEnabledConstraint?.SetAllowed(primaryAvailable);
            VisualStateManager.GoToState(this, primaryAvailable ? "PrimaryEnabled" : "PrimaryDisabled", true);
        }
    }

    /// <summary>Keeps forwarded primary actions gated by their original command source.</summary>
    protected void BindPrimaryQuickAccessAvailability(RibbonSplitButton clone)
    {
        clone.quickAccessPrimarySource = new WeakReference<RibbonSplitButton>(this);
        var bindings = QuickAccessBindingSession.For(this, clone);
        bindings.Bind(IsButtonEnabledProperty);
        bindings.Bind(CommandParameterProperty);
        bindings.Bind(CommandProperty);
        clone.Click -= OnQuickAccessPrimaryClick;
        clone.Click += OnQuickAccessPrimaryClick;
        clone.UpdatePrimaryAvailability(clone.commandAvailability.CanExecute);
    }

    private static void OnQuickAccessPrimaryClick(object sender, RoutedEventArgs args)
    {
        if (sender is RibbonSplitButton clone
            && clone.quickAccessPrimarySource is { } weakSource
            && weakSource.TryGetTarget(out var source))
        {
            source.ForwardQuickAccessPrimaryAction(args);
        }
    }

    private static void OnIsCheckedChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var splitButton = (RibbonSplitButton)sender;
        if (splitButton.IsCheckable)
        {
            switch ((bool?)args.NewValue)
            {
                case true:
                    splitButton.Checked?.Invoke(splitButton, new RoutedEventArgs());
                    break;
                case false:
                    splitButton.Unchecked?.Invoke(splitButton, new RoutedEventArgs());
                    break;
                default:
                    splitButton.Indeterminate?.Invoke(splitButton, new RoutedEventArgs());
                    break;
            }
        }

        splitButton.UpdateSplitButtonVisualState();
    }

    private static void OnIsCheckableChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var splitButton = (RibbonSplitButton)sender;
        if ((bool)args.NewValue is false && splitButton.IsChecked != false)
        {
            splitButton.IsChecked = false;
        }

        splitButton.UpdateSplitButtonVisualState();
    }

    private static void OnIsButtonEnabledChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var splitButton = (RibbonSplitButton)sender;
        splitButton.UpdatePrimaryAvailability(
            splitButton.commandAvailability?.CanExecute
            ?? (splitButton.Command?.CanExecute(splitButton.CommandParameter) ?? true));
    }

    private static void OnDropDownToolTipChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var splitButton = (RibbonSplitButton)sender;
        if (splitButton.dropDownButton is not null)
        {
            ToolTipService.SetToolTip(splitButton.dropDownButton, args.NewValue);
        }
    }

    private void UpdateSplitButtonVisualState()
    {
        VisualStateManager.GoToState(
            this,
            IsCheckable && IsChecked == true ? "Checked" : "Unchecked",
            true);
        if (!IsEnabled)
        {
            VisualStateManager.GoToState(this, "Disabled", true);
        }
    }

    /// <inheritdoc />
    public override FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new RibbonSplitButton
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        BindQuickAccessItem(clone);
        BindOneWay(DropDownToolTipProperty);
        BindOneWay(IsCheckableProperty);
        BindPrimaryQuickAccessAvailability(clone);
        BindOneWay(IsDefinitiveProperty);
        BindTwoWay(IsCheckedProperty);
        BindQuickAccessItemDropDownEvents(clone);
        return clone;

        void BindOneWay(DependencyProperty property) =>
            QuickAccessBindingSession.For(this, clone).Bind(property);

        void BindTwoWay(DependencyProperty property) =>
            QuickAccessBindingSession.For(this, clone).Bind(property, twoWay: true);
    }

    /// <inheritdoc />
    public override KeyTipPressedResult OnKeyTipPressed()
    {
        return IsEnabled ? base.OnKeyTipPressed() : KeyTipPressedResult.Empty;
    }

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer() =>
        new Fluent.Automation.Peers.RibbonSplitButtonAutomationPeer(this);
}
