namespace Fluent;

public partial class DropDownButton
{
    private bool previousSimplifiedState;

    public virtual FrameworkElement? CreateQuickAccessItem()
    {
        var clone = new DropDownButton
        {
            Size = RibbonControlSize.Small,
            CanAddToQuickAccessToolBar = false,
            ItemsSource = CreateQuickAccessItems()
        };

        BindQuickAccessItem(clone);
        BindQuickAccessItemDropDownEvents(clone);
        return clone;
    }

    protected virtual void BindQuickAccessItem(FrameworkElement element)
    {
        RibbonControl.BindQuickAccessItem(this, element);

        if (element is not DropDownButton target)
        {
            return;
        }

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
        BindTwoWay(RibbonDropDownButton.IsDropDownOpenProperty);
        return;

        void BindPresentation(DependencyProperty property) =>
            QuickAccessHelper.SynchronizePresentationValue(this, property, target, property);

        void BindOneWay(DependencyProperty property) =>
            RibbonControl.Synchronize(this, property, target, property);

        void BindTwoWay(DependencyProperty property)
        {
            RibbonControl.Synchronize(this, property, target, property);
            RibbonControl.Synchronize(target, property, this, property);
        }
    }

    protected void BindQuickAccessItemDropDownEvents(DropDownButton button)
    {
        button.DropDownOpened += OnQuickAccessOpened;
        button.DropDownOpened += OnQuickAccessDropDownOpened;
        button.DropDownClosed += OnQuickAccessDropDownClosed;
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

    private void OnQuickAccessDropDownOpened(object? sender, EventArgs e) =>
        RaiseDropDownOpened();

    private void OnQuickAccessDropDownClosed(object? sender, EventArgs e) =>
        RaiseDropDownClosed();

    private void OnSimplifiedPropertyChanged()
    {
        var newValue = IsSimplified;
        var oldValue = previousSimplifiedState;
        previousSimplifiedState = newValue;
        OnIsSimplifiedChanged(oldValue, newValue);
    }

    protected object[] CreateQuickAccessItems()
    {
        var source = ItemsSource is System.Collections.IEnumerable itemsSource
            ? itemsSource.Cast<object>()
            : Items.Cast<object>();
        return source.Select(CloneQuickAccessItem).ToArray();
    }

    private static object CloneQuickAccessItem(object item)
    {
        switch (item)
        {
            case MenuItem menuItem:
            {
                var clone = new MenuItem
                {
                    Header = QuickAccessHelper.ClonePresentationValue(menuItem.Header),
                    Description = menuItem.Description,
                    IsCheckable = menuItem.IsCheckable,
                    IsChecked = menuItem.IsChecked,
                    GroupName = menuItem.GroupName,
                    IsDefinitive = menuItem.IsDefinitive
                };
                clone.Click += (_, _) => menuItem.InvokeFromQuickAccess();
                CloneChildItems(menuItem.Items, clone.Items);
                return clone;
            }
            case Microsoft.UI.Xaml.Controls.Button button:
            {
                var clone = new Microsoft.UI.Xaml.Controls.Button
                {
                    Content = QuickAccessHelper.ClonePresentationValue(button.Content)
                };
                clone.Click += (_, _) => Fluent.Modern.Commands.RibbonInvoker.Invoke(button);
                return clone;
            }
            case UIElement element:
                return new ContentPresenter
                {
                    Content =
                        Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(element)
                        ?? element.GetType().Name
                };
            default:
                return item;
        }
    }

    private static void CloneChildItems(
        IEnumerable<UIElement> source,
        ICollection<UIElement> target)
    {
        foreach (var child in source)
        {
            var clone = CloneQuickAccessItem(child);
            target.Add(
                clone as UIElement
                ?? new ContentPresenter
                {
                    Content = clone
                });
        }
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
