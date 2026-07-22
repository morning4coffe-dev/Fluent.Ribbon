using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class FrameworkTypeNormalizerTests
{
    private readonly FrameworkTypeNormalizer normalizer = new();

    [TestCase("System.Windows.DependencyObject", "Microsoft.UI.Xaml.DependencyObject")]
    [TestCase("System.Windows.DependencyPropertyKey", "Microsoft.UI.Xaml.DependencyProperty")]
    [TestCase("System.Windows.DependencyPropertyChangedEventArgs", "Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs")]
    [TestCase("System.Windows.Documents.AdornerLayer", "Microsoft.UI.Xaml.FrameworkElement")]
    [TestCase("System.Windows.DataTemplate", "Microsoft.UI.Xaml.DataTemplate")]
    [TestCase("System.Windows.DependencyPropertyChangedEventHandler", "System.EventHandler`1<Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs>")]
    [TestCase("System.Windows.Input.Key", "Windows.System.VirtualKey")]
    [TestCase("System.Windows.Input.KeyEventArgs", "Microsoft.UI.Xaml.Input.KeyRoutedEventArgs")]
    [TestCase("System.Windows.Input.KeyboardFocusChangedEventArgs", "Microsoft.UI.Xaml.RoutedEventArgs")]
    [TestCase("System.Windows.Input.MouseButtonEventArgs", "Microsoft.UI.Xaml.Input.PointerRoutedEventArgs")]
    [TestCase("System.Windows.Input.MouseEventArgs", "Microsoft.UI.Xaml.Input.PointerRoutedEventArgs")]
    [TestCase("System.Windows.Input.MouseWheelEventArgs", "Microsoft.UI.Xaml.Input.PointerRoutedEventArgs")]
    [TestCase("System.Windows.Input.IInputElement", "Microsoft.UI.Xaml.UIElement")]
    [TestCase("System.Windows.Input.ICommandSource", "Fluent.ICommandSource")]
    [TestCase("System.Windows.IInputElement", "Microsoft.UI.Xaml.UIElement")]
    [TestCase("System.Windows.Input.RoutedCommand", "Microsoft.UI.Xaml.Input.XamlUICommand")]
    [TestCase("System.Windows.GridLength", "Microsoft.UI.Xaml.GridLength")]
    [TestCase("System.Windows.Point", "Windows.Foundation.Point")]
    [TestCase("System.Windows.RoutedEvent", "Microsoft.UI.Xaml.RoutedEvent")]
    [TestCase("System.Windows.RoutedPropertyChangedEventHandler`1", "Fluent.RoutedPropertyChangedEventHandler`1")]
    [TestCase("System.Windows.Media.Color", "Windows.UI.Color")]
    [TestCase("System.Windows.Media.HitTestResult", "Fluent.HitTestResult")]
    [TestCase("System.Windows.Media.PointHitTestParameters", "Fluent.PointHitTestParameters")]
    [TestCase("System.Windows.Media.Visual", "Microsoft.UI.Xaml.UIElement")]
    [TestCase("System.Windows.Automation.ExpandCollapseState", "Microsoft.UI.Xaml.Automation.ExpandCollapseState")]
    [TestCase("System.Windows.Automation.Provider.IInvokeProvider", "Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider")]
    [TestCase("System.Windows.Controls.ContextMenu", "Microsoft.UI.Xaml.Controls.MenuFlyout")]
    [TestCase("System.Windows.Controls.HeaderedItemsControl", "Fluent.HeaderedItemsControl")]
    [TestCase("System.Windows.Controls.ListBoxItem", "Fluent.RibbonGalleryItem")]
    [TestCase("System.Windows.Controls.ItemContainerTemplateSelector", "Microsoft.UI.Xaml.Controls.DataTemplateSelector")]
    [TestCase("System.Windows.Controls.ItemContainerGenerator", "Fluent.ItemContainerGenerator")]
    [TestCase("System.Windows.Controls.Primitives.IScrollInfo", "Fluent.IScrollInfo")]
    [TestCase("System.Windows.Controls.MenuItem", "Fluent.InteractiveMenuItemBase")]
    [TestCase("System.Windows.Controls.TabItem", "Microsoft.UI.Xaml.Controls.TabViewItem")]
    [TestCase("System.Windows.Controls.Primitives.MenuBase", "Fluent.MenuBase")]
    [TestCase("System.Windows.Controls.Primitives.StatusBar", "Fluent.StatusBarBase")]
    [TestCase("System.Windows.Controls.Primitives.StatusBarItem", "Fluent.StatusBarItemBase")]
    [TestCase("System.Windows.Controls.Primitives.CustomPopupPlacement", "Fluent.Helpers.CustomPopupPlacement")]
    [TestCase("System.Windows.Controls.Primitives.CustomPopupPlacementCallback", "Fluent.Helpers.CustomPopupPlacementCallback")]
    [TestCase("System.Windows.Controls.Primitives.PopupPrimaryAxis", "Fluent.Helpers.PopupPrimaryAxis")]
    [TestCase("System.Windows.Data.IMultiValueConverter", "Fluent.IMultiValueConverter")]
    [TestCase("System.Windows.Controls.Primitives.ButtonBase", "Microsoft.UI.Xaml.Controls.Primitives.ButtonBase")]
    [TestCase("System.Windows.Media.Imaging.BitmapSource", "Microsoft.UI.Xaml.Media.Imaging.BitmapSource")]
    public void Normalize_MapsApprovedFrameworkSubstitutions(string source, string expected)
    {
        Assert.That(this.normalizer.Normalize(source), Is.EqualTo(expected));
    }

    [Test]
    public void Normalize_DoesNotMapUnapprovedWpfInputTypes()
    {
        Assert.That(
            this.normalizer.Normalize("System.Windows.Input.StylusDevice"),
            Is.EqualTo("System.Windows.Input.StylusDevice"));
    }
}
