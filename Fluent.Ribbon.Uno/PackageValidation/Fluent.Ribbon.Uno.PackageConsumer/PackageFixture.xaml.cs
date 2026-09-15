namespace FluentUno.PackageConsumer;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed partial class PackageFixture : UserControl
{
    public PackageFixture()
    {
        InitializeComponent();
    }
}

public sealed partial class PackageHeaderSelector : DataTemplateSelector
{
    public DataTemplate? Template { get; set; }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        Template ?? throw new System.InvalidOperationException("The package fixture header template is missing.");
}
