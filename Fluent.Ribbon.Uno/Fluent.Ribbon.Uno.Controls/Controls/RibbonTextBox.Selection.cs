namespace Fluent;

public partial class RibbonTextBox
{
    private int _selectionStart;
    private int _selectionLength;
    private bool _selectionRestorePending;
    private bool _applyingSelectionTemplate;
    private bool _synchronizingSelection;
    private bool _observingSelectionLayout;
    private bool _selectionRestoreQueued;

    /// <summary>Occurs when the active text editor's selection changes.</summary>
    /// <remarks>
    /// The native event is not routed and has no public raising hook. Relaying the
    /// active editor avoids selecting an unused native text view in the wrapper.
    /// </remarks>
    public new event RoutedEventHandler? SelectionChanged;

    private void InitializeSelectionDelegation()
    {
        base.SelectionChanged += OnNativeSelectionChanged;
        Loaded += OnSelectionOwnerLoaded;
        Unloaded += OnSelectionOwnerUnloaded;
    }

    private void BeginSelectionTemplateChange()
    {
        CaptureSelection();
        _applyingSelectionTemplate = true;
        _selectionRestorePending = true;
        StopObservingSelectionLayout();
        if (_textBox is not null)
        {
            _textBox.SelectionChanged -= OnEditorSelectionChanged;
            _textBox.Loaded -= OnSelectionEditorLoaded;
            _textBox.Unloaded -= OnSelectionEditorUnloaded;
        }
    }

    private void AttachSelectionEditor()
    {
        _textBox!.SelectionChanged += OnEditorSelectionChanged;
        _textBox.Loaded += OnSelectionEditorLoaded;
        _textBox.Unloaded += OnSelectionEditorUnloaded;
    }

    private void EndSelectionTemplateChange()
    {
        _applyingSelectionTemplate = false;
        ObservePendingSelectionLayout();
    }

    private void OnNativeSelectionChanged(object sender, RoutedEventArgs args)
    {
        if (_textBox is null && !_applyingSelectionTemplate && !_synchronizingSelection && !_selectionRestorePending)
        {
            RememberSelection(base.SelectionStart, base.SelectionLength, args);
        }
    }

    private void OnEditorSelectionChanged(object sender, RoutedEventArgs args)
    {
        if (ReferenceEquals(sender, _textBox) && !_applyingSelectionTemplate
            && !_synchronizingSelection && !_selectionRestorePending)
        {
            RememberSelection(_textBox!.SelectionStart, _textBox.SelectionLength, args);
        }
    }

    private void CaptureSelection()
    {
        if (!_selectionRestorePending)
        {
            _selectionStart = _textBox?.SelectionStart ?? base.SelectionStart;
            _selectionLength = _textBox?.SelectionLength ?? base.SelectionLength;
        }
    }

    private void RememberSelection(int start, int length, RoutedEventArgs? args = null)
    {
        var changed = start != _selectionStart || length != _selectionLength;
        _selectionStart = start;
        _selectionLength = length;
        if (changed)
        {
            SelectionChanged?.Invoke(this, args ?? new RoutedEventArgs());
        }
    }

    private void OnSelectionOwnerLoaded(object sender, RoutedEventArgs args) => ObservePendingSelectionLayout();

    private void OnSelectionOwnerUnloaded(object sender, RoutedEventArgs args)
    {
        if (!IsLoaded)
        {
            RetainSelection();
        }
    }

    private void OnSelectionEditorLoaded(object sender, RoutedEventArgs args)
    {
        if (ReferenceEquals(sender, _textBox))
        {
            ObservePendingSelectionLayout();
        }
    }

    private void OnSelectionEditorUnloaded(object sender, RoutedEventArgs args)
    {
        if (ReferenceEquals(sender, _textBox) && _textBox is { IsLoaded: false })
        {
            RetainSelection();
        }
    }

    private void RetainSelection()
    {
        CaptureSelection();
        _selectionRestorePending = true;
        StopObservingSelectionLayout();
    }

    private FrameworkElement? SelectionLayoutTarget
        => _textBox ?? GetTemplateChild("ContentElement") as FrameworkElement;

    private bool IsSelectionTargetReady
        => !_applyingSelectionTemplate && IsLoaded
           && SelectionLayoutTarget is { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 };

    private void ObservePendingSelectionLayout()
    {
        if (!_selectionRestorePending || !IsLoaded || SelectionLayoutTarget is null)
        {
            return;
        }

        if (!_observingSelectionLayout)
        {
            _observingSelectionLayout = true;
            LayoutUpdated += OnSelectionLayoutUpdated;
        }

        if (!_selectionRestoreQueued)
        {
            _selectionRestoreQueued = true;
            if (!DispatcherQueue.TryEnqueue(() =>
                {
                    _selectionRestoreQueued = false;
                    RestorePendingSelection();
                }))
            {
                _selectionRestoreQueued = false;
            }
        }
    }

    private void StopObservingSelectionLayout()
    {
        if (_observingSelectionLayout)
        {
            LayoutUpdated -= OnSelectionLayoutUpdated;
            _observingSelectionLayout = false;
        }
    }

    private void OnSelectionLayoutUpdated(object? sender, object args) => RestorePendingSelection();

    private void RestorePendingSelection()
    {
        if (!_selectionRestorePending || !IsSelectionTargetReady)
        {
            return;
        }

        var range = ClampSelection(_selectionStart, _selectionLength, (_textBox?.Text ?? Text ?? string.Empty).Length);
        _selectionRestorePending = false;
        StopObservingSelectionLayout();
        ApplySelection(range.Start, range.Length);
    }

    private static (int Start, int Length) ClampSelection(int start, int length, int textLength)
    {
        start = Math.Clamp(start, 0, textLength);
        return (start, Math.Clamp(length, 0, textLength - start));
    }

    private (int Start, int Length) PendingSelection
        => ClampSelection(_selectionStart, _selectionLength, (Text ?? string.Empty).Length);

    private void ApplySelection(int start, int length, bool selectAll = false)
    {
        var editor = _textBox;
        var wasSynchronizing = _synchronizingSelection;
        _synchronizingSelection = true;
        try
        {
            if (editor is not null)
            {
                if (selectAll)
                {
                    editor.SelectAll();
                }
                else
                {
                    editor.Select(start, length);
                }
            }
            else if (selectAll)
            {
                base.SelectAll();
            }
            else
            {
                base.Select(start, length);
            }
        }
        finally
        {
            _synchronizingSelection = wasSynchronizing;
        }

        if (ReferenceEquals(editor, _textBox) && !_selectionRestorePending && !_applyingSelectionTemplate)
        {
            RememberSelection(editor?.SelectionStart ?? base.SelectionStart, editor?.SelectionLength ?? base.SelectionLength);
        }
    }

    /// <summary>Selects text in the active editor, retaining the range until its template is laid out.</summary>
    public new void Select(int start, int length)
    {
        if (start < 0)
        {
            throw new ArgumentException("The selection start cannot be negative.", nameof(start));
        }
        if (length < 0)
        {
            throw new ArgumentException("The selection length cannot be negative.", nameof(length));
        }

        if (!_selectionRestorePending && IsSelectionTargetReady)
        {
            var range = ClampSelection(start, length, (_textBox?.Text ?? Text ?? string.Empty).Length);
            ApplySelection(range.Start, range.Length);
        }
        else
        {
            var range = ClampSelection(start, length, (Text ?? string.Empty).Length);
            _selectionRestorePending = true;
            RememberSelection(range.Start, range.Length);
            ObservePendingSelectionLayout();
        }
    }

    /// <summary>Selects all text in the active editor.</summary>
    public new void SelectAll()
    {
        if (!_selectionRestorePending && IsSelectionTargetReady)
        {
            ApplySelection(0, 0, selectAll: true);
        }
        else
        {
            Select(0, (Text ?? string.Empty).Length);
        }
    }

    /// <summary>Gets or sets the caret/selection start owned by the active editor.</summary>
    public new int SelectionStart
    {
        get => _selectionRestorePending ? PendingSelection.Start : _textBox?.SelectionStart ?? base.SelectionStart;
        set => Select(value, SelectionLength);
    }

    /// <summary>Gets or sets the selection length owned by the active editor.</summary>
    public new int SelectionLength
    {
        get => _selectionRestorePending ? PendingSelection.Length : _textBox?.SelectionLength ?? base.SelectionLength;
        set => Select(SelectionStart, value);
    }

    /// <summary>Gets or replaces the text selected in the active editor.</summary>
    public new string SelectedText
    {
        get
        {
            if (!_selectionRestorePending)
            {
                return _textBox?.SelectedText ?? base.SelectedText;
            }
            var range = PendingSelection;
            return (Text ?? string.Empty).Substring(range.Start, range.Length);
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!_selectionRestorePending && IsSelectionTargetReady)
            {
                if (_textBox is not null)
                {
                    _textBox.SelectedText = value;
                }
                else
                {
                    base.SelectedText = value;
                }
            }
            else
            {
                var range = PendingSelection;
                Text = (Text ?? string.Empty).Remove(range.Start, range.Length).Insert(range.Start, value);
                Select(range.Start, value.Length);
            }
        }
    }
}
