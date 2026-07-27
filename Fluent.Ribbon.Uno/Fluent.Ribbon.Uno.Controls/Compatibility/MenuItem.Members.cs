namespace Fluent;

public partial class MenuItem
{
    /// <summary>Identifies the ribbon size property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(MenuItem),
            new PropertyMetadata(RibbonControlSize.Large));

    /// <summary>Gets or sets the ribbon size.</summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the typed size-definition property.</summary>
    public static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(MenuItem),
            new PropertyMetadata(default(RibbonControlSizeDefinition)));

    /// <summary>Gets or sets the typed size definition.</summary>
    public RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies whether the item can be checked.</summary>
    public static readonly DependencyProperty IsCheckableProperty =
        DependencyProperty.Register(
            nameof(IsCheckable),
            typeof(bool),
            typeof(MenuItem),
            new PropertyMetadata(false));

    /// <summary>Gets or sets whether the item can be checked.</summary>
    public bool IsCheckable
    {
        get => (bool)GetValue(IsCheckableProperty);
        set => SetValue(IsCheckableProperty, value);
    }

    /// <summary>Identifies the checked-state property.</summary>
    public static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool?),
            typeof(MenuItem),
            new PropertyMetadata(false));

    /// <summary>Gets or sets the checked state.</summary>
    public bool? IsChecked
    {
        get => (bool?)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Identifies the mutually exclusive group name.</summary>
    public static readonly DependencyProperty GroupNameProperty =
        DependencyProperty.Register(
            nameof(GroupName),
            typeof(string),
            typeof(MenuItem),
            new PropertyMetadata(null));

    /// <summary>Gets or sets the mutually exclusive group name.</summary>
    public string? GroupName
    {
        get => (string?)GetValue(GroupNameProperty);
        set => SetValue(GroupNameProperty, value);
    }

    /// <summary>Identifies whether invocation definitively closes an ancestor drop-down.</summary>
    public static readonly DependencyProperty IsDefinitiveProperty =
        DependencyProperty.Register(
            nameof(IsDefinitive),
            typeof(bool),
            typeof(MenuItem),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether invocation closes an ancestor drop-down.</summary>
    public bool IsDefinitive
    {
        get => (bool)GetValue(IsDefinitiveProperty);
        set => SetValue(IsDefinitiveProperty, value);
    }

    /// <summary>Identifies compatibility drop-down state.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty =
        DependencyProperty.Register(
            nameof(IsDropDownOpen),
            typeof(bool),
            typeof(MenuItem),
            new PropertyMetadata(false, OnIsDropDownOpenChanged));

    /// <summary>Gets or sets compatibility submenu state.</summary>
    public bool IsDropDownOpen
    {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Identifies the quick-access capability property.</summary>
    public static readonly DependencyProperty CanAddToQuickAccessToolBarProperty =
        RibbonProperties.CanAddToQuickAccessToolBarProperty;

    /// <summary>Gets or sets whether the item can be added to quick access.</summary>
    public bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    /// <summary>Occurs when compatibility submenu state becomes open.</summary>
    public event EventHandler? DropDownOpened;

    /// <summary>Occurs when compatibility submenu state becomes closed.</summary>
    public event EventHandler? DropDownClosed;

    private static void OnIsDropDownOpenChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var item = (MenuItem)sender;
        if ((bool)args.NewValue)
        {
            item.ShowCompatibilitySubmenu();
            PopupService.RegisterOpenDropDown(item);
            item.DropDownOpened?.Invoke(item, EventArgs.Empty);
        }
        else
        {
            PopupService.UnregisterOpenDropDown(item);
            item.HideCompatibilitySubmenu();
            item.DropDownClosed?.Invoke(item, EventArgs.Empty);
        }
    }
}
