namespace FluentUno.UITests;

using System;
using FluentRibbon.Uno.Showcase;
using NUnit.Framework;

[TestFixture]
public sealed class ShowcaseDiagnosticOptionsTests
{
    [TestCase("--port-parity-phase=3", "3")]
    [TestCase("--PORT-PARITY-PHASE=4", "4")]
    [TestCase("--port-parity-phase=", "")]
    public void FindsExactOptionIgnoringNameCase(string argument, string expected)
    {
        Assert.That(ShowcaseDiagnosticOptions.FindArgument([argument], "port-parity-phase"), Is.EqualTo(expected));
    }

    [Test]
    public void PreservesArgumentValuesAndFirstOccurrence()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ShowcaseDiagnosticOptions.FindArgument(
                [@"--autotest-log=C:\QA evidence\phase3.log"], "autotest-log"),
                Is.EqualTo(@"C:\QA evidence\phase3.log"));
            Assert.That(ShowcaseDiagnosticOptions.FindArgument(
                ["--autotest=1", "--autotest=0"], "autotest"), Is.EqualTo("1"));
            Assert.That(ShowcaseDiagnosticOptions.FindArgument(
                ["--autotest-other=1", "--autotest", "autotest=1"], "autotest"), Is.Null);
        });
    }

    [Test]
    public void EnvironmentWinsAndLaunchArgumentsRemainAFallback()
    {
        var variable = $"FLUENT_DIAGNOSTIC_TEST_{Guid.NewGuid():N}";
        try
        {
            Assert.That(ShowcaseDiagnosticOptions.Get(
                variable, "unique-diagnostic-option", "--unique-diagnostic-option=launch"), Is.EqualTo("launch"));
            Environment.SetEnvironmentVariable(variable, "environment", EnvironmentVariableTarget.Process);
            Assert.That(ShowcaseDiagnosticOptions.Get(
                variable, "unique-diagnostic-option", "--unique-diagnostic-option=launch"), Is.EqualTo("environment"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null, EnvironmentVariableTarget.Process);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExternalInputOrderingRetainsEveryCase(bool externalInput)
    {
        var cases = new[] { "qat-overloads", "gallery-selection", "inert-popup-properties", "menu-presentation" };
        var ordered = ShowcaseDiagnosticOptions.OrderPortParityCases(cases, id => id, externalInput);
        Assert.That(ordered, Is.EqualTo(externalInput
            ? new[] { "inert-popup-properties", "qat-overloads", "gallery-selection", "menu-presentation" }
            : cases));
    }

    [Test]
    public void ExternalInputDoesNotChangeEarlierPhaseOrder()
    {
        var cases = new[] { "qat-overloads", "gallery-selection", "command-enablement" };
        Assert.That(ShowcaseDiagnosticOptions.OrderPortParityCases(cases, id => id, true), Is.EqualTo(cases));
    }

    [TestCase(null, 120)]
    [TestCase("", 120)]
    [TestCase("1", 1)]
    [TestCase("900", 900)]
    [TestCase("1800", 1800)]
    public void ExternalInputTimeoutIsBoundedAndExplicit(string? value, int seconds)
    {
        Assert.That(ShowcaseDiagnosticOptions.GetExternalInputTimeout(value), Is.EqualTo(TimeSpan.FromSeconds(seconds)));
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("1801")]
    [TestCase("1.5")]
    [TestCase("bad")]
    public void InvalidExternalInputTimeoutFailsExplicitly(string value)
    {
        Assert.Throws<InvalidOperationException>(() => ShowcaseDiagnosticOptions.GetExternalInputTimeout(value));
    }

    [TestCase(null, 4, null)]
    [TestCase("", 4, null)]
    [TestCase("4", 4, 4)]
    [TestCase("2", 3, 2)]
    public void FocusedPhaseStaysWithinTheRequestedRange(string? value, int throughPhase, int? expected)
    {
        Assert.That(ShowcaseDiagnosticOptions.GetFocusedPortParityPhase(value, throughPhase), Is.EqualTo(expected));
    }

    [TestCase("0", 4)]
    [TestCase("5", 4)]
    [TestCase("3", 2)]
    [TestCase("bad", 4)]
    public void InvalidFocusedPhaseFailsExplicitly(string value, int throughPhase)
    {
        Assert.Throws<InvalidOperationException>(
            () => ShowcaseDiagnosticOptions.GetFocusedPortParityPhase(value, throughPhase));
    }
}
