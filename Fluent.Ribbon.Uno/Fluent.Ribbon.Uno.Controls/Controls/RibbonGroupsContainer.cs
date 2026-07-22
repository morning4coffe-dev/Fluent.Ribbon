namespace Fluent;

using Fluent.Extensions;
using Fluent.Internal;

/// <summary>
/// A panel that arranges <see cref="RibbonGroupBox"/> children horizontally,
/// automatically reducing and enlarging group sizes based on available width
/// using the <see cref="ReduceOrder"/> sequence.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon, adapted for Uno/WinUI.
/// WinUI does not have IScrollInfo; horizontal scrolling is handled by a parent ScrollViewer.
/// </remarks>
public partial class RibbonGroupsContainer : Panel, IScrollInfo
{
    #region Reduce Order

    /// <summary>Identifies the <see cref="ReduceOrder"/> dependency property.</summary>
    public static readonly DependencyProperty ReduceOrderProperty =
        DependencyProperty.Register(
            nameof(ReduceOrder),
            typeof(string),
            typeof(RibbonGroupsContainer),
            new PropertyMetadata(null, OnReduceOrderChanged));

    /// <summary>
    /// Gets or sets the reduce order for group sizing.
    /// Comma-separated list of group names defining the order in which groups are reduced.
    /// Enclose in parentheses as (Control.Name) to reduce/enlarge scalable elements.
    /// </summary>
    public string? ReduceOrder
    {
        get => (string?)GetValue(ReduceOrderProperty);
        set => SetValue(ReduceOrderProperty, value);
    }

    private static void OnReduceOrderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (RibbonGroupsContainer)d;

        // Increase any previously reduced items back
        var toIncrease = panel._reduceOrder.Skip(panel._reduceOrderIndex + 1).ToArray();
        foreach (var item in toIncrease)
        {
            panel.IncreaseGroupBoxSize(item);
        }

        panel._reduceOrder = ((string?)e.NewValue ?? string.Empty)
            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        panel._reduceOrderIndex = panel._reduceOrder.Length - 1;

        panel.InvalidateMeasure();
        panel.InvalidateArrange();
    }

    #endregion

    #region Fields

    private string[] _reduceOrder = Array.Empty<string>();
    private int _reduceOrderIndex;
    private bool _lastSimplifiedState;

    #endregion

    #region Layout

    /// <inheritdoc/>
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        // Check if IsSimplified changed for the first group to reset layout
        bool currentSimplifiedState = Children.OfType<RibbonGroupBox>().FirstOrDefault()?.IsSimplified ?? false;
        if (_lastSimplifiedState != currentSimplifiedState)
        {
            _lastSimplifiedState = currentSimplifiedState;
            _reduceOrderIndex = _reduceOrder.Length - 1;
            foreach (var child in Children.OfType<RibbonGroupBox>())
            {
                child.StateIntermediate = currentSimplifiedState ? RibbonGroupBoxState.Medium : RibbonGroupBoxState.Large;
            }
        }

        var desiredSize = GetChildrenDesiredSizeIntermediate();

        if (_reduceOrder.Length == 0)
        {
            return CommitStatesAndMeasure();
        }

        // If we have more space than needed, try to enlarge groups
        while (desiredSize.Width <= availableSize.Width)
        {
            if (_reduceOrderIndex >= _reduceOrder.Length - 1)
            {
                break;
            }

            _reduceOrderIndex++;
            IncreaseGroupBoxSize(_reduceOrder[_reduceOrderIndex]);
            desiredSize = GetChildrenDesiredSizeIntermediate();
        }

        // If not enough space, reduce groups
        while (desiredSize.Width > availableSize.Width)
        {
            if (_reduceOrderIndex < 0)
            {
                break;
            }

            DecreaseGroupBoxSize(_reduceOrder[_reduceOrderIndex]);
            _reduceOrderIndex--;
            desiredSize = GetChildrenDesiredSizeIntermediate();
        }

        return CommitStatesAndMeasure();
    }

    private Windows.Foundation.Size CommitStatesAndMeasure()
    {
        double finalWidth = 0;
        double finalHeight = 0;

        foreach (var child in Children)
        {
            if (child is RibbonGroupBox groupBox && groupBox.State != groupBox.StateIntermediate)
            {
                groupBox.State = groupBox.StateIntermediate;
            }

            child.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
            finalWidth += child.DesiredSize.Width;
            finalHeight = Math.Max(finalHeight, child.DesiredSize.Height);
        }

        return new Windows.Foundation.Size(finalWidth, finalHeight);
    }

    /// <inheritdoc/>
    protected override Windows.Foundation.Size ArrangeOverride(Windows.Foundation.Size finalSize)
    {
        double x = 0;

        foreach (var child in Children)
        {
            var width = child.DesiredSize.Width;
            var height = Math.Max(finalSize.Height, child.DesiredSize.Height);
            child.Arrange(new Windows.Foundation.Rect(x, 0, width, height));
            x += width;
        }

        return finalSize;
    }

    #endregion

    #region Reduce/Enlarge

    private Windows.Foundation.Size GetChildrenDesiredSizeIntermediate()
    {
        double width = 0;
        double height = 0;

        foreach (var child in Children)
        {
            if (child is RibbonGroupBox groupBox)
            {
                var desiredSize = groupBox.GetDesiredSizeIntermediate();
                width += desiredSize.Width;
                height = Math.Max(height, desiredSize.Height);
            }
            else
            {
                child.Measure(new Windows.Foundation.Size(double.PositiveInfinity, double.PositiveInfinity));
                width += child.DesiredSize.Width;
                height = Math.Max(height, child.DesiredSize.Height);
            }
        }

        return new Windows.Foundation.Size(width, height);
    }

    private void IncreaseGroupBoxSize(string name)
    {
        var groupBox = FindGroup(name);
        var scale = name.StartsWith("(", StringComparison.Ordinal);

        if (groupBox is null)
        {
            return;
        }

        if (scale)
        {
            groupBox.ScaleIntermediate++;
        }
        else
        {
            if (groupBox.IsSimplified)
            {
                groupBox.StateIntermediate = groupBox.SimplifiedStateDefinition.EnlargeState(groupBox.StateIntermediate);
            }
            else
            {
                groupBox.StateIntermediate = groupBox.StateDefinition.EnlargeState(groupBox.StateIntermediate);
            }
        }
    }

    private void DecreaseGroupBoxSize(string name)
    {
        var groupBox = FindGroup(name);
        var scale = name.StartsWith("(", StringComparison.OrdinalIgnoreCase);

        if (groupBox is null)
        {
            return;
        }

        if (scale)
        {
            groupBox.ScaleIntermediate--;
        }
        else
        {
            if (groupBox.IsSimplified)
            {
                groupBox.StateIntermediate = groupBox.SimplifiedStateDefinition.ReduceState(groupBox.StateIntermediate);
            }
            else
            {
                groupBox.StateIntermediate = groupBox.StateDefinition.ReduceState(groupBox.StateIntermediate);
            }
        }
    }

    private RibbonGroupBox? FindGroup(string name)
    {
        if (name.StartsWith("(", StringComparison.Ordinal))
        {
            name = name.Substring(1, name.Length - 2);
        }

        foreach (var child in Children)
        {
            if (child is FrameworkElement fe && fe.Name == name)
            {
                return fe as RibbonGroupBox;
            }
        }

        return null;
    }

    internal void GroupBoxCacheClearedAndStateAndScaleResetted(RibbonGroupBox groupBox)
    {
        _reduceOrderIndex = _reduceOrder.Length - 1;
        InvalidateMeasure();
        InvalidateArrange();
    }

    /// <summary>Returns the panel's child collection.</summary>
    protected virtual UIElementCollection CreateUIElementCollection(
        FrameworkElement logicalParent)
    {
        return Children;
    }

    /// <summary>Handles a child desired-size change.</summary>
    protected virtual void OnChildDesiredSizeChanged(UIElement child)
    {
        InvalidateMeasure();
    }

    #endregion
}
