namespace Fluent;

/// <summary>
/// WPF-compatible grouping and update controls for <see cref="GalleryPanel"/>.
/// </summary>
public partial class GalleryPanel
{
    /// <summary>Identifies the <see cref="ItemContainerGenerator"/> dependency property.</summary>
    public static readonly DependencyProperty ItemContainerGeneratorProperty =
        DependencyProperty.Register(
            nameof(ItemContainerGenerator),
            typeof(ItemContainerGenerator),
            typeof(GalleryPanel),
            new PropertyMetadata(null, OnItemContainerGeneratorChanged));

    /// <summary>Gets or sets the portable item-container generator.</summary>
    public ItemContainerGenerator? ItemContainerGenerator
    {
        get => (ItemContainerGenerator?)GetValue(ItemContainerGeneratorProperty);
        set => SetValue(ItemContainerGeneratorProperty, value);
    }

    private bool compatibilityUpdatesSuspended;

    private static void OnItemContainerGeneratorChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        ((GalleryPanel)sender).InvalidateMeasure();
    }
    private bool compatibilityRefreshPending;

    /// <summary>Identifies the <see cref="IsGrouped"/> dependency property.</summary>
    public static readonly DependencyProperty IsGroupedProperty =
        DependencyProperty.Register(
            nameof(IsGrouped),
            typeof(bool),
            typeof(GalleryPanel),
            new PropertyMetadata(false, OnCompatibilityLayoutChanged));

    /// <summary>Gets or sets whether gallery items are grouped.</summary>
    public bool IsGrouped
    {
        get => (bool)GetValue(IsGroupedProperty);
        set => SetValue(IsGroupedProperty, value);
    }

    /// <summary>Identifies the <see cref="GroupBy"/> dependency property.</summary>
    public static readonly DependencyProperty GroupByProperty =
        DependencyProperty.Register(
            nameof(GroupBy),
            typeof(string),
            typeof(GalleryPanel),
            new PropertyMetadata(null, OnCompatibilityLayoutChanged));

    /// <summary>Gets or sets the item property used for grouping.</summary>
    public string? GroupBy
    {
        get => (string?)GetValue(GroupByProperty);
        set => SetValue(GroupByProperty, value);
    }

    /// <summary>Identifies the <see cref="GroupByAdvanced"/> dependency property.</summary>
    public static readonly DependencyProperty GroupByAdvancedProperty =
        DependencyProperty.Register(
            nameof(GroupByAdvanced),
            typeof(Func<object?, string>),
            typeof(GalleryPanel),
            new PropertyMetadata(null, OnCompatibilityLayoutChanged));

    /// <summary>Gets or sets a custom group-name selector.</summary>
    public Func<object?, string>? GroupByAdvanced
    {
        get => (Func<object?, string>?)GetValue(GroupByAdvancedProperty);
        set => SetValue(GroupByAdvancedProperty, value);
    }

    /// <summary>Identifies the <see cref="Filter"/> dependency property.</summary>
    public static readonly DependencyProperty FilterProperty =
        DependencyProperty.Register(
            nameof(Filter),
            typeof(string),
            typeof(GalleryPanel),
            new PropertyMetadata(null, OnCompatibilityLayoutChanged));

    /// <summary>Gets or sets the comma-separated group filter.</summary>
    public string? Filter
    {
        get => (string?)GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }

    /// <summary>Defers layout refreshes until updates resume.</summary>
    public void SuspendUpdates()
    {
        compatibilityUpdatesSuspended = true;
    }

    /// <summary>Resumes updates without forcing an immediate refresh.</summary>
    public void ResumeUpdates()
    {
        compatibilityUpdatesSuspended = false;
        if (compatibilityRefreshPending)
        {
            compatibilityRefreshPending = false;
            InvalidateMeasure();
        }
    }

    /// <summary>Resumes updates and refreshes layout.</summary>
    public void ResumeUpdatesRefresh()
    {
        compatibilityUpdatesSuspended = false;
        compatibilityRefreshPending = false;
        InvalidateMeasure();
    }

    /// <summary>Gets the number of visual children.</summary>
    protected virtual int VisualChildrenCount => Children.Count;

    /// <summary>Gets a visual child by index.</summary>
    protected virtual UIElement GetVisualChild(int index)
    {
        return Children[index];
    }

    /// <summary>Handles visual child changes.</summary>
    protected virtual void OnVisualChildrenChanged(
        DependencyObject visualAdded,
        DependencyObject visualRemoved)
    {
        InvalidateMeasure();
    }

    /// <summary>Gets the panel's logical children.</summary>
    protected virtual System.Collections.IEnumerator LogicalChildren => Children.GetEnumerator();

    private static void OnCompatibilityLayoutChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        var panel = (GalleryPanel)d;
        if (panel.compatibilityUpdatesSuspended)
        {
            panel.compatibilityRefreshPending = true;
            return;
        }

        panel.InvalidateMeasure();
    }
}
