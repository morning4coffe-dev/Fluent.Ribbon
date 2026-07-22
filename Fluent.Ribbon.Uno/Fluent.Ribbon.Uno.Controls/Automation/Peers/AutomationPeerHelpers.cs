namespace Fluent.Automation.Peers;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

internal static class AutomationPeerHelpers
{
    internal static string GetHeaderName(FrameworkElement owner)
    {
        if (owner is IHeaderedControl headeredControl)
        {
            return GetObjectName(headeredControl.Header);
        }

        return owner is ContentControl contentControl
            ? GetObjectName(contentControl.Content)
            : string.Empty;
    }

    internal static string GetObjectName(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text,
            TextBlock textBlock => textBlock.Text,
            _ => value.ToString() ?? string.Empty,
        };
    }

    internal static T? FindAncestor<T>(DependencyObject child)
        where T : DependencyObject
    {
        for (var current = VisualTreeHelper.GetParent(child);
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is T match)
            {
                return match;
            }
        }

        return default;
    }
}
