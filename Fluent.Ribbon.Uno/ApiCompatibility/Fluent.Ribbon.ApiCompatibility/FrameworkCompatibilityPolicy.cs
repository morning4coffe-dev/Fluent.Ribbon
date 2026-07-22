namespace Fluent.Ribbon.ApiCompatibility;

public sealed record ApiMemberAccessibilityRule(
    ApiMemberKind Kind,
    string Contract,
    ApiAccessibility ReferenceAccessibility,
    ApiAccessibility CandidateAccessibility,
    string Reason);

public sealed class FrameworkCompatibilityPolicy
{
    private static readonly IReadOnlySet<string> BaseTypeInterfaceSubstitutions =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "Microsoft.UI.Xaml.DependencyObject"
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlySet<string>>
        FrameworkBaseTypeAncestry =
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["Microsoft.UI.Xaml.Controls.Control"] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        "Microsoft.UI.Xaml.Controls.TabViewItem"
                    },
                ["Microsoft.UI.Xaml.Controls.Primitives.Selector"] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        "Microsoft.UI.Xaml.Controls.TabView"
                    },
                ["Microsoft.UI.Xaml.Automation.Peers.SelectorAutomationPeer"] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        "Microsoft.UI.Xaml.Automation.Peers.TabViewAutomationPeer"
                    },
                ["Microsoft.UI.Xaml.Automation.Peers.ToolTipAutomationPeer"] =
                    new HashSet<string>(StringComparer.Ordinal)
                    {
                        "Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer"
                    }
            };

    private static readonly IReadOnlyList<ApiMemberAccessibilityRule> DefaultAccessibilityRules =
    [
        new(
            ApiMemberKind.Method,
            "OnApplyTemplate()",
            ApiAccessibility.Public,
            ApiAccessibility.Protected,
            "WPF exposes FrameworkElement.OnApplyTemplate publicly; WinUI exposes the override as protected."),
        new(
            ApiMemberKind.Method,
            "OnApplyTemplate()",
            ApiAccessibility.Public,
            ApiAccessibility.ProtectedInternal,
            "WPF exposes FrameworkElement.OnApplyTemplate publicly; WinUI may expose the override as protected internal.")
    ];

    private readonly IReadOnlyList<ApiMemberAccessibilityRule> accessibilityRules;

    public FrameworkCompatibilityPolicy(
        IReadOnlyList<ApiMemberAccessibilityRule>? accessibilityRules = null)
    {
        this.accessibilityRules = accessibilityRules ?? DefaultAccessibilityRules;
    }

    public bool AllowsAccessibilityDifference(ApiMember reference, ApiMember candidate)
    {
        var contract = GetContract(reference);
        return this.accessibilityRules.Any(
            rule => rule.Kind == reference.Kind
                    && rule.Contract.Equals(contract, StringComparison.Ordinal)
                    && rule.ReferenceAccessibility == reference.Accessibility
                    && rule.CandidateAccessibility == candidate.Accessibility);
    }

    public bool AllowsBaseTypeAsInterface(
        string expectedBaseType,
        IReadOnlySet<string> candidateInterfaces)
    {
        return BaseTypeInterfaceSubstitutions.Contains(expectedBaseType)
               && candidateInterfaces.Contains(expectedBaseType);
    }

    public bool AllowsFrameworkBaseTypeAncestry(
        string expectedBaseType,
        IReadOnlySet<string> candidateBaseChain)
    {
        return FrameworkBaseTypeAncestry.TryGetValue(
                   expectedBaseType,
                   out var compatibleBases)
               && compatibleBases.Overlaps(candidateBaseChain);
    }

    private static string GetContract(ApiMember member)
    {
        var separator = member.Identity.IndexOf("::", StringComparison.Ordinal);
        return separator < 0 ? member.Identity : member.Identity[(separator + 2)..];
    }
}
