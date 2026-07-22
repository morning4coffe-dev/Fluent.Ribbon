namespace Fluent.Modern.Controls;

using Fluent;

/// <summary>
/// <para><b>Modern extension</b> — presents a WinUI <see cref="IconSource"/> at ribbon icon sizes.</para>
/// </summary>
[ModernExtension]
[TemplatePart(Name = PART_IconSourceElement, Type = typeof(IconSourceElement))]
public partial class ModernIconPresenter : Control
{
    private const string PART_IconSourceElement = "PART_IconSourceElement";

    private IconSourceElement? _iconSourceElement;

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

        _iconSourceElement = GetTemplateChild(PART_IconSourceElement) as IconSourceElement;
        Update();
    }

    #endregion

    #region Methods

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ModernIconPresenter)d).Update();
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

        if (_iconSourceElement is not null)
        {
            if (Source is not null)
            {
                _iconSourceElement.IconSource = Source;
            }
            else
            {
                _iconSourceElement.ClearValue(IconSourceElement.IconSourceProperty);
            }

            _iconSourceElement.Width = size;
            _iconSourceElement.Height = size;
        }
    }

    #endregion
}
