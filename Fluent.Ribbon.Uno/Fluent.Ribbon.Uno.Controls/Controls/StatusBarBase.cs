namespace Fluent;

/// <summary>Portable substitute for WPF's status bar base.</summary>
public partial class StatusBarBase : ItemsControl
{
    /// <summary>Identifies whether the status bar contains any items.</summary>
    public static readonly DependencyProperty HasItemsProperty = Fluent.Helpers.ItemsControlBinding.HasItemsProperty;

    /// <summary>Gets whether the status bar contains any items.</summary>
    public bool HasItems => (bool)GetValue(HasItemsProperty);

    /// <summary>Initializes a status-bar items control.</summary>
    public StatusBarBase()
    {
        Items.VectorChanged += (_, _) => SetValue(HasItemsProperty, Items.Count > 0);
    }
}

/// <summary>Portable substitute for WPF's status bar item base.</summary>
public partial class StatusBarItemBase : ContentControl
{
}
