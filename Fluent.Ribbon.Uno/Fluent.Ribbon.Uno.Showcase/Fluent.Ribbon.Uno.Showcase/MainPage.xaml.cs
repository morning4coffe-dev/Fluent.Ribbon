using System.Collections.ObjectModel;
using System.Linq;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace FluentRibbon.Uno.Showcase;

public sealed partial class MainPage : Page
{
    private int _transitionCounter;

    // Shared collection for the Insert tab's two font combos; "Add item to fonts" appends here.
    private readonly ObservableCollection<string> _fontsData = new();

    // Sample palette mirrors the WPF Showcase gallery colors (Blue/Brown/Gray/Green/Orange/Pink/Red/Yellow).
    private static readonly (string Name, Windows.UI.Color Color)[] Palette =
    {
        ("Blue", Rgb(0x00, 0x78, 0xD4)),
        ("Brown", Rgb(0xA0, 0x52, 0x2D)),
        ("Gray", Rgb(0x9E, 0x9E, 0x9E)),
        ("Green", Rgb(0x10, 0x7C, 0x10)),
        ("Orange", Rgb(0xFF, 0x8C, 0x00)),
        ("Pink", Rgb(0xE3, 0x00, 0x8C)),
        ("Red", Rgb(0xE8, 0x11, 0x23)),
        ("Yellow", Rgb(0xFF, 0xB9, 0x00)),
    };

    public MainPage()
    {
        this.InitializeComponent();
        SampleColorGallery.SelectedColorChanged += OnColorGallerySelectionChanged;
        UpdateLocalizationSample();

        // The RibbonComboBox template hosts an inner ComboBox bound to ItemsSource, so
        // populate via ItemsSource (not inline items) to mirror the WPF "Toolbars" tab.
        fontNameCombo.ItemsSource = new[] { "Arial", "Calibri", "Segoe UI", "Tahoma", "Times New Roman" };
        fontSizeCombo.ItemsSource = new[] { "8", "10", "11", "12", "14", "16", "18", "24", "36", "72" };
        fontNameCombo.SelectedIndex = 0;
        fontSizeCombo.SelectedIndex = 3;

        InitializeShowcaseTabs();
        InitializeDiagnostics();
    }

    // Seeds the cloned WPF Showcase tabs (Insert/Tests/Galleries/Binding). RibbonComboBox and
    // InRibbonGallery host their content via ItemsSource / .Items, so populate them in code-behind.
    private void InitializeShowcaseTabs()
    {
        foreach (var font in new[]
                 {
                     "Arial", "Calibri", "Cambria", "Consolas", "Georgia",
                     "Segoe UI", "Tahoma", "Times New Roman", "Verdana",
                 })
        {
            _fontsData.Add(font);
        }

        insertFontsCombo1.ItemsSource = _fontsData;
        insertFontsCombo2.ItemsSource = _fontsData;
        insertFontsCombo1.SelectedIndex = 0;
        insertFontsCombo2.SelectedIndex = 0;

        var navItems = Enumerable.Range(1, 10).Select(i => $"Item {i}").ToArray();
        insertKbNav1.ItemsSource = navItems;
        insertKbNav2.ItemsSource = navItems;
        insertKbNav1.SelectedIndex = 0;
        insertKbNav2.SelectedIndex = 0;

        sharedSizeCombo.ItemsSource = Enumerable.Range(1, 20).Select(i => $"Many items entry {i}").ToArray();
        sharedSizeCombo.SelectedIndex = 0;

        for (var i = 1; i <= 9; i++)
        {
            InsertSplitGallery.Items.Add(MakeTextTile(i.ToString()));
        }

        foreach (var group in new[] { "AAAA", "BBBB", "CCCC", "DDDD", "EEEE", "FFFF", "GGGG" })
        {
            for (var i = 0; i < 5; i++)
            {
                InsertManyItemsGallery.Items.Add(MakeTextTile(group));
            }
        }

        SeedColorGallery(GalWithoutGrouping, 12);
        SeedColorGallery(GalGrouped, 16);
        SeedColorGallery(GalInRibbon, 8);
        SeedColorGallery(GalVertical, 6);

        foreach (var (name, color) in Palette)
        {
            BoundItemsGroup.Items.Add(new RibbonButton
            {
                Header = name,
                Size = RibbonControlSize.Medium,
                IconGlyph = "\uE790",
                Foreground = new SolidColorBrush(color),
            });
        }
    }

    private void HandleAddItemToFontsClick(object sender, RoutedEventArgs e)
    {
        _fontsData.Add($"Added item {_fontsData.Count}");
    }

    private void OnEnlargeClick(object sender, RoutedEventArgs e)
    {
        InsertSplitGallery.Enlarge();
    }

    private void OnReduceClick(object sender, RoutedEventArgs e)
    {
        InsertSplitGallery.Reduce();
    }

    private void SeedColorGallery(InRibbonGallery gallery, int count)
    {
        for (var i = 0; i < count; i++)
        {
            gallery.Items.Add(MakeColorTile(Palette[i % Palette.Length].Color));
        }
    }

    private static Border MakeColorTile(Windows.UI.Color color) => new()
    {
        Background = new SolidColorBrush(color),
        CornerRadius = new CornerRadius(4),
        Margin = new Thickness(1),
    };

    private static Border MakeTextTile(string text) => new()
    {
        Background = new SolidColorBrush(Rgb(0xF3, 0xF3, 0xF3)),
        BorderBrush = new SolidColorBrush(Rgb(0xD0, 0xD0, 0xD0)),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(4),
        Margin = new Thickness(1),
        Child = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Rgb(0x20, 0x20, 0x20)),
            FontSize = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };

    private static Windows.UI.Color Rgb(byte r, byte g, byte b) => Windows.UI.Color.FromArgb(0xFF, r, g, b);

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
