namespace Fluent.Modern.Controls;

using Fluent;

/// <summary>
/// <para><b>Modern extension</b> — presents a WinUI <see cref="IconSource"/> at ribbon icon sizes.</para>
/// </summary>
[ModernExtension]
[TemplatePart(Name = PART_IconHost, Type = typeof(ContentPresenter))]
public partial class ModernIconPresenter : Control
{
    private const string PART_IconHost = "PART_IconHost";

    private ContentPresenter? _iconHost;
    private IconSource? _appliedSource;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(
            nameof(Source),
            typeof(IconSource),
            typeof(ModernIconPresenter),
            new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>Identifies the <see cref="IconSize"/> dependency property.</summary>
    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(
            nameof(IconSize),
            typeof(IconSize),
            typeof(ModernIconPresenter),
            new PropertyMetadata(IconSize.Small, OnPropertyChanged));

    /// <summary>Identifies the <see cref="SmallSize"/> dependency property.</summary>
    public static readonly DependencyProperty SmallSizeProperty =
        DependencyProperty.Register(
            nameof(SmallSize),
            typeof(double),
            typeof(ModernIconPresenter),
            new PropertyMetadata(16.0, OnPropertyChanged));

    /// <summary>Identifies the <see cref="MediumSize"/> dependency property.</summary>
    public static readonly DependencyProperty MediumSizeProperty =
        DependencyProperty.Register(
            nameof(MediumSize),
            typeof(double),
            typeof(ModernIconPresenter),
            new PropertyMetadata(24.0, OnPropertyChanged));

    /// <summary>Identifies the <see cref="LargeSize"/> dependency property.</summary>
    public static readonly DependencyProperty LargeSizeProperty =
        DependencyProperty.Register(
            nameof(LargeSize),
            typeof(double),
            typeof(ModernIconPresenter),
            new PropertyMetadata(32.0, OnPropertyChanged));

    /// <summary>Identifies the <see cref="CustomSize"/> dependency property.</summary>
    public static readonly DependencyProperty CustomSizeProperty =
        DependencyProperty.Register(
            nameof(CustomSize),
            typeof(double),
            typeof(ModernIconPresenter),
            new PropertyMetadata(0.0, OnPropertyChanged));

    /// <summary>Identifies the <see cref="ActualIconDimension"/> dependency property.</summary>
    public static readonly DependencyProperty ActualIconDimensionProperty =
        DependencyProperty.Register(
            nameof(ActualIconDimension),
            typeof(double),
            typeof(ModernIconPresenter),
            new PropertyMetadata(16.0));

    /// <summary>Identifies the state-specific icon foreground override.</summary>
    public static readonly DependencyProperty IconForegroundOverrideProperty =
        DependencyProperty.Register(
            nameof(IconForegroundOverride),
            typeof(Brush),
            typeof(ModernIconPresenter),
            new PropertyMetadata(null, OnPropertyChanged));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the modern icon source to render.
    /// </summary>
    public IconSource? Source
    {
        get => (IconSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the icon size mode.
    /// </summary>
    public IconSize IconSize
    {
        get => (IconSize)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size for small icons (default 16).
    /// </summary>
    public double SmallSize
    {
        get => (double)GetValue(SmallSizeProperty);
        set => SetValue(SmallSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size for medium icons (default 24).
    /// </summary>
    public double MediumSize
    {
        get => (double)GetValue(MediumSizeProperty);
        set => SetValue(MediumSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size for large icons (default 32).
    /// </summary>
    public double LargeSize
    {
        get => (double)GetValue(LargeSizeProperty);
        set => SetValue(LargeSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size for custom icons.
    /// </summary>
    public double CustomSize
    {
        get => (double)GetValue(CustomSizeProperty);
        set => SetValue(CustomSizeProperty, value);
    }

    /// <summary>
    /// Gets the selected icon dimension in device-independent pixels.
    /// </summary>
    public double ActualIconDimension
    {
        get => (double)GetValue(ActualIconDimensionProperty);
        private set => SetValue(ActualIconDimensionProperty, value);
    }

    /// <summary>
    /// Gets or sets a temporary foreground that overrides the source foreground.
    /// </summary>
    public Brush? IconForegroundOverride
    {
        get => (Brush?)GetValue(IconForegroundOverrideProperty);
        set => SetValue(IconForegroundOverrideProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ModernIconPresenter"/> class.
    /// </summary>
    public ModernIconPresenter()
    {
        DefaultStyleKey = typeof(ModernIconPresenter);
        IsTabStop = false;
        IsHitTestVisible = false;
        IsEnabledChanged += OnIsEnabledChanged;
        Update();
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _iconHost = GetTemplateChild(PART_IconHost) as ContentPresenter;
        _appliedSource = null;
        Update();
    }

    #endregion

    #region Methods

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var presenter = (ModernIconPresenter)d;
        if (e.Property == IconForegroundOverrideProperty)
        {
            presenter._appliedSource = null;
        }

        presenter.Update();
    }

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Opacity = (bool)e.NewValue ? 1.0 : 0.5;
    }

    private void Update()
    {
        var size = IconSize switch
        {
            IconSize.Small => SmallSize,
            IconSize.Medium => MediumSize,
            IconSize.Large => LargeSize,
            IconSize.Custom => CustomSize,
            _ => SmallSize
        };

        ActualIconDimension = size;
        Width = size;
        Height = size;

        ApplyIconSource();

        if (_iconHost?.Content is IconSourceElement icon)
        {
            icon.Width = size;
            icon.Height = size;
        }
    }

    private void ApplyIconSource()
    {
        if (_iconHost is null)
        {
            return;
        }

        // Host a freshly built IconSourceElement (with a private, set-once IconSource clone) inside a
        // plain ContentPresenter, rather than mutating a single templated IconSourceElement.
        //
        // Two independent hazards are avoided:
        //  * Live mutation — RibbonGroupBox rescales its buttons from OnStateChanged and from
        //    MeasureOverride, flowing a new CurrentIconSource through here mid-pass. Reassigning
        //    IconSource on an already-realized IconSourceElement during layout corrupts its native
        //    state, and its next measure fails fatally with a stowed ArgumentException ("Value does
        //    not fall within the expected range", 0xC000027B). Building a new element and swapping it
        //    as ContentPresenter content is a safe, deferred content change.
        //  * Shared instance — a QuickAccessToolBar clone keeps the original's icon sources (see
        //    ModernRibbonButton.CreateQuickAccessItem) and the compatibility runtime clones controls
        //    into the QAT, so the same IconSource is realized under two parents at once; WinUI then
        //    throws "already the child of another element". Cloning gives every host its own element.
        if (ReferenceEquals(_appliedSource, Source))
        {
            return;
        }

        try
        {
            _iconHost.Content = Source is null
                ? null
                : new IconSourceElement
                {
                    IconSource = CloneIconSource(Source, IconForegroundOverride),
                };

            _appliedSource = Source;
        }
        catch (Exception)
        {
            // A single unrenderable icon must never bring down the app; keep the previous element.
        }
    }

    private static IconSource CloneIconSource(
        IconSource source,
        Brush? foregroundOverride)
    {
        switch (source)
        {
            case FontIconSource font:
                var fontClone = new FontIconSource
                {
                    Glyph = font.Glyph,
                    FontStyle = font.FontStyle,
                    FontWeight = font.FontWeight,
                    IsTextScaleFactorEnabled = font.IsTextScaleFactorEnabled,
                    MirroredWhenRightToLeft = font.MirroredWhenRightToLeft,
                };

                if (font.FontFamily is not null)
                {
                    fontClone.FontFamily = font.FontFamily;
                }

                if (font.FontSize > 0)
                {
                    fontClone.FontSize = font.FontSize;
                }

                var fontForeground = foregroundOverride ?? font.Foreground;
                if (fontForeground is not null)
                {
                    fontClone.Foreground = fontForeground;
                }

                return fontClone;

            case SymbolIconSource symbol:
                var symbolClone = new SymbolIconSource { Symbol = symbol.Symbol };
                var symbolForeground = foregroundOverride ?? symbol.Foreground;
                if (symbolForeground is not null)
                {
                    symbolClone.Foreground = symbolForeground;
                }

                return symbolClone;

            case PathIconSource path:
                var pathClone = new PathIconSource { Data = path.Data };
                var pathForeground = foregroundOverride ?? path.Foreground;
                if (pathForeground is not null)
                {
                    pathClone.Foreground = pathForeground;
                }

                return pathClone;

            case BitmapIconSource bitmap:
                var bitmapClone = new BitmapIconSource
                {
                    UriSource = bitmap.UriSource,
                    ShowAsMonochrome = foregroundOverride is not null || bitmap.ShowAsMonochrome,
                };

                var bitmapForeground = foregroundOverride ?? bitmap.Foreground;
                if (bitmapForeground is not null)
                {
                    bitmapClone.Foreground = bitmapForeground;
                }

                return bitmapClone;

            case ImageIconSource image:
                var imageClone = new ImageIconSource { ImageSource = image.ImageSource };
                var imageForeground = foregroundOverride ?? image.Foreground;
                if (imageForeground is not null)
                {
                    imageClone.Foreground = imageForeground;
                }

                return imageClone;

            default:
                return CloneUnknownIconSource(source, foregroundOverride);
        }
    }

    // Any IconSource type not covered above (e.g. AnimatedIconSource or a custom subclass) must still
    // get a private per-host instance: returning the shared original lets the same source be realized
    // under two parents (original + QuickAccessToolBar clone), which WinUI rejects with "already the
    // child of another element". Shallow-copy every readable/writable public property onto a fresh
    // instance of the same runtime type; if that is not possible, fall back to an empty icon rather
    // than leaking the shared instance.
    private static IconSource CloneUnknownIconSource(
        IconSource source,
        Brush? foregroundOverride)
    {
        try
        {
            if (Activator.CreateInstance(source.GetType()) is IconSource clone)
            {
                foreach (var property in source.GetType().GetProperties(
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (!property.CanRead
                        || !property.CanWrite
                        || property.GetIndexParameters().Length > 0)
                    {
                        continue;
                    }

                    try
                    {
                        property.SetValue(clone, property.GetValue(source));
                    }
                    catch (Exception)
                    {
                        // Skip properties that reject a direct copy; the clone stays usable.
                    }
                }

                if (foregroundOverride is not null)
                {
                    clone.Foreground = foregroundOverride;
                }

                return clone;
            }
        }
        catch (Exception)
        {
            // Fall through to the empty-icon fallback below.
        }

        return new FontIconSource { Glyph = string.Empty };
    }

    #endregion
}
