namespace Fluent.Ribbon.ApiCompatibility;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);
            if (options.ShowHelp)
            {
                PrintUsage();
                return 0;
            }

            var normalizer = new FrameworkTypeNormalizer();
            var reader = new MetadataApiReader(normalizer);
            var reference = reader.ReadReference(options.ReferencePath!);
            var candidate = new ApiAssemblyMerger().Merge(
                options.CandidatePaths.Select(
                    path => new CandidateApiAssembly(path, reader.ReadCandidate(path))));
            var result = new ApiComparer().Compare(reference, candidate);
            var ledger = ExceptionLedger.Load(options.ExceptionLedgerPath!);
            var approved = new List<(ApiIssue Issue, ApiExceptionEntry Entry)>();
            var unapproved = new List<ApiIssue>();

            foreach (var issue in result.Issues)
            {
                if (ledger.TryApprove(issue, out var entry))
                {
                    approved.Add((issue, entry!));
                }
                else
                {
                    unapproved.Add(issue);
                }
            }

            foreach (var issue in unapproved)
            {
                Console.WriteLine($"[GAP] {issue.Id}");
                Console.WriteLine($"      {issue.Details}");
            }

            foreach (var (issue, entry) in approved)
            {
                Console.WriteLine($"[APPROVED] {issue.Id}");
                Console.WriteLine($"           {entry.Reason}");
            }

            var fingerprintMismatches = ledger.FindFingerprintMismatches(result.Issues);
            foreach (var mismatch in fingerprintMismatches)
            {
                Console.WriteLine($"[LEDGER-MISMATCH] {mismatch.Entry.Id}");
                Console.WriteLine($"                  ledger {mismatch.Entry.DetailFingerprint}");
                Console.WriteLine($"                  actual {mismatch.ActualDetailFingerprint}");
            }

            var stale = ledger.FindStaleExceptions(result.Issues);
            foreach (var entry in stale)
            {
                Console.WriteLine($"[STALE] {entry.Id}");
            }

            ApiReportSummary.Write(Console.Out, unapproved);

            Console.WriteLine();
            var candidateAssemblyLabel = options.CandidatePaths.Count == 1 ? "assembly" : "assemblies";
            Console.WriteLine(
                $"Compared {reference.Types.Count} reference types with {candidate.Types.Count} candidate types "
                + $"from {options.CandidatePaths.Count} candidate {candidateAssemblyLabel}: "
                + $"{unapproved.Count} unapproved gap(s), {approved.Count} approved exception(s), "
                + $"{stale.Count} stale exception(s), {fingerprintMismatches.Count} fingerprint mismatch(es).");

            if (options.Mode is CompatibilityMode.Enforce && unapproved.Count > 0)
            {
                Console.Error.WriteLine("API compatibility enforcement failed.");
                return 1;
            }

            if (options.Mode is CompatibilityMode.Report && unapproved.Count > 0)
            {
                Console.WriteLine("Report mode does not fail on unapproved gaps.");
            }

            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException
                                          or BadImageFormatException
                                          or FileNotFoundException
                                          or DirectoryNotFoundException
                                          or InvalidDataException
                                          or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            Console.Error.WriteLine();
            PrintUsage();
            return 2;
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            Fluent.Ribbon API compatibility baseline

            Usage:
              dotnet run --project Fluent.Ribbon.ApiCompatibility.csproj -- \
                --reference <Fluent.dll> \
                --candidate <Fluent.Ribbon.Uno.dll> \
                [--candidate <additional-candidate.dll>]... \
                [--exceptions <exceptions.wpf-only.json>] \
                [--mode report|enforce]

            report  Prints all gaps and exits successfully (default).
            enforce Returns exit code 1 when any gap is not in the exception ledger.
            """);
    }

}
