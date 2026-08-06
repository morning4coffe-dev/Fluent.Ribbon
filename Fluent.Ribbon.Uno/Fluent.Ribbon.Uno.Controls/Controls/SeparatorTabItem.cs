namespace Fluent;

/// <summary>
/// Represents a visual separator between tab items in the ribbon tab strip.
/// </summary>
public partial class SeparatorTabItem : TabViewItem
{
    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SeparatorTabItem"/> class.
    /// </summary>
    public SeparatorTabItem()
    {
        DefaultStyleKey = typeof(SeparatorTabItem);
        IsTabStop = false;
        IsEnabled = false;
        IsEnabledChanged += OnIsEnabledChanged;
        Width = 1;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    #endregion

    private void OnIsEnabledChanged(
        object sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (IsEnabled)
        {
            IsEnabled = false;
        }
    }

    /// <inheritdoc/>
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.SeparatorTabItemAutomationPeer(this);
}
