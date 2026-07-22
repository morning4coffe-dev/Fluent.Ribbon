namespace Fluent;

/// <summary>
/// A control that manages animated content transitions.
/// Displays new content with a transition from the previous content.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// Uses WinUI Storyboard/DoubleAnimation for transitions.
/// </remarks>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_CurrentContentPresenter, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_PreviousContentPresenter, Type = typeof(ContentPresenter))]
public partial class TransitioningControl : Control
{
    /// <summary>Name of the previous-content template part.</summary>
    public const string PreviousContentPartName = "PART_PreviousContent";

    /// <summary>Name of the current-content template part.</summary>
    public const string CurrentContentPartName = "PART_CurrentContent";

    private const string PART_CurrentContentPresenter = "PART_CurrentContentPresenter";
    private const string PART_PreviousContentPresenter = "PART_PreviousContentPresenter";

    private ContentPresenter? _currentContentPresenter;
    private ContentPresenter? _previousContentPresenter;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(TransitioningControl),
            new PropertyMetadata(null, OnContentChanged));

    /// <summary>
    /// Gets or sets the content to display.
    /// </summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Identifies the <see cref="ContentStringFormat"/> dependency property.</summary>
    public static readonly DependencyProperty ContentStringFormatProperty =
        DependencyProperty.Register(
            nameof(ContentStringFormat),
            typeof(string),
            typeof(TransitioningControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the string format for content display.
    /// </summary>
    public string? ContentStringFormat
    {
        get => (string?)GetValue(ContentStringFormatProperty);
        set => SetValue(ContentStringFormatProperty, value);
    }

    /// <summary>Identifies the <see cref="IsTransitionEnabled"/> dependency property.</summary>
    public static readonly DependencyProperty IsTransitionEnabledProperty =
        DependencyProperty.Register(
            nameof(IsTransitionEnabled),
            typeof(bool),
            typeof(TransitioningControl),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets or sets whether the transition animation is enabled.
    /// </summary>
    public bool IsTransitionEnabled
    {
        get => (bool)GetValue(IsTransitionEnabledProperty);
        set => SetValue(IsTransitionEnabledProperty, value);
    }

    /// <summary>Identifies the <see cref="TransitionDuration"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionDurationProperty =
        DependencyProperty.Register(
            nameof(TransitionDuration),
            typeof(TimeSpan),
            typeof(TransitioningControl),
            new PropertyMetadata(TimeSpan.FromMilliseconds(200)));

    /// <summary>
    /// Gets or sets the duration of the transition animation.
    /// </summary>
    public TimeSpan TransitionDuration
    {
        get => (TimeSpan)GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="TransitioningControl"/> class.
    /// </summary>
    public TransitioningControl()
    {
        DefaultStyleKey = typeof(TransitioningControl);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _currentContentPresenter = GetTemplateChild(PART_CurrentContentPresenter) as ContentPresenter;
        _previousContentPresenter = GetTemplateChild(PART_PreviousContentPresenter) as ContentPresenter;

        if (_currentContentPresenter is not null)
        {
            _currentContentPresenter.Content = Content;
        }
    }

    #endregion

    #region Methods

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TransitioningControl control)
        {
            control.StartTransition(e.OldValue, e.NewValue);
        }
    }

    /// <summary>
    /// Starts the content transition.
    /// </summary>
    protected virtual void StartTransition(object? oldContent, object? newContent)
    {
        if (_currentContentPresenter is null)
        {
            return;
        }

        if (!IsTransitionEnabled || _previousContentPresenter is null)
        {
            _currentContentPresenter.Content = newContent;
            return;
        }

        // Move current content to previous
        _previousContentPresenter.Content = oldContent;
        _previousContentPresenter.Opacity = 1;

        // Set new content
        _currentContentPresenter.Content = newContent;
        _currentContentPresenter.Opacity = 0;

        if (TransitionStoryboard is not null)
        {
            TransitionStoryboard.Begin();
            return;
        }

        // Animate: fade out previous, fade in current
        var duration = new Duration(TransitionDuration);

        var fadeOut = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = duration,
        };
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeOut, _previousContentPresenter);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeOut, "Opacity");

        var fadeIn = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 0.0,
            To = 1.0,
            Duration = duration,
        };
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeIn, _currentContentPresenter);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeIn, "Opacity");

        var storyboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        storyboard.Children.Add(fadeOut);
        storyboard.Children.Add(fadeIn);

        storyboard.Completed += (s, e) =>
        {
            _previousContentPresenter.Content = null;
        };

        storyboard.Begin();
    }

    /// <summary>
    /// Stops any running transition animation.
    /// </summary>
    public virtual void StopTransition()
    {
        if (_previousContentPresenter is not null)
        {
            _previousContentPresenter.Content = null;
            _previousContentPresenter.Opacity = 0;
        }

        if (_currentContentPresenter is not null)
        {
            _currentContentPresenter.Opacity = 1;
        }
    }

    #endregion
}
