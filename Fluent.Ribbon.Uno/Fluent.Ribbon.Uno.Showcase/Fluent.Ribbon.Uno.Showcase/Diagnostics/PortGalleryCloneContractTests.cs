namespace FluentRibbon.Uno.Showcase.Diagnostics;

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Fluent;
using Fluent.Automation.Peers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

internal static class PortGalleryCloneContractTests
{
    internal static async Task VerifyAsync(Panel host, Func<Task> settle)
    {
        var first = new Model("First");
        var second = new Model("Second");
        var models = new Models { first, second };
        var source = new InRibbonGallery
        {
            Header = "Canonical gallery",
            ItemsSource = models,
            SelectedItem = second,
            ItemWidth = 80,
            ItemHeight = 30,
        };
        var copy = source.CreateQuickAccessItem() as InRibbonGallery
                   ?? throw new InvalidOperationException("The public gallery factory returned a different control contract.");
        Check(ReferenceEquals(copy.Items, source.Items)
              && ReferenceEquals(copy.GetValue(InRibbonGallery.ItemsProperty), source.Items),
            "A fresh gallery copy did not expose the canonical logical item collection.");
        Check(ReferenceEquals(copy.SelectedItem, second) && copy.SelectedIndex == 1,
            "A fresh closed gallery copy lost source-model selection.");
        copy.SelectedIndex = 0;
        Check(ReferenceEquals(copy.SelectedItem, first) && ReferenceEquals(source.SelectedItem, first),
            "Closed-copy index selection did not update the canonical source model.");
        models.Move(0, 1);
        Check(copy.SelectedIndex == 1 && ReferenceEquals(copy.SelectedItem, first),
            "A never-opened copy did not follow a source move.");
        var third = new Model("Third");
        models.Add(third);
        source.SelectedItem = third;
        Check(copy.Items.Count == 3 && ReferenceEquals(copy.SelectedItem, third) && copy.SelectedIndex == 2,
            "A never-opened copy did not observe source data/selection changes.");
        var replacement = new Models { second, third };
        source.ItemsSource = replacement;
        Check(models.Subscribers == 0 && copy.Items.Count == 2
              && ReferenceEquals(copy.SelectedItem, third) && copy.SelectedIndex == 1,
            "Replacing an unloaded source's ItemsSource lost identity or retained its old collection listener.");

        var retainedContainer = source.Items[1];
        var secondCopy = (InRibbonGallery)source.CreateQuickAccessItem();
        Check(ReferenceEquals(secondCopy.Items, copy.Items) && ReferenceEquals(source.Items[1], retainedContainer),
            "Creating another closed copy rebuilt canonical item controls.");
        secondCopy.SelectedItem = second;
        Check(ReferenceEquals(source.SelectedItem, second) && ReferenceEquals(copy.SelectedItem, second),
            "Selection from a second closed copy did not update the first copy and source.");
        host.Children.Add(secondCopy);
        await settle();
        host.Children.Remove(secondCopy);
        await settle();
        source.SelectedItem = third;
        Check(ReferenceEquals(copy.SelectedItem, third) && replacement.Subscribers == 1,
            "Deactivating one copy stopped the remaining copy's canonical data observation.");

        host.Children.Add(source);
        await settle();
        host.Children.Remove(source);
        await settle();
        host.Children.Add(copy);
        try
        {
            await settle();
            Check(!source.IsLoaded && copy.IsLoaded, "The source-unloaded gallery fixture is not exercising the intended lifecycle.");
            var retained = source.Items[1];
            for (var iteration = 0; iteration < 2; iteration++)
            {
                copy.IsDropDownOpen = true;
                await settle();
                Check(copy.DropDownPopup?.IsOpen == true && retained is FrameworkElement { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 },
                    "The same gallery copy did not mount its actual canonical item controls.");
                Check(IsDescendant(retained!, copy.DropDownPopup!.Child),
                    "The popup displayed replacement text instead of the canonical native item.");
                new GalleryItemWrapperAutomationPeer((RibbonGalleryItem)source.Items[0]).Select();
                Check(ReferenceEquals(copy.SelectedItem, second) && ReferenceEquals(source.SelectedItem, second),
                    "Actual item automation lost the original model selection identity.");
                copy.IsDropDownOpen = false;
                await settle();
                Check(ReferenceEquals(copy.SelectedItem, second) && ReferenceEquals(copy.Items, source.Items),
                    "Closing the popup cleared the copy's public model/items contract.");
                copy.SelectedItem = third;
                Check(ReferenceEquals(source.SelectedItem, third) && ReferenceEquals(source.Items[1], retained),
                    "Closed-copy selection or same-copy reopening replaced a canonical item container.");
            }

            replacement.Remove(third);
            Check(copy.SelectedItem is null && copy.SelectedIndex == -1
                  && source.SelectedItem is null && source.SelectedIndex == -1,
                "Removing the selected model left stale source/copy selection.");
        }
        finally
        {
            copy.IsDropDownOpen = false;
            host.Children.Remove(copy);
            await settle();
        }

        Check(replacement.Subscribers == 0, "An unloaded gallery copy retained its collection observation.");
        var fourth = new Model("Fourth");
        replacement.Add(fourth);
        host.Children.Add(copy);
        try
        {
            await settle();
            Check(copy.Items.Count == 2 && ReferenceEquals(copy.Items, source.Items),
                "Reloading the same copy did not reconcile source changes made while observation was suspended.");
            copy.SelectedIndex = 1;
            Check(ReferenceEquals(copy.SelectedItem, fourth) && ReferenceEquals(source.SelectedItem, fourth),
                "Reloading the same copy lost its source-model selection mapping.");
        }
        finally
        {
            host.Children.Remove(copy);
            await settle();
        }
        Check(replacement.Subscribers == 0, "Gallery-copy reload left active collection listeners after final removal.");
    }

    private static bool IsDescendant(DependencyObject item, DependencyObject? root)
    {
        for (DependencyObject? current = item; current is not null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, root)) return true;
        }
        return false;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed record Model(string Title)
    {
        public override string ToString() => Title;
    }

    private sealed class Models : ObservableCollection<Model>, INotifyCollectionChanged
    {
        internal int Subscribers { get; private set; }
        event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
        {
            add { CollectionChanged += value; Subscribers++; }
            remove { CollectionChanged -= value; Subscribers--; }
        }
    }
}
