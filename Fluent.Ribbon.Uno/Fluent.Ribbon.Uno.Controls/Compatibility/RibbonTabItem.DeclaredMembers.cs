namespace Fluent;

public partial class RibbonTabItem
{
    /// <summary>Identifies the inherited tab-header property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        Microsoft.UI.Xaml.Controls.TabViewItem.HeaderProperty;

    /// <summary>Gets or sets the tab header.</summary>
    public new object? Header
    {
        get => base.Header;
        set => base.Header = value;
    }

    /// <summary>Identifies the inherited header-template property.</summary>
    public new static readonly DependencyProperty HeaderTemplateProperty =
        Microsoft.UI.Xaml.Controls.TabViewItem.HeaderTemplateProperty;

    /// <summary>Gets or sets the tab header template.</summary>
    public new DataTemplate? HeaderTemplate
    {
        get => base.HeaderTemplate;
        set => base.HeaderTemplate = value;
    }

    /// <summary>Identifies the inherited selection property.</summary>
    public new static readonly DependencyProperty IsSelectedProperty =
        Microsoft.UI.Xaml.Controls.TabViewItem.IsSelectedProperty;

    /// <summary>Gets or sets whether the tab is selected.</summary>
    public new bool IsSelected
    {
        get => base.IsSelected;
        set => base.IsSelected = value;
    }
}
