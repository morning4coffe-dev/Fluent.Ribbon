namespace Fluent.Modern.Helpers;

/// <summary>
/// <para><b>Modern extension</b> — applies an opt-in ribbon flow direction to a modern surface.</para>
/// </summary>
[ModernExtension]
public static class RibbonFlow
{
    private static readonly DependencyProperty OriginalFlowDirectionProperty =
        DependencyProperty.RegisterAttached(
            "OriginalFlowDirection",
            typeof(FlowDirection),
            typeof(RibbonFlow),
            new PropertyMetadata(FlowDirection.LeftToRight));

    private static readonly DependencyProperty OriginalFlowDirectionCapturedProperty =
        DependencyProperty.RegisterAttached(
            "OriginalFlowDirectionCaptured",
            typeof(bool),
            typeof(RibbonFlow),
            new PropertyMetadata(false));

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
            if (e.NewValue is bool isRightToLeft && isRightToLeft)
            {
                if (!(bool)element.GetValue(OriginalFlowDirectionCapturedProperty))
                {
                    element.SetValue(OriginalFlowDirectionProperty, element.FlowDirection);
                    element.SetValue(OriginalFlowDirectionCapturedProperty, true);
                }

                element.FlowDirection = FlowDirection.RightToLeft;
            }
            else if ((bool)element.GetValue(OriginalFlowDirectionCapturedProperty))
            {
                element.FlowDirection =
                    (FlowDirection)element.GetValue(OriginalFlowDirectionProperty);
                element.ClearValue(OriginalFlowDirectionProperty);
                element.ClearValue(OriginalFlowDirectionCapturedProperty);
            }
        }
        catch
        {
            // Modern helpers must never throw into app code.
        }
    }

    #endregion
}
