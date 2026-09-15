namespace FluentUno.UITests;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

internal static class ShowcaseTestHost
{
    internal static async Task<string> RunAsync(
        string logFileName,
        IReadOnlyDictionary<string, string>? options = null)
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_UNO_UI_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Assert.Ignore("Set RUN_UNO_UI_TESTS=1 to run out-of-process Showcase tests.");
        }

        var repositoryRoot = FindRepositoryRoot(TestContext.CurrentContext.TestDirectory);
        var logPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, logFileName);
        File.Delete(logPath);
        using var process = new Process
        {
            StartInfo = CreateStartInfo(repositoryRoot),
        };
        foreach (var name in process.StartInfo.Environment.Keys
                     .Where(name => name.StartsWith("SHOWCASE_", StringComparison.Ordinal)
                                    && name.EndsWith("_AUTOTEST_ONLY", StringComparison.Ordinal))
                     .ToArray())
        {
            process.StartInfo.Environment.Remove(name);
        }

        process.StartInfo.Environment.Remove("SHOWCASE_PORT_PARITY_AUTOTEST_PHASE");
        process.StartInfo.Environment["SHOWCASE_AUTOTEST"] = "1";
        process.StartInfo.Environment["SHOWCASE_AUTOTEST_EXIT"] = "1";
        process.StartInfo.Environment["SHOWCASE_AUTOTEST_LOG"] = logPath;
        process.StartInfo.Environment["UNO_FORCE_SOFTWARE_RENDERING"] = "1";
        if (options is not null)
        {
            foreach (var (name, value) in options)
            {
                process.StartInfo.Environment[name] = value;
            }
        }

        Assert.That(process.Start(), Is.True);
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        if (!await WaitForExitAsync(process, TimeSpan.FromMinutes(6)))
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            Assert.Fail(
                "The Showcase auto-test did not exit within six minutes."
                + Environment.NewLine
                + await ReadWithTimeoutAsync(standardOutput)
                + Environment.NewLine
                + await ReadWithTimeoutAsync(standardError));
        }

        var output = await standardOutput;
        var error = await standardError;
        var log = File.Exists(logPath) ? await File.ReadAllTextAsync(logPath) : string.Empty;
        var diagnostics = $"{output}{Environment.NewLine}{error}{Environment.NewLine}{log}";
        if (File.Exists(logPath))
        {
            TestContext.AddTestAttachment(logPath);
        }

        Assert.Multiple(() =>
        {
            Assert.That(process.ExitCode, Is.Zero, diagnostics);
            Assert.That(log, Does.Contain("COMPLETE"), diagnostics);
            Assert.That(log, Does.Contain("RESULT PASS"), diagnostics);
            Assert.That(log, Does.Not.Contain(" FAIL"), diagnostics);
            Assert.That(log, Does.Not.Contain("THREW"), diagnostics);
            Assert.That(log, Does.Not.Contain("FATAL"), diagnostics);
            Assert.That(diagnostics, Does.Not.Contain("[CRASH]"), diagnostics);
            Assert.That(diagnostics, Does.Not.Contain("unhandled exception").IgnoreCase, diagnostics);
        });
        return log;
    }

    private static ProcessStartInfo CreateStartInfo(string repositoryRoot)
    {
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        var appOverride = Environment.GetEnvironmentVariable("SHOWCASE_AUTOTEST_APP");
        if (!string.IsNullOrWhiteSpace(appOverride))
        {
            var appPath = Path.GetFullPath(appOverride, repositoryRoot);
            if (!File.Exists(appPath))
            {
                throw new FileNotFoundException("The configured Showcase application was not found.", appPath);
            }

            if (string.Equals(Path.GetExtension(appPath), ".dll", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.FileName = "dotnet";
                startInfo.ArgumentList.Add(appPath);
            }
            else
            {
                startInfo.FileName = appPath;
            }

            return startInfo;
        }

        startInfo.FileName = "dotnet";
        foreach (var argument in new[]
                 {
                     "run",
                     "--project",
                     Path.Combine(repositoryRoot, "Fluent.Ribbon.Uno", "Fluent.Ribbon.Uno.Showcase",
                         "Fluent.Ribbon.Uno.Showcase", "Fluent.Ribbon.Uno.Showcase.csproj"),
                     "-f", "net10.0-desktop",
                     "-c", "Release",
                     "-p:TargetFrameworks=net10.0-desktop",
                     "--no-build",
                 })
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        try
        {
            await process.WaitForExitAsync(cancellation.Token);
            return true;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return false;
        }
    }

    private static async Task<string> ReadWithTimeoutAsync(Task<string> readTask)
    {
        var completed = await Task.WhenAny(readTask, Task.Delay(TimeSpan.FromSeconds(10)));
        return ReferenceEquals(completed, readTask)
            ? await readTask
            : "<process output did not close within ten seconds>";
    }

    private static string FindRepositoryRoot(string startingDirectory)
    {
        for (DirectoryInfo? directory = new DirectoryInfo(startingDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Fluent.Ribbon.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate Fluent.Ribbon.sln above '{startingDirectory}'.");
    }
}
