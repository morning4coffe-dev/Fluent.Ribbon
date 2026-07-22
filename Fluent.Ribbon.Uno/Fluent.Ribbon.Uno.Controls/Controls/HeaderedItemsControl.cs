namespace Fluent;

/// <summary>
/// Portable substitute for WPF's headered items control.
/// </summary>
public class HeaderedItemsControl : ItemsControl
{
    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(HeaderedItemsControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the header.</summary>
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
            typeof(HeaderedItemsControl),
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
            typeof(HeaderedItemsControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the header-template selector.</summary>
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }
}
