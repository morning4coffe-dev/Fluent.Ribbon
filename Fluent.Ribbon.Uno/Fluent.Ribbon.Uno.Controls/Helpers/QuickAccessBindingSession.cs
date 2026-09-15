namespace Fluent;

using System.Runtime.CompilerServices;

internal sealed class QuickAccessBindingSession
{
    private static readonly ConditionalWeakTable<FrameworkElement, QuickAccessBindingSession> Sessions = new();
    private readonly WeakReference<FrameworkElement> source;
    private readonly WeakReference<FrameworkElement> target;
    private readonly List<Link> links = [];
    private bool active = true;
    private bool updating;
    private bool wasInQuickAccess;

    private QuickAccessBindingSession(FrameworkElement source, FrameworkElement target)
    {
        this.source = new(source);
        this.target = new(target);
        target.Loaded += OnLoaded;
        target.Unloaded += OnUnloaded;
        target.RegisterPropertyChangedCallback(
            RibbonProperties.IsElementInQuickAccessToolBarProperty, OnMembershipChanged);
    }

    internal static QuickAccessBindingSession For(FrameworkElement source, FrameworkElement target)
    {
        if (Sessions.TryGetValue(target, out var existing))
        {
            if (!existing.source.TryGetTarget(out var owner) || !ReferenceEquals(owner, source))
            {
                throw new InvalidOperationException("A quick access copy cannot bind to two different providers.");
            }

            return existing;
        }

        var session = new QuickAccessBindingSession(source, target);
        Sessions.Add(target, session);
        return session;
    }

    internal event Action? Activated;
    internal event Action? Deactivated;

    internal bool IsActive => active;
    internal int SubscriptionCount => links.Sum(link => (link.SourceToken is null ? 0 : 1) + (link.TargetToken is null ? 0 : 1));

    internal void Bind(
        DependencyProperty sourceProperty,
        DependencyProperty targetProperty,
        bool twoWay = false,
        Func<object?, object?>? convert = null,
        Func<FrameworkElement, object?, object?>? convertBack = null)
    {
        if (links.Any(link => link.SourceProperty == sourceProperty && link.TargetProperty == targetProperty))
        {
            return;
        }

        var link = new Link(sourceProperty, targetProperty, twoWay, convert, convertBack);
        links.Add(link);
        if (active)
        {
            Connect(link);
        }
    }

    internal void Bind(DependencyProperty property, bool twoWay = false) => Bind(property, property, twoWay);

    internal void BindPresentation(DependencyProperty sourceProperty, DependencyProperty targetProperty) =>
        Bind(sourceProperty, targetProperty, convert: QuickAccessHelper.ClonePresentationValue);

    internal void BindCommon(bool enabled = true)
    {
        Bind(FrameworkElement.DataContextProperty);
        Bind(UIElement.OpacityProperty);
        Bind(FrameworkElement.FlowDirectionProperty);
        Bind(RibbonProperties.CustomIconSizeProperty);
        Bind(RibbonProperties.QATIconSizeProperty, RibbonProperties.IconSizeProperty);
        if (enabled)
        {
            Bind(Control.IsEnabledProperty);
        }
    }

    private void Connect(Link link)
    {
        if (!source.TryGetTarget(out var owner) || !target.TryGetTarget(out var copy))
        {
            return;
        }

        Copy(link, reverse: false);
        var weakSession = new WeakReference<QuickAccessBindingSession>(this);
        long sourceToken = 0;
        sourceToken = owner.RegisterPropertyChangedCallback(link.SourceProperty, (sender, property) =>
        {
            if (weakSession.TryGetTarget(out var session))
            {
                session.Copy(link, reverse: false);
            }
            else
            {
                sender.UnregisterPropertyChangedCallback(property, sourceToken);
            }
        });
        link.SourceToken = sourceToken;
        if (link.TwoWay)
        {
            link.TargetToken = copy.RegisterPropertyChangedCallback(link.TargetProperty, (_, _) =>
            {
                if (weakSession.TryGetTarget(out var session))
                {
                    session.Copy(link, reverse: true);
                }
            });
        }
    }

    private void Copy(Link link, bool reverse)
    {
        if (updating || !active || !source.TryGetTarget(out var owner) || !target.TryGetTarget(out var copy))
        {
            return;
        }

        updating = true;
        try
        {
            if (reverse)
            {
                var value = copy.GetValue(link.TargetProperty);
                if (link.ConvertBack is not null)
                {
                    value = link.ConvertBack(owner, value);
                }

                SetIfChanged(owner, link.SourceProperty, value);
                SetIfChanged(copy, link.TargetProperty, owner.GetValue(link.SourceProperty));
            }
            else
            {
                var value = owner.GetValue(link.SourceProperty);
                if (link.Convert is not null)
                {
                    DeactivatePresentation(copy.GetValue(link.TargetProperty));
                }
                SetIfChanged(copy, link.TargetProperty, link.Convert is null ? value : link.Convert(value));
            }
        }
        finally
        {
            updating = false;
        }
    }

    private static void SetIfChanged(DependencyObject element, DependencyProperty property, object? value)
    {
        if (!Equals(element.GetValue(property), value))
        {
            element.SetValue(property, value);
        }
    }

    private void Activate()
    {
        if (active)
        {
            return;
        }

        active = true;
        foreach (var link in links)
        {
            Connect(link);
        }

        Activated?.Invoke();
    }

    private void Deactivate()
    {
        if (!active)
        {
            return;
        }

        active = false;
        source.TryGetTarget(out var owner);
        target.TryGetTarget(out var copy);
        foreach (var link in links)
        {
            if (link.Convert is not null && copy is not null)
            {
                DeactivatePresentation(copy.GetValue(link.TargetProperty));
            }

            if (link.SourceToken is { } sourceToken)
            {
                owner?.UnregisterPropertyChangedCallback(link.SourceProperty, sourceToken);
                link.SourceToken = null;
            }

            if (link.TargetToken is { } targetToken)
            {
                copy?.UnregisterPropertyChangedCallback(link.TargetProperty, targetToken);
                link.TargetToken = null;
            }
        }

        Deactivated?.Invoke();
    }

    private static void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (sender is FrameworkElement element && Sessions.TryGetValue(element, out var session))
        {
            session.Activate();
        }
    }

    private static void OnUnloaded(object sender, RoutedEventArgs args)
    {
        if (sender is FrameworkElement { IsLoaded: false } element && Sessions.TryGetValue(element, out var session))
        {
            session.Deactivate();
        }
    }

    private static void DeactivatePresentation(object? value)
    {
        if (value is FrameworkElement element && Sessions.TryGetValue(element, out var session))
        {
            session.Deactivate();
        }
    }

    private static void OnMembershipChanged(DependencyObject sender, DependencyProperty property)
    {
        if (sender is not FrameworkElement element || !Sessions.TryGetValue(element, out var session))
        {
            return;
        }

        if ((bool)sender.GetValue(property))
        {
            session.wasInQuickAccess = true;
            session.Activate();
        }
        else if (session.wasInQuickAccess)
        {
            session.Deactivate();
        }
    }

    private sealed class Link(
        DependencyProperty sourceProperty,
        DependencyProperty targetProperty,
        bool twoWay,
        Func<object?, object?>? convert,
        Func<FrameworkElement, object?, object?>? convertBack)
    {
        internal DependencyProperty SourceProperty { get; } = sourceProperty;
        internal DependencyProperty TargetProperty { get; } = targetProperty;
        internal bool TwoWay { get; } = twoWay;
        internal Func<object?, object?>? Convert { get; } = convert;
        internal Func<FrameworkElement, object?, object?>? ConvertBack { get; } = convertBack;
        internal long? SourceToken { get; set; }
        internal long? TargetToken { get; set; }
    }
}
