using System.Collections.ObjectModel;
using System.Linq;
using Fluent;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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
        App.LogAutoTestStartup("MAIN PAGE CONSTRUCTOR BEGIN");
        this.InitializeComponent();
        App.LogAutoTestStartup("MAIN PAGE XAML INITIALIZED");
        qatPink.Target = pinkButton;
        qatPaste.Target = pasteButton;
        qatCut.Target = cutButton;
        qatCopy.Target = copyButton;
        qatGallery.Target = GalInRibbon;
        qatBold.Target = btnBold;
        SampleColorGallery.SelectedColorChanged += OnColorGallerySelectionChanged;
        UpdateLocalizationSample();

        // Populate via ItemsSource to mirror the WPF "Toolbars" tab.
        foreach (var item in new object[]
        {
            CreateComboBoxGroupHeader("Theme Fonts"),
            "Arial",
            "Tahoma",
            CreateComboBoxGroupHeader("Recent Used Fonts"),
            "Tahoma",
            CreateComboBoxGroupHeader("All Fonts"),
            "Arial",
            "Segoe UI",
            "Tahoma",
            "Webdings",
            "Winding",
        })
        {
            fontNameCombo.Items.Add(item);
        }

        fontSizeCombo.ItemsSource = new[]
        {
            "7", "8", "9", "10", "11", "12", "14", "16", "18",
            "20", "22", "24", "28", "32", "36", "48", "72",
        };
        fontNameCombo.SelectedIndex = 1;
        fontSizeCombo.SelectedIndex = 1;

        InitializeShowcaseTabs();
        InitializeModernShowcase(); // Modern extensions (beyond WPF) — see Modern\README.md
        InitializeDiagnostics();
        App.LogAutoTestStartup("MAIN PAGE CONSTRUCTOR END");
    }

#if WINDOWS
    internal void ConfigureWindowTitleBar(Window window)
    {
        Loaded += (_, _) =>
        {
            MainRibbon.ApplyTemplate();
            if (MainRibbon.TitleBarHost is not FrameworkElement titleBarHost
                || MainRibbon.TitleBarDragRegion is not FrameworkElement dragRegion)
            {
                return;
            }

            window.ExtendsContentIntoTitleBar = true;

            var scale = titleBarHost.XamlRoot?.RasterizationScale ?? 1.0;
            var appTitleBar = window.AppWindow.TitleBar;
            titleBarHost.MinHeight = Math.Max(22, appTitleBar.Height / scale);
            titleBarHost.Margin = new Thickness(
                appTitleBar.LeftInset / scale,
                0,
                appTitleBar.RightInset / scale,
                0);
            window.SetTitleBar(dragRegion);
        };
    }
#endif

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
        SeedGroupedColorGallery(GalGrouped);
        GalGroupedAdvanced.GroupByAdvanced = item =>
            item is FrameworkElement element ? element.Tag?.ToString() ?? string.Empty : string.Empty;
        SeedGroupedColorGallery(GalGroupedAdvanced);
        for (var i = 1; i <= 8; i++)
        {
            GalInRibbon.Items.Add(
                new RibbonGalleryItem
                {
                    Content = i.ToString(),
                    Group = i <= 4 ? "Group 1" : "Group 2",
                });
        }

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

    private void SeedGroupedColorGallery(InRibbonGallery gallery)
    {
        for (var i = 0; i < Palette.Length; i++)
        {
            var (name, color) = Palette[i];
            gallery.Items.Add(MakeGroupedColorTile(name, color, i < 5 ? "Group A" : "Group B"));
        }
    }

    private static Grid MakeGroupedColorTile(
        string name,
        Windows.UI.Color color,
        string group)
    {
        var tile = new Grid
        {
            Tag = group,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        tile.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        tile.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        tile.Children.Add(
            new Border
            {
                Width = 14,
                Height = 14,
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(Rgb(0x90, 0x90, 0x90)),
                BorderThickness = new Thickness(0.5),
                CornerRadius = new CornerRadius(1),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });

        var label = new TextBlock
        {
            Text = name,
            FontSize = 9,
            Margin = new Thickness(2, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        Grid.SetColumn(label, 1);
        tile.Children.Add(label);

        return tile;
    }

    private static ComboBoxItem CreateComboBoxGroupHeader(string text)
    {
        var header = new ComboBoxItem
        {
            Content = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            },
            IsEnabled = false,
            IsHitTestVisible = false,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
        };
        AutomationProperties.SetName(header, text);
        return header;
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

    private void OnToggleRtl(object sender, RoutedEventArgs e)
    {
        FlowDirection = FlowDirection == FlowDirection.RightToLeft
            ? FlowDirection.LeftToRight
            : FlowDirection.RightToLeft;
    }

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            var theme = toggleButton.IsChecked == true
                ? ElementTheme.Dark
                : ElementTheme.Light;
            ApplyShowcaseTheme(theme);
        }
    }

    private void ApplyShowcaseTheme(ElementTheme theme)
    {
        RequestedTheme = theme;
        DarkModeButton.IsChecked = theme == ElementTheme.Dark;

        if (XamlRoot?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
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

    private void OnColorGallerySelectionChanged(object sender, RoutedEventArgs args)
    {
        var color = (sender as ColorGallery)?.SelectedColor;
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
