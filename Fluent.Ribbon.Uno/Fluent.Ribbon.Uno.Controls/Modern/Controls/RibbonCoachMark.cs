namespace Fluent.Modern.Controls;

/// <summary>
/// <para><b>Modern extension</b> — shows WinUI TeachingTip coach marks for ribbon feature discovery.</para>
/// </summary>
[ModernExtension]
public static class RibbonCoachMark
{
    private static readonly DependencyProperty ActiveTeachingTipProperty =
        DependencyProperty.RegisterAttached(
            "ActiveTeachingTip",
            typeof(TeachingTip),
            typeof(RibbonCoachMark),
            new PropertyMetadata(null));

    #region Dependency Properties

    /// <summary>Identifies the Title attached dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.RegisterAttached(
            "Title",
            typeof(string),
            typeof(RibbonCoachMark),
            new PropertyMetadata(string.Empty));

    /// <summary>Identifies the Subtitle attached dependency property.</summary>
    public static readonly DependencyProperty SubtitleProperty =
        DependencyProperty.RegisterAttached(
            "Subtitle",
            typeof(string),
            typeof(RibbonCoachMark),
            new PropertyMetadata(string.Empty));

    /// <summary>Identifies the IsOpen attached dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.RegisterAttached(
            "IsOpen",
            typeof(bool),
            typeof(RibbonCoachMark),
            new PropertyMetadata(false, OnIsOpenChanged));

    #endregion

    #region Attached Property Accessors

    /// <summary>
    /// Gets the coach-mark title for an element.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <returns>The configured title.</returns>
    public static string GetTitle(DependencyObject element)
    {
        return element is null ? string.Empty : (string)element.GetValue(TitleProperty);
    }

    /// <summary>
    /// Sets the coach-mark title for an element.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="value">The title.</param>
    public static void SetTitle(DependencyObject element, string value)
    {
        element?.SetValue(TitleProperty, value ?? string.Empty);
    }

    /// <summary>
    /// Gets the coach-mark subtitle for an element.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <returns>The configured subtitle.</returns>
    public static string GetSubtitle(DependencyObject element)
    {
        return element is null ? string.Empty : (string)element.GetValue(SubtitleProperty);
    }

    /// <summary>
    /// Sets the coach-mark subtitle for an element.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="value">The subtitle.</param>
    public static void SetSubtitle(DependencyObject element, string value)
    {
        element?.SetValue(SubtitleProperty, value ?? string.Empty);
    }

    /// <summary>
    /// Gets whether a coach mark is open for an element.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <returns><c>true</c> when the attached coach mark should be open; otherwise <c>false</c>.</returns>
    public static bool GetIsOpen(DependencyObject element)
    {
        return element is not null && (bool)element.GetValue(IsOpenProperty);
    }

    /// <summary>
    /// Opens or closes a coach mark for an element.
    /// </summary>
    /// <param name="element">The target element.</param>
    /// <param name="value"><c>true</c> to open the coach mark; otherwise <c>false</c>.</param>
    public static void SetIsOpen(DependencyObject element, bool value)
    {
        element?.SetValue(IsOpenProperty, value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Shows a targeted TeachingTip coach mark for the specified ribbon element.
    /// </summary>
    /// <param name="target">The target element.</param>
    /// <param name="title">The coach-mark title.</param>
    /// <param name="message">The coach-mark message.</param>
    /// <returns>The created TeachingTip.</returns>
    public static TeachingTip Show(FrameworkElement target, string title, string message)
    {
        var tip = new TeachingTip
        {
            Target = target,
            Title = title ?? string.Empty,
            Subtitle = message ?? string.Empty,
        };

        try
        {
            var host = FindHostPanel(target);
            if (host is not null)
            {
                host.Children.Add(tip);
                tip.IsOpen = true;
            }
        }
        catch
        {
            // Coach marks are optional feature-discovery UI; missing hosts must not break the ribbon.
        }

        return tip;
    }

    /// <summary>
    /// Closes and detaches a TeachingTip coach mark.
    /// </summary>
    /// <param name="tip">The tip to close.</param>
    public static void Close(TeachingTip tip)
    {
        if (tip is null)
        {
            return;
        }

        try
        {
            tip.IsOpen = false;
            if (FindParentPanel(tip) is { } parent)
            {
                parent.Children.Remove(tip);
            }
        }
        catch
        {
            // Ignore teardown errors so attached-property toggles stay no-throw.
        }
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement target)
        {
            return;
        }

        try
        {
            var existing = target.GetValue(ActiveTeachingTipProperty) as TeachingTip;
            if (existing is not null)
            {
                Close(existing);
                target.ClearValue(ActiveTeachingTipProperty);
            }

            if (e.NewValue is true)
            {
                var tip = Show(target, GetTitle(target), GetSubtitle(target));
                target.SetValue(ActiveTeachingTipProperty, tip);
            }
        }
        catch
        {
            // Attached coach marks should never throw during layout or binding updates.
        }
    }

    private static Panel? FindHostPanel(FrameworkElement target)
    {
        return FindParentPanel(target) ?? target.XamlRoot?.Content as Panel;
    }

    private static Panel? FindParentPanel(DependencyObject? element)
    {
        var current = element;
        while (current is not null)
        {
            if (current is Panel panel)
            {
                return panel;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    #endregion
}