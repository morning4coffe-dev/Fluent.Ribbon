namespace Fluent;

public partial class DropDownButton
{
    private bool previousSimplifiedState;

    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new DropDownButton
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false
        };

        BindQuickAccessItem(clone);
        BindQuickAccessItemDropDownEvents(clone);
        return clone;
    }

    protected virtual void BindQuickAccessItem(FrameworkElement element)
    {
        if (element is not DropDownButton target)
        {
            return;
        }

        var bindings = QuickAccessBindingSession.For(this, target);
        bindings.BindCommon();
        BindPresentation(RibbonDropDownButton.HeaderProperty);
        BindPresentation(IconProperty);
        BindPresentation(LargeIconProperty);
        BindPresentation(MediumIconProperty);
        BindOneWay(RibbonDropDownButton.IconGlyphProperty);
        BindOneWay(RibbonDropDownButton.MenuHeaderProperty);
        BindOneWay(RibbonDropDownButton.HasTriangleProperty);
        BindOneWay(RibbonDropDownButton.ResizeModeProperty);
        BindOneWay(RibbonDropDownButton.MaxDropDownHeightProperty);
        BindOneWay(RibbonDropDownButton.DropDownHeightProperty);
        BindOneWay(RibbonDropDownButton.ClosePopupOnMouseDownProperty);
        BindOneWay(RibbonDropDownButton.ClosePopupOnMouseDownDelayProperty);
        BindOneWay(RibbonDropDownButton.HeaderTemplateProperty);
        BindOneWay(HeaderTemplateSelectorProperty);
        BindOneWay(DismissOnClickOutsideProperty);
        BindOneWay(ItemTemplateProperty);
        BindOneWay(ItemTemplateSelectorProperty);
        BindOneWay(ItemContainerStyleProperty);
        BindOneWay(ItemContainerStyleSelectorProperty);
        BindOneWay(ItemsPanelProperty);
        BindOneWay(DisplayMemberPathProperty);
        ToolTipService.SetToolTip(target, ToolTipService.GetToolTip(this) ?? Header);
        return;

        void BindPresentation(DependencyProperty property) =>
            bindings.BindPresentation(property, property);

        void BindOneWay(DependencyProperty property) =>
            bindings.Bind(property);
    }

    /// <summary>Synchronizes a clone property for the lifetime of the quick-access presentation.</summary>
    protected void BindQuickAccessProperty(
        FrameworkElement target,
        DependencyProperty sourceProperty,
        DependencyProperty targetProperty,
        bool twoWay = false) =>
        QuickAccessBindingSession.For(this, target).Bind(sourceProperty, targetProperty, twoWay);

    protected void BindQuickAccessItemDropDownEvents(DropDownButton button)
    {
        button.BindSharedQuickAccessPresentation(this);
        var owner = new WeakReference<DropDownButton>(this);
        button.DropDownOpened += (_, _) =>
        {
            if (owner.TryGetTarget(out var source))
            {
                source.RaiseDropDownOpened();
            }
        };
        button.DropDownClosed += (_, _) =>
        {
            if (owner.TryGetTarget(out var source))
            {
                source.RaiseDropDownClosed();
            }
        };
    }

    protected virtual void OnDropDownOpened()
    {
    }

    protected virtual void OnDropDownClosed()
    {
    }

    protected virtual void OnIsSimplifiedChanged(bool oldValue, bool newValue)
    {
    }

    protected void OnQuickAccessOpened(object? sender, EventArgs e)
    {
        if (sender is not DropDownButton button)
        {
            return;
        }

        button.DropDownClosed -= OnQuickAccessMenuClosedOrUnloaded;
        button.Unloaded -= OnQuickAccessMenuClosedOrUnloaded;
        button.DropDownClosed += OnQuickAccessMenuClosedOrUnloaded;
        button.Unloaded += OnQuickAccessMenuClosedOrUnloaded;
    }

    protected void OnQuickAccessMenuClosedOrUnloaded(object? sender, EventArgs e)
    {
        DetachQuickAccessMenu(sender);
    }

#if WINDOWS
    private void OnQuickAccessMenuClosedOrUnloaded(object? sender, RoutedEventArgs e)
    {
        DetachQuickAccessMenu(sender);
    }
#endif

    private void DetachQuickAccessMenu(object? sender)
    {
        if (sender is not DropDownButton button)
        {
            return;
        }

        button.DropDownClosed -= OnQuickAccessMenuClosedOrUnloaded;
        button.Unloaded -= OnQuickAccessMenuClosedOrUnloaded;
    }

    private void OnSimplifiedPropertyChanged()
    {
        var newValue = IsSimplified;
        var oldValue = previousSimplifiedState;
        previousSimplifiedState = newValue;
        OnIsSimplifiedChanged(oldValue, newValue);
    }

    /// <summary>Creates a provider/data snapshot for derived controls. Use the drop-down lifecycle binding for live content.</summary>
    protected object[] CreateQuickAccessItems()
    {
        var source = ItemsSource is System.Collections.IEnumerable itemsSource
            ? itemsSource.Cast<object>()
            : Items.Cast<object>();
        return source.Select(item => item switch
        {
            IQuickAccessItemProvider provider => provider.CreateQuickAccessItem()
                ?? throw new NotSupportedException("An item provider did not create a quick access copy."),
            UIElement => throw new NotSupportedException(
                "Arbitrary UIElements require live content transfer. BindQuickAccessItemDropDownEvents " +
                "connects that transfer without assigning an ItemsSource snapshot."),
            _ => item,
        }).ToArray();
    }

    public override KeyTipPressedResult OnKeyTipPressed() =>
        base.OnKeyTipPressed();

    public new void OnKeyTipBack() => base.OnKeyTipBack();

    public new void UpdateSimplifiedState(bool isSimplified) =>
        base.UpdateSimplifiedState(isSimplified);

    RibbonControlSizeDefinition IRibbonControl.SizeDefinition
    {
        get => SizeDefinition;
        set => SizeDefinition = value;
    }

    RibbonControlSizeDefinition ISimplifiedRibbonControl.SimplifiedSizeDefinition
    {
        get => SimplifiedSizeDefinition;
        set => SimplifiedSizeDefinition = value;
    }

    object? ILargeIconProvider.LargeIcon
    {
        get => LargeIcon;
        set => LargeIcon = value;
    }

    object? IMediumIconProvider.MediumIcon
    {
        get => MediumIcon;
        set => MediumIcon = value;
    }
}
