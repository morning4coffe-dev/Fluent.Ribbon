using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Fluent.Ribbon.ApiCompatibility;

public sealed record ApiExceptionEntry(string Id, string Reason, string? DetailFingerprint = null);

public sealed record ApiExceptionDocument(int SchemaVersion, IReadOnlyList<ApiExceptionEntry> Exceptions);

public sealed record ApiExceptionMismatch(
    ApiExceptionEntry Entry,
    ApiIssue Issue,
    string ActualDetailFingerprint);

public static partial class IssueDetailFingerprint
{
    public static string Compute(string details)
    {
        ArgumentNullException.ThrowIfNull(details);
        var clauses = details
            .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(static clause => Whitespace().Replace(clause, " ").Trim())
            .OrderBy(static clause => clause, StringComparer.Ordinal);
        var normalized = string.Join("; ", clauses);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return $"sha256:{Convert.ToHexStringLower(hash)}";
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex("^sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    internal static partial Regex Sha256Format();
}

public sealed class ExceptionLedger
{
    private readonly IReadOnlyDictionary<string, ApiExceptionEntry> entries;

    private ExceptionLedger(IReadOnlyDictionary<string, ApiExceptionEntry> entries)
    {
        this.entries = entries;
    }

    public static ExceptionLedger Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(File.ReadAllText(path));
    }

    public static ExceptionLedger Parse(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var document = JsonSerializer.Deserialize<ApiExceptionDocument>(json, options)
                       ?? throw new InvalidDataException("The exception ledger is empty.");

        if (document.SchemaVersion is not (1 or 2))
        {
            throw new InvalidDataException($"Unsupported exception ledger schema version {document.SchemaVersion}.");
        }

        var entries = new Dictionary<string, ApiExceptionEntry>(StringComparer.Ordinal);
        foreach (var entry in document.Exceptions ?? [])
        {
            if (string.IsNullOrWhiteSpace(entry.Id) || string.IsNullOrWhiteSpace(entry.Reason))
            {
                throw new InvalidDataException("Every exception must contain a non-empty id and reason.");
            }

            if (document.SchemaVersion >= 2 && string.IsNullOrWhiteSpace(entry.DetailFingerprint))
            {
                throw new InvalidDataException(
                    $"Exception '{entry.Id}' must contain a detailFingerprint in schema version 2.");
            }

            if (entry.DetailFingerprint is not null
                && IssueDetailFingerprint.Sha256Format().IsMatch(entry.DetailFingerprint) is false)
            {
                throw new InvalidDataException(
                    $"Exception '{entry.Id}' has an unsupported detailFingerprint format.");
            }

            if (entries.TryAdd(entry.Id, entry) is false)
            {
                throw new InvalidDataException($"Duplicate exception id '{entry.Id}'.");
            }
        }

        return new ExceptionLedger(entries);
    }

    public bool TryApprove(ApiIssue issue, out ApiExceptionEntry? entry)
    {
        if (this.entries.TryGetValue(issue.Id, out entry) is false)
        {
            return false;
        }

        return entry.DetailFingerprint is null
               || entry.DetailFingerprint.Equals(
                   IssueDetailFingerprint.Compute(issue.Details),
                   StringComparison.Ordinal);
    }

    public IReadOnlyList<ApiExceptionMismatch> FindFingerprintMismatches(IEnumerable<ApiIssue> issues)
    {
        return issues
            .Select(issue =>
            {
                if (this.entries.TryGetValue(issue.Id, out var entry) is false
                    || entry.DetailFingerprint is null)
                {
                    return null;
                }

                var actual = IssueDetailFingerprint.Compute(issue.Details);
                return entry.DetailFingerprint.Equals(actual, StringComparison.Ordinal)
                    ? null
                    : new ApiExceptionMismatch(entry, issue, actual);
            })
            .Where(static mismatch => mismatch is not null)
            .Cast<ApiExceptionMismatch>()
            .OrderBy(static mismatch => mismatch.Entry.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<ApiExceptionEntry> FindStaleExceptions(IEnumerable<ApiIssue> issues)
    {
        var activeIds = issues.Select(static issue => issue.Id).ToHashSet(StringComparer.Ordinal);
        return this.entries.Values
            .Where(entry => activeIds.Contains(entry.Id) is false)
            .OrderBy(static entry => entry.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
