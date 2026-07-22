namespace Fluent.Ribbon.ApiCompatibility;

public enum ApiIssueKind
{
    IncompatibleMember,
    IncompatibleType,
    MissingMember,
    MissingType
}

public sealed record ApiIssue(ApiIssueKind Kind, string Id, string Symbol, string Details);

public sealed record ApiComparisonResult(IReadOnlyList<ApiIssue> Issues);

public sealed class ApiComparer
{
    private readonly FrameworkCompatibilityPolicy frameworkPolicy;

    public ApiComparer(FrameworkCompatibilityPolicy? frameworkPolicy = null)
    {
        this.frameworkPolicy = frameworkPolicy ?? new FrameworkCompatibilityPolicy();
    }

    public ApiComparisonResult Compare(ApiAssembly reference, ApiAssembly candidate)
    {
        var issues = new List<ApiIssue>();
        var candidateSurface = new CandidateSurface(candidate);

        foreach (var expectedType in reference.Types.Values.OrderBy(static type => type.Name, StringComparer.Ordinal))
        {
            if (candidate.Types.TryGetValue(expectedType.Name, out var actualType) is false)
            {
                issues.Add(new ApiIssue(
                    ApiIssueKind.MissingType,
                    $"missing-type:{expectedType.Name}",
                    expectedType.Name,
                    "The candidate assembly does not expose this public type."));
                continue;
            }

            var typeDifferences = GetTypeDifferences(expectedType, actualType, candidateSurface);
            if (typeDifferences.Count > 0)
            {
                issues.Add(new ApiIssue(
                    ApiIssueKind.IncompatibleType,
                    $"incompatible-type:{expectedType.Name}",
                    expectedType.Name,
                    string.Join("; ", typeDifferences)));
            }

            var effectiveMembers = candidateSurface.GetEffectiveMembers(actualType);
            foreach (var expectedMember in expectedType.Members.Values.OrderBy(static member => member.Identity, StringComparer.Ordinal))
            {
                var contract = GetMemberContract(expectedMember);
                if (effectiveMembers.TryGetValue(contract, out var actualMember) is false)
                {
                    actualMember = candidateSurface.FindSourceCompatibleMember(
                        actualType,
                        expectedMember);
                    if (actualMember is null)
                    {
                        issues.Add(new ApiIssue(
                            ApiIssueKind.MissingMember,
                            $"missing-member:{expectedMember.Identity}",
                            expectedMember.DisplayName,
                            "The candidate type does not expose this normalized member contract, directly or through inheritance."));
                        continue;
                    }
                }

                var memberDifferences = this.GetMemberDifferences(
                    expectedMember,
                    actualMember,
                    candidateSurface);
                if (memberDifferences.Count > 0)
                {
                    issues.Add(new ApiIssue(
                        ApiIssueKind.IncompatibleMember,
                        $"incompatible-member:{expectedMember.Identity}",
                        expectedMember.DisplayName,
                        string.Join("; ", memberDifferences)));
                }
            }
        }

        return new ApiComparisonResult(issues);
    }

    private List<string> GetTypeDifferences(
        ApiType expected,
        ApiType actual,
        CandidateSurface candidateSurface)
    {
        var differences = new List<string>();
        var baseChain = candidateSurface.GetBaseChain(actual);
        var effectiveInterfaces = candidateSurface.GetEffectiveInterfaces(actual);

        AddDifference(differences, expected.Kind == actual.Kind, $"kind expected {expected.Kind}, actual {actual.Kind}");
        AddDifference(differences, expected.GenericArity == actual.GenericArity, $"generic arity expected {expected.GenericArity}, actual {actual.GenericArity}");
        AddDifference(differences, IsAccessibilityCompatible(expected.Accessibility, actual.Accessibility), $"accessibility expected {expected.Accessibility}, actual {actual.Accessibility}");
        AddDifference(
            differences,
            expected.BaseType is null
            || baseChain.Contains(expected.BaseType)
            || this.frameworkPolicy.AllowsFrameworkBaseTypeAncestry(
                expected.BaseType,
                baseChain)
            || this.frameworkPolicy.AllowsBaseTypeAsInterface(
                expected.BaseType,
                effectiveInterfaces),
            $"base chain does not contain expected {expected.BaseType ?? "<none>"}; direct candidate base is {actual.BaseType ?? "<none>"}");
        AddDifference(differences, expected.Interfaces.IsSubsetOf(effectiveInterfaces), "one or more implemented or inherited interfaces are missing");
        AddDifference(differences, expected.IsAbstract || actual.IsAbstract is false, "candidate became abstract");
        AddDifference(differences, expected.IsSealed || actual.IsSealed is false, "candidate became sealed");

        return differences;
    }

    private List<string> GetMemberDifferences(
        ApiMember expected,
        ApiMember actual,
        CandidateSurface candidateSurface)
    {
        var differences = new List<string>();

        AddDifference(differences, expected.Kind == actual.Kind, $"kind expected {expected.Kind}, actual {actual.Kind}");
        AddDifference(
            differences,
            IsAccessibilityCompatible(expected.Accessibility, actual.Accessibility)
            || this.frameworkPolicy.AllowsAccessibilityDifference(expected, actual),
            $"accessibility expected {expected.Accessibility}, actual {actual.Accessibility}");
        AddDifference(differences, expected.IsStatic == actual.IsStatic, $"static expected {expected.IsStatic}, actual {actual.IsStatic}");
        AddDifference(differences, expected.Type == actual.Type, $"type expected {expected.Type}, actual {actual.Type}");
        AddDifference(differences, expected.GenericArity == actual.GenericArity, $"generic arity expected {expected.GenericArity}, actual {actual.GenericArity}");
        AddParameterDifferences(
            differences,
            expected.Parameters,
            actual.Parameters,
            candidateSurface);
        AddDifference(
            differences,
            expected.IsAbstract || actual.IsAbstract is false,
            "candidate member became abstract");
        AddDifference(differences, expected.IsAbstract is false || actual.IsAbstract || actual.IsVirtual, "candidate member is no longer abstract or virtual");
        AddDifference(differences, expected.IsVirtual is false || actual.IsVirtual, "candidate member is no longer virtual");
        AddDifference(differences, expected.IsFinal || actual.IsFinal is false, "candidate member became final");
        AddDifference(differences, expected.IsReadOnly == actual.IsReadOnly, $"readonly expected {expected.IsReadOnly}, actual {actual.IsReadOnly}");
        AddDifference(differences, expected.IsLiteral == actual.IsLiteral, $"literal expected {expected.IsLiteral}, actual {actual.IsLiteral}");
        AddDifference(
            differences,
            expected.LiteralValue == actual.LiteralValue,
            $"literal value expected {FormatConstant(expected.LiteralValue)}, actual {FormatConstant(actual.LiteralValue)}");
        AddAccessorDifferences(differences, expected.Accessors, actual.Accessors);

        return differences;
    }

    private static string FormatConstant(ApiConstant? value) => value?.ToString() ?? "<none>";

    private static void AddAccessorDifferences(
        ICollection<string> differences,
        IReadOnlyDictionary<string, ApiAccessor>? expected,
        IReadOnlyDictionary<string, ApiAccessor>? actual)
    {
        if (expected is null)
        {
            return;
        }

        foreach (var (name, expectedAccessor) in expected)
        {
            if (actual is null || actual.TryGetValue(name, out var actualAccessor) is false)
            {
                differences.Add($"missing {name} accessor");
                continue;
            }

            if (IsAccessibilityCompatible(expectedAccessor.Accessibility, actualAccessor.Accessibility) is false)
            {
                differences.Add($"{name} accessibility expected {expectedAccessor.Accessibility}, actual {actualAccessor.Accessibility}");
            }

            if (expectedAccessor.IsStatic != actualAccessor.IsStatic)
            {
                differences.Add($"{name} static expected {expectedAccessor.IsStatic}, actual {actualAccessor.IsStatic}");
            }
        }
    }

    private static void AddParameterDifferences(
        ICollection<string> differences,
        IReadOnlyList<ApiParameter> expected,
        IReadOnlyList<ApiParameter> actual,
        CandidateSurface candidateSurface)
    {
        if (expected.Count != actual.Count)
        {
            differences.Add($"parameter count expected {expected.Count}, actual {actual.Count}");
            return;
        }

        for (var index = 0; index < expected.Count; index++)
        {
            if (AreParametersCompatible(
                    expected[index],
                    actual[index],
                    candidateSurface) is false)
            {
                differences.Add(
                    $"parameter {index + 1} expected {expected[index].Display}, actual {actual[index].Display}");
            }
        }
    }

    private static bool AreParametersCompatible(
        ApiParameter expected,
        ApiParameter actual,
        CandidateSurface candidateSurface)
    {
        return expected.Modifier == actual.Modifier
               && expected.IsOptional == actual.IsOptional
               && expected.DefaultValue == actual.DefaultValue
               && (expected.Type == actual.Type
                   || string.IsNullOrEmpty(expected.Modifier)
                   && candidateSurface.IsAssignableTo(
                       expected.Type,
                       actual.Type));
    }

    private static bool IsAccessibilityCompatible(ApiAccessibility expected, ApiAccessibility actual)
    {
        return expected switch
        {
            ApiAccessibility.Public => actual is ApiAccessibility.Public,
            ApiAccessibility.ProtectedInternal => actual is ApiAccessibility.ProtectedInternal or ApiAccessibility.Public,
            ApiAccessibility.Protected => actual is ApiAccessibility.Protected or ApiAccessibility.ProtectedInternal or ApiAccessibility.Public,
            _ => false
        };
    }

    private static void AddDifference(ICollection<string> differences, bool condition, string message)
    {
        if (condition is false)
        {
            differences.Add(message);
        }
    }

    private static string GetMemberContract(ApiMember member)
    {
        var declaringTypeSeparator = member.Identity.IndexOf("::", StringComparison.Ordinal);
        if (declaringTypeSeparator < 0)
        {
            throw new InvalidDataException($"Member identity '{member.Identity}' does not contain a declaring type separator.");
        }

        return $"{member.Kind}:{member.Identity[(declaringTypeSeparator + 2)..]}";
    }

    private static string GetMemberContractStem(ApiMember member)
    {
        var contract = GetMemberContract(member);
        var parameterStart = contract.IndexOf('(', StringComparison.Ordinal);
        return parameterStart < 0 ? contract : contract[..parameterStart];
    }

    private sealed class CandidateSurface
    {
        private readonly ApiAssembly candidate;
        private readonly Dictionary<string, IReadOnlySet<string>> baseChains = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlySet<string>> effectiveInterfaces = new(StringComparer.Ordinal);
        private readonly Dictionary<string, IReadOnlyDictionary<string, ApiMember>> effectiveMembers = new(StringComparer.Ordinal);

        public CandidateSurface(ApiAssembly candidate)
        {
            this.candidate = candidate;
        }

        public IReadOnlySet<string> GetBaseChain(ApiType type)
        {
            if (this.baseChains.TryGetValue(type.Name, out var cached))
            {
                return cached;
            }

            var chain = new HashSet<string>(StringComparer.Ordinal);
            var baseTypeName = type.BaseType;
            while (baseTypeName is not null && chain.Add(baseTypeName))
            {
                if (this.candidate.Types.TryGetValue(baseTypeName, out var baseType) is false)
                {
                    break;
                }

                baseTypeName = baseType.BaseType;
            }

            this.baseChains.Add(type.Name, chain);
            return chain;
        }

        public IReadOnlySet<string> GetEffectiveInterfaces(ApiType type)
        {
            if (this.effectiveInterfaces.TryGetValue(type.Name, out var cached))
            {
                return cached;
            }

            var interfaces = new HashSet<string>(StringComparer.Ordinal);
            var visitedTypes = new HashSet<string>(StringComparer.Ordinal);
            AddTypeInterfaces(type, interfaces, visitedTypes);

            var baseTypeName = type.BaseType;
            while (baseTypeName is not null && visitedTypes.Add(baseTypeName))
            {
                if (this.candidate.Types.TryGetValue(baseTypeName, out var baseType) is false)
                {
                    break;
                }

                AddTypeInterfaces(baseType, interfaces, visitedTypes);
                baseTypeName = baseType.BaseType;
            }

            this.effectiveInterfaces.Add(type.Name, interfaces);
            return interfaces;
        }

        public IReadOnlyDictionary<string, ApiMember> GetEffectiveMembers(ApiType type)
        {
            if (this.effectiveMembers.TryGetValue(type.Name, out var cached))
            {
                return cached;
            }

            var members = new Dictionary<string, ApiMember>(StringComparer.Ordinal);
            var visitedTypes = new HashSet<string>(StringComparer.Ordinal);
            AddMembers(type, members, includeConstructors: true);
            visitedTypes.Add(type.Name);

            var baseTypeName = type.BaseType;
            while (baseTypeName is not null && visitedTypes.Add(baseTypeName))
            {
                if (this.candidate.Types.TryGetValue(baseTypeName, out var baseType) is false)
                {
                    break;
                }

                AddMembers(baseType, members, includeConstructors: false);
                baseTypeName = baseType.BaseType;
            }

            if (type.Kind is ApiTypeKind.Interface)
            {
                AddInterfaceMembers(type, members, visitedTypes);
            }

            this.effectiveMembers.Add(type.Name, members);
            return members;
        }

        public ApiMember? FindSourceCompatibleMember(
            ApiType type,
            ApiMember expected)
        {
            if (expected.Kind is not (ApiMemberKind.Constructor or ApiMemberKind.Method))
            {
                return null;
            }

            var expectedStem = GetMemberContractStem(expected);
            return GetEffectiveMembers(type).Values
                .Where(
                    actual => actual.Kind == expected.Kind
                              && actual.GenericArity == expected.GenericArity
                              && actual.Parameters.Count == expected.Parameters.Count
                              && GetMemberContractStem(actual)
                                  .Equals(expectedStem, StringComparison.Ordinal)
                              && expected.Parameters
                                  .Zip(
                                      actual.Parameters,
                                      (expectedParameter, actualParameter) =>
                                          AreParametersCompatible(
                                              expectedParameter,
                                              actualParameter,
                                              this))
                                  .All(static compatible => compatible))
                .OrderByDescending(
                    actual => expected.Parameters
                        .Zip(
                            actual.Parameters,
                            static (expectedParameter, actualParameter) =>
                                expectedParameter.Type == actualParameter.Type)
                        .Count(static exact => exact))
                .FirstOrDefault();
        }

        public bool IsAssignableTo(string sourceTypeName, string targetTypeName)
        {
            if (sourceTypeName.Equals(targetTypeName, StringComparison.Ordinal))
            {
                return true;
            }

            return this.candidate.Types.TryGetValue(
                       sourceTypeName,
                       out var sourceType)
                   && (GetBaseChain(sourceType).Contains(targetTypeName)
                       || GetEffectiveInterfaces(sourceType)
                           .Contains(targetTypeName));
        }

        private void AddTypeInterfaces(
            ApiType type,
            ISet<string> interfaces,
            ISet<string> visitedTypes)
        {
            foreach (var interfaceName in type.Interfaces)
            {
                if (interfaces.Add(interfaceName)
                    && this.candidate.Types.TryGetValue(interfaceName, out var interfaceType)
                    && visitedTypes.Add(interfaceName))
                {
                    AddTypeInterfaces(interfaceType, interfaces, visitedTypes);
                }
            }
        }

        private void AddInterfaceMembers(
            ApiType type,
            IDictionary<string, ApiMember> members,
            ISet<string> visitedTypes)
        {
            foreach (var interfaceName in type.Interfaces)
            {
                if (visitedTypes.Add(interfaceName)
                    && this.candidate.Types.TryGetValue(interfaceName, out var interfaceType))
                {
                    AddMembers(interfaceType, members, includeConstructors: false);
                    AddInterfaceMembers(interfaceType, members, visitedTypes);
                }
            }
        }

        private static void AddMembers(
            ApiType type,
            IDictionary<string, ApiMember> members,
            bool includeConstructors)
        {
            foreach (var member in type.Members.Values)
            {
                if (includeConstructors is false && member.Kind is ApiMemberKind.Constructor)
                {
                    continue;
                }

                members.TryAdd(GetMemberContract(member), member);
            }
        }
    }
}
