namespace Fluent.Modern
{
    /// <summary>
    /// <para><b>Modern extension</b> — declares the preferred input density.</para>
    /// </summary>
    [ModernExtension]
    public enum RibbonInputMode
    {
        /// <summary>Uses mouse-oriented ribbon metrics.</summary>
        Mouse,

        /// <summary>Uses touch-oriented ribbon metrics.</summary>
        Touch,
    }
}

namespace Fluent.Modern.Helpers
{
    /// <summary>
    /// <para><b>Modern extension</b> — declares mouse or touch input preference.</para>
    /// </summary>
    /// <remarks>
    /// Runtime template rewrites are intentionally avoided because WinUI cannot safely
    /// replace live ribbon metrics. Merge <c>RibbonTouchDensity.xaml</c> before the ribbon
    /// is realized to apply the supplied touch metrics.
    /// </remarks>
    [ModernExtension]
    public static class RibbonInputMode
    {
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
            DependencyObject,
            InputModeState> InputModes = new();

        /// <summary>The touch target resource key provided by the density dictionary.</summary>
        public const string TouchTargetMinHeightKey = "ModernTouchTargetMinHeight";

        /// <summary>The touch padding resource key provided by the density dictionary.</summary>
        public const string TouchRibbonButtonPaddingKey = "RibbonButtonPadding";

        /// <summary>The URI of the opt-in touch density dictionary.</summary>
        public const string TouchDensityDictionaryUri =
            "ms-appx:///Fluent.Ribbon.Uno/Themes/Modern/RibbonTouchDensity.xaml";

        /// <summary>
        /// Identifies the legacy InputMode attached dependency property.
        /// </summary>
        /// <remarks>
        /// On native WinUI, <see cref="SetInputMode"/> intentionally avoids calling
        /// <see cref="DependencyObject.SetValue(DependencyProperty, object)"/> because
        /// mutating this attached property on a realized ribbon can terminate the process.
        /// The accessor methods are therefore the authoritative runtime state.
        /// </remarks>
        [Obsolete(
            "Use GetInputMode and SetInputMode. Runtime input preference is stored without mutating the WinUI dependency-property system.")]
        public static readonly DependencyProperty InputModeProperty =
            DependencyProperty.RegisterAttached(
                "InputMode",
                typeof(global::Fluent.Modern.RibbonInputMode),
                typeof(RibbonInputMode),
                new PropertyMetadata(
                    global::Fluent.Modern.RibbonInputMode.Mouse,
                    OnLegacyInputModeChanged));

        /// <summary>Gets the preferred input mode.</summary>
        public static global::Fluent.Modern.RibbonInputMode GetInputMode(
            DependencyObject element)
        {
            ArgumentNullException.ThrowIfNull(element);
            if (InputModes.TryGetValue(element, out var state))
            {
                return state.Value;
            }

#pragma warning disable CS0618
            var legacyValue = element.GetValue(InputModeProperty);
#pragma warning restore CS0618
            return legacyValue is global::Fluent.Modern.RibbonInputMode value
                ? value
                : global::Fluent.Modern.RibbonInputMode.Mouse;
        }

        /// <summary>Sets the preferred input mode.</summary>
        public static void SetInputMode(
            DependencyObject element,
            global::Fluent.Modern.RibbonInputMode value)
        {
            ArgumentNullException.ThrowIfNull(element);
            if (value is not global::Fluent.Modern.RibbonInputMode.Mouse
                and not global::Fluent.Modern.RibbonInputMode.Touch)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown input mode.");
            }

#if WINDOWS
            InputModes.GetOrCreateValue(element).Value = value;
#else
#pragma warning disable CS0618
            element.SetValue(InputModeProperty, value);
#pragma warning restore CS0618
#endif
        }

        private static void OnLegacyInputModeChanged(
            DependencyObject sender,
            DependencyPropertyChangedEventArgs args)
        {
            if (args.NewValue is global::Fluent.Modern.RibbonInputMode value)
            {
                InputModes.GetOrCreateValue(sender).Value = value;
            }
        }

        /// <summary>
        /// Gets whether touch input is declared or the touch density dictionary is merged.
        /// </summary>
        public static bool IsTouchDensityApplied(FrameworkElement element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return GetInputMode(element) == global::Fluent.Modern.RibbonInputMode.Touch
                   || element.Resources.MergedDictionaries.Any(
                       dictionary =>
                           dictionary.Source?.AbsoluteUri.Equals(
                               TouchDensityDictionaryUri,
                               StringComparison.OrdinalIgnoreCase) == true);
        }

        private sealed class InputModeState
        {
            public global::Fluent.Modern.RibbonInputMode Value { get; set; }
        }
    }
}
