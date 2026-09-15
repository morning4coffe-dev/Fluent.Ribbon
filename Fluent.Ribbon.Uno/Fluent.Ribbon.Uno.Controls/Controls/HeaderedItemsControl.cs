namespace Fluent;

/// <summary>
/// Portable substitute for WPF's headered items control.
/// </summary>
public partial class HeaderedItemsControl : ItemsControl
{
    /// <summary>Identifies whether the control contains any items.</summary>
    public static readonly DependencyProperty HasItemsProperty = Fluent.Helpers.ItemsControlBinding.HasItemsProperty;

    /// <summary>Gets whether the control contains any items.</summary>
    public bool HasItems => (bool)GetValue(HasItemsProperty);

    /// <summary>Identifies whether the control has a header.</summary>
    public static readonly DependencyProperty HasHeaderProperty =
        DependencyProperty.Register(nameof(HasHeader), typeof(bool), typeof(HeaderedItemsControl), new PropertyMetadata(false));

    /// <summary>Gets whether the control has a header.</summary>
    public bool HasHeader => (bool)GetValue(HasHeaderProperty);

    /// <summary>Initializes a headered items control.</summary>
    public HeaderedItemsControl()
    {
        Items.VectorChanged += (_, _) => SetValue(HasItemsProperty, Items.Count > 0);
    }

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(HeaderedItemsControl),
            new PropertyMetadata(null, static (sender, args) => sender.SetValue(HasHeaderProperty, args.NewValue is not null)));

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
