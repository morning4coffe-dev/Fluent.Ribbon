namespace Fluent;

/// <summary>
/// The on-screen chip that displays a key tip's accelerator text while KeyTip
/// navigation is active. Positioned by <see cref="KeyTipService"/> on an overlay canvas.
/// </summary>
/// <remarks>
/// Split out from <see cref="KeyTip"/> (which now only carries attached properties)
/// so the visual can safely derive from <see cref="Control"/> without the attached
/// alignment/margin properties colliding with the intrinsic FrameworkElement ones.
/// </remarks>
public partial class KeyTipVisual : Control
{
    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(KeyTipVisual),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the key tip text displayed in the chip.
    /// </summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyTipVisual"/> class.
    /// </summary>
    public KeyTipVisual()
    {
        DefaultStyleKey = typeof(KeyTipVisual);
    }
}
