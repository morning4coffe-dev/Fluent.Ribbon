namespace Fluent;

/// <summary>
/// Defines the layout blueprint for a <see cref="RibbonToolBar"/> at a specific size.
/// Contains rows of control definitions and group definitions.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon.
/// </remarks>
[ContentProperty(Name = nameof(Rows))]
public partial class RibbonToolBarLayoutDefinition : DependencyObject
{
    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty =
        DependencyProperty.Register(
            nameof(Size),
            typeof(RibbonControlSize),
            typeof(RibbonToolBarLayoutDefinition),
            new PropertyMetadata(RibbonControlSize.Large));

    /// <summary>
    /// Gets or sets the ribbon size this layout definition applies to.
    /// </summary>
    public RibbonControlSize Size
    {
        get => (RibbonControlSize)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Identifies the <see cref="RowCount"/> dependency property.</summary>
    public static readonly DependencyProperty RowCountProperty =
        DependencyProperty.Register(
            nameof(RowCount),
            typeof(int),
            typeof(RibbonToolBarLayoutDefinition),
            new PropertyMetadata(3));

    /// <summary>
    /// Gets or sets the number of rows in this layout.
    /// </summary>
    public int RowCount
    {
        get => (int)GetValue(RowCountProperty);
        set => SetValue(RowCountProperty, value);
    }

    /// <summary>Identifies the <see cref="ForSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty ForSimplifiedProperty =
        DependencyProperty.Register(
            nameof(ForSimplified),
            typeof(bool),
            typeof(RibbonToolBarLayoutDefinition),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether this layout is for simplified ribbon mode.
    /// </summary>
    public bool ForSimplified
    {
        get => (bool)GetValue(ForSimplifiedProperty);
        set => SetValue(ForSimplifiedProperty, value);
    }

    /// <summary>
    /// Gets the rows of control definitions.
    /// </summary>
    public ObservableCollection<RibbonToolBarRow> Rows { get; } = new();
}
