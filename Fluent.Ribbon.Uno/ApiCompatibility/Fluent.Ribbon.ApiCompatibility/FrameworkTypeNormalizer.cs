namespace Fluent.Ribbon.ApiCompatibility;

public sealed class FrameworkTypeNormalizer
{
    private static readonly IReadOnlyDictionary<string, string> ExactMappings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["System.Windows.Application"] = "Microsoft.UI.Xaml.Application",
            ["System.Windows.CornerRadius"] = "Microsoft.UI.Xaml.CornerRadius",
            ["System.Windows.DataTemplate"] = "Microsoft.UI.Xaml.DataTemplate",
            ["System.Windows.DependencyObject"] = "Microsoft.UI.Xaml.DependencyObject",
            ["System.Windows.DependencyProperty"] = "Microsoft.UI.Xaml.DependencyProperty",
            ["System.Windows.DependencyPropertyKey"] = "Microsoft.UI.Xaml.DependencyProperty",
            ["System.Windows.DependencyPropertyChangedEventArgs"] = "Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs",
            ["System.Windows.DependencyPropertyChangedEventHandler"] = "System.EventHandler`1<Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs>",
            ["System.Windows.Documents.AdornerLayer"] = "Microsoft.UI.Xaml.FrameworkElement",
            ["System.Windows.FlowDirection"] = "Microsoft.UI.Xaml.FlowDirection",
            ["System.Windows.FrameworkElement"] = "Microsoft.UI.Xaml.FrameworkElement",
            ["System.Windows.GridLength"] = "Microsoft.UI.Xaml.GridLength",
            ["System.Windows.HorizontalAlignment"] = "Microsoft.UI.Xaml.HorizontalAlignment",
            ["System.Windows.IInputElement"] = "Microsoft.UI.Xaml.UIElement",
            ["System.Windows.Controls.ContextMenu"] = "Microsoft.UI.Xaml.Controls.MenuFlyout",
            ["System.Windows.Controls.HeaderedItemsControl"] = "Fluent.HeaderedItemsControl",
            ["System.Windows.Controls.ListBoxItem"] = "Fluent.RibbonGalleryItem",
            ["System.Windows.Controls.ItemContainerTemplateSelector"] = "Microsoft.UI.Xaml.Controls.DataTemplateSelector",
            ["System.Windows.Controls.ItemContainerGenerator"] = "Fluent.ItemContainerGenerator",
            ["System.Windows.Controls.Primitives.IScrollInfo"] = "Fluent.IScrollInfo",
            ["System.Windows.Controls.MenuItem"] = "Fluent.InteractiveMenuItemBase",
            ["System.Windows.Controls.TabItem"] = "Microsoft.UI.Xaml.Controls.TabViewItem",
            ["System.Windows.Controls.Primitives.MenuBase"] = "Fluent.MenuBase",
            ["System.Windows.Controls.Primitives.StatusBar"] = "Fluent.StatusBarBase",
            ["System.Windows.Controls.Primitives.StatusBarItem"] = "Fluent.StatusBarItemBase",
            ["System.Windows.Controls.Primitives.CustomPopupPlacement"] = "Fluent.Helpers.CustomPopupPlacement",
            ["System.Windows.Controls.Primitives.CustomPopupPlacementCallback"] = "Fluent.Helpers.CustomPopupPlacementCallback",
            ["System.Windows.Controls.Primitives.PopupPrimaryAxis"] = "Fluent.Helpers.PopupPrimaryAxis",
            ["System.Windows.Data.IMultiValueConverter"] = "Fluent.IMultiValueConverter",
            ["System.Windows.Input.IInputElement"] = "Microsoft.UI.Xaml.UIElement",
            ["System.Windows.Input.ICommandSource"] = "Fluent.ICommandSource",
            ["System.Windows.Input.Key"] = "Windows.System.VirtualKey",
            ["System.Windows.Input.KeyEventArgs"] = "Microsoft.UI.Xaml.Input.KeyRoutedEventArgs",
            ["System.Windows.Input.KeyboardFocusChangedEventArgs"] = "Microsoft.UI.Xaml.RoutedEventArgs",
            ["System.Windows.Input.MouseButtonEventArgs"] = "Microsoft.UI.Xaml.Input.PointerRoutedEventArgs",
            ["System.Windows.Input.MouseEventArgs"] = "Microsoft.UI.Xaml.Input.PointerRoutedEventArgs",
            ["System.Windows.Input.MouseWheelEventArgs"] = "Microsoft.UI.Xaml.Input.PointerRoutedEventArgs",
            ["System.Windows.Input.RoutedCommand"] = "Microsoft.UI.Xaml.Input.XamlUICommand",
            ["System.Windows.Input.RoutedUICommand"] = "Microsoft.UI.Xaml.Input.XamlUICommand",
            ["System.Windows.Point"] = "Windows.Foundation.Point",
            ["System.Windows.Rect"] = "Windows.Foundation.Rect",
            ["System.Windows.ResourceDictionary"] = "Microsoft.UI.Xaml.ResourceDictionary",
            ["System.Windows.RoutedEvent"] = "Microsoft.UI.Xaml.RoutedEvent",
            ["System.Windows.RoutedEventArgs"] = "Microsoft.UI.Xaml.RoutedEventArgs",
            ["System.Windows.RoutedEventHandler"] = "Microsoft.UI.Xaml.RoutedEventHandler",
            ["System.Windows.RoutedPropertyChangedEventHandler`1"] = "Fluent.RoutedPropertyChangedEventHandler`1",
            ["System.Windows.Size"] = "Windows.Foundation.Size",
            ["System.Windows.Style"] = "Microsoft.UI.Xaml.Style",
            ["System.Windows.Thickness"] = "Microsoft.UI.Xaml.Thickness",
            ["System.Windows.UIElement"] = "Microsoft.UI.Xaml.UIElement",
            ["System.Windows.VerticalAlignment"] = "Microsoft.UI.Xaml.VerticalAlignment",
            ["System.Windows.Visibility"] = "Microsoft.UI.Xaml.Visibility",
            ["System.Windows.Media.Color"] = "Windows.UI.Color",
            ["System.Windows.Media.HitTestResult"] = "Fluent.HitTestResult",
            ["System.Windows.Media.PointHitTestParameters"] = "Fluent.PointHitTestParameters",
            ["System.Windows.Media.Visual"] = "Microsoft.UI.Xaml.UIElement"
        };

    private static readonly (string Source, string Target)[] PrefixMappings =
    [
        ("System.Windows.RoutedPropertyChangedEventHandler`1<", "Fluent.RoutedPropertyChangedEventHandler`1<"),
        ("System.Windows.Automation.Peers.", "Microsoft.UI.Xaml.Automation.Peers."),
        ("System.Windows.Automation.Provider.", "Microsoft.UI.Xaml.Automation.Provider."),
        ("System.Windows.Automation.", "Microsoft.UI.Xaml.Automation."),
        ("System.Windows.Controls.Primitives.", "Microsoft.UI.Xaml.Controls.Primitives."),
        ("System.Windows.Media.Animation.", "Microsoft.UI.Xaml.Media.Animation."),
        ("System.Windows.Media.Imaging.", "Microsoft.UI.Xaml.Media.Imaging."),
        ("System.Windows.Controls.", "Microsoft.UI.Xaml.Controls."),
        ("System.Windows.Data.", "Microsoft.UI.Xaml.Data."),
        ("System.Windows.Markup.", "Microsoft.UI.Xaml.Markup."),
        ("System.Windows.Media.", "Microsoft.UI.Xaml.Media."),
        ("System.Windows.Shapes.", "Microsoft.UI.Xaml.Shapes.")
    ];

    public string Normalize(string typeName)
    {
        if (ExactMappings.TryGetValue(typeName, out var replacement))
        {
            return replacement;
        }

        foreach (var (source, target) in PrefixMappings)
        {
            if (typeName.StartsWith(source, StringComparison.Ordinal))
            {
                return target + typeName[source.Length..];
            }
        }

        return typeName;
    }
}
