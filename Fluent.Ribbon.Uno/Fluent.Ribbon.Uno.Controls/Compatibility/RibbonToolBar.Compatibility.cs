namespace Fluent;

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using Fluent.Extensibility;
using Windows.Foundation;

/// <summary>
/// WPF-compatible API and portable behavior for <see cref="RibbonToolBar"/>.
/// </summary>
public partial class RibbonToolBar :
    IRibbonControl,
    IQuickAccessItemProvider,
    IRibbonSizeChangedSink,
    ISimplifiedStateControl
{
    private static readonly RibbonControlSizeDefinition DefaultSizeDefinition =
        new(RibbonControlSize.Large, RibbonControlSize.Middle, RibbonControlSize.Small);
    private readonly Dictionary<
        RibbonToolBarLayoutDefinition,
        List<(DependencyProperty Property, long Token)>> _layoutPropertyTokens = new();
    private readonly HashSet<INotifyCollectionChanged> _layoutCollections = new();
    private readonly HashSet<RibbonToolBarControlGroupDefinition> _groupDefinitions = new();
    private readonly HashSet<RibbonToolBarControlDefinition> _controlDefinitions = new();

    /// <summary>Identifies the separator style property.</summary>
    public static readonly DependencyProperty SeparatorStyleProperty =
        DependencyProperty.Register(
            nameof(SeparatorStyle),
            typeof(Style),
            typeof(RibbonToolBar),
            new PropertyMetadata(null, OnCompatibilityLayoutPropertyChanged));

    /// <summary>Gets or sets the style used by separators between toolbar columns.</summary>
    public Style? SeparatorStyle
    {
        get => (Style?)GetValue(SeparatorStyleProperty);
        set => SetValue(SeparatorStyleProperty, value);
    }

    /// <summary>Identifies the toolbar header property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(RibbonToolBar),
            new PropertyMetadata(null));

    /// <inheritdoc />
    public new object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the toolbar icon property.</summary>
    public new static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
            nameof(Icon),
            typeof(object),
            typeof(RibbonToolBar),
            new PropertyMetadata(null));

    /// <inheritdoc />
    public new object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the toolbar key tip.</summary>
    public new string? KeyTip
    {
        get => Fluent.KeyTip.GetKeys(this);
        set => Fluent.KeyTip.SetKeys(this, value);
    }

    /// <inheritdoc />
    public new RibbonControlSize Size
    {
        get => RibbonProperties.GetSize(this);
        set => RibbonProperties.SetSize(this, value);
    }

    /// <inheritdoc />
    public new RibbonControlSizeDefinition SizeDefinition
    {
        get => (RibbonControlSizeDefinition)GetValue(SizeDefinitionProperty);
        set => SetValue(SizeDefinitionProperty, value);
    }

    /// <summary>Identifies the toolbar size-definition property.</summary>
    public new static readonly DependencyProperty SizeDefinitionProperty =
        DependencyProperty.Register(
            nameof(SizeDefinition),
            typeof(RibbonControlSizeDefinition),
            typeof(RibbonToolBar),
            new PropertyMetadata(
                DefaultSizeDefinition,
                OnCompatibilityLayoutPropertyChanged));

    /// <inheritdoc />
    public new bool CanAddToQuickAccessToolBar
    {
        get => RibbonProperties.GetCanAddToQuickAccessToolBar(this);
        set => RibbonProperties.SetCanAddToQuickAccessToolBar(this, value);
    }

    /// <summary>
    /// Gets the user-defined toolbar children.
    /// </summary>
    public ObservableCollection<FrameworkElement> Children => Items;

    private void InitializeCompatibility()
    {
        CanAddToQuickAccessToolBar = false;
        LayoutDefinitions.CollectionChanged += OnCompatibilityDefinitionsChanged;
        RefreshLayoutSubscriptions();
    }

    private static void OnCompatibilityLayoutPropertyChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
        => ((RibbonToolBar)sender).RebuildLayout();

    private void OnCompatibilityDefinitionsChanged(
        object? sender,
        NotifyCollectionChangedEventArgs args)
    {
        RefreshLayoutSubscriptions();
        RebuildLayout();
    }

    private void RefreshLayoutSubscriptions()
    {
        foreach (var (definition, tokens) in _layoutPropertyTokens)
        {
            foreach (var (property, token) in tokens)
            {
                definition.UnregisterPropertyChangedCallback(property, token);
            }
        }

        foreach (var collection in _layoutCollections)
        {
            collection.CollectionChanged -= OnNestedLayoutCollectionChanged;
        }

        foreach (var group in _groupDefinitions)
        {
            group.ChildrenChanged -= OnNestedLayoutCollectionChanged;
        }

        foreach (var definition in _controlDefinitions)
        {
            definition.PropertyChanged -= OnControlDefinitionPropertyChanged;
        }

        _layoutPropertyTokens.Clear();
        _layoutCollections.Clear();
        _groupDefinitions.Clear();
        _controlDefinitions.Clear();

        foreach (var layout in LayoutDefinitions)
        {
            var tokens = new List<(DependencyProperty, long)>();
            RegisterLayoutProperty(layout, RibbonToolBarLayoutDefinition.SizeProperty, tokens);
            RegisterLayoutProperty(layout, RibbonToolBarLayoutDefinition.SizeDefinitionProperty, tokens);
            RegisterLayoutProperty(layout, RibbonToolBarLayoutDefinition.RowCountProperty, tokens);
            RegisterLayoutProperty(layout, RibbonToolBarLayoutDefinition.ForSimplifiedProperty, tokens);
            _layoutPropertyTokens[layout] = tokens;

            SubscribeCollection(layout.Rows);
            foreach (var row in layout.Rows)
            {
                SubscribeCollection(row.Children);
                foreach (var child in row.Children)
                {
                    SubscribeDefinition(child);
                }
            }
        }
    }

    private void RegisterLayoutProperty(
        RibbonToolBarLayoutDefinition layout,
        DependencyProperty property,
        ICollection<(DependencyProperty Property, long Token)> tokens)
    {
        var token = layout.RegisterPropertyChangedCallback(
            property,
            (sender, changedProperty) => RebuildLayout());
        tokens.Add((property, token));
    }

    private void SubscribeCollection(INotifyCollectionChanged collection)
    {
        if (_layoutCollections.Add(collection))
        {
            collection.CollectionChanged += OnNestedLayoutCollectionChanged;
        }
    }

    private void SubscribeDefinition(DependencyObject definition)
    {
        if (definition is RibbonToolBarControlDefinition controlDefinition
            && _controlDefinitions.Add(controlDefinition))
        {
            controlDefinition.PropertyChanged += OnControlDefinitionPropertyChanged;
        }

        if (definition is RibbonToolBarControlGroupDefinition groupDefinition
            && _groupDefinitions.Add(groupDefinition))
        {
            groupDefinition.ChildrenChanged += OnNestedLayoutCollectionChanged;
            foreach (var child in groupDefinition.Children)
            {
                SubscribeDefinition(child);
            }
        }
    }

    private void OnNestedLayoutCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs args)
    {
        RefreshLayoutSubscriptions();
        RebuildLayout();
    }

    private void OnControlDefinitionPropertyChanged(
        object? sender,
        PropertyChangedEventArgs args)
        => RebuildLayout();

    /// <inheritdoc />
    public void OnSizePropertyChanged(
        RibbonControlSize previous,
        RibbonControlSize current)
        => RebuildLayout();

    /// <inheritdoc />
    public void UpdateSimplifiedState(bool isSimplified)
        => IsSimplified = isSimplified;

    private static void UpdateChildSimplifiedState(
        DependencyObject child,
        bool isSimplified)
    {
        if (child is ISimplifiedStateControl simplifiedControl)
        {
            simplifiedControl.UpdateSimplifiedState(isSimplified);
        }

        if (child is Panel panel)
        {
            foreach (var nestedChild in panel.Children)
            {
                UpdateChildSimplifiedState(nestedChild, isSimplified);
            }
        }
    }

    /// <inheritdoc />
    public override FrameworkElement CreateQuickAccessItem()
        => throw new NotImplementedException();

    /// <inheritdoc />
    public override KeyTipPressedResult OnKeyTipPressed() => KeyTipPressedResult.Empty;

    /// <inheritdoc />
    public override void OnKeyTipBack()
    {
    }

    /// <summary>
    /// Gets the number of portable visual children represented by the active layout.
    /// </summary>
    protected virtual int VisualChildrenCount =>
        _layoutPanel?.Children.Count ?? Children.Count;

    /// <summary>
    /// Gets a portable visual child from the active layout.
    /// </summary>
    protected virtual UIElement GetVisualChild(int index)
    {
        if (_layoutPanel is not null)
        {
            return _layoutPanel.Children[index];
        }

        return Children[index];
    }

    /// <summary>
    /// Gets the portable logical children collection.
    /// </summary>
    protected override IEnumerator LogicalChildren => Children.GetEnumerator();

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
        => base.MeasureOverride(availableSize);

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
        => base.ArrangeOverride(finalSize);

    void ILogicalChildSupport.AddLogicalChild(object child)
    {
        // WinUI logical ownership follows the template's visual tree.
    }

    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
        // WinUI logical ownership follows the template's visual tree.
    }
}
