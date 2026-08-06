namespace Fluent;

using System.Runtime.CompilerServices;

internal static class RibbonLocalizationUpdateHelper
{
    private static readonly ConditionalWeakTable<FrameworkElement, Subscription> Subscriptions = new();

    internal static void Track(FrameworkElement owner, Action update)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(update);

        if (Subscriptions.TryGetValue(owner, out _))
        {
            return;
        }

        Subscriptions.Add(owner, new Subscription(owner, update));
    }

    private sealed class Subscription
    {
        private readonly FrameworkElement owner;
        private readonly Action update;
        private bool isSubscribed;

        internal Subscription(FrameworkElement owner, Action update)
        {
            this.owner = owner;
            this.update = update;
            owner.Loaded += OnLoaded;
            owner.Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            if (!isSubscribed)
            {
                RibbonLocalization.Current.PropertyChanged += OnLocalizationChanged;
                isSubscribed = true;
            }

            update();
        }

        private void OnUnloaded(object sender, RoutedEventArgs args)
        {
            if (isSubscribed)
            {
                RibbonLocalization.Current.PropertyChanged -= OnLocalizationChanged;
                isSubscribed = false;
            }
        }

        private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (IsLocalizationChange(args.PropertyName))
            {
                update();
            }
        }
    }

    internal static bool IsLocalizationChange(string? propertyName)
        => propertyName is nameof(RibbonLocalization.Localization)
            or nameof(RibbonLocalization.Culture)
            or null
            or "";
}
