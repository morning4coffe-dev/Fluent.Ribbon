namespace Fluent.Modern
{
    /// <summary>
    /// <para><b>Modern extension</b> — declares the input density mode used by modern ribbon helpers.</para>
    /// </summary>
    [ModernExtension]
    public enum RibbonInputMode
    {
        /// <summary>
        /// Uses the default mouse-oriented ribbon density.
        /// </summary>
        Mouse,

        /// <summary>
        /// Uses larger touch-friendly ribbon density resources.
        /// </summary>
        Touch,
    }
}

namespace Fluent.Modern.Helpers
{
    /// <summary>
    /// <para><b>Modern extension</b> — applies modern mouse or touch density resources on demand.</para>
    /// </summary>
    [ModernExtension]
    public static class RibbonInputMode
    {
        /// <summary>The touch target resource key provided by the modern touch density dictionary.</summary>
        public const string TouchTargetMinHeightKey = "ModernTouchTargetMinHeight";

        /// <summary>The touch padding resource key provided by the modern touch density dictionary.</summary>
        public const string TouchRibbonButtonPaddingKey = "RibbonButtonPadding";

        /// <summary>The touch dictionary URI merged when input mode is Touch.</summary>
        public const string TouchDensityDictionaryUri = "ms-appx:///Fluent.Ribbon.Uno/Themes/Modern/RibbonTouchDensity.xaml";

        #region Dependency Properties

        /// <summary>Identifies the InputMode attached dependency property.</summary>
        public static readonly DependencyProperty InputModeProperty =
            DependencyProperty.RegisterAttached(
                "InputMode",
                typeof(global::Fluent.Modern.RibbonInputMode),
                typeof(RibbonInputMode),
                new PropertyMetadata(global::Fluent.Modern.RibbonInputMode.Mouse, OnInputModeChanged));

        #endregion

        #region Attached Property Accessors

        /// <summary>
        /// Gets the input density mode applied to the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The input density mode.</returns>
        public static global::Fluent.Modern.RibbonInputMode GetInputMode(DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return (global::Fluent.Modern.RibbonInputMode)element.GetValue(InputModeProperty);
        }

        /// <summary>
        /// Sets the input density mode applied to the element.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="value">The input density mode.</param>
        public static void SetInputMode(DependencyObject element, global::Fluent.Modern.RibbonInputMode value)
        {
            ArgumentNullException.ThrowIfNull(element);
            element.SetValue(InputModeProperty, value);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets whether the modern touch density resource dictionary is currently applied to the element.
        /// </summary>
        /// <param name="element">The element to inspect.</param>
        /// <returns><c>true</c> when the touch density dictionary is merged; otherwise <c>false</c>.</returns>
        public static bool IsTouchDensityApplied(FrameworkElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return FindTouchDensityDictionary(element) is not null;
        }

        #endregion

        #region Property Changed

        private static void OnInputModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element)
            {
                return;
            }

            try
            {
                if (e.NewValue is global::Fluent.Modern.RibbonInputMode.Touch)
                {
                    EnsureTouchDensity(element);
                }
                else
                {
                    RemoveTouchDensity(element);
                }
            }
            catch
            {
                // Modern helpers must never throw into app code.
            }
        }

        #endregion

        #region Helpers

        private static void EnsureTouchDensity(FrameworkElement element)
        {
            if (FindTouchDensityDictionary(element) is not null)
            {
                return;
            }

            element.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(TouchDensityDictionaryUri),
            });
        }

        private static void RemoveTouchDensity(FrameworkElement element)
        {
            var dictionary = FindTouchDensityDictionary(element);
            if (dictionary is not null)
            {
                element.Resources.MergedDictionaries.Remove(dictionary);
            }
        }

        private static ResourceDictionary? FindTouchDensityDictionary(FrameworkElement element)
        {
            foreach (var dictionary in element.Resources.MergedDictionaries)
            {
                if (IsTouchDensityDictionary(dictionary))
                {
                    return dictionary;
                }
            }

            return null;
        }

        private static bool IsTouchDensityDictionary(ResourceDictionary dictionary)
        {
            return dictionary.Source?.AbsoluteUri.Equals(TouchDensityDictionaryUri, StringComparison.OrdinalIgnoreCase) == true;
        }

        #endregion
    }
}
