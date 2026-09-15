namespace FluentRibbon.Uno.Showcase;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

internal static class ShowcaseDiagnosticOptions
{
    internal static string? Get(string environmentVariable, string argumentName, string launchArguments = "")
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        if (!string.IsNullOrEmpty(value))
        {
            return value;
        }

        return FindArgument(Environment.GetCommandLineArgs(), argumentName)
               ?? FindArgument(launchArguments.Split(
                   ' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), argumentName);
    }

    internal static string? FindArgument(IEnumerable<string> arguments, string argumentName)
    {
        var prefix = $"--{argumentName}=";
        foreach (var argument in arguments)
        {
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return argument[prefix.Length..];
            }
        }
        return null;
    }

    internal static IEnumerable<T> OrderPortParityCases<T>(
        IEnumerable<T> cases, Func<T, string> getId, bool externalInput) =>
        externalInput
            ? cases.OrderBy(test => getId(test) == "inert-popup-properties" ? 0 : 1)
            : cases;

    internal static int? GetFocusedPortParityPhase(string? phase, int throughPhase)
    {
        if (string.IsNullOrEmpty(phase))
        {
            return null;
        }
        if (!int.TryParse(phase, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || value < 1 || value > throughPhase)
        {
            throw new InvalidOperationException("A focused parity phase must be within the selected cumulative phase range.");
        }
        return value;
    }

    internal static TimeSpan GetExternalInputTimeout(string? seconds)
    {
        if (string.IsNullOrEmpty(seconds))
        {
            return TimeSpan.FromMinutes(2);
        }
        if (!int.TryParse(seconds, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            || value is < 1 or > 1800)
        {
            throw new InvalidOperationException("The external pointer response timeout must be between 1 and 1800 seconds.");
        }
        return TimeSpan.FromSeconds(value);
    }

    internal static void ConfigureAutoTestProcess()
    {
        foreach (var (environmentVariable, argumentName) in new[]
                 {
                     ("SHOWCASE_AUTOTEST", "autotest"),
                     ("SHOWCASE_PORT_PARITY_AUTOTEST_PHASE", "port-parity-phase"),
                     ("SHOWCASE_NATIVE_POPUP_EXTERNAL_INPUT", "native-popup-external-input"),
                     ("SHOWCASE_NATIVE_POPUP_INPUT_TIMEOUT_SECONDS", "native-popup-input-timeout"),
                     ("SHOWCASE_AUTOTEST_LOG", "autotest-log"),
                     ("SHOWCASE_AUTOTEST_EXIT", "autotest-exit"),
                 })
        {
            if (Get(environmentVariable, argumentName) is { } value)
            {
                Environment.SetEnvironmentVariable(environmentVariable, value, EnvironmentVariableTarget.Process);
            }
        }
    }
}
