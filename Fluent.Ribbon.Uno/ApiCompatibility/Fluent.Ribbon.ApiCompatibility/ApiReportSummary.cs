namespace Fluent.Ribbon.ApiCompatibility;

public static class ApiReportSummary
{
    public static void Write(TextWriter writer, IReadOnlyCollection<ApiIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(issues);

        writer.WriteLine();
        writer.WriteLine("Unapproved gaps by category:");
        foreach (var group in issues
                     .GroupBy(static issue => issue.Kind)
                     .OrderBy(static group => group.Key))
        {
            writer.WriteLine($"  {group.Key}: {group.Count()}");
        }

        writer.WriteLine("Unapproved gaps by type:");
        foreach (var typeGroup in issues
                     .GroupBy(static issue => GetOwningType(issue), StringComparer.Ordinal)
                     .OrderByDescending(static group => group.Count())
                     .ThenBy(static group => group.Key, StringComparer.Ordinal))
        {
            var categories = typeGroup
                .GroupBy(static issue => issue.Kind)
                .OrderBy(static group => group.Key)
                .Select(static group => $"{group.Key}={group.Count()}");
            writer.WriteLine($"  {typeGroup.Key}: {typeGroup.Count()} ({string.Join(", ", categories)})");
        }
    }

    internal static string GetOwningType(ApiIssue issue)
    {
        var symbol = issue.Symbol;
        var memberSeparator = symbol.IndexOf("::", StringComparison.Ordinal);
        if (memberSeparator < 0)
        {
            return symbol;
        }

        var prefixSeparator = symbol.IndexOf(':');
        return prefixSeparator >= 0
            ? symbol[(prefixSeparator + 1)..memberSeparator]
            : symbol[..memberSeparator];
    }
}
