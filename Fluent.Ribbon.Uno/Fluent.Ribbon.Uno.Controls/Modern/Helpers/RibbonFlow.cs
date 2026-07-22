namespace Fluent.Modern.Helpers;

/// <summary>
/// <para><b>Modern extension</b> — applies an opt-in ribbon flow direction to a modern surface.</para>
/// </summary>
[ModernExtension]
public static class RibbonFlow
{
    #region Dependency Properties

    /// <summary>Identifies the IsRightToLeft attached dependency property.</summary>
    public static readonly DependencyProperty IsRightToLeftProperty =
        DependencyProperty.RegisterAttached(
            "IsRightToLeft",
            typeof(bool),
            typeof(RibbonFlow),
            new PropertyMetadata(false, OnIsRightToLeftChanged));

    #endregion

    #region Attached Property Accessors

    /// <summary>
    /// Gets whether right-to-left flow is applied to the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns><c>true</c> when right-to-left flow is applied; otherwise <c>false</c>.</returns>
    public static bool GetIsRightToLeft(DependencyObject? element)
    {
        try
        {
            return element is not null && (bool)element.GetValue(IsRightToLeftProperty);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sets whether right-to-left flow is applied to the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value"><c>true</c> to apply right-to-left flow; otherwise <c>false</c>.</param>
    public static void SetIsRightToLeft(DependencyObject? element, bool value)
    {
        try
        {
            element?.SetValue(IsRightToLeftProperty, value);
        }
        catch
        {
            // Modern helpers must never throw into app code.
        }
    }

    #endregion

    #region Property Changed

    private static void OnIsRightToLeftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        try
        {
            element.FlowDirection = e.NewValue is bool isRightToLeft && isRightToLeft
                ? FlowDirection.RightToLeft
                : FlowDirection.LeftToRight;
        }
        catch
        {
            // Modern helpers must never throw into app code.
        }
    }

    #endregion
}
