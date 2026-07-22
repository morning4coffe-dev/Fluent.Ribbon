namespace Fluent;

using System.Collections;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;

public partial class DropDownButton
{
    private object? currentContainerItem;

    /// <summary>Identifies the item-container template-selector property.</summary>
    public static readonly DependencyProperty ItemContainerTemplateSelectorProperty =
        DependencyProperty.Register(
            nameof(ItemContainerTemplateSelector),
            typeof(DataTemplateSelector),
            typeof(DropDownButton),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the item-container template selector.</summary>
    public DataTemplateSelector? ItemContainerTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ItemContainerTemplateSelectorProperty);
        set => SetValue(ItemContainerTemplateSelectorProperty, value);
    }

    /// <summary>Identifies whether container templates are enabled.</summary>
    public static readonly DependencyProperty UsesItemContainerTemplateProperty =
        DependencyProperty.Register(
            nameof(UsesItemContainerTemplate),
            typeof(bool),
            typeof(DropDownButton),
            new PropertyMetadata(false));

    /// <summary>Gets or sets whether item-container templates are enabled.</summary>
    public bool UsesItemContainerTemplate
    {
        get => (bool)GetValue(UsesItemContainerTemplateProperty);
        set => SetValue(UsesItemContainerTemplateProperty, value);
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item)
    {
        if (base.IsItemItsOwnContainerOverride(item))
        {
            return true;
        }

        if (UsesItemContainerTemplate)
        {
            currentContainerItem = item;
        }

        return item is UIElement;
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride()
    {
        if (UsesItemContainerTemplate)
        {
            var item = currentContainerItem;
            currentContainerItem = null;
            var template = ItemContainerTemplateSelector?.SelectTemplate(item, this)
                           ?? ItemTemplate;
            if (template?.LoadContent() is DependencyObject container)
            {
                return container;
            }
        }

        return base.GetContainerForItemOverride();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        if (!e.Handled)
        {
            if (e.Key == VirtualKey.Escape && IsDropDownOpen)
            {
                CloseDropDown();
                e.Handled = true;
            }
            else if (e.Key is VirtualKey.Enter or VirtualKey.Space or VirtualKey.Down)
            {
                OnKeyTipPressed();
                e.Handled = true;
            }
        }

        base.OnKeyDown(e);
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

            if (Gallery is not null)
            {
                yield return Gallery;
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
}
