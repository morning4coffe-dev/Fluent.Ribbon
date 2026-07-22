namespace FluentUno.Tests.Helpers;

using Fluent.Helpers;
using NUnit.Framework;

[TestFixture]
public class DoubleHelperTests
{
    [TestCase(-2.0, 0.0)]
    [TestCase(0.5, 0.5)]
    [TestCase(4.0, 1.0)]
    public void ClampShouldKeepValueInsideRange(double value, double expected)
    {
        Assert.That(DoubleHelper.Clamp(value, 0.0, 1.0), Is.EqualTo(expected));
    }

    [TestCase(double.NaN)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(double.PositiveInfinity)]
    public void GetFiniteOrDefaultShouldReplaceNonFiniteValues(double value)
    {
        Assert.That(DoubleHelper.GetFiniteOrDefault(value, 42.0), Is.EqualTo(42.0));
    }
}
