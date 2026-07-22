namespace Fluent;

/// <summary>
/// Arranges left- and right-aligned status-bar children and zero-measures overflow.
/// </summary>
public partial class StatusBarPanel : Panel
{
    private readonly List<UIElement> _leftChildren = new();
    private readonly List<UIElement> _rightChildren = new();
    private readonly List<UIElement> _otherChildren = new();

    private int _lastRightIndex;
    private int _lastLeftIndex;

    /// <inheritdoc />
    protected override Windows.Foundation.Size MeasureOverride(
        Windows.Foundation.Size availableSize)
    {
        _leftChildren.Clear();
        _rightChildren.Clear();
        _otherChildren.Clear();

        foreach (var child in Children.OfType<FrameworkElement>())
        {
            switch (child.HorizontalAlignment)
            {
                case HorizontalAlignment.Left:
                    _leftChildren.Add(child);
                    break;
                case HorizontalAlignment.Right:
                    _rightChildren.Add(child);
                    break;
                default:
                    _otherChildren.Add(child);
                    break;
            }
        }

        _lastRightIndex = _rightChildren.Count;
        _lastLeftIndex = _leftChildren.Count;

        var zero = new Windows.Foundation.Size(0, 0);
        var infinite = new Windows.Foundation.Size(
            double.PositiveInfinity,
            double.PositiveInfinity);
        var width = 0D;
        var height = 0D;
        var canAdd = true;

        for (var index = 0; index < _rightChildren.Count; index++)
        {
            var child = _rightChildren[index];
            if (!canAdd)
            {
                child.Measure(zero);
                continue;
            }

            child.Measure(infinite);
            height = Math.Max(height, child.DesiredSize.Height);
            if (width + child.DesiredSize.Width <= availableSize.Width)
            {
                width += child.DesiredSize.Width;
                continue;
            }

            canAdd = false;
            child.Measure(zero);
            _lastRightIndex = index;
            _lastLeftIndex = 0;
        }

        for (var index = 0; index < _leftChildren.Count; index++)
        {
            var child = _leftChildren[index];
            if (!canAdd)
            {
                child.Measure(zero);
                continue;
            }

            child.Measure(infinite);
            height = Math.Max(height, child.DesiredSize.Height);
            if (width + child.DesiredSize.Width <= availableSize.Width)
            {
                width += child.DesiredSize.Width;
                continue;
            }

            canAdd = false;
            child.Measure(zero);
            _lastLeftIndex = index;
        }

        foreach (var child in _otherChildren)
        {
            child.Measure(zero);
        }

        return new Windows.Foundation.Size(width, height);
    }

    /// <inheritdoc />
    protected override Windows.Foundation.Size ArrangeOverride(
        Windows.Foundation.Size finalSize)
    {
        var zero = new Windows.Foundation.Rect(0, 0, 0, 0);
        var rightShift = 0D;

        for (var index = _rightChildren.Count - 1; index >= 0; index--)
        {
            var child = _rightChildren[index];
            if (_lastRightIndex > index)
            {
                rightShift += child.DesiredSize.Width;
                child.Arrange(
                    new Windows.Foundation.Rect(
                        finalSize.Width - rightShift,
                        0,
                        child.DesiredSize.Width,
                        finalSize.Height));
            }
            else
            {
                child.Arrange(zero);
            }
        }

        var leftShift = 0D;
        for (var index = 0; index < _leftChildren.Count; index++)
        {
            var child = _leftChildren[index];
            if (index < _lastLeftIndex)
            {
                child.Arrange(
                    new Windows.Foundation.Rect(
                        leftShift,
                        0,
                        child.DesiredSize.Width,
                        finalSize.Height));
                leftShift += child.DesiredSize.Width;
            }
            else
            {
                child.Arrange(zero);
            }
        }

        foreach (var child in _otherChildren)
        {
            child.Arrange(zero);
        }

        return finalSize;
    }
}
