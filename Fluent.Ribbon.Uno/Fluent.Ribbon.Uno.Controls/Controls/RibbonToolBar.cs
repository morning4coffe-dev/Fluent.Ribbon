namespace Fluent;

/// <summary>
/// A toolbar control that arranges its children according to layout definitions.
/// Different layouts can be specified for different ribbon sizes, and the toolbar
/// will automatically switch between them.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
[ContentProperty(Name = nameof(Items))]
public partial class RibbonToolBar : RibbonControl
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="Items"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty =
        DependencyProperty.Register(
            nameof(Items),
            typeof(ObservableCollection<FrameworkElement>),
            typeof(RibbonToolBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of toolbar items.
    /// </summary>
    public ObservableCollection<FrameworkElement> Items
    {
        get => (ObservableCollection<FrameworkElement>)GetValue(ItemsProperty);
        private set => SetValue(ItemsProperty, value);
    }

    /// <summary>Identifies the <see cref="LayoutDefinitions"/> dependency property.</summary>
    public static readonly DependencyProperty LayoutDefinitionsProperty =
        DependencyProperty.Register(
            nameof(LayoutDefinitions),
            typeof(ObservableCollection<RibbonToolBarLayoutDefinition>),
            typeof(RibbonToolBar),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets the collection of layout definitions for this toolbar.
    /// </summary>
    public ObservableCollection<RibbonToolBarLayoutDefinition> LayoutDefinitions
    {
        get => (ObservableCollection<RibbonToolBarLayoutDefinition>)GetValue(LayoutDefinitionsProperty);
        private set => SetValue(LayoutDefinitionsProperty, value);
    }

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty =
        DependencyProperty.Register(
            nameof(IsSimplified),
            typeof(bool),
            typeof(RibbonToolBar),
            new PropertyMetadata(false, OnLayoutStateChanged));

    /// <summary>
    /// Gets or sets whether the toolbar is in simplified mode.
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)GetValue(IsSimplifiedProperty);
        set => SetValue(IsSimplifiedProperty, value);
    }

    #endregion

    #region Fields

    private Grid? _layoutPanel;
    private bool _templateApplied;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonToolBar"/> class.
    /// </summary>
    public RibbonToolBar()
    {
        DefaultStyleKey = typeof(RibbonToolBar);
        Items = new ObservableCollection<FrameworkElement>();
        LayoutDefinitions = new ObservableCollection<RibbonToolBarLayoutDefinition>();

        Items.CollectionChanged += (_, _) => InvalidateLayout();
        LayoutDefinitions.CollectionChanged += (_, _) => InvalidateLayout();
        InitializeCompatibility();
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _templateApplied = true;
        RebuildLayout();
    }

    #endregion

    #region Layout

    private static void OnLayoutStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonToolBar toolbar)
        {
            var isSimplified = (bool)e.NewValue;
            foreach (var child in toolbar.Children)
            {
                UpdateChildSimplifiedState(child, isSimplified);
            }

            toolbar.RebuildLayout();
        }
    }

    private void InvalidateLayout()
    {
        RebuildLayout();
    }

    private void RebuildLayout()
    {
        // Never build layout before the template is applied. Doing so during XAML
        // parsing reparents items into a transient Grid, which the strict WinUI3
        // parser rejects ("Element is already the child of another element").
        // PART_ContentPanel only exists after OnApplyTemplate, so this is also a no-op there.
        if (!_templateApplied)
        {
            return;
        }

        var definition = GetCurrentLayoutDefinition();

        if (definition is null)
        {
            // Fall back to simple horizontal wrap
            BuildDefaultLayout();
            return;
        }

        BuildDefinedLayout(definition);
    }

    /// <summary>
    /// Gets the layout definition matching the current state.
    /// </summary>
    private RibbonToolBarLayoutDefinition? GetCurrentLayoutDefinition()
    {
        if (LayoutDefinitions.Count == 0)
        {
            return null;
        }

        var matchingMode = LayoutDefinitions
            .Where(definition => definition.ForSimplified == IsSimplified)
            .ToList();
        if (matchingMode.Count == 0)
        {
            matchingMode = LayoutDefinitions.ToList();
        }

        var currentSize = RibbonProperties.GetSize(this);
        var exact = matchingMode.FirstOrDefault(definition => definition.Size == currentSize);
        if (exact is not null)
        {
            return exact;
        }

        var preference = currentSize switch
        {
            RibbonControlSize.Large =>
                new[] { RibbonControlSize.Middle, RibbonControlSize.Small },
            RibbonControlSize.Middle =>
                new[] { RibbonControlSize.Small, RibbonControlSize.Large },
            _ =>
                new[] { RibbonControlSize.Middle, RibbonControlSize.Large },
        };

        foreach (var size in preference)
        {
            var fallback = matchingMode.FirstOrDefault(definition => definition.Size == size);
            if (fallback is not null)
            {
                return fallback;
            }
        }

        return matchingMode[0];
    }

    private void BuildDefaultLayout()
    {
        _layoutPanel = new Grid();
        _layoutPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _layoutPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _layoutPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Arrange items into 3 rows
        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            DetachFromParent(item);
            Grid.SetRow(item, i % 3);
            Grid.SetColumn(item, i / 3);
            _layoutPanel.Children.Add(item);
        }

        // Ensure enough columns
        var columnCount = (int)Math.Ceiling(Items.Count / 3.0);
        for (var c = 0; c < columnCount; c++)
        {
            _layoutPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        Content = _layoutPanel;
    }

    private void BuildDefinedLayout(RibbonToolBarLayoutDefinition definition)
    {
        _layoutPanel = new Grid();

        for (var r = 0; r < definition.RowCount; r++)
        {
            _layoutPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        }

        var maxColumns = 0;

        for (var rowIndex = 0; rowIndex < definition.Rows.Count && rowIndex < definition.RowCount; rowIndex++)
        {
            var row = definition.Rows[rowIndex];
            var colIndex = 0;

            foreach (var child in row.Children)
            {
                if (child is RibbonToolBarControlDefinition controlDef)
                {
                    var targetControl = FindItemByName(controlDef.Target);
                    if (targetControl is not null)
                    {
                        DetachFromParent(targetControl);

                        if (targetControl is IScalableRibbonControl scalable)
                        {
                            scalable.ScaleTo(controlDef.Size);
                        }

                        targetControl.Width = controlDef.Width;

                        Grid.SetRow(targetControl, rowIndex);
                        Grid.SetColumn(targetControl, colIndex);
                        _layoutPanel.Children.Add(targetControl);
                        colIndex++;
                    }
                }
                else if (child is RibbonToolBarControlGroupDefinition groupDef)
                {
                    var groupPanel = new RibbonToolBarControlGroup
                    {
                        IsFirstInRow = colIndex == 0,
                        IsLastInRow = ReferenceEquals(child, row.Children.LastOrDefault()),
                    };

                    foreach (var groupChild in groupDef.Children)
                    {
                        var targetControl = FindItemByName(groupChild.Target);
                        if (targetControl is not null)
                        {
                            DetachFromParent(targetControl);

                            if (targetControl is IScalableRibbonControl scalable)
                            {
                                scalable.ScaleTo(groupChild.Size);
                            }

                            targetControl.Width = groupChild.Width;
                            groupPanel.Items.Add(targetControl);
                        }
                    }

                    Grid.SetRow(groupPanel, rowIndex);
                    Grid.SetColumn(groupPanel, colIndex);
                    _layoutPanel.Children.Add(groupPanel);
                    colIndex++;
                }
            }

            maxColumns = Math.Max(maxColumns, colIndex);
        }

        for (var c = 0; c < maxColumns; c++)
        {
            _layoutPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        Content = _layoutPanel;
    }

    private FrameworkElement? FindItemByName(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return Items.FirstOrDefault(item => item.Name == name);
    }

    private static void DetachFromParent(UIElement element)
    {
        if (element is FrameworkElement fe && fe.Parent is Panel panel)
        {
            panel.Children.Remove(element);
        }
    }

    // Internal content holder
    private object? Content
    {
        set
        {
            // Clear and set layout panel as visual content via ContentPresenter in template
            // For simplicity, we use a single-child approach
            if (value is UIElement element)
            {
                // The control template should have a PART_ContentPanel
                var contentPanel = GetTemplateChild("PART_ContentPanel") as Panel;
                if (contentPanel is not null)
                {
                    contentPanel.Children.Clear();
                    contentPanel.Children.Add(element);
                }
            }
        }
    }

    #endregion
}
