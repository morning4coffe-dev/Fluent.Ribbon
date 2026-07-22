namespace Fluent;

using Fluent.Internal;

/// <summary>
/// Presents icons at different sizes (Small, Medium, Large, Custom) with automatic
/// size selection and disabled-state opacity.
/// </summary>
public partial class IconPresenter : ContentControl
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="IconSize"/> dependency property.</summary>
    public static readonly DependencyProperty IconSizeProperty =
        DependencyProperty.Register(
            nameof(IconSize),
            typeof(IconSize),
            typeof(IconPresenter),
            new PropertyMetadata(IconSize.Small, OnPropertyChanged));

    /// <summary>Identifies the <see cref="SmallSize"/> dependency property.</summary>
    public static readonly DependencyProperty SmallSizeProperty =
        DependencyProperty.Register(
            nameof(SmallSize),
            typeof(Windows.Foundation.Size),
            typeof(IconPresenter),
            new PropertyMetadata(new Windows.Foundation.Size(16.0, 16.0), OnPropertyChanged));

    /// <summary>Identifies the <see cref="MediumSize"/> dependency property.</summary>
    public static readonly DependencyProperty MediumSizeProperty =
        DependencyProperty.Register(
            nameof(MediumSize),
            typeof(Windows.Foundation.Size),
            typeof(IconPresenter),
            new PropertyMetadata(new Windows.Foundation.Size(24.0, 24.0), OnPropertyChanged));

    /// <summary>Identifies the <see cref="LargeSize"/> dependency property.</summary>
    public static readonly DependencyProperty LargeSizeProperty =
        DependencyProperty.Register(
            nameof(LargeSize),
            typeof(Windows.Foundation.Size),
            typeof(IconPresenter),
            new PropertyMetadata(new Windows.Foundation.Size(32.0, 32.0), OnPropertyChanged));

    /// <summary>Identifies the <see cref="CustomSize"/> dependency property.</summary>
    public static readonly DependencyProperty CustomSizeProperty =
        DependencyProperty.Register(
            nameof(CustomSize),
            typeof(Windows.Foundation.Size),
            typeof(IconPresenter),
            new PropertyMetadata(new Windows.Foundation.Size(0.0, 0.0), OnPropertyChanged));

    /// <summary>Identifies the <see cref="CurrentIconSizeSize"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentIconSizeSizeProperty =
        DependencyProperty.Register(
            nameof(CurrentIconSizeSize),
            typeof(Windows.Foundation.Size),
            typeof(IconPresenter),
            new PropertyMetadata(new Windows.Foundation.Size(16.0, 16.0)));

    /// <summary>Identifies the <see cref="SmallIcon"/> dependency property.</summary>
    public static readonly DependencyProperty SmallIconProperty =
        DependencyProperty.Register(
            nameof(SmallIcon),
            typeof(object),
            typeof(IconPresenter),
            new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>Identifies the <see cref="MediumIcon"/> dependency property.</summary>
    public static readonly DependencyProperty MediumIconProperty =
        DependencyProperty.Register(
            nameof(MediumIcon),
            typeof(object),
            typeof(IconPresenter),
            new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>Identifies the <see cref="LargeIcon"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconProperty =
        DependencyProperty.Register(
            nameof(LargeIcon),
            typeof(object),
            typeof(IconPresenter),
            new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>Identifies the <see cref="CustomIcon"/> dependency property.</summary>
    public static readonly DependencyProperty CustomIconProperty =
        DependencyProperty.Register(
            nameof(CustomIcon),
            typeof(object),
            typeof(IconPresenter),
            new PropertyMetadata(null, OnPropertyChanged));

    /// <summary>Identifies the <see cref="OptimalIcon"/> dependency property.</summary>
    public static readonly DependencyProperty OptimalIconProperty =
        DependencyProperty.Register(
            nameof(OptimalIcon),
            typeof(object),
            typeof(IconPresenter),
            new PropertyMetadata(null));

    #endregion

    #region Properties

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
    public Windows.Foundation.Size SmallSize
    {
        get => (Windows.Foundation.Size)GetValue(SmallSizeProperty);
        set => SetValue(SmallSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size for medium icons (default 24).
    /// </summary>
    public Windows.Foundation.Size MediumSize
    {
        get => (Windows.Foundation.Size)GetValue(MediumSizeProperty);
        set => SetValue(MediumSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size for large icons (default 32).
    /// </summary>
    public Windows.Foundation.Size LargeSize
    {
        get => (Windows.Foundation.Size)GetValue(LargeSizeProperty);
        set => SetValue(LargeSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the size for custom icons.
    /// </summary>
    public Windows.Foundation.Size CustomSize
    {
        get => (Windows.Foundation.Size)GetValue(CustomSizeProperty);
        set => SetValue(CustomSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the resolved size for the current icon mode.
    /// </summary>
    public Windows.Foundation.Size CurrentIconSizeSize
    {
        get => (Windows.Foundation.Size)GetValue(CurrentIconSizeSizeProperty);
        set => SetValue(CurrentIconSizeSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets the small icon source.
    /// </summary>
    public object? SmallIcon
    {
        get => GetValue(SmallIconProperty);
        set => SetValue(SmallIconProperty, value);
    }

    /// <summary>
    /// Gets or sets the medium icon source.
    /// </summary>
    public object? MediumIcon
    {
        get => GetValue(MediumIconProperty);
        set => SetValue(MediumIconProperty, value);
    }

    /// <summary>
    /// Gets or sets the large icon source.
    /// </summary>
    public object? LargeIcon
    {
        get => GetValue(LargeIconProperty);
        set => SetValue(LargeIconProperty, value);
    }

    /// <summary>
    /// Gets or sets the custom icon source.
    /// </summary>
    public object? CustomIcon
    {
        get => GetValue(CustomIconProperty);
        set => SetValue(CustomIconProperty, value);
    }

    /// <summary>
    /// Gets the optimal icon based on the current <see cref="IconSize"/>.
    /// </summary>
    public object? OptimalIcon
    {
        get => GetValue(OptimalIconProperty);
        set => SetValue(OptimalIconProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="IconPresenter"/> class.
    /// </summary>
    public IconPresenter()
    {
        IsTabStop = false;
        IsHitTestVisible = false;

        IsEnabledChanged += OnIsEnabledChanged;

        Update();
    }

    #endregion

    #region Methods

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((IconPresenter)d).Update();
    }

    private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        Opacity = (bool)e.NewValue ? 1.0 : 0.5;
    }

    private void Update()
    {
        UpdateSize();

        var optimalIcon = GetOptimalIcon();
        if (OptimalIcon != optimalIcon)
        {
            OptimalIcon = optimalIcon;
        }

        UpdateContent();
    }

    private void UpdateSize()
    {
        var size = IconSize switch
        {
            IconSize.Small => SmallSize,
            IconSize.Medium => MediumSize,
            IconSize.Large => LargeSize,
            IconSize.Custom => CustomSize,
            _ => SmallSize
        };

        CurrentIconSizeSize = size;

        if (!DoubleUtil.AreClose(Width, size.Width))
        {
            Width = size.Width;
        }

        if (!DoubleUtil.AreClose(Height, size.Height))
        {
            Height = size.Height;
        }
    }

    private void UpdateContent()
    {
        var icon = OptimalIcon;

        if (icon is ImageSource imageSource)
        {
            Content = new Image
            {
                Source = imageSource,
                Stretch = Stretch.Uniform,
            };
        }
        else
        {
            Content = icon;
        }
    }

    /// <summary>
    /// Gets the optimal icon for the current size, falling back to other sizes if unavailable.
    /// </summary>
    public object? GetOptimalIcon()
    {
        return IconSize switch
        {
            IconSize.Small => SmallIcon ?? MediumIcon ?? LargeIcon,
            IconSize.Medium => MediumIcon ?? LargeIcon ?? SmallIcon,
            IconSize.Large => LargeIcon ?? MediumIcon ?? SmallIcon,
            IconSize.Custom => CustomIcon ?? LargeIcon ?? MediumIcon ?? SmallIcon,
            _ => LargeIcon ?? MediumIcon ?? SmallIcon
        };
    }

    #endregion
}
