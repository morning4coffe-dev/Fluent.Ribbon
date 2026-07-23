namespace FluentUno.UITests;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

[TestFixture]
public class ShowcaseAutoTestTests
{
    [Test]
    [Category("UI")]
    public async Task DesktopShowcaseShouldCompleteWithoutDiagnosticFailures()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_UNO_UI_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Assert.Ignore("Set RUN_UNO_UI_TESTS=1 to run the out-of-process Showcase test.");
        }

        var repositoryRoot = FindRepositoryRoot(TestContext.CurrentContext.TestDirectory);
        var projectPath = Path.Combine(
            repositoryRoot,
            "Fluent.Ribbon.Uno",
            "Fluent.Ribbon.Uno.Showcase",
            "Fluent.Ribbon.Uno.Showcase",
            "Fluent.Ribbon.Uno.Showcase.csproj");
        var logPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "showcase-autotest.log");
        File.Delete(logPath);

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project \"{projectPath}\" -f net10.0-desktop -c Release -p:TargetFrameworks=net10.0-desktop -p:UseSharedCompilation=false",
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
        };

        process.StartInfo.Environment["SHOWCASE_AUTOTEST"] = "1";
        process.StartInfo.Environment["SHOWCASE_AUTOTEST_EXIT"] = "1";
        process.StartInfo.Environment["SHOWCASE_AUTOTEST_LOG"] = logPath;

        Assert.That(process.Start(), Is.True);

        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        var exited = await WaitForExitAsync(process, TimeSpan.FromMinutes(3));

        if (!exited)
        {
            process.Kill(entireProcessTree: true);
            Assert.Fail("The Desktop Showcase auto-test did not exit within three minutes.");
        }

        var output = await standardOutput;
        var error = await standardError;
        var log = File.Exists(logPath) ? await File.ReadAllTextAsync(logPath) : string.Empty;
        var diagnostics = $"{output}{Environment.NewLine}{error}{Environment.NewLine}{log}";

        Assert.Multiple(() =>
        {
            Assert.That(process.ExitCode, Is.Zero, diagnostics);
            Assert.That(log, Does.Contain("COMPLETE"), diagnostics);
            Assert.That(log, Does.Contain("RESULT PASS"), diagnostics);
            Assert.That(log, Does.Not.Contain(" FAIL"), diagnostics);
            Assert.That(log, Does.Not.Contain("THREW"), diagnostics);
            Assert.That(log, Does.Not.Contain("FATAL"), diagnostics);
            Assert.That(diagnostics, Does.Not.Contain("[CRASH]"), diagnostics);
            Assert.That(diagnostics, Does.Not.Contain("Unhandled exception"), diagnostics);
        });
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var timeoutCancellation = new System.Threading.CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(timeoutCancellation.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private static string FindRepositoryRoot(string startingDirectory)
    {
        var directory = new DirectoryInfo(startingDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Fluent.Ribbon.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate Fluent.Ribbon.sln above '{startingDirectory}'.");
    }
}
