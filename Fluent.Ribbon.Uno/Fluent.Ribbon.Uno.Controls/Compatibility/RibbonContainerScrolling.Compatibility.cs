namespace Fluent;

using Windows.Foundation;

/// <summary>
/// Portable scrolling surface for <see cref="RibbonGroupsContainer"/>.
/// WinUI has no IScrollInfo, so these members delegate to a parent <see cref="ScrollViewer"/>.
/// </summary>
public partial class RibbonGroupsContainer
{
    private ScrollViewer? _scrollOwner;

    /// <summary>Gets or sets the scroll viewer owning this panel.</summary>
    public ScrollViewer? ScrollOwner
    {
        get => _scrollOwner ?? RibbonScrollCompatibility.FindOwner(this);
        set => _scrollOwner = value;
    }

    /// <summary>Gets or sets whether horizontal scrolling is enabled.</summary>
    public bool CanHorizontallyScroll { get; set; } = true;

    /// <summary>Gets or sets whether vertical scrolling is enabled.</summary>
    public bool CanVerticallyScroll { get; set; }

    /// <summary>Gets the horizontal extent.</summary>
    public double ExtentWidth => ScrollOwner?.ExtentWidth ?? RibbonScrollCompatibility.GetChildrenWidth(this);

    /// <summary>Gets the vertical extent.</summary>
    public double ExtentHeight => ScrollOwner?.ExtentHeight ?? DesiredSize.Height;

    /// <summary>Gets the horizontal offset.</summary>
    public double HorizontalOffset => ScrollOwner?.HorizontalOffset ?? 0;

    /// <summary>Gets the vertical offset.</summary>
    public double VerticalOffset => ScrollOwner?.VerticalOffset ?? 0;

    /// <summary>Gets the viewport width.</summary>
    public double ViewportWidth => ScrollOwner?.ViewportWidth ?? ActualWidth;

    /// <summary>Gets the viewport height.</summary>
    public double ViewportHeight => ScrollOwner?.ViewportHeight ?? ActualHeight;

    /// <summary>Scrolls left by one line.</summary>
    public virtual void LineLeft() => SetHorizontalOffset(HorizontalOffset - RibbonScrollCompatibility.LineDelta);

    /// <summary>Scrolls right by one line.</summary>
    public virtual void LineRight() => SetHorizontalOffset(HorizontalOffset + RibbonScrollCompatibility.LineDelta);

    /// <summary>Scrolls up by one line.</summary>
    public virtual void LineUp() => SetVerticalOffset(VerticalOffset - RibbonScrollCompatibility.LineDelta);

    /// <summary>Scrolls down by one line.</summary>
    public virtual void LineDown() => SetVerticalOffset(VerticalOffset + RibbonScrollCompatibility.LineDelta);

    /// <summary>Scrolls left by one mouse-wheel unit.</summary>
    public virtual void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - RibbonScrollCompatibility.WheelDelta);

    /// <summary>Scrolls right by one mouse-wheel unit.</summary>
    public virtual void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + RibbonScrollCompatibility.WheelDelta);

    /// <summary>Scrolls up by one mouse-wheel unit.</summary>
    public virtual void MouseWheelUp() => SetVerticalOffset(VerticalOffset - RibbonScrollCompatibility.WheelDelta);

    /// <summary>Scrolls down by one mouse-wheel unit.</summary>
    public virtual void MouseWheelDown() => SetVerticalOffset(VerticalOffset + RibbonScrollCompatibility.WheelDelta);

    /// <summary>Scrolls one page left.</summary>
    public virtual void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);

    /// <summary>Scrolls one page right.</summary>
    public virtual void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);

    /// <summary>Scrolls one page up.</summary>
    public virtual void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);

    /// <summary>Scrolls one page down.</summary>
    public virtual void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);

    /// <summary>Sets the horizontal scroll offset.</summary>
    public virtual void SetHorizontalOffset(double offset) =>
        RibbonScrollCompatibility.SetHorizontalOffset(this, ScrollOwner, offset);

    /// <summary>Sets the vertical scroll offset.</summary>
    public virtual void SetVerticalOffset(double offset) =>
        RibbonScrollCompatibility.SetVerticalOffset(this, ScrollOwner, offset);

    /// <summary>Scrolls until the specified child rectangle is visible.</summary>
    public virtual Rect MakeVisible(UIElement visual, Rect rectangle) =>
        RibbonScrollCompatibility.MakeVisible(this, ScrollOwner, visual, rectangle);
}

/// <summary>
/// Portable scrolling surface for <see cref="RibbonTabsContainer"/>.
/// </summary>
public partial class RibbonTabsContainer
{
    private ScrollViewer? _scrollOwner;

    /// <summary>Gets or sets the scroll viewer owning this panel.</summary>
    public ScrollViewer? ScrollOwner
    {
        get => _scrollOwner ?? RibbonScrollCompatibility.FindOwner(this);
        set => _scrollOwner = value;
    }

    /// <summary>Gets or sets whether horizontal scrolling is enabled.</summary>
    public bool CanHorizontallyScroll { get; set; } = true;

    /// <summary>Gets or sets whether vertical scrolling is enabled.</summary>
    public bool CanVerticallyScroll { get; set; }

    /// <summary>Gets the horizontal extent.</summary>
    public double ExtentWidth => ScrollOwner?.ExtentWidth ?? RibbonScrollCompatibility.GetChildrenWidth(this);

    /// <summary>Gets the vertical extent.</summary>
    public double ExtentHeight => ScrollOwner?.ExtentHeight ?? DesiredSize.Height;

    /// <summary>Gets the horizontal offset.</summary>
    public double HorizontalOffset => ScrollOwner?.HorizontalOffset ?? 0;

    /// <summary>Gets the vertical offset.</summary>
    public double VerticalOffset => ScrollOwner?.VerticalOffset ?? 0;

    /// <summary>Gets the viewport width.</summary>
    public double ViewportWidth => ScrollOwner?.ViewportWidth ?? ActualWidth;

    /// <summary>Gets the viewport height.</summary>
    public double ViewportHeight => ScrollOwner?.ViewportHeight ?? ActualHeight;

    /// <summary>Scrolls left by one line.</summary>
    public virtual void LineLeft() => SetHorizontalOffset(HorizontalOffset - RibbonScrollCompatibility.LineDelta);

    /// <summary>Scrolls right by one line.</summary>
    public virtual void LineRight() => SetHorizontalOffset(HorizontalOffset + RibbonScrollCompatibility.LineDelta);

    /// <summary>Scrolls up by one line.</summary>
    public virtual void LineUp() => SetVerticalOffset(VerticalOffset - RibbonScrollCompatibility.LineDelta);

    /// <summary>Scrolls down by one line.</summary>
    public virtual void LineDown() => SetVerticalOffset(VerticalOffset + RibbonScrollCompatibility.LineDelta);

    /// <summary>Scrolls left by one mouse-wheel unit.</summary>
    public virtual void MouseWheelLeft() => SetHorizontalOffset(HorizontalOffset - RibbonScrollCompatibility.WheelDelta);

    /// <summary>Scrolls right by one mouse-wheel unit.</summary>
    public virtual void MouseWheelRight() => SetHorizontalOffset(HorizontalOffset + RibbonScrollCompatibility.WheelDelta);

    /// <summary>Scrolls up by one mouse-wheel unit.</summary>
    public virtual void MouseWheelUp() => SetVerticalOffset(VerticalOffset - RibbonScrollCompatibility.WheelDelta);

    /// <summary>Scrolls down by one mouse-wheel unit.</summary>
    public virtual void MouseWheelDown() => SetVerticalOffset(VerticalOffset + RibbonScrollCompatibility.WheelDelta);

    /// <summary>Scrolls one page left.</summary>
    public virtual void PageLeft() => SetHorizontalOffset(HorizontalOffset - ViewportWidth);

    /// <summary>Scrolls one page right.</summary>
    public virtual void PageRight() => SetHorizontalOffset(HorizontalOffset + ViewportWidth);

    /// <summary>Scrolls one page up.</summary>
    public virtual void PageUp() => SetVerticalOffset(VerticalOffset - ViewportHeight);

    /// <summary>Scrolls one page down.</summary>
    public virtual void PageDown() => SetVerticalOffset(VerticalOffset + ViewportHeight);

    /// <summary>Sets the horizontal scroll offset.</summary>
    public virtual void SetHorizontalOffset(double offset) =>
        RibbonScrollCompatibility.SetHorizontalOffset(this, ScrollOwner, offset);

    /// <summary>Sets the vertical scroll offset.</summary>
    public virtual void SetVerticalOffset(double offset) =>
        RibbonScrollCompatibility.SetVerticalOffset(this, ScrollOwner, offset);

    /// <summary>Scrolls until the specified child rectangle is visible.</summary>
    public virtual Rect MakeVisible(UIElement visual, Rect rectangle) =>
        RibbonScrollCompatibility.MakeVisible(this, ScrollOwner, visual, rectangle);
}

internal static class RibbonScrollCompatibility
{
    internal const double LineDelta = 16;
    internal const double WheelDelta = 48;

    internal static ScrollViewer? FindOwner(DependencyObject element)
    {
        for (var current = VisualTreeHelper.GetParent(element);
             current is not null;
             current = VisualTreeHelper.GetParent(current))
        {
            if (current is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }
        }

        return null;
    }

    internal static double GetChildrenWidth(Panel panel) =>
        panel.Children.Sum(child => child.DesiredSize.Width);

    internal static void SetHorizontalOffset(
        FrameworkElement element,
        ScrollViewer? owner,
        double offset)
    {
        if (owner is null)
        {
            element.StartBringIntoView();
            return;
        }

        var maximum = Math.Max(0, owner.ExtentWidth - owner.ViewportWidth);
        owner.ChangeView(Math.Clamp(offset, 0, maximum), null, null);
    }

    internal static void SetVerticalOffset(
        FrameworkElement element,
        ScrollViewer? owner,
        double offset)
    {
        if (owner is null)
        {
            element.StartBringIntoView();
            return;
        }

        var maximum = Math.Max(0, owner.ExtentHeight - owner.ViewportHeight);
        owner.ChangeView(null, Math.Clamp(offset, 0, maximum), null);
    }

    internal static Rect MakeVisible(
        FrameworkElement element,
        ScrollViewer? owner,
        UIElement visual,
        Rect rectangle)
    {
        try
        {
            var point = visual.TransformToVisual(element).TransformPoint(
                new Point(rectangle.X, rectangle.Y));
            var target = new Rect(point.X, point.Y, rectangle.Width, rectangle.Height);
            if (owner is not null)
            {
                SetHorizontalOffset(element, owner, point.X + owner.HorizontalOffset);
                SetVerticalOffset(element, owner, point.Y + owner.VerticalOffset);
            }
            else
            {
                visual.StartBringIntoView();
            }

            return target;
        }
        catch
        {
            visual.StartBringIntoView();
            return rectangle;
        }
    }
}
