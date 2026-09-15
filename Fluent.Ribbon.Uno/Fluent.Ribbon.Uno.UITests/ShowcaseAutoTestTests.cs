namespace FluentUno.UITests;

using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class ShowcaseAutoTestTests
{
    [Test]
    [Category("UI")]
    public async Task DesktopShowcaseShouldCompleteWithoutDiagnosticFailures()
    {
        await ShowcaseTestHost.RunAsync("showcase-autotest.log");
    }
}
