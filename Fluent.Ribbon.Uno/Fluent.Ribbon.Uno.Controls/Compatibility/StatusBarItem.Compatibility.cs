namespace Fluent;

public partial class StatusBarItem
{
    private Visibility visibilityBeforeUnchecked = Visibility.Visible;

    /// <summary>Identifies the status value property.</summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(string),
            typeof(StatusBarItem),
            new PropertyMetadata(null, OnValueChanged));

    /// <summary>Gets or sets the status value.</summary>
    public string? Value
    {
        get => (string?)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Identifies whether the item may be shown or hidden by status-bar customization.</summary>
    public static readonly DependencyProperty IsCheckableProperty =
        DependencyProperty.Register(
            nameof(IsCheckable),
            typeof(bool),
            typeof(StatusBarItem),
            new PropertyMetadata(true));

    /// <summary>Gets or sets whether the item may be checked.</summary>
    public bool IsCheckable
    {
        get => (bool)GetValue(IsCheckableProperty);
        set => SetValue(IsCheckableProperty, value);
    }

    /// <summary>Occurs when the item becomes checked.</summary>
    public event RoutedEventHandler? Checked;

    /// <summary>Occurs when the item becomes unchecked.</summary>
    public event RoutedEventHandler? Unchecked;

    private void InitializeCompatibility()
    {
        RegisterPropertyChangedCallback(
            IsCheckedProperty,
            static (sender, _) => ((StatusBarItem)sender).OnCheckedStateChanged());
    }

    private static void OnValueChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var item = (StatusBarItem)sender;
        if (item.Content is null)
        {
            item.Content = args.NewValue;
        }
    }

    private void OnCheckedStateChanged()
    {
        if (IsChecked)
        {
            if (Visibility == Visibility.Collapsed)
            {
                Visibility = visibilityBeforeUnchecked;
            }

            Checked?.Invoke(this, new RoutedEventArgs());
        }
        else
        {
            if (Visibility != Visibility.Collapsed)
            {
                visibilityBeforeUnchecked = Visibility;
            }

            Visibility = Visibility.Collapsed;
            Unchecked?.Invoke(this, new RoutedEventArgs());
        }
    }

    /// <inheritdoc />
    protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
        => new Fluent.Automation.Peers.RibbonStatusBarItemAutomationPeer(this);
}
