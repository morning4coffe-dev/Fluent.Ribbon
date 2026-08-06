namespace Fluent;

internal static class FocusRoutingHelper
{
    internal static FocusState ResolveDelegatedFocusState(
        FocusState delegatedFocusState,
        FocusState editorFocusState)
        => delegatedFocusState is FocusState.Keyboard or FocusState.Pointer
            ? delegatedFocusState
            : editorFocusState;

    internal static WeakReference<UIElement>? CaptureFocusedElement(
        FrameworkElement owner,
        bool onlyWhenOutsideOwner = false)
    {
        if (owner.XamlRoot is not { } xamlRoot
            || FocusManager.GetFocusedElement(xamlRoot) is not UIElement focusedElement)
        {
            return null;
        }

        if (onlyWhenOutsideOwner && IsDescendantOf(focusedElement, owner))
        {
            return null;
        }

        return new WeakReference<UIElement>(focusedElement);
    }

    internal static bool RestoreFocus(ref WeakReference<UIElement>? focusReference)
    {
        var reference = focusReference;
        focusReference = null;

        if (reference is null
            || !reference.TryGetTarget(out var target)
            || !IsEffectivelyVisible(target)
            || !IsEffectivelyEnabled(target))
        {
            return false;
        }

        return target.Focus(FocusState.Programmatic);
    }

    internal static bool FocusFirst(DependencyObject root)
    {
        if (root is Control control
            && control.IsTabStop
            && control.IsEnabled
            && control.Visibility == Visibility.Visible
            && control.Focus(FocusState.Programmatic))
        {
            return true;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            if (FocusFirst(VisualTreeHelper.GetChild(root, index)))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsDescendantOf(DependencyObject? element, DependencyObject ancestor)
    {
        for (var current = element; current is not null; current = GetParent(current))
        {
            if (ReferenceEquals(current, ancestor))
            {
                return true;
            }
        }

        return false;
    }

    internal static T? FindAncestor<T>(DependencyObject? element)
        where T : DependencyObject
    {
        for (var current = element; current is not null; current = GetParent(current))
        {
            if (current is T match)
            {
                return match;
            }
        }

        return default;
    }

    internal static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        if (root is T match)
        {
            return match;
        }

        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            if (FindDescendant<T>(VisualTreeHelper.GetChild(root, index)) is T descendant)
            {
                return descendant;
            }
        }

        return default;
    }

    internal static bool IsEffectivelyVisible(DependencyObject element)
    {
        for (DependencyObject? current = element; current is FrameworkElement frameworkElement; current = GetParent(current))
        {
            if (frameworkElement.Visibility != Visibility.Visible)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool IsEffectivelyEnabled(DependencyObject element)
    {
        for (DependencyObject? current = element; current is not null; current = GetParent(current))
        {
            if (current is Control { IsEnabled: false })
            {
                return false;
            }
        }

        return true;
    }

    private static DependencyObject? GetParent(DependencyObject element)
        => VisualTreeHelper.GetParent(element);
}
