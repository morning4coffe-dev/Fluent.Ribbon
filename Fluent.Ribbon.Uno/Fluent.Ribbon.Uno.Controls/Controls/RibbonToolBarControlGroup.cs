namespace Fluent;

/// <summary>
/// A container that groups toolbar items visually, tracking whether it's first/last in its row.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WPF uses ItemsControl; Uno uses Panel-based approach.
/// </remarks>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonToolBarControlGroup : Control
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<UIElement>),
            typeof(RibbonToolBarControlGroup),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of controls in this group.
    /// </summary>
    public ObservableCollection<UIElement> Items
    {
        get => (ObservableCollection<UIElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="IsFirstInRow"/> dependency property.</summary>
    public static readonly DependencyProperty IsFirstInRowProperty =
        DependencyProperty.Register(
            nameof(IsFirstInRow),
            typeof(bool),
            typeof(RibbonToolBarControlGroup),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether this group is the first in its row.
    /// </summary>
    public bool IsFirstInRow
    {
        get => (bool)GetValue(IsFirstInRowProperty);
        set => SetValue(IsFirstInRowProperty, value);
    }

    /// <summary>Identifies the <see cref="IsLastInRow"/> dependency property.</summary>
    public static readonly DependencyProperty IsLastInRowProperty =
        DependencyProperty.Register(
            nameof(IsLastInRow),
            typeof(bool),
            typeof(RibbonToolBarControlGroup),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether this group is the last in its row.
    /// </summary>
    public bool IsLastInRow
    {
        get => (bool)GetValue(IsLastInRowProperty);
        set => SetValue(IsLastInRowProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonToolBarControlGroup"/> class.
    /// </summary>
    public RibbonToolBarControlGroup()
    {
        DefaultStyleKey = typeof(RibbonToolBarControlGroup);
        Items = new ObservableCollection<UIElement>();
    }

    #endregion
}
