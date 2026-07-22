namespace Fluent;

/// <summary>
/// Represents a menu item in the <see cref="RibbonStatusBar"/>.
/// Acts as a toggleable item for showing/hiding status bar elements.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// </remarks>
public partial class StatusBarMenuItem : MenuItem
{
    private long _checkedPropertyToken;
    private long _titlePropertyToken;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(StatusBarMenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header text.
    /// </summary>
    public new object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
    public new static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(StatusBarMenuItem),
            new PropertyMetadata(true, OnIsCheckedChanged));

    /// <summary>
    /// Gets or sets whether the status bar item is visible.
    /// </summary>
    public new bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Identifies the <see cref="StatusBarItem"/> dependency property.</summary>
    public static readonly DependencyProperty StatusBarItemProperty =
        DependencyProperty.Register(
            nameof(StatusBarItem),
            typeof(StatusBarItem),
            typeof(StatusBarMenuItem),
            new PropertyMetadata(null, OnStatusBarItemChanged));

    /// <summary>
    /// Gets or sets the linked status bar item whose visibility is controlled.
    /// </summary>
    public StatusBarItem? StatusBarItem
    {
        get => (StatusBarItem?)GetValue(StatusBarItemProperty);
        set => SetValue(StatusBarItemProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusBarMenuItem"/> class.
    /// </summary>
    public StatusBarMenuItem()
    {
        DefaultStyleKey = typeof(StatusBarMenuItem);
    }

    /// <summary>
    /// Initializes a menu item linked to a status-bar item.
    /// </summary>
    public StatusBarMenuItem(StatusBarItem item)
        : this()
    {
        StatusBarItem = item;
    }

    #endregion

    #region Methods

    private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusBarMenuItem menuItem && menuItem.StatusBarItem is not null)
        {
            menuItem.StatusBarItem.IsChecked = (bool)e.NewValue;
        }
    }

    private static void OnStatusBarItemChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var menuItem = (StatusBarMenuItem)sender;
        if (args.OldValue is StatusBarItem oldItem)
        {
            if (menuItem._checkedPropertyToken != 0)
            {
                oldItem.UnregisterPropertyChangedCallback(
                    StatusBarItem.IsCheckedProperty,
                    menuItem._checkedPropertyToken);
                menuItem._checkedPropertyToken = 0;
            }

            if (menuItem._titlePropertyToken != 0)
            {
                oldItem.UnregisterPropertyChangedCallback(
                    StatusBarItem.TitleProperty,
                    menuItem._titlePropertyToken);
                menuItem._titlePropertyToken = 0;
            }
        }

        if (args.NewValue is not StatusBarItem statusItem)
        {
            return;
        }

        menuItem.Header = statusItem.Title ?? statusItem.Content;
        menuItem.IsChecked = statusItem.IsChecked;
        menuItem.IsEnabled = IsStatusItemCheckable(statusItem);

        menuItem._checkedPropertyToken = statusItem.RegisterPropertyChangedCallback(
            StatusBarItem.IsCheckedProperty,
            (item, property) =>
            {
                menuItem.IsChecked = ((StatusBarItem)item).IsChecked;
            });

        menuItem._titlePropertyToken = statusItem.RegisterPropertyChangedCallback(
            StatusBarItem.TitleProperty,
            (item, property) =>
            {
                var target = (StatusBarItem)item;
                menuItem.Header = target.Title ?? target.Content;
            });
    }

    private static bool IsStatusItemCheckable(StatusBarItem item)
    {
        var property = item.GetType().GetProperty("IsCheckable");
        return property?.PropertyType != typeof(bool)
               || (bool)(property.GetValue(item) ?? true);
    }

    /// <inheritdoc/>
    protected override void OnInvoke()
    {
        if (StatusBarItem is null || !IsStatusItemCheckable(StatusBarItem))
        {
            return;
        }

        StatusBarItem.IsChecked = !StatusBarItem.IsChecked;
        IsChecked = StatusBarItem.IsChecked;
    }

    internal void InvokeForAutomation() => OnInvoke();

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.StatusBarMenuItemAutomationPeer(this);

    #endregion
}
