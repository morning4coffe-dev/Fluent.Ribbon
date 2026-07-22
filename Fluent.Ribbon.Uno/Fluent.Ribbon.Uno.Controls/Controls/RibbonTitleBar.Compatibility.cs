namespace Fluent;

/// <summary>
/// WPF-compatible title-bar layout members.
/// </summary>
public partial class RibbonTitleBar
{
    /// <summary>Identifies the <see cref="HeaderAlignment"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderAlignmentProperty =
        DependencyProperty.Register(
            nameof(HeaderAlignment),
            typeof(HorizontalAlignment),
            typeof(RibbonTitleBar),
            new PropertyMetadata(HorizontalAlignment.Center));

    /// <summary>Gets or sets the title header alignment.</summary>
    public HorizontalAlignment HeaderAlignment
    {
        get => (HorizontalAlignment)GetValue(HeaderAlignmentProperty);
        set => SetValue(HeaderAlignmentProperty, value);
    }

    /// <inheritdoc />
    protected override Windows.Foundation.Size MeasureOverride(
        Windows.Foundation.Size availableSize)
    {
        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc />
    protected override Windows.Foundation.Size ArrangeOverride(
        Windows.Foundation.Size finalSize)
    {
        return base.ArrangeOverride(finalSize);
    }
}
