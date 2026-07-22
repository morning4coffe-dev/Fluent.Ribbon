namespace Fluent;

public partial class MenuItem
{
    /// <summary>Identifies whether the item exposes separate primary and menu actions.</summary>
    public static readonly DependencyProperty IsSplitProperty =
        DependencyProperty.Register(
            nameof(IsSplit),
            typeof(bool),
            typeof(MenuItem),
            new PropertyMetadata(false));

    /// <summary>Gets or sets whether the item exposes separate primary and menu actions.</summary>
    public bool IsSplit
    {
        get => (bool)GetValue(IsSplitProperty);
        set => SetValue(IsSplitProperty, value);
    }

    /// <summary>Identifies the maximum drop-down height property.</summary>
    public static readonly DependencyProperty MaxDropDownHeightProperty =
        DependencyProperty.Register(
            nameof(MaxDropDownHeight),
            typeof(double),
            typeof(MenuItem),
            new PropertyMetadata(double.NaN));

    /// <summary>Gets or sets the maximum submenu height.</summary>
    public double MaxDropDownHeight
    {
        get => (double)GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    /// <summary>Identifies the drop-down resize capability property.</summary>
    public static readonly DependencyProperty ResizeModeProperty =
        DependencyProperty.Register(
            nameof(ResizeMode),
            typeof(ContextMenuResizeMode),
            typeof(MenuItem),
            new PropertyMetadata(ContextMenuResizeMode.None));

    /// <summary>Gets or sets the requested submenu resize capability.</summary>
    public ContextMenuResizeMode ResizeMode
    {
        get => (ContextMenuResizeMode)GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    /// <summary>Gets or sets whether a compatibility context menu is open.</summary>
    public bool IsContextMenuOpened { get; set; }
}
