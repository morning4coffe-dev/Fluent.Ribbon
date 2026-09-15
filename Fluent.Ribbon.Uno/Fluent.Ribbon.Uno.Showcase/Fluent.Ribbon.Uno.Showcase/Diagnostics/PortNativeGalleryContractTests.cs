#if WINDOWS
namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Fluent;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

internal static class PortNativeGalleryContractTests
{
    internal static async Task VerifyAsync(Panel host, Func<Task> settle)
    {
        App.LogAutoTestStartup("NATIVE GALLERY LIFETIME BEGIN");
        var retired = await ExerciseLifetimes(host, settle);
        // Keep the UI dispatcher available to native reference-tracker cleanup.
        await Task.Run(Collect);
        await settle();
        await DrainNativeReleaseQueue(host.DispatcherQueue);
        await Task.Run(Collect);
        Require(retired.All(owner => !owner.Owner.TryGetTarget(out _) && !owner.Peer.TryGetTarget(out _)
                                    && !owner.Source.TryGetTarget(out _)),
            "Retired native gallery owners, peers, or sources remained rooted. "
            + string.Join("; ", retired.Select((owner, index) =>
                $"{index}:owner={owner.Owner.TryGetTarget(out _)},peer={owner.Peer.TryGetTarget(out _)},source={owner.Source.TryGetTarget(out _)}")));
        Require(retired.SelectMany(owner => owner.Containers).All(container => !container.TryGetTarget(out _)),
            "An obsolete native gallery container remained rooted.");
        Require(retired.SelectMany(owner => owner.Presentation).All(element => !element.TryGetTarget(out _)),
            "An obsolete native gallery presenter, panel, or group heading remained rooted.");
        App.LogAutoTestStartup("NATIVE GALLERY LIFETIME COMPLETE cycles=24 owners=24 authored-and-models=24");
    }

    private static async Task DrainNativeReleaseQueue(DispatcherQueue dispatcher)
    {
        // Native reference-tracker releases run on the UI queue after managed
        // finalization. Check weak handles only after that queued work can finish.
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () => completion.SetResult()))
        {
            throw new InvalidOperationException("The native release-queue checkpoint could not be dispatched.");
        }
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<List<RetiredGallery>> ExerciseLifetimes(Panel host, Func<Task> settle)
    {
        var retired = new List<RetiredGallery>();
        for (var cycle = 0; cycle < 24; cycle++)
        {
            retired.Add(await VerifyCycle(host, settle, cycle));
            Collect();
            await settle();
            App.LogAutoTestStartup($"NATIVE GALLERY LIFETIME PASS cycle={cycle}");
        }
        return retired;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<RetiredGallery> VerifyCycle(Panel host, Func<Task> settle, int cycle)
    {
        RibbonGallery gallery = cycle % 2 == 0 ? new Gallery() : new RibbonGallery();
        gallery.Name = $"NativeGalleryLifetime_{cycle}";
        gallery.Width = 360;
        gallery.Height = 240;
        gallery.ItemWidth = 72;
        gallery.ItemHeight = 40;
        gallery.MinItemsInRow = 2;
        gallery.MaxItemsInRow = 3;
        gallery.ItemTemplate = Template();
        var command = new LifetimeCommand();
        var authored = new GalleryItem
        {
            Content = new TextBlock { Text = $"Retained authored content {cycle}" },
            Group = "Alpha",
            KeyTip = "GA",
            Command = command,
        };
        var editor = new Microsoft.UI.Xaml.Controls.TextBox
        {
            Text = $"Retained editor state {cycle}",
            MinWidth = 0,
            MinHeight = 0,
        };
        var repeated = new LifetimeModel("Repeated source occurrence", "Alpha");
        var source = new ObservableCollection<object>
        {
            authored, repeated, new LifetimeModel("Beta model", "Beta"), repeated, editor,
        };
        gallery.ItemsSource = source;
        var containers = new List<WeakReference<UIElement>>();
        var presentation = new List<WeakReference<DependencyObject>>();
        host.Children.Add(gallery);
        try
        {
            await settle();
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(gallery) as FrameworkElementAutomationPeer
                       ?? throw new InvalidOperationException("The initial real native gallery peer is missing.");
            var selection = peer.GetPattern(PatternInterface.Selection) as ISelectionProvider
                            ?? throw new InvalidOperationException("The real gallery peer has no Selection provider.");
            AssertOwner(gallery);
            AssertGrid(gallery, 3);
            Require(gallery.Items[1] is RibbonGalleryItem && gallery.Items[3] is RibbonGalleryItem
                    && !ReferenceEquals(gallery.Items[1], gallery.Items[3])
                    && (gallery is not Gallery || gallery.Items[1] is GalleryItem),
                "The native gallery replaced the core/facade factory or collapsed repeated model occurrences.");
            containers.AddRange(gallery.Items.Select(item => new WeakReference<UIElement>(item)));
            await VerifyItemsPanel(gallery, presentation, settle);
            var panel = (Panel)VisualTreeHelper.GetParent(authored);

            var actionPeer = FrameworkElementAutomationPeer.CreatePeerForElement(authored)!;
            var invoke = actionPeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider
                         ?? throw new InvalidOperationException("The rendered authored GalleryItem has no Invoke provider.");
            invoke.Invoke();
            authored.OnKeyTipPressed();
            Require(command.Executions == 2 && ReferenceEquals(gallery.SelectedItem, authored),
                "The native presentation lost authored command, KeyTip, or selection identity.");
            command.Enabled = false;
            authored.OnKeyTipPressed();
            var availability = typeof(RibbonGalleryItem).GetField(
                "commandAvailability", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(authored);
            var suspended = availability?.GetType().GetField(
                "suspended", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.GetValue(availability);
            Require(command.Executions == 2 && !authored.IsEnabled,
                $"Native gallery command gating regressed. cycle={cycle}, executions={command.Executions}, "
                + $"enabled={authored.IsEnabled}, loaded={authored.IsLoaded}, suspended={suspended}.");
            command.Enabled = true;

            gallery.SelectedIndex = 3;
            UIElement? selected = gallery.Items[3];
            Require(ReferenceEquals(gallery.SelectedItem, repeated) && selection.GetSelection().Length == 1,
                "The real retained gallery peer omitted a selected source model.");
            gallery.GroupBy = nameof(LifetimeModel.Group);
            gallery.IsGrouped = true;
            await settle();
            AssertOwner(gallery);
            AssertGroups(gallery, "Alpha", "Beta");
            Require(ReferenceEquals(VisualTreeHelper.GetParent(authored), panel)
                    && ReferenceEquals(VisualTreeHelper.GetParent(selected), panel),
                "Grouping reparented original native item containers.");
            AssertSameRow(gallery, 0, 1);
            AssertSameRow(gallery, 1, 3);
            Require(Bounds(gallery.Items[2], panel).Y > Bounds(selected, panel).Bottom,
                "Grouped model positions overlap the preceding group.");
            gallery.SelectedFilter = new GalleryGroupFilter { Title = "Only beta", Groups = "Beta" };
            await settle();
            AssertGroups(gallery, "Beta");
            Require(authored.Visibility == Visibility.Collapsed && selected.Visibility == Visibility.Collapsed
                    && editor.Visibility == Visibility.Visible && gallery.Items[2].Visibility == Visibility.Visible
                    && selection.GetSelection().Length == 0 && peer.GetChildren().Count == 2,
                "Native filtering lost visible groups, ungrouped content, or UIA visibility.");
            gallery.SelectedFilter = null;
            gallery.IsGrouped = false;
            gallery.GroupBy = null;
            gallery.Orientation = Orientation.Vertical;
            await settle();
            AssertGrid(gallery, 1);
            gallery.Orientation = Orientation.Horizontal;
            gallery.MinItemsInRow = 2;
            gallery.MaxItemsInRow = 2;
            gallery.ItemWidth = 80;
            gallery.ItemHeight = 44;
            await settle();
            AssertGrid(gallery, 2);

            var changes = new List<SelectionChangedEventArgs>();
            gallery.SelectionChanged += RecordSelection;
            void RecordSelection(object sender, SelectionChangedEventArgs args) => changes.Add(args);
            source.Move(3, 0);
            source.Insert(1, repeated);
            await settle();
            Require(ReferenceEquals(gallery.Items[0], selected) && gallery.SelectedIndex == 0
                    && changes.Count == 0,
                "Native source reorder/insertion stole a repeated selected occurrence.");
            containers.Add(new WeakReference<UIElement>(gallery.Items[1]));
            containers.Add(new WeakReference<UIElement>(gallery.Items[4]));
            source[4] = new LifetimeModel("Replacement model", "Gamma");
            source.RemoveAt(3);
            source.RemoveAt(1);
            await settle();
            AssertOwner(gallery);
            Require(ReferenceEquals(gallery.Items[0], selected) && gallery.SelectedIndex == 0 && changes.Count == 0,
                "Unselected native replacements/removals changed the selected occurrence.");
            source.RemoveAt(0);
            await settle();
            Require(gallery.SelectedItem is null && gallery.SelectedIndex == -1
                    && ((RibbonGalleryItem)selected).IsSelected == false
                    && gallery.ItemFromContainer(selected) is null
                    && changes.Count == 1 && ReferenceEquals(changes[0].RemovedItems[0], repeated)
                    && changes[0].AddedItems.Count == 0,
                "Removing the final selected occurrence left stale containers or selection events.");
            selected = null;
            changes.Clear();
            gallery.SelectionChanged -= RecordSelection;
            Collect();
            await settle();
            AssertPeer(peer, gallery);
            AssertOwner(gallery);

            gallery.GroupBy = nameof(LifetimeModel.Group);
            authored.Group = "Changed authored group";
            await settle();
            AssertGroups(gallery, "Changed authored group", "Gamma");
            CapturePresentation(gallery, presentation);
            gallery.Template = GalleryTemplate();
            await settle();
            AssertOwner(gallery);
            AssertGroups(gallery, "Changed authored group", "Gamma");
            CapturePresentation(gallery, presentation);
            gallery.ClearValue(Control.TemplateProperty);
            await settle();
            AssertOwner(gallery);
            Require(editor.Text == $"Retained editor state {cycle}", "Retemplating replaced live authored editor state.");

            gallery.SelectedItem = authored;
            host.Children.Remove(gallery);
            await settle();
            Collect();
            await settle();
            AssertPeer(peer, gallery);
            source.Insert(0, new LifetimeModel("Unloaded insertion", "Delta"));
            source[2] = new LifetimeModel("Unloaded replacement", "Gamma");
            host.Children.Add(gallery);
            await settle();
            AssertOwner(gallery);
            AssertPeer(peer, gallery);
            Require(ReferenceEquals(gallery.SelectedItem, authored) && gallery.SelectedIndex == 1
                    && ReferenceEquals(gallery.ContainerFromItem(authored), authored),
                "Reload replaced authored identity or lost selection after unloaded source edits.");

            containers.AddRange(gallery.Items.Select(item => new WeakReference<UIElement>(item)));
            gallery.ItemsSource = null;
            await settle();
            Require(gallery.Items.Count == 0 && ((ItemsControl)gallery).Items.Count == 0
                    && gallery.SelectedItem is null, "Clearing native ItemsSource left a divergent view.");
            gallery.Items.Add(authored);
            gallery.Items.Add(editor);
            var nativeItems = ((ItemsControl)gallery).Items;
            nativeItems.Add(new LifetimeModel("Inherited native Items", "Gamma"));
            await settle();
            AssertOwner(gallery);
            containers.Add(new WeakReference<UIElement>(gallery.Items[2]));
            nativeItems[2] = new LifetimeModel("Inherited native replacement", "Gamma");
            gallery.Items.Move(0, 1);
            gallery.Items.Remove(editor);
            gallery.SelectedIndex = 0;
            await settle();
            AssertOwner(gallery);
            AssertPeer(peer, gallery);
            Require(ReferenceEquals(gallery.SelectedItem, authored) && command.Executions == 2,
                "Inherited/native Items edits changed authored selection or reexecuted a command.");

            CapturePresentation(gallery, presentation);
            containers.AddRange(gallery.Items.Select(item => new WeakReference<UIElement>(item)));
            return new RetiredGallery(new(gallery), new(peer), new(source), containers, presentation);
        }
        finally
        {
            // Retire a populated source, so native CItemsControl destruction clears real owned children.
            host.Children.Remove(gallery);
            await settle();
        }
    }

    private static void AssertOwner(RibbonGallery gallery)
    {
        var native = (ItemsControl)gallery;
        Require(gallery.IsLoaded && gallery.ActualWidth > 0 && gallery.Items.Count == native.Items.Count,
            "The native gallery is not actually rendered or its native item view diverged.");
        Panel? panel = null;
        for (var index = 0; index < gallery.Items.Count; index++)
        {
            var item = gallery.Items[index];
            var container = native.ContainerFromIndex(index) as UIElement;
            Require(container is ListBoxItem && (item is ListBoxItem
                        ? ReferenceEquals(container, item)
                        : ReferenceEquals(((ListBoxItem)container).Content, item))
                    && native.IndexFromContainer(container) == index
                    && ReferenceEquals(native.ItemFromContainer(container), gallery.ItemFromContainer(item))
                    && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(container), gallery)
                    && ReferenceEquals(gallery.ContainerFromIndex(index), item),
                $"The original native ListBox is not the sole generator/owner of gallery occurrence {index}.");
            var parent = VisualTreeHelper.GetParent(container!) as Panel;
            panel ??= parent;
            Require(parent is UniformItemsPanel && ReferenceEquals(parent, panel)
                    && parent.Children.Contains(container) && container is FrameworkElement { IsLoaded: true },
                "A native gallery item escaped the source-owned items panel.");
            Require(item is FrameworkElement { IsLoaded: true } visual
                    && (item.Visibility == Visibility.Collapsed || visual.ActualWidth > 0 && visual.ActualHeight > 0),
                "An original authored/model gallery visual was replaced by an empty native shell.");
            if (gallery.ItemFromContainer(item) is LifetimeModel model)
            {
                Require(Descendants<TextBlock>(item).Any(text =>
                        Equals(text.Tag, "NativeGalleryLifetimeModel") && text.Text == model.Title && text.IsLoaded),
                    "The native model container lost its actual data template/content.");
            }
        }
        if (panel is not null)
        {
            Require(panel.Children.Count == gallery.Items.Count,
                "A group heading or secondary host was inserted into the native generator's children.");
        }
    }

    private static async Task VerifyItemsPanel(
        RibbonGallery gallery, List<WeakReference<DependencyObject>> retired, Func<Task> settle)
    {
        CapturePresentation(gallery, retired);
        var originalContainers = gallery.Items.ToArray();
        gallery.ItemsPanel = (ItemsPanelTemplate)XamlReader.Load("""
            <ItemsPanelTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                <StackPanel Orientation="Horizontal" />
            </ItemsPanelTemplate>
            """);
        await settle();
        var native = (ItemsControl)gallery;
        var panel = VisualTreeHelper.GetParent(native.ContainerFromIndex(0)) as StackPanel;
        Require(panel is { Orientation: Orientation.Horizontal } && panel.IsLoaded
                && gallery.Items.SequenceEqual(originalContainers),
            "Changing the native ItemsPanel recreated canonical gallery containers or ignored the caller's layout.");
        retired.Add(new WeakReference<DependencyObject>(panel!));
        for (var index = 0; index < gallery.Items.Count; index++)
        {
            var container = (UIElement)native.ContainerFromIndex(index);
            Require(ReferenceEquals(VisualTreeHelper.GetParent(container), panel)
                    && ReferenceEquals(ItemsControl.ItemsControlFromItemContainer(container), gallery),
                "A caller-supplied ItemsPanel transferred gallery items to a different native owner.");
            if (index > 0)
            {
                var previous = Bounds((UIElement)native.ContainerFromIndex(index - 1), panel!);
                var current = Bounds(container, panel!);
                Require(Math.Abs(previous.Y - current.Y) <= 1.1 && current.X >= previous.Right - 1.1,
                    "The native caller-supplied horizontal panel did not arrange the actual containers.");
            }
        }
        gallery.ClearValue(ItemsControl.ItemsPanelProperty);
        await settle();
        AssertOwner(gallery);
        AssertGrid(gallery, 3);
    }

    private static void AssertGrid(RibbonGallery gallery, int columns)
    {
        var panel = (Panel)VisualTreeHelper.GetParent(gallery.Items[0]);
        for (var index = 0; index < gallery.Items.Count; index++)
        {
            var bounds = Bounds((UIElement)((ItemsControl)gallery).ContainerFromIndex(index)!, panel);
            Require(Math.Abs(bounds.X - index % columns * gallery.ItemWidth) <= 1.1
                    && Math.Abs(bounds.Y - index / columns * gallery.ItemHeight) <= 1.1
                    && Math.Abs(bounds.Width - gallery.ItemWidth) <= 1.1
                    && Math.Abs(bounds.Height - gallery.ItemHeight) <= 1.1,
                $"Actual native gallery cell {index} does not follow orientation/row/dimension constraints: {bounds}.");
        }
    }

    private static void AssertSameRow(RibbonGallery gallery, int first, int second)
    {
        var panel = (Panel)VisualTreeHelper.GetParent(gallery.Items[first]);
        var a = Bounds(gallery.Items[first], panel);
        var b = Bounds(gallery.Items[second], panel);
        Require(Math.Abs(a.Y - b.Y) <= 1.1 && Math.Abs(b.X - a.Right) <= 1.1,
            "Items in the same native gallery group were not arranged in adjacent source-owned cells.");
    }

    private static void AssertGroups(RibbonGallery gallery, params string[] expected)
    {
        var headers = Descendants<TextBlock>(gallery)
            .Where(header => AutomationProperties.GetHeadingLevel(header) == AutomationHeadingLevel.Level3
                             && header.Visibility == Visibility.Visible).ToArray();
        Require(headers.Select(header => header.Text).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(expected.OrderBy(name => name, StringComparer.Ordinal)),
            $"Native gallery group headings differ: {string.Join("|", headers.Select(header => header.Text))}.");
        foreach (var header in headers)
        {
            Require(header.IsLoaded && header.ActualWidth > 0 && header.ActualHeight > 0
                    && AutomationProperties.GetName(header) == header.Text,
                "A native group heading is not a real rendered, named UIA heading.");
            var first = gallery.Items.First(item =>
                item is RibbonGalleryItem { Group.Length: > 0 } authored
                    ? authored.Group == header.Text
                    : (item as FrameworkElement)?.DataContext is LifetimeModel model && model.Group == header.Text);
            Require(Bounds(header, gallery).Bottom <= Bounds(first, gallery).Y + 1.1,
                "A native group heading overlaps the group's actual items.");
        }
    }

    private static void CapturePresentation(RibbonGallery gallery, List<WeakReference<DependencyObject>> retired)
    {
        retired.AddRange(Descendants<DependencyObject>(gallery).Where(element =>
                element is ItemsPresenter or UniformItemsPanel
                || element is TextBlock header
                && AutomationProperties.GetHeadingLevel(header) == AutomationHeadingLevel.Level3)
            .Select(element => new WeakReference<DependencyObject>(element)));
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match)
        {
            yield return match;
        }
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            foreach (var item in Descendants<T>(VisualTreeHelper.GetChild(root, index)))
            {
                yield return item;
            }
        }
    }

    private static Rect Bounds(UIElement element, UIElement relativeTo)
    {
        var framework = (FrameworkElement)element;
        return element.TransformToVisual(relativeTo).TransformBounds(
            new Rect(0, 0, framework.ActualWidth, framework.ActualHeight));
    }

    private static void AssertPeer(FrameworkElementAutomationPeer peer, RibbonGallery gallery)
        => Require(ReferenceEquals(peer.Owner, gallery)
                   && ReferenceEquals(peer, FrameworkElementAutomationPeer.FromElement(gallery)),
            "The original retained real gallery peer was lost or replaced.");

    private static DataTemplate Template() => (DataTemplate)XamlReader.Load("""
        <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
            <TextBlock Text="{Binding Title}" Tag="NativeGalleryLifetimeModel" />
        </DataTemplate>
        """);

    private static ControlTemplate GalleryTemplate() => (ControlTemplate)XamlReader.Load("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                         xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                         xmlns:fluent="using:Fluent" TargetType="fluent:RibbonGallery">
            <ScrollViewer x:Name="PART_ScrollViewer" HorizontalScrollBarVisibility="Disabled"
                          VerticalScrollBarVisibility="Auto">
                <Grid>
                    <ItemsPresenter x:Name="ItemsPresenter" />
                    <Canvas x:Name="PART_GroupHeaders" IsHitTestVisible="False" />
                </Grid>
            </ScrollViewer>
        </ControlTemplate>
        """);

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

    private sealed record RetiredGallery(
        WeakReference<RibbonGallery> Owner,
        WeakReference<FrameworkElementAutomationPeer> Peer,
        WeakReference<ObservableCollection<object>> Source,
        List<WeakReference<UIElement>> Containers,
        List<WeakReference<DependencyObject>> Presentation);

    private sealed partial class LifetimeModel(string title, string group)
    {
        public string Title { get; } = title;
        public string Group { get; } = group;
    }

    private sealed class LifetimeCommand : ICommand
    {
        private bool enabled = true;
        public event EventHandler? CanExecuteChanged;
        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public int Executions { get; private set; }
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) => Executions++;
    }
}
#endif
