namespace Fluent.Modern.Controls;

using Fluent;
using Fluent.Modern.Automation;
using Fluent.Modern.Commands;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// <para><b>Modern extension</b> — provides Tell Me command search over a ribbon.</para>
/// </summary>
[ModernExtension]
[TemplatePart(Name = PART_AutoSuggestBox, Type = typeof(AutoSuggestBox))]
public partial class RibbonSearchBox : Control
{
    private const string PART_AutoSuggestBox = "PART_AutoSuggestBox";

    private AutoSuggestBox? _autoSuggestBox;
    private RibbonCommandCatalog? _catalog;
    private bool _isLoaded;

    #region Dependency Properties

    /// <summary>Identifies the <see cref="Ribbon"/> dependency property.</summary>
    public static readonly DependencyProperty RibbonProperty =
        DependencyProperty.Register(
            nameof(Ribbon),
            typeof(Ribbon),
            typeof(RibbonSearchBox),
            new PropertyMetadata(null, OnRibbonChanged));

    /// <summary>Identifies the <see cref="PlaceholderText"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderTextProperty =
        DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(RibbonSearchBox),
            new PropertyMetadata(string.Empty, OnPlaceholderTextChanged));

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(RibbonSearchBox),
            new PropertyMetadata(string.Empty, OnTextPropertyChanged));

    /// <summary>Identifies the <see cref="MaxResults"/> dependency property.</summary>
    public static readonly DependencyProperty MaxResultsProperty =
        DependencyProperty.Register(
            nameof(MaxResults),
            typeof(int),
            typeof(RibbonSearchBox),
            new PropertyMetadata(8));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the ribbon searched by this control.
    /// </summary>
    public Ribbon? Ribbon
    {
        get => (Ribbon?)GetValue(RibbonProperty);
        set => SetValue(RibbonProperty, value);
    }

    /// <summary>
    /// Gets or sets the placeholder text shown when the search box is empty.
    /// </summary>
    public string PlaceholderText
    {
        get => (string)GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the current search text.
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    /// <summary>
    /// Gets or sets the maximum number of suggestions returned.
    /// </summary>
    public int MaxResults
    {
        get => (int)GetValue(MaxResultsProperty);
        set => SetValue(MaxResultsProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonSearchBox"/> class.
    /// </summary>
    public RibbonSearchBox()
    {
        DefaultStyleKey = typeof(RibbonSearchBox);
        GotFocus += OnGotFocus;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        RibbonLocalizationUpdateHelper.Track(this, RefreshLocalizedDefaults);
    }

    #endregion

    #region Template

    /// <inheritdoc/>
    protected override void OnApplyTemplate()
    {
        if (_autoSuggestBox is not null)
        {
            _autoSuggestBox.TextChanged -= OnTextChanged;
            _autoSuggestBox.SuggestionChosen -= OnSuggestionChosen;
            _autoSuggestBox.QuerySubmitted -= OnQuerySubmitted;
        }

        base.OnApplyTemplate();

        _autoSuggestBox = GetTemplateChild(PART_AutoSuggestBox) as AutoSuggestBox;
        if (_autoSuggestBox is not null)
        {
            _autoSuggestBox.IsTabStop = false;
            _autoSuggestBox.UseSystemFocusVisuals = true;
            _autoSuggestBox.PlaceholderText = PlaceholderText;
            _autoSuggestBox.Text = Text;
            AutomationProperties.SetAccessibilityView(_autoSuggestBox, AccessibilityView.Raw);
            _autoSuggestBox.ApplyTemplate();
            MarkImplementationTreeRaw(_autoSuggestBox);
            var automationId = AutomationProperties.GetAutomationId(this);
            if (!string.IsNullOrWhiteSpace(automationId))
            {
                AutomationProperties.SetAutomationId(_autoSuggestBox, $"{automationId}.Input");
            }

            _autoSuggestBox.TextChanged += OnTextChanged;
            _autoSuggestBox.SuggestionChosen += OnSuggestionChosen;
            _autoSuggestBox.QuerySubmitted += OnQuerySubmitted;
        }
    }

    private static void MarkImplementationTreeRaw(DependencyObject root)
    {
        var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, index);
            if (child is FrameworkElement element)
            {
                AutomationProperties.SetAccessibilityView(element, AccessibilityView.Raw);
                if (element is Control control)
                {
                    control.IsTabStop = false;
                }
            }

            MarkImplementationTreeRaw(child);
        }
    }

    #endregion

    #region Automation

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer()
        // Modern a11y: custom automation peer.
        => new RibbonSearchBoxAutomationPeer(this);

    #endregion

    #region Methods

    private void RefreshLocalizedDefaults()
    {
        var localization = RibbonLocalization.Current.Localization;
        Fluent.Automation.Peers.AutomationPeerHelpers.SetValueIfUnsetOrGenerated(
            this,
            PlaceholderTextProperty,
            localization.RibbonSearchPlaceholder);
        Fluent.Automation.Peers.AutomationPeerHelpers.SetNameIfUnsetOrGenerated(
            this,
            localization.RibbonSearchName);
    }

    private static void OnRibbonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var searchBox = (RibbonSearchBox)d;
        if (searchBox._isLoaded)
        {
            searchBox.BuildCatalog();
        }
        else
        {
            searchBox.DisposeCatalog();
        }
    }

    private static void OnPlaceholderTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var searchBox = (RibbonSearchBox)d;
        if (searchBox._autoSuggestBox is not null)
        {
            searchBox._autoSuggestBox.PlaceholderText = (string)e.NewValue;
        }
    }

    private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var searchBox = (RibbonSearchBox)d;
        var oldText = e.OldValue as string ?? string.Empty;
        var text = e.NewValue as string ?? string.Empty;
        if (searchBox._autoSuggestBox is not null
            && !string.Equals(searchBox._autoSuggestBox.Text, text, StringComparison.Ordinal))
        {
            searchBox._autoSuggestBox.Text = text;
        }

        if (FrameworkElementAutomationPeer.FromElement(searchBox)
            is RibbonSearchBoxAutomationPeer peer)
        {
            peer.RaiseValueChanged(oldText, text);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = true;
        EnsureCatalog();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _isLoaded = false;
        DisposeCatalog();
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        EnsureCatalog();

        // GotFocus bubbles from the inner AutoSuggestBox too; forwarding focus again from there
        // would fight the AutoSuggestBox's own internal focus handling.
        if (!ReferenceEquals(e.OriginalSource, this))
        {
            return;
        }

        var focusState = FocusState switch
        {
            Microsoft.UI.Xaml.FocusState.Keyboard => Microsoft.UI.Xaml.FocusState.Keyboard,
            Microsoft.UI.Xaml.FocusState.Pointer => Microsoft.UI.Xaml.FocusState.Pointer,
            _ => Microsoft.UI.Xaml.FocusState.Programmatic,
        };
        FocusEditor(focusState);
    }

    private bool FocusEditor(FocusState focusState)
    {
        if (_autoSuggestBox is null)
        {
            return false;
        }

        var isTabStop = _autoSuggestBox.IsTabStop;
        try
        {
            _autoSuggestBox.IsTabStop = true;
            return _autoSuggestBox.Focus(focusState);
        }
        finally
        {
            _autoSuggestBox.IsTabStop = isTabStop;
        }
    }

    private void OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (!string.Equals(Text, sender.Text, StringComparison.Ordinal))
        {
            Text = sender.Text;
        }

        if (string.IsNullOrWhiteSpace(sender.Text))
        {
            sender.ItemsSource = null;
            sender.IsSuggestionListOpen = false;
            return;
        }

        EnsureCatalog();
        var suggestions = _catalog?.Search(sender.Text, MaxResults)
                          ?? Array.Empty<RibbonCommandDescriptor>();
        sender.ItemsSource = suggestions;
        sender.IsSuggestionListOpen = suggestions.Count > 0;
    }

    private void OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        if (args.SelectedItem is RibbonCommandDescriptor descriptor)
        {
            sender.Text = descriptor.DisplayName;
        }
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        if (args.ChosenSuggestion is RibbonCommandDescriptor chosenDescriptor)
        {
            Execute(chosenDescriptor);
            return;
        }

        EnsureCatalog();
        var descriptor = _catalog?.Search(args.QueryText, 1).FirstOrDefault();
        if (descriptor is not null)
        {
            Execute(descriptor);
        }
    }

    private void Execute(RibbonCommandDescriptor descriptor)
    {
        if (!descriptor.IsAvailable)
        {
            return;
        }

        descriptor.Navigate();
        descriptor.Invoke();
        if (_autoSuggestBox is not null)
        {
            _autoSuggestBox.ItemsSource = null;
            _autoSuggestBox.IsSuggestionListOpen = false;
        }
    }

    private void EnsureCatalog()
    {
        if (_catalog is null)
        {
            BuildCatalog();
        }
    }

    private void BuildCatalog()
    {
        DisposeCatalog();
        if (Ribbon is null)
        {
            return;
        }

        _catalog = new RibbonCommandCatalog(Ribbon);
        _catalog.Changed += OnCatalogChanged;
    }

    private void DisposeCatalog()
    {
        if (_catalog is null)
        {
            return;
        }

        _catalog.Changed -= OnCatalogChanged;
        _catalog.Dispose();
        _catalog = null;
    }

    private void OnCatalogChanged(object? sender, EventArgs e)
    {
        if (_autoSuggestBox is not null && !string.IsNullOrWhiteSpace(_autoSuggestBox.Text))
        {
            _autoSuggestBox.ItemsSource = _catalog?.Search(_autoSuggestBox.Text, MaxResults)
                ?? Array.Empty<RibbonCommandDescriptor>();
        }
    }

    internal string AutomationValue => _autoSuggestBox?.Text ?? Text;

    internal bool IsAutomationReadOnly =>
        !IsEnabled || _autoSuggestBox is { IsEnabled: false };

    internal void SetAutomationValue(string value)
    {
        Text = value;
        if (_autoSuggestBox is not null)
        {
            _autoSuggestBox.Text = value;
        }
    }

    #endregion
}
