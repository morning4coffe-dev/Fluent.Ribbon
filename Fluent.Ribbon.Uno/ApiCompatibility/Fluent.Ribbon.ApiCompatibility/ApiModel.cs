namespace Fluent.Ribbon.ApiCompatibility;

public enum ApiAccessibility
{
    Protected,
    ProtectedInternal,
    Public
}

public enum ApiMemberKind
{
    Constructor,
    Event,
    Field,
    Method,
    Property
}

public enum ApiTypeKind
{
    Class,
    Delegate,
    Enum,
    Interface,
    Struct
}

public sealed record ApiAccessor(ApiAccessibility Accessibility, bool IsStatic);

public sealed record ApiConstant(string TypeCode, string Value)
{
    public override string ToString() => $"{this.TypeCode}:{this.Value}";
}

public sealed record ApiParameter(
    string Type,
    string Modifier = "",
    bool IsOptional = false,
    ApiConstant? DefaultValue = null)
{
    public string Identity => $"{this.Modifier}{this.Type}";

    public string Display =>
        this.DefaultValue is not null
            ? $"{this.Identity} = {this.DefaultValue}"
            : this.IsOptional
                ? $"{this.Identity} optional"
                : this.Identity;
}

public sealed record ApiMember(
    string Identity,
    string DisplayName,
    ApiMemberKind Kind,
    ApiAccessibility Accessibility,
    bool IsStatic,
    string Type,
    IReadOnlyList<ApiParameter> Parameters,
    int GenericArity = 0,
    bool IsAbstract = false,
    bool IsVirtual = false,
    bool IsFinal = false,
    bool IsReadOnly = false,
    bool IsLiteral = false,
    IReadOnlyDictionary<string, ApiAccessor>? Accessors = null,
    ApiConstant? LiteralValue = null);

public sealed record ApiType(
    string Name,
    ApiTypeKind Kind,
    ApiAccessibility Accessibility,
    int GenericArity,
    string? BaseType,
    IReadOnlySet<string> Interfaces,
    bool IsAbstract,
    bool IsSealed,
    IReadOnlyDictionary<string, ApiMember> Members);

public sealed record ApiAssembly(IReadOnlyDictionary<string, ApiType> Types);
