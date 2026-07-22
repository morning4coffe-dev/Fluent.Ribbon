namespace Fluent.Modern.Controls;

using Fluent;
using Fluent.Modern.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// <para><b>Modern extension</b> — a ribbon button with first-class WinUI <see cref="IconSource"/> support.</para>
/// </summary>
[ModernExtension]
public partial class ModernRibbonButton : RibbonButton
{
    #region Dependency Properties

    /// <summary>Identifies the <see cref="LargeIconSource"/> dependency property.</summary>
    public static readonly DependencyProperty LargeIconSourceProperty =
        DependencyProperty.Register(
            nameof(LargeIconSource),
            typeof(IconSource),
            typeof(ModernRibbonButton),
            new PropertyMetadata(null, OnIconSourceChanged));

    /// <summary>Identifies the <see cref="SmallIconSource"/> dependency property.</summary>
    public static readonly DependencyProperty SmallIconSourceProperty =
        DependencyProperty.Register(
            nameof(SmallIconSource),
            typeof(IconSource),
            typeof(ModernRibbonButton),
            new PropertyMetadata(null, OnIconSourceChanged));

    /// <summary>Identifies the <see cref="CurrentIconSource"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentIconSourceProperty =
        DependencyProperty.Register(
            nameof(CurrentIconSource),
            typeof(IconSource),
            typeof(ModernRibbonButton),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="HasIconSource"/> dependency property.</summary>
    public static readonly DependencyProperty HasIconSourceProperty =
        DependencyProperty.Register(
            nameof(HasIconSource),
            typeof(bool),
            typeof(ModernRibbonButton),
            new PropertyMetadata(false));

    #endregion

    private readonly long _sizeChangedToken;

    #region Properties

    /// <summary>
    /// Gets or sets the large modern icon source.
    /// </summary>
    public IconSource? LargeIconSource
    {
        get => (IconSource?)GetValue(LargeIconSourceProperty);
        set => SetValue(LargeIconSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the small/medium modern icon source.
    /// </summary>
    public IconSource? SmallIconSource
    {
        get => (IconSource?)GetValue(SmallIconSourceProperty);
        set => SetValue(SmallIconSourceProperty, value);
    }

    /// <summary>
    /// Gets the active modern icon source for the current ribbon size.
    /// </summary>
    public IconSource? CurrentIconSource
    {
        get => (IconSource?)GetValue(CurrentIconSourceProperty);
        private set => SetValue(CurrentIconSourceProperty, value);
    }

    /// <summary>
    /// Gets a value indicating whether a modern icon source is active.
    /// </summary>
    public bool HasIconSource
    {
        get => (bool)GetValue(HasIconSourceProperty);
        private set => SetValue(HasIconSourceProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ModernRibbonButton"/> class.
    /// </summary>
    public ModernRibbonButton()
    {
        DefaultStyleKey = typeof(ModernRibbonButton);
        _sizeChangedToken = RegisterPropertyChangedCallback(SizeProperty, OnSizePropertyChanged);
        UpdateCurrentIconSource();
    }

    #endregion

    #region Automation

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
        // Modern a11y: custom automation peer.
        => new ModernRibbonButtonAutomationPeer(this);

    #endregion

    #region Methods

    private static void OnIconSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ModernRibbonButton)d).UpdateCurrentIconSource();
    }

    private void OnSizePropertyChanged(DependencyObject sender, DependencyProperty dp)
    {
        UpdateCurrentIconSource();
    }

    private void UpdateCurrentIconSource()
    {
        CurrentIconSource = Size == RibbonControlSize.Large
            ? LargeIconSource ?? SmallIconSource
            : SmallIconSource ?? LargeIconSource;

        HasIconSource = CurrentIconSource is not null;
    }

    #endregion
}
