using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace FluentRibbon.Uno.Showcase;

public sealed partial class MainPage : Page
{
    private int _transitionCounter;

    public MainPage()
    {
        this.InitializeComponent();
        SampleColorGallery.SelectedColorChanged += OnColorGallerySelectionChanged;
        UpdateLocalizationSample();
    }

    private void OnToggleTableTools(object sender, RoutedEventArgs e)
    {
        TableToolsGroup.Visibility = TableToolsGroup.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnOpenBackstage(object sender, RoutedEventArgs e)
    {
        BackstageView.IsOpen = true;
    }

    private void OnToggleMinimize(object sender, RoutedEventArgs e)
    {
        MainRibbon.ToggleMinimize();
    }

    private void OnToggleSimplified(object sender, RoutedEventArgs e)
    {
        MainRibbon.IsSimplified = !MainRibbon.IsSimplified;
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            var theme = toggleButton.IsChecked == true
                ? ElementTheme.Dark
                : ElementTheme.Light;

            RequestedTheme = theme;

            // Also set on the XamlRoot content to propagate theme to all controls
            if (XamlRoot?.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = theme;
            }
        }
    }

    private void OnShowStartScreen(object sender, RoutedEventArgs e)
    {
        SampleStartScreen.IsOpen = !SampleStartScreen.IsOpen;
    }

    private void OnChangeTransitionContent(object sender, RoutedEventArgs e)
    {
        _transitionCounter++;
        var messages = new[]
        {
            "Hello from TransitioningControl!",
            "Content changed with animation ✨",
            "Smooth transitions between states",
            "Ported from WPF Fluent.Ribbon 🎉",
            "Works on all Uno platforms!"
        };
        SampleTransition.Content = new TextBlock
        {
            Text = messages[_transitionCounter % messages.Length],
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private void OnColorGallerySelectionChanged(object? sender, Windows.UI.Color? color)
    {
        if (color.HasValue)
        {
            var c = color.Value;
            SelectedColorText.Text = $"Selected: #{c.R:X2}{c.G:X2}{c.B:X2} (R:{c.R} G:{c.G} B:{c.B})";
        }
        else
        {
            SelectedColorText.Text = "Selected: (none)";
        }
    }

    private void OnSetLanguageEnglish(object sender, RoutedEventArgs e) => SetLanguage("en");
    private void OnSetLanguageGerman(object sender, RoutedEventArgs e) => SetLanguage("de");
    private void OnSetLanguageFrench(object sender, RoutedEventArgs e) => SetLanguage("fr");
    private void OnSetLanguageSpanish(object sender, RoutedEventArgs e) => SetLanguage("es");
    private void OnSetLanguageChinese(object sender, RoutedEventArgs e) => SetLanguage("zh");
    private void OnSetLanguageJapanese(object sender, RoutedEventArgs e) => SetLanguage("ja");

    private void SetLanguage(string cultureCode)
    {
        RibbonLocalization.Current.Culture = new System.Globalization.CultureInfo(cultureCode);
        UpdateLocalizationSample();
    }

    private void UpdateLocalizationSample()
    {
        var loc = RibbonLocalization.Current.Localization;
        if (loc is not null)
        {
            LocalizationSample.Text =
                $"BackstageButtonText: \"{loc.BackstageButtonText}\" | " +
                $"MinimizeRibbon: \"{loc.MinimizeRibbon}\" | " +
                $"MoreColors: \"{loc.MoreColors}\" | " +
                $"QuickAccessToolBarMenuShowAbove: \"{loc.QuickAccessToolBarMenuShowAbove}\"";
        }
    }
    private async void OnLauncherClick(object sender, RoutedEventArgs e)
    {
        var group = sender as RibbonGroupBox;
        var dialog = new ContentDialog
        {
            Title = group?.Header?.ToString() ?? "Settings",
            Content = "Dialog launcher clicked for " + (group?.Header?.ToString() ?? "this group") + ".",
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        _ = dialog.ShowAsync();
    }
}
