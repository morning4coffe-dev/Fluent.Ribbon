namespace Fluent;

/// <summary>
/// Represents a label that can display text on two lines,
/// splitting at soft hyphens or the nearest space to center.
/// </summary>
[TemplatePart(Name = "PART_TextRun", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_TextRun2", Type = typeof(TextBlock))]
public partial class TwoLineLabel : Control
{
    #region Fields

    private TextBlock? _textRun;
    private TextBlock? _textRun2;

    #endregion

    #region Dependency Properties

    /// <summary>Identifies the <see cref="HasTwoLines"/> dependency property.</summary>
    public static readonly DependencyProperty HasTwoLinesProperty =
        DependencyProperty.Register(
            nameof(HasTwoLines),
            typeof(bool),
            typeof(TwoLineLabel),
            new PropertyMetadata(true, OnHasTwoLinesChanged));

    /// <summary>
    /// Gets or sets whether the label must display text on two lines.
    /// </summary>
    public bool HasTwoLines
    {
        get => (bool)GetValue(HasTwoLinesProperty);
        set => SetValue(HasTwoLinesProperty, value);
    }

    /// <summary>Identifies the <see cref="HasGlyph"/> dependency property.</summary>
    public static readonly DependencyProperty HasGlyphProperty =
        DependencyProperty.Register(
            nameof(HasGlyph),
            typeof(bool),
            typeof(TwoLineLabel),
            new PropertyMetadata(false, OnHasGlyphChanged));

    /// <summary>
    /// Gets or sets whether the label has a glyph indicator.
    /// </summary>
    public bool HasGlyph
    {
        get => (bool)GetValue(HasGlyphProperty);
        set => SetValue(HasGlyphProperty, value);
    }

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TwoLineLabel),
            new PropertyMetadata(string.Empty, OnTextChanged));

    /// <summary>
    /// Gets or sets the text content.
    /// </summary>
    public string? Text
    {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoLineLabel"/> class.
    /// </summary>
    public TwoLineLabel()
    {
        DefaultStyleKey = typeof(TwoLineLabel);
        IsTabStop = false;
    }

    #endregion

    #region Template

    /// <inheritdoc />
    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _textRun = GetTemplateChild("PART_TextRun") as TextBlock;
        _textRun2 = GetTemplateChild("PART_TextRun2") as TextBlock;

        UpdateTextRun();
    }

    #endregion

    #region Event Handling

    private static void OnHasTwoLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TwoLineLabel)d).UpdateTextRun();
    }

    private static void OnHasGlyphChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TwoLineLabel)d).UpdateTextRun();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((TwoLineLabel)d).UpdateTextRun();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates text runs and adds newline if HasTwoLines is true.
    /// </summary>
    private void UpdateTextRun()
    {
        if (_textRun is null || _textRun2 is null)
        {
            return;
        }

        var text = Text?.Trim();

        if (!HasTwoLines || string.IsNullOrEmpty(text))
        {
            _textRun.Text = text;
            _textRun2.Text = string.Empty;
            return;
        }

        // Find soft hyphen (char 173), break at its position and display a normal hyphen
        var hyphenIndex = text!.IndexOf((char)173);

        if (hyphenIndex >= 0)
        {
            _textRun.Text = text.Substring(0, hyphenIndex) + "-";
            _textRun2.Text = text.Substring(hyphenIndex + 1) + " ";
        }
        else
        {
            var centerIndex = text.Length / 2;

            // Find spaces nearest to center from left and right
            var leftSpaceIndex = text.LastIndexOf(" ", centerIndex, centerIndex, StringComparison.CurrentCulture);
            var rightSpaceIndex = text.IndexOf(" ", centerIndex, StringComparison.CurrentCulture);

            if (leftSpaceIndex == -1 && rightSpaceIndex == -1)
            {
                _textRun.Text = text;
                _textRun2.Text = string.Empty;
            }
            else if (leftSpaceIndex == -1)
            {
                _textRun.Text = text.Substring(0, rightSpaceIndex);
                _textRun2.Text = text.Substring(rightSpaceIndex) + " ";
            }
            else if (rightSpaceIndex == -1)
            {
                _textRun.Text = text.Substring(0, leftSpaceIndex);
                _textRun2.Text = text.Substring(leftSpaceIndex) + " ";
            }
            else
            {
                // Find nearest to center space and split there
                if (Math.Abs(centerIndex - leftSpaceIndex) < Math.Abs(centerIndex - rightSpaceIndex))
                {
                    _textRun.Text = text.Substring(0, leftSpaceIndex);
                    _textRun2.Text = text.Substring(leftSpaceIndex) + " ";
                }
                else
                {
                    _textRun.Text = text.Substring(0, rightSpaceIndex);
                    _textRun2.Text = text.Substring(rightSpaceIndex) + " ";
                }
            }
        }
    }

    #endregion
}
