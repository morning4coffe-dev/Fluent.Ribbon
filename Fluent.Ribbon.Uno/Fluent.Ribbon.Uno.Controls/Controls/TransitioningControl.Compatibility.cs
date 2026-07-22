namespace Fluent;

using Microsoft.UI.Xaml.Media.Animation;

/// <summary>
/// WPF-compatible transition properties.
/// </summary>
public partial class TransitioningControl
{
    /// <summary>Identifies the <see cref="NextContent"/> dependency property.</summary>
    public static readonly DependencyProperty NextContentProperty =
        DependencyProperty.Register(
            nameof(NextContent),
            typeof(object),
            typeof(TransitioningControl),
            new PropertyMetadata(null, OnNextContentChanged));

    /// <summary>Gets or sets the next content to display.</summary>
    public object? NextContent
    {
        get => GetValue(NextContentProperty);
        set => SetValue(NextContentProperty, value);
    }

    /// <summary>Identifies the <see cref="TransitionStoryboard"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionStoryboardProperty =
        DependencyProperty.Register(
            nameof(TransitionStoryboard),
            typeof(Storyboard),
            typeof(TransitioningControl),
            new PropertyMetadata(null));

    /// <summary>Gets or sets a custom transition storyboard.</summary>
    public Storyboard? TransitionStoryboard
    {
        get => (Storyboard?)GetValue(TransitionStoryboardProperty);
        set => SetValue(TransitionStoryboardProperty, value);
    }

    private static void OnNextContentChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        ((TransitioningControl)d).Content = e.NewValue;
    }
}
