namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System.Collections;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

public sealed partial class PortDataBindingFixture : UserControl
{
    public PortDataBindingFixture()
    {
        InitializeComponent();
    }

    internal Fluent.MenuItem Menu => DataMenu;
    internal StatusBar Status => DataStatus;
    internal RibbonGroupBox Group => DataGroup;
    internal Gallery Gallery => DataGallery;
    internal InRibbonGallery InlineGallery => DataInlineGallery;
    internal DataTemplate PrimaryTemplate => (DataTemplate)Resources["PrimaryItemTemplate"];
    internal DataTemplate AlternateTemplate => (DataTemplate)Resources["AlternateItemTemplate"];
    internal ItemsPanelTemplate CustomPanel => (ItemsPanelTemplate)Resources["CustomItemsPanel"];
}

internal static class PortDataApiFixture
{
    internal static void CompileConsumer(
        Fluent.MenuItem menu,
        StatusBar status,
        RibbonGroupBox group,
        Gallery gallery,
        IEnumerable items,
        DataTemplate template)
    {
        menu.ItemsSource = items;
        menu.ItemTemplate = template;
        status.ItemsSource = items;
        status.ItemTemplate = template;
        group.ItemsSource = items;
        group.ItemTemplate = template;
        gallery.ItemsSource = items;
        gallery.SelectionChanged += OnSelectionChanged;
        gallery.SelectionChanged -= OnSelectionChanged;
    }

    private static void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        _ = args.AddedItems;
        _ = args.RemovedItems;
    }
}
