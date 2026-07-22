namespace Fluent.Modern.Helpers;

/// <summary>
/// <para><b>Modern extension</b> — enables opt-in XYFocus/gamepad navigation for modern ribbon surfaces.</para>
/// </summary>
[ModernExtension]
public static class RibbonFocus
{
    #region Dependency Properties

    /// <summary>Identifies the EnableXYFocus attached dependency property.</summary>
    public static readonly DependencyProperty EnableXYFocusProperty =
        DependencyProperty.RegisterAttached(
            "EnableXYFocus",
            typeof(bool),
            typeof(RibbonFocus),
            new PropertyMetadata(false, OnEnableXYFocusChanged));

    #endregion

    #region Attached Property Accessors

    /// <summary>
    /// Gets whether XYFocus/gamepad navigation is enabled for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns><c>true</c> when XYFocus is enabled; otherwise <c>false</c>.</returns>
    public static bool GetEnableXYFocus(DependencyObject? element)
    {
        try
        {
            return element is not null && (bool)element.GetValue(EnableXYFocusProperty);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sets whether XYFocus/gamepad navigation is enabled for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value"><c>true</c> to enable XYFocus; otherwise <c>false</c>.</param>
    public static void SetEnableXYFocus(DependencyObject? element, bool value)
    {
        try
        {
            element?.SetValue(EnableXYFocusProperty, value);
        }
        catch
        {
            // Modern helpers must never throw into app code.
        }
    }

    #endregion

    #region Property Changed

    private static void OnEnableXYFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        try
        {
            if (e.NewValue is bool enabled && enabled)
            {
                element.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;
                element.XYFocusUpNavigationStrategy = XYFocusNavigationStrategy.NavigationDirectionDistance;
                element.XYFocusDownNavigationStrategy = XYFocusNavigationStrategy.NavigationDirectionDistance;
                element.XYFocusLeftNavigationStrategy = XYFocusNavigationStrategy.NavigationDirectionDistance;
                element.XYFocusRightNavigationStrategy = XYFocusNavigationStrategy.NavigationDirectionDistance;
            }
            else
            {
                element.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Auto;
                element.XYFocusUpNavigationStrategy = XYFocusNavigationStrategy.Auto;
                element.XYFocusDownNavigationStrategy = XYFocusNavigationStrategy.Auto;
                element.XYFocusLeftNavigationStrategy = XYFocusNavigationStrategy.Auto;
                element.XYFocusRightNavigationStrategy = XYFocusNavigationStrategy.Auto;
            }
        }
        catch
        {
            // Modern helpers must never throw into app code.
        }
    }

    #endregion
}
