namespace Fluent;

using Fluent.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;

public partial class RibbonComboBox
{
    private readonly List<(DependencyObject Element, DependencyProperty Property, long Token)> _headerAutomationSubscriptions = [];
    private ContentPresenter? _automationHeaderPresenter;
    private FrameworkElement? _automationHeaderLabel;
    private FrameworkElement? _externalAutomationLabel;
    private long _headerLabelNameToken;
    private long _headerLabelTextToken;
    private long _externalLabelNameToken;
    private long _externalLabelTextToken;
    private bool _managesEditableLabel;
    private bool _observingHeaderLayout;
    private bool _headerAutomationQueued;
    private bool _updatingHeaderAutomation;
    private bool _ownsEditableName;
    private string? _generatedEditableName;

    private void InitializeHeaderAutomation()
    {
        foreach (var property in new[] { AutomationProperties.NameProperty, HeaderProperty, PlaceholderTextProperty })
        {
            RegisterPropertyChangedCallback(property, (_, _) =>
            {
                UpdateEditableAutomationName();
                QueueHeaderAutomation();
            });
        }
        Loaded += OnHeaderAutomationLoaded;
        Unloaded += OnHeaderAutomationUnloaded;
    }

    private void AttachHeaderAutomation()
    {
        if (_editableTextBox is null)
        {
            return;
        }

        ObserveHeaderProperty(_editableTextBox, AutomationProperties.NameProperty, OnEditableAutomationPropertyChanged);
        ObserveHeaderProperty(_editableTextBox, AutomationProperties.LabeledByProperty, OnEditableAutomationPropertyChanged);
        if (_headerPresenter is not ContentPresenter { Tag: "Fluent.EditorHeader" } presenter)
        {
            return;
        }

        _automationHeaderPresenter = presenter;
        var labelBinding = _editableTextBox.GetBindingExpression(AutomationProperties.LabeledByProperty)?.ParentBinding;
        object? elementName = labelBinding?.ElementName;
        var isHeaderElementBinding = Equals(elementName, "HeaderText")
                                     || (elementName is not null and not string
                                         && ReferenceEquals(AutomationProperties.GetLabeledBy(_editableTextBox), presenter));
        if (isHeaderElementBinding && string.IsNullOrEmpty(labelBinding?.Path?.Path))
        {
            // Native TextBox peers need the rendered label's peer, not the
            // ContentPresenter wrapper. Uno also represents ElementName with a
            // compiled subject; its resolved value must be our header presenter.
            _editableTextBox.ClearValue(AutomationProperties.LabeledByProperty);
            _managesEditableLabel = true;
        }

        foreach (var property in new[]
                 {
                     ContentPresenter.ContentProperty, ContentPresenter.ContentTemplateProperty,
                     ContentPresenter.ContentTemplateSelectorProperty,
                 })
        {
            ObserveHeaderProperty(presenter, property, (_, _) => QueueHeaderAutomation());
        }
        QueueHeaderAutomation();
    }

    private void ObserveHeaderProperty(
        DependencyObject element, DependencyProperty property, DependencyPropertyChangedCallback callback)
        => _headerAutomationSubscriptions.Add((element, property, element.RegisterPropertyChangedCallback(property, callback)));

    private void DetachHeaderAutomation()
    {
        StopObservingHeaderLayout();
        SetHeaderAutomationLabel(null);
        SetExternalAutomationLabel(null);
        foreach (var subscription in _headerAutomationSubscriptions)
        {
            subscription.Element.UnregisterPropertyChangedCallback(subscription.Property, subscription.Token);
        }
        _headerAutomationSubscriptions.Clear();
        _automationHeaderPresenter = null;
        _managesEditableLabel = false;
        _ownsEditableName = false;
        _generatedEditableName = null;
    }

    private void OnHeaderAutomationLoaded(object sender, RoutedEventArgs args) => QueueHeaderAutomation();

    private void OnHeaderAutomationUnloaded(object sender, RoutedEventArgs args)
    {
        StopObservingHeaderLayout();
        SetHeaderAutomationLabel(null);
        SetExternalAutomationLabel(null);
    }

    private void QueueHeaderAutomation()
    {
        if (!IsLoaded || _automationHeaderPresenter is null)
        {
            return;
        }

        if (!_observingHeaderLayout)
        {
            _observingHeaderLayout = true;
            LayoutUpdated += OnHeaderAutomationLayoutUpdated;
        }
        if (!_headerAutomationQueued)
        {
            _headerAutomationQueued = true;
            if (!DispatcherQueue.TryEnqueue(() =>
                {
                    _headerAutomationQueued = false;
                    RefreshHeaderAutomation();
                }))
            {
                _headerAutomationQueued = false;
            }
        }
    }

    private void OnHeaderAutomationLayoutUpdated(object? sender, object args) => RefreshHeaderAutomation();

    private void StopObservingHeaderLayout()
    {
        if (_observingHeaderLayout)
        {
            LayoutUpdated -= OnHeaderAutomationLayoutUpdated;
            _observingHeaderLayout = false;
        }
    }

    private void RefreshHeaderAutomation()
    {
        if (!IsLoaded || _automationHeaderPresenter is null)
        {
            return;
        }

        var label = FindHeaderAutomationLabel(_automationHeaderPresenter);
        SetHeaderAutomationLabel(label);
        UpdateEditableAutomationName();
        if (label is not null || _automationHeaderPresenter.Content is null
            || _automationHeaderPresenter is { ActualWidth: > 0, ActualHeight: > 0 })
        {
            StopObservingHeaderLayout();
        }
    }

    private static FrameworkElement? FindHeaderAutomationLabel(DependencyObject root, int depth = 0)
    {
        if (depth > 32)
        {
            return null;
        }
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is FrameworkElement element
                && (element is TextBlock || !string.IsNullOrWhiteSpace(AutomationProperties.GetName(element))))
            {
                return element;
            }
            if (FindHeaderAutomationLabel(child, depth + 1) is { } label)
            {
                return label;
            }
        }
        return null;
    }

    private void SetHeaderAutomationLabel(FrameworkElement? label)
    {
        if (ReferenceEquals(label, _automationHeaderLabel))
        {
            return;
        }
        if (_automationHeaderLabel is { } previous)
        {
            previous.UnregisterPropertyChangedCallback(AutomationProperties.NameProperty, _headerLabelNameToken);
            if (previous is TextBlock)
            {
                previous.UnregisterPropertyChangedCallback(TextBlock.TextProperty, _headerLabelTextToken);
            }
        }

        _automationHeaderLabel = label;
        if (label is not null)
        {
            _headerLabelNameToken = label.RegisterPropertyChangedCallback(
                AutomationProperties.NameProperty, (_, _) => UpdateEditableAutomationName());
            if (label is TextBlock)
            {
                _headerLabelTextToken = label.RegisterPropertyChangedCallback(
                    TextBlock.TextProperty, (_, _) => UpdateEditableAutomationName());
            }
        }
    }

    private void SetExternalAutomationLabel(FrameworkElement? label)
    {
        if (ReferenceEquals(label, _automationHeaderLabel))
        {
            label = null;
        }
        if (ReferenceEquals(label, _externalAutomationLabel))
        {
            return;
        }
        if (_externalAutomationLabel is { } previous)
        {
            previous.UnregisterPropertyChangedCallback(AutomationProperties.NameProperty, _externalLabelNameToken);
            if (previous is TextBlock)
            {
                previous.UnregisterPropertyChangedCallback(TextBlock.TextProperty, _externalLabelTextToken);
            }
        }
        _externalAutomationLabel = label;
        if (label is not null)
        {
            _externalLabelNameToken = label.RegisterPropertyChangedCallback(
                AutomationProperties.NameProperty, (_, _) => UpdateEditableAutomationName());
            if (label is TextBlock)
            {
                _externalLabelTextToken = label.RegisterPropertyChangedCallback(
                    TextBlock.TextProperty, (_, _) => UpdateEditableAutomationName());
            }
        }
    }

    private void OnEditableAutomationPropertyChanged(DependencyObject sender, DependencyProperty property)
    {
        if (!_updatingHeaderAutomation)
        {
            UpdateEditableAutomationName();
            QueueHeaderAutomation();
        }
    }

    private void UpdateEditableAutomationName()
    {
        if (_editableTextBox is null || _updatingHeaderAutomation)
        {
            return;
        }

        _updatingHeaderAutomation = true;
        try
        {
            if (_managesEditableLabel
                && _editableTextBox.GetBindingExpression(AutomationProperties.LabeledByProperty) is null)
            {
                AutomationPeerHelpers.SetValueIfUnsetOrGenerated(
                    _editableTextBox, AutomationProperties.LabeledByProperty, _automationHeaderLabel);
            }

            var label = AutomationProperties.GetLabeledBy(_editableTextBox);
            SetExternalAutomationLabel(IsLoaded ? label as FrameworkElement : null);
            var name = AutomationProperties.GetName(this);
            if (string.IsNullOrWhiteSpace(name))
            {
                name = AutomationPeerHelpers.GetObjectName(label);
            }
            if (string.IsNullOrWhiteSpace(name))
            {
                name = AutomationPeerHelpers.GetHeaderOrPlaceholderName(this);
            }

            var current = AutomationProperties.GetName(_editableTextBox);
            var local = _editableTextBox.ReadLocalValue(AutomationProperties.NameProperty);
            if (_editableTextBox.GetBindingExpression(AutomationProperties.NameProperty) is not null
                || (_ownsEditableName
                    ? !string.Equals(current, _generatedEditableName, StringComparison.Ordinal)
                      || !Equals(local, _generatedEditableName)
                    : local != DependencyProperty.UnsetValue || !string.IsNullOrEmpty(current)))
            {
                _ownsEditableName = false;
                return;
            }

            // WinRT string getters need value comparison, not managed-reference
            // identity. Never acquire a consumer's local value or binding.
            if (!string.Equals(current, name, StringComparison.Ordinal))
            {
                _ownsEditableName = true;
                _generatedEditableName = name;
                AutomationProperties.SetName(_editableTextBox, name);
                FrameworkElementAutomationPeer.FromElement(_editableTextBox)?.RaisePropertyChangedEvent(
                    AutomationElementIdentifiers.NameProperty, current, name);
            }
        }
        finally
        {
            _updatingHeaderAutomation = false;
        }
    }
}
