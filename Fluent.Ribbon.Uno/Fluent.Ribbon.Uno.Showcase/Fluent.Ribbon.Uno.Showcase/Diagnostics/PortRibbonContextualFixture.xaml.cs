namespace FluentRibbon.Uno.Showcase.Diagnostics;

using Fluent;
using Microsoft.UI.Xaml.Controls;

public sealed partial class PortRibbonContextualFixture : UserControl
{
    public PortRibbonContextualFixture()
    {
        InitializeComponent();
    }

    internal Ribbon Ribbon => TestRibbon;
    internal RibbonTabItem Tab => ToolsTab;
    internal RibbonContextualTabGroup Group => ToolsGroup;
}
