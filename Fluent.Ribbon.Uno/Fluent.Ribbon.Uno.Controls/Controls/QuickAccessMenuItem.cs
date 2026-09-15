namespace Fluent;

/// <summary>
/// Represents a menu item for adding/removing items from the quick access toolbar.
/// </summary>
public partial class QuickAccessMenuItem : MenuItem, IHeaderedControl
{
    private WeakReference<Ribbon>? _customizationRibbon;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public new static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
            nameof(Header),
            typeof(object),
            typeof(QuickAccessMenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the header/label of the menu item.
    /// </summary>
    public new object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Target"/> dependency property.</summary>
    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register(
            nameof(Target),
            typeof(UIElement),
            typeof(QuickAccessMenuItem),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the target element to add/remove from the quick access toolbar.
    /// </summary>
    /// <remarks>
    /// While owned by a ribbon, an initially checked entry can wait for its target to be assigned or bound.
    /// Clearing a resolved target releases the entry's quick-access request and unchecks it.
    /// </remarks>
    public UIElement? Target
    {
        get => (UIElement?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    /// <summary>Identifies the <see cref="IsChecked"/> dependency property.</summary>
    public new static readonly DependencyProperty IsCheckedProperty =
        DependencyProperty.Register(
            nameof(IsChecked),
            typeof(bool),
            typeof(QuickAccessMenuItem),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets whether the item is checked (shown in the QAT).
    /// </summary>
    /// <remarks>
    /// While Target is unresolved, a checked entry retains its request until a target arrives.
    /// Programmatic changes remain available when the ribbon disables user item customization.
    /// </remarks>
    public new bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickAccessMenuItem"/> class.
    /// </summary>
    public QuickAccessMenuItem()
    {
        DefaultStyleKey = typeof(QuickAccessMenuItem);
    }

    #endregion

    #region Methods

    /// <inheritdoc/>
    protected override void OnInvoke() => OnClick();

    /// <inheritdoc/>
    protected override bool IsEnabledCore => base.IsEnabledCore && CanCustomizeTarget();

    /// <inheritdoc/>
    protected override void OnClick()
    {
        if (!CanInvoke)
        {
            return;
        }

        IsChecked = !IsChecked;
        InvokeItem();
        if (IsDefinitive)
        {
            PopupService.RaiseDismissPopupEvent(this, DismissPopupMode.Always);
        }
    }

    internal void SetCustomizationRibbon(Ribbon ribbon) =>
        _customizationRibbon = new WeakReference<Ribbon>(ribbon);

    internal void ReleaseCustomizationRibbon(Ribbon ribbon)
    {
        if (_customizationRibbon?.TryGetTarget(out var owner) == true && ReferenceEquals(owner, ribbon))
        {
            _customizationRibbon = null;
        }
    }

    private bool CanCustomizeTarget()
    {
        var ribbon = _customizationRibbon?.TryGetTarget(out var owner) == true
            ? owner
            : QuickAccessHelper.FindOwningRibbon(this);
        return ribbon is null
               || (Target is { } target && ribbon.CanCustomizeQuickAccessItem(target, add: !IsChecked));
    }

    #endregion
}
