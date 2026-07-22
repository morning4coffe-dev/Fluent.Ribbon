namespace Fluent.Modern.Media;

using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// <para><b>Modern extension</b> — enables optional Fluent composition transitions and connected animations.</para>
/// </summary>
[ModernExtension]
public static class RibbonAnimations
{
    #region Dependency Properties

    /// <summary>Identifies the EnableImplicitTransitions attached dependency property.</summary>
    public static readonly DependencyProperty EnableImplicitTransitionsProperty =
        DependencyProperty.RegisterAttached(
            "EnableImplicitTransitions",
            typeof(bool),
            typeof(RibbonAnimations),
            new PropertyMetadata(false, OnEnableImplicitTransitionsChanged));

    #endregion

    #region Attached Property Accessors

    /// <summary>
    /// Gets whether implicit composition transitions are enabled for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns><c>true</c> when implicit transitions are enabled; otherwise <c>false</c>.</returns>
    public static bool GetEnableImplicitTransitions(DependencyObject element)
    {
        if (element is null)
        {
            return false;
        }

        return (bool)element.GetValue(EnableImplicitTransitionsProperty);
    }

    /// <summary>
    /// Sets whether implicit composition transitions are enabled for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value"><c>true</c> to enable implicit transitions; otherwise <c>false</c>.</param>
    public static void SetEnableImplicitTransitions(DependencyObject element, bool value)
    {
        if (element is null)
        {
            return;
        }

        element.SetValue(EnableImplicitTransitionsProperty, value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepares a connected animation from a source element.
    /// </summary>
    /// <param name="source">The source element.</param>
    /// <param name="key">The connected animation key.</param>
    /// <returns><c>true</c> when the animation was prepared; otherwise <c>false</c>.</returns>
    public static bool PrepareConnected(UIElement source, string key)
    {
        try
        {
            if (source is null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(key, source) is not null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to start a previously prepared connected animation on a target element.
    /// </summary>
    /// <param name="target">The target element.</param>
    /// <param name="key">The connected animation key.</param>
    /// <returns><c>true</c> when the animation started; otherwise <c>false</c>.</returns>
    public static bool TryStartConnected(UIElement target, string key)
    {
        try
        {
            if (target is null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return ConnectedAnimationService.GetForCurrentView().GetAnimation(key)?.TryStart(target) == true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Property Changed

    private static void OnEnableImplicitTransitionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        try
        {
            ApplyImplicitTransitions(element, e.NewValue is bool enabled && enabled);
        }
        catch
        {
            // Modern helpers must never throw into app code.
        }
    }

    #endregion

    #region Helpers

    private static void ApplyImplicitTransitions(FrameworkElement element, bool enabled)
    {
        var visual = ElementCompositionPreview.GetElementVisual(element);
        if (!enabled)
        {
            visual.ImplicitAnimations = null;
            return;
        }

        var compositor = visual.Compositor;
        var transitions = compositor.CreateImplicitAnimationCollection();
        transitions["Offset"] = CreateOffsetAnimation(compositor);
        transitions["Opacity"] = CreateOpacityAnimation(compositor);
        visual.ImplicitAnimations = transitions;
    }

    private static Vector3KeyFrameAnimation CreateOffsetAnimation(Compositor compositor)
    {
        var animation = compositor.CreateVector3KeyFrameAnimation();
        animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
        animation.Duration = TimeSpan.FromMilliseconds(180);
        return animation;
    }

    private static ScalarKeyFrameAnimation CreateOpacityAnimation(Compositor compositor)
    {
        var animation = compositor.CreateScalarKeyFrameAnimation();
        animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
        animation.Duration = TimeSpan.FromMilliseconds(120);
        return animation;
    }

    #endregion
}
