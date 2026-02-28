namespace Fluent;

/// <summary>
/// Represents the Start Screen view displayed on application startup.
/// Contains a left pane with recent documents and a right pane with templates/actions.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
[ContentProperty(Name = nameof(Content))]
[TemplatePart(Name = PART_LeftPane, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PART_RightPane, Type = typeof(ContentPresenter))]
public partial class StartScreen : Control
{
    private const string PART_LeftPane = "PART_LeftPane";
    private const string PART_RightPane = "PART_RightPane";

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Content"/> dependency property.</summary>
    public static readonly DependencyProperty ContentProperty =
        DependencyProperty.Register(
            nameof(Content),
            typeof(object),
            typeof(StartScreen),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the main content (right pane).
    /// </summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Identifies the <see cref="LeftPaneContent"/> dependency property.</summary>
    public static readonly DependencyProperty LeftPaneContentProperty =
        DependencyProperty.Register(
            nameof(LeftPaneContent),
            typeof(object),
            typeof(StartScreen),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the left pane content.
    /// </summary>
    public object? LeftPaneContent
    {
        get => GetValue(LeftPaneContentProperty);
        set => SetValue(LeftPaneContentProperty, value);
    }

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(StartScreen),
            new PropertyMetadata(false, OnIsOpenChanged));

    /// <summary>
    /// Gets or sets whether the start screen is open.
    /// </summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Identifies the <see cref="LeftPaneWidth"/> dependency property.</summary>
    public static readonly DependencyProperty LeftPaneWidthProperty =
        DependencyProperty.Register(
            nameof(LeftPaneWidth),
            typeof(double),
            typeof(StartScreen),
            new PropertyMetadata(300.0));

    /// <summary>
    /// Gets or sets the width of the left pane.
    /// </summary>
    public double LeftPaneWidth
    {
        get => (double)GetValue(LeftPaneWidthProperty);
        set => SetValue(LeftPaneWidthProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StartScreen"/> class.
    /// </summary>
    public StartScreen()
    {
        DefaultStyleKey = typeof(StartScreen);
    }

    #endregion

    #region Methods

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StartScreen screen)
        {
            VisualStateManager.GoToState(screen, (bool)e.NewValue ? "Open" : "Closed", true);
        }
    }

    #endregion
}
