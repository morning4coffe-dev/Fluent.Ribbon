namespace Fluent.Modern.Controls;

using Fluent.Modern.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// <para><b>Modern extension</b> — hosts a WinUI InfoBar for contextual ribbon notifications.</para>
/// </summary>
[ModernExtension]
[TemplatePart(Name = InfoBarPartName, Type = typeof(InfoBar))]
public partial class RibbonInfoBarHost : Control
{
    private const string InfoBarPartName = "PART_InfoBar";
    private InfoBar? infoBar;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(RibbonInfoBarHost),
            new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Message"/> dependency property.</summary>
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(
            nameof(Message),
            typeof(string),
            typeof(RibbonInfoBarHost),
            new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Severity"/> dependency property.</summary>
    public static readonly DependencyProperty SeverityProperty =
        DependencyProperty.Register(
            nameof(Severity),
            typeof(InfoBarSeverity),
            typeof(RibbonInfoBarHost),
            new PropertyMetadata(InfoBarSeverity.Informational));

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(RibbonInfoBarHost),
            new PropertyMetadata(false, OnIsOpenChanged));

    /// <summary>Identifies the <see cref="IsClosable"/> dependency property.</summary>
    public static readonly DependencyProperty IsClosableProperty =
        DependencyProperty.Register(
            nameof(IsClosable),
            typeof(bool),
            typeof(RibbonInfoBarHost),
            new PropertyMetadata(true));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the notification message.
    /// </summary>
    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the notification severity.
    /// </summary>
    public InfoBarSeverity Severity
    {
        get => (InfoBarSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the notification is open.
    /// </summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the notification can be closed by the user.
    /// </summary>
    public bool IsClosable
    {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonInfoBarHost"/> class.
    /// </summary>
    public RibbonInfoBarHost()
    {
        DefaultStyleKey = typeof(RibbonInfoBarHost);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        if (infoBar is not null)
        {
            infoBar.Closed -= OnInfoBarClosed;
        }

        base.OnApplyTemplate();
        infoBar = GetTemplateChild(InfoBarPartName) as InfoBar;
        if (infoBar is not null)
        {
            infoBar.Closed += OnInfoBarClosed;
            infoBar.IsOpen = IsOpen;
        }
    }

    private static void OnIsOpenChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (sender is RibbonInfoBarHost host && host.infoBar is not null)
        {
            host.infoBar.IsOpen = (bool)args.NewValue;
        }
    }

    private void OnInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        IsOpen = false;
    }

    internal InfoBar? InfoBarForTesting => infoBar;

    #endregion

    #region Automation

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
        // Modern a11y: custom automation peer.
        => new RibbonInfoBarHostAutomationPeer(this);

    #endregion

    #region Methods

    /// <summary>
    /// Shows a contextual ribbon notification.
    /// </summary>
    /// <param name="severity">The notification severity.</param>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    public void Show(InfoBarSeverity severity, string title, string message)
    {
        Severity = severity;
        Title = title;
        Message = message;
        IsOpen = true;
    }

    #endregion
}