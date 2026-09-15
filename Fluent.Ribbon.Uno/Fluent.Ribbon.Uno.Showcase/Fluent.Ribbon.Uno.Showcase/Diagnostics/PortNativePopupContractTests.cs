#if WINDOWS
namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using NativeButton = Microsoft.UI.Xaml.Controls.Button;
using NativeTextBox = Microsoft.UI.Xaml.Controls.TextBox;

internal static class PortNativePopupContractTests
{
    internal static async Task VerifyAsync(Panel host, Func<Task> settle)
    {
        await VerifyPendingReanchorTemplates(host, settle);
        await VerifyLivePresentation(host, settle);
        App.LogAutoTestStartup("NATIVE POPUP LIFETIME BEGIN");
        var retired = new List<RetiredOwner>();
        for (var cycle = 0; cycle < 24; cycle++)
        {
            retired.Add(await VerifyCycle(host, settle, cycle));
            Collect();
            await settle();
            App.LogAutoTestStartup($"NATIVE POPUP LIFETIME PASS cycle={cycle}");
        }
        Collect();
        await settle();
        Require(retired.All(owner => !owner.Source.TryGetTarget(out _) && !owner.Copy.TryGetTarget(out _)),
            "Retired native popup sources/copies remained rooted.");
        Require(retired.SelectMany(owner => owner.Templates).All(template =>
                !template.Popup.TryGetTarget(out _) && !template.Presenter.TryGetTarget(out _)),
            "Obsolete source-template Popup/ItemsPresenter wrappers remained rooted.");
        App.LogAutoTestStartup("NATIVE POPUP LIFETIME COMPLETE cycles=24");
    }

    private static async Task VerifyPendingReanchorTemplates(Panel host, Func<Task> settle)
    {
        foreach (var menu in new[] { false, true })
        {
            foreach (var boundStyle in new[] { false, true })
            {
                ItemsControl source = menu
                    ? new Fluent.MenuItem { Header = "Reanchor menu" }
                    : new Fluent.DropDownButton { Header = "Reanchor dropdown" };
                var item = new NativeButton { Content = "Original reanchor item", Width = 180, Height = 32 };
                if (source is Fluent.MenuItem sourceMenu)
                {
                    sourceMenu.Items.Add(item);
                }
                else
                {
                    source.Items.Add(item);
                }
                var sourceControl = (Control)source;
                var values = new ContentControl { Content = Style(menu) };
                Binding? styleBinding = null;
                if (boundStyle)
                {
                    styleBinding = new Binding
                    {
                        Source = values, Path = new PropertyPath("Content"), Mode = BindingMode.OneWay,
                    };
                    source.SetBinding(FrameworkElement.StyleProperty, styleBinding);
                }
                else
                {
                    sourceControl.Template = Template(menu);
                }
                var first = (RibbonDropDownButton)((IQuickAccessItemProvider)source).CreateQuickAccessItem()!;
                var second = (RibbonDropDownButton)((IQuickAccessItemProvider)source).CreateQuickAccessItem()!;
                host.Children.Add(source);
                host.Children.Add(first);
                host.Children.Add(second);
                try
                {
                    await settle();
                    ((IDropDownControl)source).IsDropDownOpen = true;
                    await settle();
                    Require(Part(sourceControl, "PART_Popup") is Popup { IsOpen: true },
                        "The source popup did not open before the reanchor transition.");

                    foreach (var target in new[] { first, second })
                    {
                        target.IsDropDownOpen = true;
                        var pendingName = menu ? "pendingQuickAccessBorrower" : "_pendingPopupAnchor";
                        var declaringType = menu ? typeof(Fluent.MenuItem) : typeof(RibbonDropDownButton);
                        var pendingField = declaringType.GetField(pendingName, BindingFlags.Instance | BindingFlags.NonPublic)
                                           ?? throw new InvalidOperationException("The pending anchor inspection is unavailable.");
                        Require(pendingField.GetValue(source) is not null,
                            "The native reanchor did not retain its target until old-popup closure.");
                        var template = Template(menu);
                        if (boundStyle)
                        {
                            values.Content = Style(menu, template);
                        }
                        else
                        {
                            sourceControl.Template = template;
                        }
                        await settle();
                        var popup = ActivePopup(target);
                        Require(popup.IsOpen && ReferenceEquals(popup, Part(sourceControl, "PART_Popup"))
                                && item.IsLoaded && item.ActualHeight > 0 && IsDescendant(item, popup.Child),
                            "Retemplating a closing popup stranded its pending QAT anchor or original content.");
                        Require(ReferenceEquals(source.ContainerFromItem(item), item)
                                && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(item), source),
                            "Reanchor/template retirement changed the native item owner.");
                        Require(!((IDropDownControl)source).IsDropDownOpen
                                && (ReferenceEquals(target, first) || !first.IsDropDownOpen),
                            "The obsolete source or first-copy opening remained requested.");
                        if (styleBinding is not null)
                        {
                            Require(ReferenceEquals(source.GetBindingExpression(FrameworkElement.StyleProperty)?.ParentBinding, styleBinding),
                                "Reanchor retirement replaced the caller's Style binding.");
                        }
                    }
                    second.IsDropDownOpen = false;
                    await settle();
                    if (menu)
                    {
                        var observation = typeof(Fluent.MenuItem).GetField(
                            "nativeQuickAccessObservation", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw new InvalidOperationException("The native menu observation inspection is unavailable.");
                        Require(observation.GetValue(source) is null,
                            "A retired/reanchored menu kept its presentation observation after closing.");
                    }
                    App.LogAutoTestStartup($"NATIVE PENDING REANCHOR PASS menu={menu} styleBinding={boundStyle}");
                }
                finally
                {
                    first.IsDropDownOpen = false;
                    second.IsDropDownOpen = false;
                    ((IDropDownControl)source).IsDropDownOpen = false;
                    host.Children.Remove(source);
                    host.Children.Remove(first);
                    host.Children.Remove(second);
                    await settle();
                }
            }
        }
    }

    private static async Task VerifyLivePresentation(Panel host, Func<Task> settle)
    {
        var source = new Fluent.DropDownButton { Header = "Native live presentation", MenuHeader = "Original menu header" };
        var first = new NativeButton { Content = "First native item", Width = 90, Height = 32 };
        var second = new NativeButton { Content = "Second native item", Width = 90, Height = 32 };
        var gallery = new RibbonGallery { Width = 200, Height = 60 };
        gallery.Items.Add(new RibbonGalleryItem { Content = "Original gallery item" });
        source.Gallery = gallery;
        source.Items.Add(first);
        source.Items.Add(second);
        var copy = (Fluent.DropDownButton)source.CreateQuickAccessItem()!;
        host.Children.Add(source);
        host.Children.Add(copy);
        try
        {
            await settle();
            copy.IsDropDownOpen = true;
            await settle();
            var popup = ActivePopup(copy);
            var root = popup.Child as ResizeableContentControl
                       ?? throw new InvalidOperationException("The native presentation has no actual resize root.");
            var header = Part(source, "PART_MenuHeader") as TextBlock
                         ?? throw new InvalidOperationException("The native template has no menu header.");
            Require(header.IsLoaded && header.Text == source.MenuHeader && gallery.IsLoaded
                    && IsDescendant(gallery, root),
                "Native MenuHeader/Gallery did not enter the actual source-template popup.");
            source.MenuHeader = "Updated menu header";
            var replacement = new RibbonGallery { Width = 210, Height = 60 };
            replacement.Items.Add(new RibbonGalleryItem { Content = "Replacement gallery item" });
            source.Gallery = replacement;
            await settle();
            Require(header.Text == source.MenuHeader && replacement.IsLoaded && !gallery.IsLoaded
                    && ReferenceEquals(popup.Child, root),
                "A live MenuHeader/Gallery replacement was dropped or replaced the native popup root.");

            source.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load("""
                <ItemsPanelTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                    <StackPanel Orientation="Horizontal" />
                </ItemsPanelTemplate>
                """);
            await settle();
            var a = first.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
            var b = second.TransformToVisual(root).TransformPoint(new Windows.Foundation.Point());
            Require(first.IsLoaded && second.IsLoaded && Math.Abs(a.Y - b.Y) <= 1.1 && b.X > a.X + 80
                    && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(first), source),
                "A live native ItemsPanel change did not change actual layout through the same source generator.");

            copy.ResizeMode = ContextMenuResizeMode.Both;
            copy.DropDownHeight = 180;
            copy.MaxDropDownHeight = 240;
            copy.FlowDirection = FlowDirection.RightToLeft;
            copy.RenderTransform = new TranslateTransform { X = 30, Y = 10 };
            await settle();
            Require(root.ResizeMode == ContextMenuResizeMode.Both && Math.Abs(root.ActualHeight - 180) <= 1.1
                    && root.FlowDirection == FlowDirection.RightToLeft,
                "Live native QAT resize/height/direction options did not reach the actual popup.");
            var bounds = root.TransformToVisual(copy.XamlRoot.Content).TransformBounds(
                new Windows.Foundation.Rect(0, 0, root.ActualWidth, root.ActualHeight));
            Require(bounds.Left >= -1.1 && bounds.Top >= -1.1
                    && bounds.Right <= copy.XamlRoot.Size.Width + 1.1 && bounds.Bottom <= copy.XamlRoot.Size.Height + 1.1,
                "The RTL/transformed native anchor placed actual popup content outside the viewport.");
            copy.IsDropDownOpen = false;
            await settle();
            Require(!replacement.IsLoaded && !first.IsLoaded && !second.IsLoaded,
                "Closing the native presentation retained live gallery or item content.");
            App.LogAutoTestStartup("NATIVE POPUP PRESENTATION PASS");
        }
        finally
        {
            copy.IsDropDownOpen = false;
            source.IsDropDownOpen = false;
            host.Children.Remove(source);
            host.Children.Remove(copy);
            await settle();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<RetiredOwner> VerifyCycle(Panel host, Func<Task> settle, int cycle)
    {
        var kind = cycle % 4;
        var profile = (cycle / 4) % 3;
        var initiallyLoaded = cycle < 12;
        ItemsControl source = kind switch
        {
            0 => new Fluent.DropDownButton { Header = "Native lifecycle dropdown" },
            1 => new RibbonSplitButton { Header = "Native lifecycle core split" },
            2 => new Fluent.SplitButton { Header = "Native lifecycle facade split" },
            _ => new Fluent.MenuItem { Header = "Native lifecycle menu" },
        };
        var menu = source as Fluent.MenuItem;
        source.Name = $"NativePopupSource_{cycle}";
        App.LogAutoTestStartup($"NATIVE POPUP LIFETIME START cycle={cycle} kind={kind} profile={profile} loaded={initiallyLoaded}");
        var command = new LifetimeCommand();
        var editor = new NativeTextBox { Name = $"NativePopupEditor_{cycle}", Text = $"Retained editor {cycle}", Width = 180, MinHeight = 32 };
        var action = new NativeButton { Name = $"NativePopupAction_{cycle}", Content = $"Retained action {cycle}", Command = command, MinHeight = 32 };
        var clicks = 0;
        action.Click += (_, _) => clicks++;
        if (menu is not null)
        {
            menu.Items.Add(editor);
            menu.Items.Add(action);
        }
        else
        {
            source.Items.Add(editor);
            source.Items.Add(action);
        }
        var sourceControl = (Control)source;
        var loads = 0;
        source.Loaded += (_, _) => loads++;
        var bindingValues = new ContentControl();
        Binding? styleBinding = null;
        if (profile == 1)
        {
            sourceControl.Template = Template(menu is not null);
        }
        else if (profile == 2)
        {
            bindingValues.Content = Style(menu is not null);
            styleBinding = new Binding { Source = bindingValues, Path = new PropertyPath("Content"), Mode = BindingMode.OneWay };
            source.SetBinding(FrameworkElement.StyleProperty, styleBinding);
        }

        var copy = (RibbonDropDownButton)((IQuickAccessItemProvider)source).CreateQuickAccessItem()!;
        copy.Name = $"NativePopupCopy_{cycle}";
        var templates = new List<RetiredTemplate>();
        if (initiallyLoaded)
        {
            host.Children.Add(source);
        }
        host.Children.Add(copy);
        try
        {
            await settle();
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(copy) as FrameworkElementAutomationPeer
                       ?? throw new InvalidOperationException("The initial real native QAT peer is missing.");
            var expand = peer.GetPattern(PatternInterface.ExpandCollapse) as IExpandCollapseProvider
                         ?? throw new InvalidOperationException("The initial real QAT peer cannot expand its native popup.");
            expand.Expand();
            await settle();
            AssertContent(source, copy, editor, action, initiallyLoaded, loads, initiallyLoaded ? 1 : 0);
            App.LogAutoTestStartup($"NATIVE POPUP LIFETIME initial-open cycle={cycle}");
            if (!initiallyLoaded)
            {
                Collect();
                await settle();
                AssertContent(source, copy, editor, action, false, loads, 0);
                host.Children.Insert(0, source);
                await settle();
                AssertContent(source, copy, editor, action, true, loads, 1);
                if (profile == 0)
                {
                    Require(sourceControl.ReadLocalValue(Control.TemplateProperty) == DependencyProperty.UnsetValue
                            && sourceControl.GetBindingExpression(Control.TemplateProperty) is null,
                        "Default-template initialization blocked styling after the first real source Loading.");
                }
            }
            if (styleBinding is not null)
            {
                Require(ReferenceEquals(source.GetBindingExpression(FrameworkElement.StyleProperty)?.ParentBinding, styleBinding),
                    "Popup preparation replaced the caller's Style binding.");
            }

            await VerifySamePopupReopen(copy, expand, settle);
            host.Children.Remove(source);
            await settle();
            Collect();
            await settle();
            AssertContent(source, copy, editor, action, false, loads, 1);
            AssertPeer(peer, copy);

            templates.Add(CaptureTemplate(source));
            var firstTemplate = Template(menu is not null);
            if (styleBinding is not null)
            {
                bindingValues.Content = Style(menu is not null, firstTemplate);
            }
            else
            {
                sourceControl.Template = firstTemplate;
            }
            await settle();
            AssertContent(source, copy, editor, action, false, loads, 1);
            Require(ReferenceEquals(sourceControl.Template, firstTemplate), "An open native source ignored caller template intent.");
            AssertRetiredClosed(templates);
            Collect();
            await settle();
            AssertPeer(peer, copy);
            App.LogAutoTestStartup($"NATIVE POPUP LIFETIME open-retemplate cycle={cycle}");

            expand.Collapse();
            await settle();
            Require(!copy.IsDropDownOpen && !editor.IsLoaded && !action.IsLoaded,
                "A native template popup retained its content after explicit closure.");
            templates.Add(CaptureTemplate(source));
            var templateValues = new ContentControl { Content = Template(menu is not null) };
            var templateBinding = new Binding { Source = templateValues, Path = new PropertyPath("Content"), Mode = BindingMode.OneWay };
            source.SetBinding(Control.TemplateProperty, templateBinding);
            await settle();
            Require(!copy.IsDropDownOpen, "Closed source retemplating reopened without a request.");
            AssertRetiredClosed(templates);
            expand.Expand();
            await settle();
            AssertContent(source, copy, editor, action, false, loads, 1);
            Require(ReferenceEquals(source.GetBindingExpression(Control.TemplateProperty)?.ParentBinding, templateBinding),
                "Opening replaced a caller's Template binding.");

            templates.Add(CaptureTemplate(source));
            templateValues.Content = Template(menu is not null);
            await settle();
            AssertContent(source, copy, editor, action, false, loads, 1);
            Require(ReferenceEquals(sourceControl.Template, templateValues.Content)
                    && ReferenceEquals(source.GetBindingExpression(Control.TemplateProperty)?.ParentBinding, templateBinding),
                "A live caller Template binding did not rebind the actual native popup.");
            AssertRetiredClosed(templates);
            host.Children.Insert(0, source);
            await settle();
            AssertContent(source, copy, editor, action, true, loads, 2);
            Require(ReferenceEquals(source.GetBindingExpression(Control.TemplateProperty)?.ParentBinding, templateBinding),
                "Source reloading cleared the caller's Template binding.");
            host.Children.Remove(source);
            await settle();
            Collect();
            await settle();
            AssertContent(source, copy, editor, action, false, loads, 2);
            AssertPeer(peer, copy);
            App.LogAutoTestStartup($"NATIVE POPUP LIFETIME source-reload-gc cycle={cycle}");

            expand.Collapse();
            expand.Expand();
            await settle();
            AssertContent(source, copy, editor, action, false, loads, 2);
            var invoke = FrameworkElementAutomationPeer.CreatePeerForElement(action)
                ?.GetPattern(PatternInterface.Invoke) as IInvokeProvider
                ?? throw new InvalidOperationException("The original rendered native action lost its Invoke provider.");
            invoke.Invoke();
            await settle();
            Require(command.Executions == 1 && clicks == 1 && editor.Text == $"Retained editor {cycle}",
                "Native popup churn replaced editor state or duplicated the original command.");

            host.Children.Remove(copy);
            await settle();
            Require(!copy.IsDropDownOpen && !editor.IsLoaded && !action.IsLoaded,
                "Unloading the active native QAT anchor left source popup content alive.");
            host.Children.Add(copy);
            await settle();
            AssertPeer(peer, copy);
            expand.Expand();
            await settle();
            Collect();
            await settle();
            AssertContent(source, copy, editor, action, false, loads, 2);
            AssertPeer(peer, copy);
            Require(command.Executions == 1 && clicks == 1, "Reloading the same native copy reexecuted its command.");
            expand.Collapse();
            await settle();
            Require(!editor.IsLoaded && !action.IsLoaded, "Closing the final native popup did not unload original content.");
            GC.KeepAlive(peer);
            return new(new(source), new(copy), templates);
        }
        finally
        {
            copy.IsDropDownOpen = false;
            ((IDropDownControl)source).IsDropDownOpen = false;
            host.Children.Remove(source);
            host.Children.Remove(copy);
            await settle();
        }
    }

    private static async Task VerifySamePopupReopen(
        RibbonDropDownButton copy, IExpandCollapseProvider expand, Func<Task> settle)
    {
        var popup = ActivePopup(copy);
        var root = popup.Child;
        expand.Collapse();
        await settle();
        expand.Expand();
        await settle();
        Require(ReferenceEquals(ActivePopup(copy), popup) && ReferenceEquals(popup.Child, root),
            "An ordinary reopen replaced the current source-template Popup/root.");
    }

    internal static Popup ActivePopup(RibbonDropDownButton copy)
    {
        var accessor = typeof(RibbonDropDownButton).GetProperty(
            "OpenNativePopup", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("The actual native popup accessor is missing.");
        return accessor.GetValue(copy) as Popup
               ?? throw new InvalidOperationException("The requested native source-owned Popup is not active.");
    }

    private static void AssertContent(ItemsControl source, RibbonDropDownButton copy, NativeTextBox editor,
        NativeButton action, bool sourceLoaded, int actualLoads, int expectedLoads)
    {
        var popup = ActivePopup(copy);
        var root = popup.Child as FrameworkElement;
        Require(source.IsLoaded == sourceLoaded && actualLoads == expectedLoads
                && (sourceLoaded || VisualTreeHelper.GetParent(source) is null),
            "Native popup preparation changed the original source's load/parent lifecycle.");
        Require(copy.IsLoaded && copy.IsDropDownOpen && popup.IsOpen
                && root is { IsLoaded: true, ActualWidth: >= 180, ActualHeight: > 40 }
                && editor.IsLoaded && action.IsLoaded && editor.ActualWidth > 0 && editor.ActualHeight > 0
                && action.ActualWidth > 0 && action.ActualHeight > 0
                && IsDescendant(editor, root) && IsDescendant(action, root),
            "The native source-template Popup does not render the actual original measured content. "
            + $"source={source.Name}, copyLoaded={copy.IsLoaded}, requested={copy.IsDropDownOpen}, open={popup.IsOpen}, "
            + $"rootLoaded={root?.IsLoaded},rootSize={root?.ActualWidth}x{root?.ActualHeight}, "
            + $"editorLoaded={editor.IsLoaded},editorSize={editor.ActualWidth}x{editor.ActualHeight},actionLoaded={action.IsLoaded}.");
        Require(ReferenceEquals(Part(source, "PART_Popup"), popup)
                && Part(source, "ItemsPresenter") is ItemsPresenter { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 },
            "The native popup/presenter escaped the current SOURCE ControlTemplate.");
        Require(source.Items.Count == 2 && ReferenceEquals(source.Items[0], editor)
                && ReferenceEquals(source.Items[1], action)
                && ReferenceEquals(source.ContainerFromItem(editor), editor)
                && ReferenceEquals(source.ContainerFromItem(action), action)
                && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(editor), source)
                && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(action), source),
            "Native inherited Items/container ownership or original element identity changed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static RetiredTemplate CaptureTemplate(ItemsControl source) => new(
        new(Part(source, "PART_Popup") as Popup ?? throw new InvalidOperationException("The retiring template Popup is missing.")),
        new(Part(source, "ItemsPresenter") as ItemsPresenter ?? throw new InvalidOperationException("The retiring template presenter is missing.")));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertRetiredClosed(IEnumerable<RetiredTemplate> templates)
    {
        foreach (var template in templates)
        {
            if (template.Popup.TryGetTarget(out var popup))
            {
                Require(!popup.IsOpen && popup.Child is not FrameworkElement { IsLoaded: true },
                    "An obsolete source-template Popup retained a live presentation.");
            }
        }
    }

    private static object? Part(Control source, string name) =>
        (typeof(Control).GetMethod("GetTemplateChild", BindingFlags.Instance | BindingFlags.NonPublic)
         ?? throw new InvalidOperationException("Native template-part inspection is unavailable.")).Invoke(source, [name]);

    private static Style Style(bool menu, ControlTemplate? template = null) => new(menu ? typeof(Fluent.MenuItem) : typeof(RibbonDropDownButton))
    {
        Setters = { new Setter(Control.TemplateProperty, template ?? Template(menu)) },
    };

    private static ControlTemplate Template(bool menu)
    {
        var target = menu ? "MenuItem" : "RibbonDropDownButton";
        var content = menu ? string.Empty : """
            <StackPanel x:Name="PART_PopupItemsPanel">
                <TextBlock x:Name="PART_MenuHeader" Visibility="Collapsed" />
                <Rectangle x:Name="PART_HeaderSeparator" Visibility="Collapsed" />
                <ContentControl x:Name="PART_Gallery" Visibility="Collapsed" />
                <Rectangle x:Name="PART_GallerySeparator" Visibility="Collapsed" />
            </StackPanel>
            """;
        return (ControlTemplate)XamlReader.Load($$"""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:fluent="using:Fluent" TargetType="fluent:{{target}}">
                <Grid>
                    <StackPanel Orientation="Horizontal">
                        <Button x:Name="PART_Button" MinWidth="120" MinHeight="32" Content="{TemplateBinding Header}" />
                        <Button x:Name="PART_DropDownButton" MinWidth="32" MinHeight="32" />
                        <Button x:Name="PART_PrimaryButton" MinWidth="32" MinHeight="32" />
                        <Button x:Name="PART_SubmenuButton" MinWidth="32" MinHeight="32" />
                    </StackPanel>
                    <Grid x:Name="PART_ItemsOwner" Width="0" Height="0" Visibility="Collapsed" IsHitTestVisible="False">
                        <ItemsPresenter x:Name="ItemsPresenter" />
                    </Grid>
                    <Popup x:Name="PART_Popup" IsLightDismissEnabled="True" FlowDirection="LeftToRight">
                        <fluent:ResizeableContentControl x:Name="PART_PopupContentControl" Padding="2">
                            <ScrollViewer x:Name="PART_ScrollViewer" HorizontalScrollBarVisibility="Disabled"
                                          VerticalScrollBarVisibility="Auto" IsTabStop="False">
                                {{content}}
                            </ScrollViewer>
                        </fluent:ResizeableContentControl>
                    </Popup>
                </Grid>
            </ControlTemplate>
            """);
    }

    private static bool IsDescendant(DependencyObject item, DependencyObject root)
    {
        for (DependencyObject? current = item; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, root))
            {
                return true;
            }
        }
        return false;
    }

    private static void AssertPeer(FrameworkElementAutomationPeer peer, RibbonDropDownButton copy) =>
        Require(ReferenceEquals(peer.Owner, copy) && ReferenceEquals(peer, FrameworkElementAutomationPeer.FromElement(copy)),
            "The original retained real QAT peer was lost or replaced.");

    private static void Collect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed record RetiredTemplate(WeakReference<Popup> Popup, WeakReference<ItemsPresenter> Presenter);
    private sealed record RetiredOwner(WeakReference<ItemsControl> Source, WeakReference<RibbonDropDownButton> Copy,
        List<RetiredTemplate> Templates);
    private sealed class LifetimeCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public int Executions { get; private set; }
        public void Execute(object? parameter) => Executions++;
    }
}
#endif
