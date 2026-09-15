namespace FluentRibbon.Uno.Showcase.Diagnostics;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed partial class PortParityFixture : UserControl
{
    public PortParityFixture()
    {
        InitializeComponent();
    }

    internal Panel Host => ItemsHost;

    internal DataTemplate ItemTemplate => (DataTemplate)Resources["PortParityItemTemplate"];

    internal DataTemplate ButtonTemplate => (DataTemplate)Resources["PortParityButtonTemplate"];

    internal DataTemplate HeaderTemplate => (DataTemplate)Resources["PortParityHeaderTemplate"];
}
