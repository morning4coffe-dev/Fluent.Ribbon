namespace Fluent;

using System.Collections;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

public partial class MenuItem :
    IDropDownItemOwner,
    IQuickAccessItemProvider,
    IRibbonControl,
    IDropDownControl,
    IToggleButton
{
    private ItemsControl? submenuItemsHost;
    private DependencyObject? dropDownOwner;

    /// <summary>Identifies whether access-key markers are recognized.</summary>
    public static readonly DependencyProperty RecognizesAccessKeyProperty =
        DependencyProperty.RegisterAttached(
            nameof(RecognizesAccessKey),
            typeof(bool),
            typeof(MenuItem),
            new PropertyMetadata(true));

    /// <summary>Gets whether access-key markers are recognized.</summary>
    public bool RecognizesAccessKey
    {
        get => GetRecognizesAccessKey(this);
        set => SetRecognizesAccessKey(this, value);
    }

    /// <summary>Gets whether access-key markers are recognized on an element.</summary>
    public static bool GetRecognizesAccessKey(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(RecognizesAccessKeyProperty);
    }

    /// <summary>Sets whether access-key markers are recognized on an element.</summary>
    public static void SetRecognizesAccessKey(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(RecognizesAccessKeyProperty, value);
    }

    /// <summary>Gets the current submenu popup when the template provides one.</summary>
    public Popup? DropDownPopup { get; private set; }

    /// <summary>Gets the logical parent used by non-menu hosts.</summary>
    public object? LogicalParent =>
        Parent ?? VisualTreeHelper.GetParent(this);

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        DropDownPopup = GetTemplateChild("PART_Popup") as Popup ?? DropDownPopup;
    }

    /// <summary>Creates the default item container.</summary>
    protected virtual DependencyObject GetContainerForItemOverride()
    {
        return new MenuItem();
    }

    /// <summary>Gets whether an item is already its own container.</summary>
    protected virtual bool IsItemItsOwnContainerOverride(object item)
    {
        return item is UIElement;
    }

    /// <summary>Handles context-menu opening.</summary>
    protected virtual void OnContextMenuOpening(ContextMenuEventArgs e)
    {
        IsContextMenuOpened = true;
        IsDropDownOpen = false;
    }

    /// <summary>Handles context-menu closing.</summary>
    protected virtual void OnContextMenuClosing(ContextMenuEventArgs e)
    {
        IsContextMenuOpened = false;
    }

    /// <summary>Handles keyboard-focus changes.</summary>
    protected virtual void OnIsKeyboardFocusedChanged(
        DependencyPropertyChangedEventArgs e)
    {
    }

    /// <summary>Handles pointer entry.</summary>
    protected virtual void OnMouseEnter(PointerRoutedEventArgs e)
    {
        if (!IsContextMenuOpened
            && HasSubItems
            && dropDownOwner is RibbonDropDownButton or MenuItem)
        {
            IsDropDownOpen = true;
        }
    }

    /// <summary>Handles pointer exit.</summary>
    protected virtual void OnMouseLeave(PointerRoutedEventArgs e)
    {
        if (!IsContextMenuOpened
            && HasSubItems
            && dropDownOwner is RibbonDropDownButton)
        {
            IsDropDownOpen = false;
        }
    }

    /// <summary>Handles primary-pointer release.</summary>
    protected virtual void OnMouseLeftButtonUp(PointerRoutedEventArgs e)
    {
    }

    /// <summary>Handles pointer-wheel input.</summary>
    protected virtual void OnMouseWheel(PointerRoutedEventArgs e)
    {
    }

    /// <inheritdoc />
    protected override void OnPointerEntered(PointerRoutedEventArgs e)
    {
        base.OnPointerEntered(e);
        OnMouseEnter(e);
    }

    /// <inheritdoc />
    protected override void OnPointerExited(PointerRoutedEventArgs e)
    {
        base.OnPointerExited(e);
        OnMouseLeave(e);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        OnMouseLeftButtonUp(e);
    }

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        OnMouseWheel(e);
    }

    /// <summary>Gets logical children retained for WPF source compatibility.</summary>
    protected virtual IEnumerator LogicalChildren
    {
        get
        {
            foreach (var item in Items)
            {
                yield return item;
            }

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

    void IDropDownItemOwner.SetDropDownOwner(DependencyObject owner)
    {
        dropDownOwner = owner;
    }

    private void ShowCompatibilitySubmenu()
    {
        if (Items.Count == 0 || XamlRoot is null)
        {
            return;
        }

        if (DropDownPopup is null)
        {
            foreach (var item in Items.OfType<IDropDownItemOwner>())
            {
                item.SetDropDownOwner(this);
            }

            submenuItemsHost = new ItemsControl
            {
                ItemsSource = Items,
                MinWidth = Math.Max(160, ActualWidth)
            };
            DropDownPopup = new Popup
            {
                Child = submenuItemsHost,
                IsLightDismissEnabled = true,
                XamlRoot = XamlRoot
            };
            DropDownPopup.Closed += (_, _) =>
            {
                if (IsDropDownOpen)
                {
                    IsDropDownOpen = false;
                }
            };
        }
        else
        {
            foreach (var item in Items.OfType<IDropDownItemOwner>())
            {
                item.SetDropDownOwner(this);
            }

            DropDownPopup.XamlRoot = XamlRoot;
            if (submenuItemsHost is not null)
            {
                submenuItemsHost.ItemsSource = Items;
                submenuItemsHost.MinWidth = Math.Max(160, ActualWidth);
            }
        }

        var origin = TransformToVisual(null)
            .TransformPoint(new Windows.Foundation.Point(ActualWidth, 0));
        DropDownPopup.HorizontalOffset = origin.X;
        DropDownPopup.VerticalOffset = origin.Y;
        FlyoutShowHelper.OpenDeferred(DropDownPopup);
    }

    private void HideCompatibilitySubmenu()
    {
        if (DropDownPopup is not null)
        {
            DropDownPopup.IsOpen = false;
        }
    }

}
