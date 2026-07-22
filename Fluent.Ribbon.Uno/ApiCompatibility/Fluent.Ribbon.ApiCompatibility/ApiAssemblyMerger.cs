namespace Fluent.Ribbon.ApiCompatibility;

public sealed record CandidateApiAssembly(string Source, ApiAssembly Assembly);

public sealed class ApiAssemblyMerger
{
    public ApiAssembly Merge(IEnumerable<CandidateApiAssembly> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var types = new Dictionary<string, ApiType>(StringComparer.Ordinal);
        var typeSources = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            foreach (var (typeName, type) in candidate.Assembly.Types)
            {
                if (types.TryAdd(typeName, type) is false)
                {
                    throw new InvalidDataException(
                        $"Candidate assemblies '{typeSources[typeName]}' and '{candidate.Source}' "
                        + $"both define public type '{typeName}'.");
                }

                typeSources.Add(typeName, candidate.Source);
            }
        }

        return new ApiAssembly(types);
    }
}
