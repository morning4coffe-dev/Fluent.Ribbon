namespace Fluent;

using Windows.Foundation;

/// <summary>Portable point hit-test parameters.</summary>
public class PointHitTestParameters
{
    /// <summary>Initializes a new instance.</summary>
    public PointHitTestParameters(Point hitPoint)
    {
        HitPoint = hitPoint;
    }

    /// <summary>Gets the point being tested.</summary>
    public Point HitPoint { get; }
}

/// <summary>Portable hit-test result.</summary>
public class HitTestResult
{
    /// <summary>Initializes a new instance.</summary>
    public HitTestResult(DependencyObject visualHit)
    {
        VisualHit = visualHit;
    }

    /// <summary>Gets the hit visual.</summary>
    public DependencyObject VisualHit { get; }
}
