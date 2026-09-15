namespace FluentUno.UITests;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
[NonParallelizable]
[Category("UI")]
[Category("PortParity")]
public class PortParityAutoTestTests
{
    private static readonly IReadOnlyDictionary<int, string[]> RequiredCases =
        new Dictionary<int, string[]>
        {
            [1] =
            [
                "qat-overloads", "qat-tracking", "itemscontrol-inheritance",
                "gallery-selection", "tab-collection", "contextual-group",
            ],
            [2] =
            [
                "gallery-enter", "command-enablement", "wpf-keytip-scopes",
                "minimized-content", "startscreen-host", "spinner-converter",
            ],
            [3] = ["qat-clones", "inert-popup-properties", "menu-presentation"],
            [4] = ["ribbon-inert-options", "editor-header-templates"],
        };

    [Test]
    public Task DesktopPortParityPhase1() => RunPhaseAsync(1);

    [Test]
    public Task DesktopPortParityPhase2() => RunPhaseAsync(2);

    [Test]
    public Task DesktopPortParityPhase3() => RunPhaseAsync(3);

    [Test]
    public Task DesktopPortParityPhase4() => RunPhaseAsync(4);

    [Test]
    public Task DesktopPresentationContracts() => RunPhaseAsync(4, focused: true);

    private static async Task RunPhaseAsync(int phase, bool focused = false)
    {
        var options = new Dictionary<string, string>
        {
            ["SHOWCASE_PORT_PARITY_AUTOTEST_PHASE"] = phase.ToString(CultureInfo.InvariantCulture),
        };
        if (focused)
        {
            options["SHOWCASE_PORT_PARITY_ONLY_PHASE"] = phase.ToString(CultureInfo.InvariantCulture);
        }
        var log = await ShowcaseTestHost.RunAsync(
            $"showcase-port-parity-phase-{phase}{(focused ? "-focused" : string.Empty)}.log", options);
        var requiredCases = RequiredCases.Where(entry => focused ? entry.Key == phase : entry.Key <= phase).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(
                log,
                Does.Contain(focused
                    ? $"PORT-PARITY FOCUSED COMPLETE phase={phase} cases={requiredCases.Sum(entry => entry.Value.Length)}"
                    : $"PORT-PARITY COMPLETE throughPhase={phase} cases={requiredCases.Sum(entry => entry.Value.Length)}"));
            foreach (var (casePhase, identifiers) in requiredCases)
            {
                foreach (var identifier in identifiers)
                {
                    Assert.That(
                        log,
                        Does.Contain($"PORT-PARITY PASS phase={casePhase} id={identifier}"),
                        $"The native Showcase did not pass required parity case '{identifier}'.");
                }
            }
        });
    }
}
