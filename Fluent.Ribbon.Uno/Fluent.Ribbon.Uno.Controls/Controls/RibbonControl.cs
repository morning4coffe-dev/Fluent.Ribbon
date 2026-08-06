using System.Collections;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Fluent;

/// <summary>
/// Portable compatibility base for custom Fluent ribbon controls.
/// </summary>
/// <remarks>
/// WinUI has no <c>ICommandSource</c> or routed-command target. <see cref="CommandTarget"/>
/// therefore uses <see cref="UIElement"/> as the nearest portable target descriptor.
/// Derived controls remain responsible for deciding when to call <see cref="ExecuteCommand"/>.
/// </remarks>
public abstract partial class RibbonControl :
    Control,
    ICommandSource,
    IQuickAccessItemProvider,
    IRibbonControl
{
    private static readonly RibbonControlSizeDefinition UnsetSizeDefinition =
        new(
            (RibbonControlSize)(-1),
            (RibbonControlSize)(-1),
            (RibbonControlSize)(-1));

    private bool commandCanExecute = true;

    /// <summary>Initializes a new compatibility ribbon control.</summary>
    protected RibbonControl()
    {
        QuickAccessHelper.AttachContextMenu(this);
    }

    /// <summary>Identifies the <see cref="KeyTip"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTipProperty =
        DependencyProperty.Register(
            nameof(KeyTip),
            typeof(string),
            typeof(RibbonControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the keyboard key tip.</summary>
    public string? KeyTip
    {
        get => (string?)GetValue(KeyTipProperty);
        set => SetValue(KeyTipProperty, value);
    }

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the header content.</summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplate),
            typeof(DataTemplate),
            typeof(RibbonControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the header template.</summary>
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplateSelector"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(HeaderTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(RibbonControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the header template selector.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Identifies the <see cref="Icon"/> dependency property.</summary>
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(RibbonControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the icon content.</summary>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(RibbonControl),
            new PropertyMetadata(null, OnCommandChanged));

    /// <summary>Gets or sets the portable command.</summary>
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
            typeof(RibbonControl),
            new PropertyMetadata(null, OnCommandParameterChanged));

    /// <summary>Gets or sets the command parameter.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Identifies the <see cref="CommandTarget"/> dependency property.</summary>
    public static readonly DependencyProperty CommandTargetProperty =
        DependencyProperty.Register(
            nameof(CommandTarget),
            typeof(UIElement),
            typeof(RibbonControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the WinUI element associated with command routing.
    /// </summary>
    /// <remarks>
    /// <see cref="ICommand"/> itself does not consume a target; this property is retained
    /// for source compatibility and for command implementations that inspect their owner.
    /// </remarks>
    public UIElement? CommandTarget
    {
        get => (UIElement?)GetValue(CommandTargetProperty);
        set => SetValue(CommandTargetProperty, value);
    }

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonControl),
            new PropertyMetadata(RibbonControlSize.Large));

    /// <summary>Gets or sets the ribbon control size.</summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="SizeDefinition"/> dependency property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(RibbonControl),
            new PropertyMetadata(UnsetSizeDefinition));

    /// <summary>Gets or sets the typed ribbon size definition.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get
        {
            var definition =
                (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
            return IsUnsetSizeDefinition(definition)
                ? default
                : definition;
        }
        set => SetValue(SizeDefinitionProperty, value);
    }

    internal static bool TryGetEffectiveSizeDefinition(
        DependencyObject element,
        out RibbonControlSizeDefinition definition)
    {
        definition =
            (RibbonControlSizeDefinition)element.GetValue(SizeDefinitionProperty);
        if (!IsUnsetSizeDefinition(definition))
        {
            return true;
        }

        definition = default;
        return false;
    }

    internal static bool IsUnsetSizeDefinition(
        RibbonControlSizeDefinition definition)
        => definition.Large == (RibbonControlSize)(-1)
           && definition.Medium == (RibbonControlSize)(-1)
           && definition.Small == (RibbonControlSize)(-1);

    /// <summary>Identifies the <see cref="CanAddToQuickAccessToolBar"/> dependency property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        DependencyProperty.Register(
            nameof(CanAddToQuickAccessToolBar),
            typeof(bool),
            typeof(RibbonControl),
            new PropertyMetadata(true, OnCanAddToQuickAccessToolBarChanged));

    /// <summary>Gets or sets whether this control may be added to quick access.</summary>
    public bool CanAddToQuickAccessToolBar
    {
        get => (bool)GetValue(CanAddToQuickAccessToolBarProperty);
        set => SetValue(CanAddToQuickAccessToolBarProperty, value);
    }

    /// <summary>Creates the compact quick-access representation.</summary>
    public abstract FrameworkElement? CreateQuickAccessItem();

    /// <summary>
    /// Binds portable shared properties to a quick-access element.
    /// </summary>
    public static void BindQuickAccessItem(FrameworkElement source, FrameworkElement element)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(element);

        Synchronize(
            source,
            FrameworkElement.DataContextProperty,
            element,
            FrameworkElement.DataContextProperty);
        Synchronize(
            source,
            UIElement.OpacityProperty,
            element,
            UIElement.OpacityProperty);

        if (source is Control sourceControl && element is Control targetControl)
        {
            Synchronize(
                sourceControl,
                Control.IsEnabledProperty,
                targetControl,
                Control.IsEnabledProperty);
        }

        Synchronize(
            source,
            RibbonProperties.CustomIconSizeProperty,
            element,
            RibbonProperties.CustomIconSizeProperty);
        Synchronize(
            source,
            RibbonProperties.QATIconSizeProperty,
            element,
            RibbonProperties.IconSizeProperty);

        if (source is RibbonControl sourceRibbonControl
            && element is RibbonControl targetRibbonControl)
        {
            Synchronize(
                sourceRibbonControl,
                HeaderProperty,
                targetRibbonControl,
                HeaderProperty);
            Synchronize(
                sourceRibbonControl,
                HeaderTemplateProperty,
                targetRibbonControl,
                HeaderTemplateProperty);
            Synchronize(
                sourceRibbonControl,
                HeaderTemplateSelectorProperty,
                targetRibbonControl,
                HeaderTemplateSelectorProperty);
            Synchronize(
                sourceRibbonControl,
                IconProperty,
                targetRibbonControl,
                IconProperty);
            Synchronize(
                sourceRibbonControl,
                CommandProperty,
                targetRibbonControl,
                CommandProperty);
            Synchronize(
                sourceRibbonControl,
                CommandParameterProperty,
                targetRibbonControl,
                CommandParameterProperty);
            Synchronize(
                sourceRibbonControl,
                CommandTargetProperty,
                targetRibbonControl,
                CommandTargetProperty);
            targetRibbonControl.Size = RibbonControlSize.Small;
        }

        var toolTip = Microsoft.UI.Xaml.Controls.ToolTipService.GetToolTip(source);
        if (toolTip is not null)
        {
            Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(element, toolTip);
        }
        else if (source is IHeaderedControl headered && headered.Header is not null)
        {
            Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(element, headered.Header);
        }
    }

    /// <summary>Handles changes to quick-access availability.</summary>
    public static void OnCanAddToQuickAccessToolBarChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
    }

    /// <summary>Handles a key-tip invocation.</summary>
    public virtual KeyTipPressedResult OnKeyTipPressed() => KeyTipPressedResult.Empty;

    /// <summary>Handles key-tip back navigation.</summary>
    public virtual void OnKeyTipBack()
    {
    }

    /// <summary>Gets whether the control and its command are currently enabled.</summary>
    protected virtual bool IsEnabledCore => IsEnabled && commandCanExecute;

    /// <summary>Gets the logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
    {
        get
        {
            if (Icon is not null)
            {
                yield return Icon;
            }

            if (Header is not null)
            {
                yield return Header;
            }
        }
    }

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonControlAutomationPeer(this);

    /// <summary>Returns whether the current command can execute.</summary>
    protected bool CanExecuteCommand()
    {
        return Command?.CanExecute(CommandParameter) ?? true;
    }

    /// <summary>Executes the current command when it can execute.</summary>
    protected void ExecuteCommand()
    {
        if (Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }

    /// <summary>
    /// Returns the portable XAML-root work area containing the control.
    /// </summary>
    /// <remarks>
    /// WinUI/Uno does not expose WPF's cross-platform monitor work-area API. The
    /// XAML-root bounds are returned in DIPs; unattached controls return an empty rectangle.
    /// </remarks>
    public static Rect GetControlWorkArea(FrameworkElement? control)
    {
        var size = control?.XamlRoot?.Size;
        return size is { } value ? new Rect(0, 0, value.Width, value.Height) : default;
    }

    /// <summary>
    /// Returns the portable monitor approximation containing the control.
    /// </summary>
    /// <remarks>
    /// The portable implementation is the XAML-root bounds, so it intentionally
    /// matches <see cref="GetControlWorkArea"/>. Platform heads may provide richer APIs.
    /// </remarks>
    public static Rect GetControlMonitor(FrameworkElement? control) => GetControlWorkArea(control);

    /// <summary>Finds the nearest parent <see cref="Ribbon"/> in the visual tree.</summary>
    public static Ribbon? GetParentRibbon(DependencyObject? obj)
    {
        var current = obj;
        while (current is not null)
        {
            if (current is Ribbon ribbon)
            {
                return ribbon;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static void OnCommandChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (sender is not RibbonControl control)
        {
            return;
        }

        if (args.OldValue is ICommand oldCommand)
        {
            oldCommand.CanExecuteChanged -= control.OnCommandCanExecuteChanged;
        }

        if (args.NewValue is ICommand newCommand)
        {
            newCommand.CanExecuteChanged += control.OnCommandCanExecuteChanged;
        }

        control.UpdateCommandCapability();
    }

    private static void OnCommandParameterChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (sender is RibbonControl control)
        {
            control.UpdateCommandCapability();
        }
    }

    private void OnCommandCanExecuteChanged(object? sender, EventArgs args)
    {
        UpdateCommandCapability();
    }

    private void UpdateCommandCapability()
    {
        commandCanExecute = CanExecuteCommand();
    }

    internal static void Synchronize(
        DependencyObject source,
        DependencyProperty sourceProperty,
        DependencyObject target,
        DependencyProperty targetProperty)
    {
        CopyValue(source, sourceProperty, target, targetProperty);

        var weakTarget = new WeakReference<DependencyObject>(target);
        long token = 0;
        token = source.RegisterPropertyChangedCallback(
            sourceProperty,
            (sender, changedProperty) =>
            {
                if (weakTarget.TryGetTarget(out var liveTarget))
                {
                    CopyValue(sender, changedProperty, liveTarget, targetProperty);
                }
                else
                {
                    sender.UnregisterPropertyChangedCallback(changedProperty, token);
                }
            });
    }

    private static void CopyValue(
        DependencyObject source,
        DependencyProperty sourceProperty,
        DependencyObject target,
        DependencyProperty targetProperty)
    {
        var value = source.GetValue(sourceProperty);
        if (Equals(target.GetValue(targetProperty), value) is false)
        {
            target.SetValue(targetProperty, value);
        }
    }
}
