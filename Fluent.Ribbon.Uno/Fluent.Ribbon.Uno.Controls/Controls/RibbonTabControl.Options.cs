namespace Fluent;

using WinUIButton = Microsoft.UI.Xaml.Controls.Button;

public partial class RibbonTabControl
{
    private FrameworkElement? _tabHeaderStrip;
    private WinUIButton? _displayOptionsButton;
    private MenuFlyout? _displayOptionsFlyout;

    private void InitializeDisplayOptions()
    {
        RibbonLocalizationUpdateHelper.Track(this, RefreshDisplayOptionsMetadata);
        Unloaded += (_, _) => CloseDisplayOptions();
    }

    private void UpdateDisplayOptionsTemplateParts()
    {
        CloseDisplayOptions();
        if (_displayOptionsButton is not null)
        {
            _displayOptionsButton.Click -= OnDisplayOptionsButtonClick;
        }

        _tabHeaderStrip = GetTemplateChild("TabListView") as FrameworkElement
                          ?? GetTemplateChild(PART_ItemsPresenter) as FrameworkElement
                          ?? GetTemplateChild("PART_TabsContainer") as FrameworkElement;
        _displayOptionsButton = GetTemplateChild("PART_DisplayOptionsButton") as WinUIButton;
        if (_displayOptionsButton is not null)
        {
            _displayOptionsButton.Click += OnDisplayOptionsButtonClick;
        }

        UpdateTabHeaderVisibility();
        UpdateDisplayOptionsVisibility();
        RefreshDisplayOptionsMetadata();
    }

    private static void OnAreTabHeadersVisibleChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var tabControl = (RibbonTabControl)sender;
        tabControl.UpdateTabHeaderVisibility();
        tabControl.UpdateSelectedContent();
    }

    private void UpdateTabHeaderVisibility()
    {
        // Collapsing TabViewItems themselves also removes them from selectable content.
        // The list is only the header strip; the ribbon owns its separate content presenter.
        if (_tabHeaderStrip is not null)
        {
            _tabHeaderStrip.Visibility = AreTabHeadersVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static void OnDisplayOptionsAvailabilityChanged(
        DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        var tabControl = (RibbonTabControl)sender;
        tabControl.CloseDisplayOptions();
        tabControl.UpdateDisplayOptionsVisibility();
    }

    private void UpdateDisplayOptionsVisibility()
    {
        if (_displayOptionsButton is not null)
        {
            _displayOptionsButton.Visibility = IsDisplayOptionsButtonVisible
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void RefreshDisplayOptionsMetadata()
    {
        if (_displayOptionsButton is not null)
        {
            var localization = RibbonLocalization.Current.Localization;
            Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
                _displayOptionsButton, localization.DisplayOptionsButtonScreenTipTitle);
            Fluent.Automation.Peers.AutomationPeerHelpers.SetToolTipIfUnsetOrGenerated(
                _displayOptionsButton, localization.DisplayOptionsButtonScreenTipText);
        }

        CloseDisplayOptions();
    }

    private void OnDisplayOptionsButtonClick(object sender, RoutedEventArgs args) => OpenDisplayOptions();

    private bool CanOpenDisplayOptions() =>
        IsLoaded
        && XamlRoot is not null
        && IsDisplayOptionsButtonVisible
        && FocusRoutingHelper.IsEffectivelyEnabled(this)
        && FocusRoutingHelper.IsEffectivelyVisible(this)
        && _displayOptionsButton is { IsEnabled: true, Visibility: Visibility.Visible, Flyout: null };

    internal MenuFlyout? OpenDisplayOptions()
    {
        if (!CanOpenDisplayOptions())
        {
            return null;
        }

        CloseDisplayOptions();
        var flyout = new MenuFlyout();
        var localization = RibbonLocalization.Current.Localization;
        if (CanMinimize)
        {
            flyout.Items.Add(new MenuFlyoutItem { Text = localization.ShowRibbon, IsEnabled = false });
            AddOption(localization.ExpandRibbon, isChecked: !IsMinimized, simplified: false, value: false);
            AddOption(localization.MinimizeRibbon, isChecked: IsMinimized, simplified: false, value: true);
        }

        if (CanUseSimplified)
        {
            if (flyout.Items.Count > 0)
            {
                flyout.Items.Add(new MenuFlyoutSeparator());
            }

            flyout.Items.Add(new MenuFlyoutItem { Text = localization.RibbonLayout, IsEnabled = false });
            AddOption(localization.UseClassicRibbon, isChecked: !IsSimplified, simplified: true, value: false);
            AddOption(localization.UseSimplifiedRibbon, isChecked: IsSimplified, simplified: true, value: true);
        }

        if (flyout.Items.Count == 0)
        {
            return null;
        }

        _displayOptionsFlyout = flyout;
        FlyoutShowHelper.ShowDeferred(flyout, _displayOptionsButton,
            () => ReferenceEquals(_displayOptionsFlyout, flyout) && CanOpenDisplayOptions());
        return flyout;

        void AddOption(string label, bool isChecked, bool simplified, bool value)
        {
            flyout.Items.Add(new ToggleMenuFlyoutItem
            {
                Text = label,
                IsChecked = isChecked,
                Command = Ribbon.CreateGuardedMenuCommand(
                    label,
                    () => CanOpenDisplayOptions() && (simplified ? CanUseSimplified : CanMinimize),
                    () => ApplyDisplayOption(simplified, value)),
            });
        }
    }

    private void ApplyDisplayOption(bool simplified, bool value)
    {
        CloseDisplayOptions();
        if (QuickAccessHelper.FindOwningRibbon(this) is { } ribbon
            && ReferenceEquals(ribbon.TabControl, this))
        {
            if (simplified)
            {
                ribbon.IsSimplified = value;
            }
            else
            {
                ribbon.IsMinimized = value;
            }
        }
        else if (simplified)
        {
            IsSimplified = value;
        }
        else
        {
            IsMinimized = value;
        }
    }

    private void CloseDisplayOptions()
    {
        _displayOptionsFlyout?.Hide();
        _displayOptionsFlyout = null;
    }
}
