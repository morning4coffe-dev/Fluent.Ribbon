namespace Fluent.Modern.Helpers;

using Fluent;

/// <summary>
/// <para><b>Modern extension</b> — opt-in responsive behavior for adapting a ribbon to available width.</para>
/// </summary>
[ModernExtension]
public static class RibbonAdaptiveBehavior
{
    private const double DefaultSimplifiedThreshold = 900d;
    private const double DefaultMinimizedThreshold = 500d;

    private static readonly DependencyProperty AssociatedRibbonProperty =
        DependencyProperty.RegisterAttached(
            "AssociatedRibbon",
            typeof(Ribbon),
            typeof(RibbonAdaptiveBehavior),
            new PropertyMetadata(null));

    private static readonly DependencyProperty OriginalIsSimplifiedProperty =
        DependencyProperty.RegisterAttached(
            "OriginalIsSimplified",
            typeof(bool),
            typeof(RibbonAdaptiveBehavior),
            new PropertyMetadata(false));

    private static readonly DependencyProperty OriginalIsMinimizedProperty =
        DependencyProperty.RegisterAttached(
            "OriginalIsMinimized",
            typeof(bool),
            typeof(RibbonAdaptiveBehavior),
            new PropertyMetadata(false));

    private static readonly DependencyProperty OriginalStateCapturedProperty =
        DependencyProperty.RegisterAttached(
            "OriginalStateCaptured",
            typeof(bool),
            typeof(RibbonAdaptiveBehavior),
            new PropertyMetadata(false));

    #region Dependency Properties

    /// <summary>Identifies the IsEnabled attached dependency property.</summary>
    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(RibbonAdaptiveBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>Identifies the SimplifiedThreshold attached dependency property.</summary>
    public static readonly DependencyProperty SimplifiedThresholdProperty =
        DependencyProperty.RegisterAttached(
            "SimplifiedThreshold",
            typeof(double),
            typeof(RibbonAdaptiveBehavior),
            new PropertyMetadata(DefaultSimplifiedThreshold, OnThresholdChanged));

    /// <summary>Identifies the MinimizedThreshold attached dependency property.</summary>
    public static readonly DependencyProperty MinimizedThresholdProperty =
        DependencyProperty.RegisterAttached(
            "MinimizedThreshold",
            typeof(double),
            typeof(RibbonAdaptiveBehavior),
            new PropertyMetadata(DefaultMinimizedThreshold, OnThresholdChanged));

    #endregion

    #region Attached Property Accessors

    /// <summary>
    /// Gets whether adaptive layout is enabled for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns><c>true</c> when adaptive layout is enabled; otherwise <c>false</c>.</returns>
    public static bool GetIsEnabled(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(IsEnabledProperty);
    }

    /// <summary>
    /// Sets whether adaptive layout is enabled for the element.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value"><c>true</c> to enable adaptive layout; otherwise <c>false</c>.</param>
    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IsEnabledProperty, value);
    }

    /// <summary>
    /// Gets the width below which the ribbon switches to simplified mode.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The simplified threshold in effective pixels.</returns>
    public static double GetSimplifiedThreshold(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (double)element.GetValue(SimplifiedThresholdProperty);
    }

    /// <summary>
    /// Sets the width below which the ribbon switches to simplified mode.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The simplified threshold in effective pixels.</param>
    public static void SetSimplifiedThreshold(DependencyObject element, double value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(SimplifiedThresholdProperty, value);
    }

    /// <summary>
    /// Gets the width below which the ribbon switches to minimized mode.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <returns>The minimized threshold in effective pixels.</returns>
    public static double GetMinimizedThreshold(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (double)element.GetValue(MinimizedThresholdProperty);
    }

    /// <summary>
    /// Sets the width below which the ribbon switches to minimized mode.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="value">The minimized threshold in effective pixels.</param>
    public static void SetMinimizedThreshold(DependencyObject element, double value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(MinimizedThresholdProperty, value);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Evaluates the ribbon state for a deterministic width using the default modern breakpoints.
    /// </summary>
    /// <param name="ribbon">The ribbon to update.</param>
    /// <param name="width">The available width in effective pixels.</param>
    public static void Evaluate(Ribbon ribbon, double width)
    {
        ArgumentNullException.ThrowIfNull(ribbon);
        Evaluate(ribbon, width, DefaultSimplifiedThreshold, DefaultMinimizedThreshold);
    }

    private static void Evaluate(Ribbon ribbon, double width, double simplifiedThreshold, double minimizedThreshold)
    {
        if (double.IsNaN(width) || double.IsInfinity(width))
        {
            return;
        }

        var shouldMinimize = width < minimizedThreshold;
        var shouldSimplify = !shouldMinimize && width < simplifiedThreshold;

        SetIfChanged(ribbon, Ribbon.IsSimplifiedProperty, shouldSimplify, ribbon.IsSimplified);
        SetIfChanged(ribbon, Ribbon.IsMinimizedProperty, shouldMinimize, ribbon.IsMinimized);
    }

    #endregion

    #region Property Changed

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        try
        {
            if (e.OldValue is bool oldValue && oldValue)
            {
                Unsubscribe(element);
                RestoreOriginalState(element);
            }

            if (e.NewValue is bool newValue && newValue)
            {
                Subscribe(element);
                Apply(element, element.ActualWidth);
            }
        }
        catch
        {
            // Modern helpers must never throw into app code.
        }
    }

    private static void OnThresholdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FrameworkElement element && GetIsEnabled(element))
        {
            try
            {
                Apply(element, element.ActualWidth);
            }
            catch
            {
                // Modern helpers must never throw into app code.
            }
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && GetIsEnabled(element))
        {
            try
            {
                Apply(element, element.ActualWidth);
            }
            catch
            {
                // Modern helpers must never throw into app code.
            }
        }
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is FrameworkElement element && GetIsEnabled(element))
        {
            try
            {
                Apply(element, e.NewSize.Width);
            }
            catch
            {
                // Modern helpers must never throw into app code.
            }
        }
    }

    #endregion

    #region Helpers

    private static void Subscribe(FrameworkElement element)
    {
        Unsubscribe(element);
        element.Loaded += OnLoaded;
        element.SizeChanged += OnSizeChanged;
    }

    private static void Unsubscribe(FrameworkElement element)
    {
        element.Loaded -= OnLoaded;
        element.SizeChanged -= OnSizeChanged;
    }

    private static void Apply(FrameworkElement element, double width)
    {
        var ribbon = GetRibbon(element);
        if (ribbon is null)
        {
            return;
        }

        CaptureOriginalState(element, ribbon);
        Evaluate(ribbon, width, GetSimplifiedThreshold(element), GetMinimizedThreshold(element));
    }

    private static Ribbon? GetRibbon(FrameworkElement element)
    {
        if (element is Ribbon ribbon)
        {
            element.SetValue(AssociatedRibbonProperty, ribbon);
            return ribbon;
        }

        if (element.GetValue(AssociatedRibbonProperty) is Ribbon associatedRibbon)
        {
            return associatedRibbon;
        }

        var foundRibbon = FindDescendantRibbon(element);
        if (foundRibbon is not null)
        {
            element.SetValue(AssociatedRibbonProperty, foundRibbon);
        }

        return foundRibbon;
    }

    private static Ribbon? FindDescendantRibbon(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is Ribbon ribbon)
            {
                return ribbon;
            }

            var descendant = FindDescendantRibbon(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static void CaptureOriginalState(FrameworkElement element, Ribbon ribbon)
    {
        if ((bool)element.GetValue(OriginalStateCapturedProperty))
        {
            return;
        }

        element.SetValue(OriginalIsSimplifiedProperty, ribbon.IsSimplified);
        element.SetValue(OriginalIsMinimizedProperty, ribbon.IsMinimized);
        element.SetValue(OriginalStateCapturedProperty, true);
    }

    private static void RestoreOriginalState(FrameworkElement element)
    {
        if (!(bool)element.GetValue(OriginalStateCapturedProperty))
        {
            return;
        }

        if (GetRibbon(element) is Ribbon ribbon)
        {
            SetIfChanged(ribbon, Ribbon.IsSimplifiedProperty, (bool)element.GetValue(OriginalIsSimplifiedProperty), ribbon.IsSimplified);
            SetIfChanged(ribbon, Ribbon.IsMinimizedProperty, (bool)element.GetValue(OriginalIsMinimizedProperty), ribbon.IsMinimized);
        }

        element.ClearValue(OriginalIsSimplifiedProperty);
        element.ClearValue(OriginalIsMinimizedProperty);
        element.ClearValue(OriginalStateCapturedProperty);
        element.ClearValue(AssociatedRibbonProperty);
    }

    private static void SetIfChanged(Ribbon ribbon, DependencyProperty property, bool value, bool currentValue)
    {
        if (currentValue != value)
        {
            ribbon.SetValue(property, value);
        }
    }

    #endregion
}
