namespace Fluent;

using Windows.Foundation;

/// <summary>
/// Contains the metadata used to display a KeyTip.
/// </summary>
public class KeyTipInformation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeyTipInformation"/> class.
    /// </summary>
    public KeyTipInformation(string keys, FrameworkElement associatedElement, bool hide)
    {
        if (string.IsNullOrEmpty(keys))
        {
            throw new ArgumentNullException(nameof(keys));
        }

        AssociatedElement = associatedElement ?? throw new ArgumentNullException(nameof(associatedElement));
        Keys = keys;
        VisualTarget = associatedElement;
        DefaultVisibility = hide ? Visibility.Collapsed : Visibility.Visible;
        KeyTip = new KeyTip
        {
            Content = keys,
            Visibility = DefaultVisibility,
            IsEnabled = true,
        };
        BackupVisibility = DefaultVisibility;
    }

    /// <summary>
    /// Gets the KeyTip key sequence.
    /// </summary>
    public string Keys { get; }

    /// <summary>
    /// Gets the associated element.
    /// </summary>
    public FrameworkElement AssociatedElement { get; }

    /// <summary>
    /// Gets or sets the visual placement target.
    /// </summary>
    public FrameworkElement VisualTarget { get; set; }

    /// <summary>
    /// Gets the initial visibility.
    /// </summary>
    public Visibility DefaultVisibility { get; }

    /// <summary>
    /// Gets the compatibility KeyTip instance.
    /// </summary>
    public KeyTip KeyTip { get; }

    /// <summary>
    /// Gets or sets the KeyTip position.
    /// </summary>
    public Point Position { get; set; }

    /// <summary>
    /// Gets or sets the backed-up visibility.
    /// </summary>
    public Visibility BackupVisibility { get; set; }

    /// <summary>
    /// Gets whether the KeyTip is visible.
    /// </summary>
    public bool IsVisible => KeyTip.IsVisible;

    /// <summary>
    /// Gets or sets the KeyTip visibility.
    /// </summary>
    public Visibility Visibility
    {
        get => KeyTip.Visibility;
        set => KeyTip.Visibility = value;
    }

    /// <summary>
    /// Gets whether the KeyTip can currently participate in navigation.
    /// This reflects changes made after the information object was created.
    /// </summary>
    public bool IsEnabled
        => KeyTip.IsEnabled
           && FocusRoutingHelper.IsEffectivelyVisible(AssociatedElement)
           && FocusRoutingHelper.IsEffectivelyEnabled(AssociatedElement);
}
