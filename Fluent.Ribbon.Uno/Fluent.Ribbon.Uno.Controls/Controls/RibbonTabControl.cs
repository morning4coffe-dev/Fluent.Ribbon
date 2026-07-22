namespace Fluent;

/// <summary>
/// Represents a tab control specifically designed for the Ribbon.
/// </summary>
[TemplatePart(Name = PART_ItemsPresenter, Type = typeof(ItemsPresenter))]
[TemplatePart(Name = PART_ContentPresenter, Type = typeof(ContentPresenter))]
public partial class RibbonTabControl : TabView
{
    private const string PART_ItemsPresenter = "PART_ItemsPresenter";
    private const string PART_ContentPresenter = "PART_ContentPresenter";
    // Backing field for the content presenter part from the control template
    private ContentPresenter? _contentPresenter;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="IsMinimized"/> dependency property.</summary>
    public static readonly DependencyProperty IsMinimizedProperty =
        DependencyProperty.Register(
            nameof(IsMinimized),
            typeof(bool),
            typeof(RibbonTabControl),
            new PropertyMetadata(false, OnIsMinimizedChanged));

    /// <summary>
    /// Gets or sets whether the tab control content is minimized.
    /// </summary>
    public bool IsMinimized
    {
        get => (bool)GetValue(IsMinimizedProperty);
        set => SetValue(IsMinimizedProperty, value);
    }

    /// <summary>Identifies the <see cref="ContentHeight"/> dependency property.</summary>
    public static readonly DependencyProperty ContentHeightProperty =
        DependencyProperty.Register(
            nameof(ContentHeight),
            typeof(double),
            typeof(RibbonTabControl),
            new PropertyMetadata(94.0));

    /// <summary>
    /// Gets or sets the height of the content area.
    /// </summary>
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        set => SetValue(ContentHeightProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonTabControl"/> class.
    /// </summary>
    public RibbonTabControl()
    {
        // Use TabView's default style/template — no custom template needed
        IsAddTabButtonVisible = false;
        TabWidthMode = TabViewWidthMode.SizeToContent;
        InitializeCompatibility();
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _contentPresenter = GetTemplateChild(PART_ContentPresenter) as ContentPresenter;
        UpdateCompatibilityTemplateParts();
        UpdateMinimizedState();
    }

    private static void OnIsMinimizedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonTabControl tabControl)
        {
            tabControl.OnMinimizedCompatibilityChanged((bool)e.NewValue);
            tabControl.UpdateMinimizedState();
        }
    }

    private void UpdateMinimizedState()
    {
        VisualStateManager.GoToState(this, IsMinimized ? "Minimized" : "Normal", true);

        // Manually find the content presenter in TabView if template doesn't handle it
        if (_contentPresenter is not null)
        {
            _contentPresenter.Visibility = IsMinimized && !IsDropDownOpen
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }

    #endregion

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonTabControlAutomationPeer(this);
}
