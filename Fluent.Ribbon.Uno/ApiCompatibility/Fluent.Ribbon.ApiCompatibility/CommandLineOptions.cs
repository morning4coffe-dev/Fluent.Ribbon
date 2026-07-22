namespace Fluent.Ribbon.ApiCompatibility;

public enum CompatibilityMode
{
    Enforce,
    Report
}

public sealed record CommandLineOptions(
    string? ReferencePath,
    IReadOnlyList<string> CandidatePaths,
    string? ExceptionLedgerPath,
    CompatibilityMode Mode,
    bool ShowHelp)
{
    public static CommandLineOptions Parse(string[] args)
    {
        if (args.Any(static argument => argument is "-h" or "--help"))
        {
            return new CommandLineOptions(null, [], null, CompatibilityMode.Report, true);
        }

        string? reference = null;
        var candidates = new List<string>();
        string? exceptions = null;
        var mode = CompatibilityMode.Report;

        for (var index = 0; index < args.Length; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--reference":
                    reference = ReadValue(args, ref index, argument);
                    break;
                case "--candidate":
                    candidates.Add(ReadValue(args, ref index, argument));
                    break;
                case "--exceptions":
                    exceptions = ReadValue(args, ref index, argument);
                    break;
                case "--mode":
                    var value = ReadValue(args, ref index, argument);
                    mode = value.ToLowerInvariant() switch
                    {
                        "report" => CompatibilityMode.Report,
                        "enforce" => CompatibilityMode.Enforce,
                        _ => throw new ArgumentException($"Unknown mode '{value}'.")
                    };
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{argument}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException("--reference is required.");
        }

        if (candidates.Count == 0)
        {
            throw new ArgumentException("At least one --candidate is required.");
        }

        exceptions ??= Path.Combine(AppContext.BaseDirectory, "exceptions.wpf-only.json");
        return new CommandLineOptions(
            Path.GetFullPath(reference),
            candidates.Select(Path.GetFullPath).ToArray(),
            Path.GetFullPath(exceptions),
            mode,
            false);
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        index++;
        if (index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"{option} requires a value.");
        }

        return args[index];
    }
}
