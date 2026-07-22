using NUnit.Framework;

namespace Fluent.Ribbon.ApiCompatibility.Tests;

[TestFixture]
public sealed class ApiComparerTests
{
    [Test]
    public void Compare_ReportsMissingTypesAndMembersWithStableIds()
    {
        var expectedMember = Method("Fluent.Ribbon", "Execute", "System.Void");
        var reference = Assembly(Type("Fluent.Ribbon", expectedMember), Type("Fluent.WpfOnly"));
        var candidate = Assembly(Type("Fluent.Ribbon"));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(
            issues.Select(static issue => issue.Id),
            Is.EquivalentTo(
            [
                "missing-member:M:Fluent.Ribbon::Execute()",
                "missing-type:Fluent.WpfOnly"
            ]));
    }

    [Test]
    public void Compare_AllowsBroaderAccessibilityAndAdditionalInterfaces()
    {
        var expectedMember = Method(
            "Fluent.Ribbon",
            "OnChanged",
            "System.Void",
            ApiAccessibility.Protected);
        var actualMember = expectedMember with { Accessibility = ApiAccessibility.Public };
        var referenceType = Type(
            "Fluent.Ribbon",
            expectedMember,
            interfaces: new HashSet<string>(["System.IDisposable"], StringComparer.Ordinal));
        var candidateType = Type(
            "Fluent.Ribbon",
            actualMember,
            interfaces: new HashSet<string>(["System.IDisposable", "System.IAsyncDisposable"], StringComparer.Ordinal));

        var issues = new ApiComparer().Compare(Assembly(referenceType), Assembly(candidateType)).Issues;

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Compare_ReportsIncompatibleReturnType()
    {
        var expectedMember = Method("Fluent.Ribbon", "GetValue", "System.String");
        var actualMember = expectedMember with { Type = "System.Object" };

        var issues = new ApiComparer().Compare(
            Assembly(Type("Fluent.Ribbon", expectedMember)),
            Assembly(Type("Fluent.Ribbon", actualMember))).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Id, Is.EqualTo("incompatible-member:M:Fluent.Ribbon::GetValue()"));
        Assert.That(issues[0].Details, Does.Contain("type expected System.String, actual System.Object"));
    }

    [Test]
    public void Compare_ReportsRemovedPropertySetter()
    {
        var getter = new ApiAccessor(ApiAccessibility.Public, false);
        var expectedMember = Property(
            "Fluent.Ribbon",
            "Header",
            new Dictionary<string, ApiAccessor>(StringComparer.Ordinal)
            {
                ["get"] = getter,
                ["set"] = getter
            });
        var actualMember = expectedMember with
        {
            Accessors = new Dictionary<string, ApiAccessor>(StringComparer.Ordinal)
            {
                ["get"] = getter
            }
        };

        var issues = new ApiComparer().Compare(
            Assembly(Type("Fluent.Ribbon", expectedMember)),
            Assembly(Type("Fluent.Ribbon", actualMember))).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Details, Does.Contain("missing set accessor"));
    }

    [Test]
    public void Compare_UsesInheritedMembersForWrapperTypes()
    {
        var expectedMember = Method("Fluent.Button", "Execute", "System.Void");
        var inheritedMember = Method("Fluent.RibbonButton", "Execute", "System.Void");
        var reference = Assembly(
            Type(
                "Fluent.Button",
                expectedMember,
                baseType: "Microsoft.UI.Xaml.Controls.Button"));
        var candidate = Assembly(
            Type("Fluent.Button", baseType: "Fluent.RibbonButton"),
            Type(
                "Fluent.RibbonButton",
                inheritedMember,
                baseType: "Microsoft.UI.Xaml.Controls.Button"));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Compare_DerivedHiddenMemberWinsOverCompatibleBaseMember()
    {
        var expectedMember = Method("Fluent.Button", "GetValue", "System.String");
        var hiddenMember = Method("Fluent.Button", "GetValue", "System.Object");
        var compatibleBaseMember = Method("Fluent.RibbonButton", "GetValue", "System.String");
        var reference = Assembly(
            Type("Fluent.Button", expectedMember, baseType: "Fluent.RibbonButton"));
        var candidate = Assembly(
            Type("Fluent.Button", hiddenMember, baseType: "Fluent.RibbonButton"),
            Type("Fluent.RibbonButton", compatibleBaseMember));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Kind, Is.EqualTo(ApiIssueKind.IncompatibleMember));
        Assert.That(issues[0].Details, Does.Contain("type expected System.String, actual System.Object"));
    }

    [Test]
    public void Compare_DoesNotInheritConstructors()
    {
        var expectedConstructor = Constructor("Fluent.Button", "System.String");
        var candidateConstructor = Constructor("Fluent.Button");
        var baseConstructor = Constructor("Fluent.RibbonButton", "System.String");
        var reference = Assembly(
            Type("Fluent.Button", expectedConstructor, baseType: "Fluent.RibbonButton"));
        var candidate = Assembly(
            Type("Fluent.Button", candidateConstructor, baseType: "Fluent.RibbonButton"),
            Type("Fluent.RibbonButton", baseConstructor));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Id, Is.EqualTo("missing-member:M:Fluent.Button::.ctor(System.String)"));
    }

    [Test]
    public void Compare_AcceptsConstructorParameterWidenedToFacadeBase()
    {
        var expectedConstructor = Constructor(
            "Fluent.Automation.Peers.RibbonButtonAutomationPeer",
            "Fluent.Button");
        var candidateConstructor = Constructor(
            "Fluent.Automation.Peers.RibbonButtonAutomationPeer",
            "Fluent.RibbonButton");
        var reference = Assembly(
            Type(
                "Fluent.Automation.Peers.RibbonButtonAutomationPeer",
                expectedConstructor));
        var candidate = Assembly(
            Type(
                "Fluent.Automation.Peers.RibbonButtonAutomationPeer",
                candidateConstructor),
            Type("Fluent.Button", baseType: "Fluent.RibbonButton"),
            Type(
                "Fluent.RibbonButton",
                baseType: "Microsoft.UI.Xaml.Controls.Button"));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Compare_AcceptsExpectedBaseThroughIntermediateCandidateBases()
    {
        var reference = Assembly(
            Type("Fluent.Button", baseType: "Microsoft.UI.Xaml.Controls.Button"));
        var candidate = Assembly(
            Type("Fluent.Button", baseType: "Fluent.RibbonButton"),
            Type("Fluent.RibbonButton", baseType: "Fluent.IntermediateButton"),
            Type("Fluent.IntermediateButton", baseType: "Microsoft.UI.Xaml.Controls.Button"));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Compare_IncludesInterfacesInheritedFromCandidateBaseTypes()
    {
        var reference = Assembly(
            Type(
                "Fluent.Button",
                interfaces: new HashSet<string>(["Fluent.IRibbonControl"], StringComparer.Ordinal),
                baseType: "Fluent.RibbonButton"));
        var candidate = Assembly(
            Type("Fluent.Button", baseType: "Fluent.RibbonButton"),
            Type(
                "Fluent.RibbonButton",
                interfaces: new HashSet<string>(["Fluent.IRibbonControl"], StringComparer.Ordinal)));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Compare_AllowsUnoDependencyObjectInterfaceProjection()
    {
        var reference = Assembly(
            Type(
                "Fluent.RibbonProperties",
                baseType: "Microsoft.UI.Xaml.DependencyObject"));
        var candidate = Assembly(
            Type(
                "Fluent.RibbonProperties",
                interfaces: new HashSet<string>(
                    ["Microsoft.UI.Xaml.DependencyObject"],
                    StringComparer.Ordinal),
                baseType: "System.Object"));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Compare_AllowsReviewedTabViewItemControlAncestry()
    {
        var reference = Assembly(
            Type(
                "Fluent.RibbonTabItem",
                baseType: "Microsoft.UI.Xaml.Controls.Control"));
        var candidate = Assembly(
            Type("Fluent.RibbonTabItem", baseType: "Fluent.RibbonTab"),
            Type(
                "Fluent.RibbonTab",
                baseType: "Microsoft.UI.Xaml.Controls.TabViewItem"));

        var issues = new ApiComparer().Compare(reference, candidate).Issues;

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Compare_ReportsMutatedConstAndEnumLiteralValues()
    {
        var expectedMember = Field(
            "Fluent.Mode",
            "Primary",
            "Fluent.Mode",
            new ApiConstant("Int32", "1"));
        var actualMember = expectedMember with
        {
            LiteralValue = new ApiConstant("Int32", "2")
        };

        var issues = new ApiComparer().Compare(
            Assembly(Type("Fluent.Mode", expectedMember)),
            Assembly(Type("Fluent.Mode", actualMember))).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Details, Does.Contain("literal value expected Int32:1, actual Int32:2"));
    }

    [Test]
    public void Compare_ReportsMutatedOptionalParameterDefault()
    {
        var expectedMember = Method(
            "Fluent.Ribbon",
            "Resize",
            "System.Void",
            parameters:
            [
                new ApiParameter(
                    "System.Int32",
                    IsOptional: true,
                    DefaultValue: new ApiConstant("Int32", "3"))
            ]);
        var actualMember = expectedMember with
        {
            Parameters =
            [
                new ApiParameter(
                    "System.Int32",
                    IsOptional: true,
                    DefaultValue: new ApiConstant("Int32", "4"))
            ]
        };

        var issues = new ApiComparer().Compare(
            Assembly(Type("Fluent.Ribbon", expectedMember)),
            Assembly(Type("Fluent.Ribbon", actualMember))).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Details, Does.Contain("parameter 1 expected System.Int32 = Int32:3"));
        Assert.That(issues[0].Details, Does.Contain("actual System.Int32 = Int32:4"));
    }

    [Test]
    public void Compare_ReportsRemovedOptionalParameterDefault()
    {
        var expectedMember = Method(
            "Fluent.Ribbon",
            "Resize",
            "System.Void",
            parameters:
            [
                new ApiParameter(
                    "System.Int32",
                    IsOptional: true,
                    DefaultValue: new ApiConstant("Int32", "3"))
            ]);
        var actualMember = expectedMember with
        {
            Parameters = [new ApiParameter("System.Int32", IsOptional: true)]
        };

        var issues = new ApiComparer().Compare(
            Assembly(Type("Fluent.Ribbon", expectedMember)),
            Assembly(Type("Fluent.Ribbon", actualMember))).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Details, Does.Contain("actual System.Int32 optional"));
    }

    [Test]
    public void Compare_AllowsReviewedOnApplyTemplateAccessibilitySubstitution()
    {
        var expectedMember = Method(
            "Fluent.Ribbon",
            "OnApplyTemplate",
            "System.Void",
            ApiAccessibility.Public);
        var actualMember = expectedMember with { Accessibility = ApiAccessibility.Protected };

        var issues = new ApiComparer().Compare(
            Assembly(Type("Fluent.Ribbon", expectedMember)),
            Assembly(Type("Fluent.Ribbon", actualMember))).Issues;

        Assert.That(issues, Is.Empty);
    }

    [Test]
    public void Compare_DoesNotGloballyRelaxPublicToProtectedAccessibility()
    {
        var expectedMember = Method(
            "Fluent.Ribbon",
            "Execute",
            "System.Void",
            ApiAccessibility.Public);
        var actualMember = expectedMember with { Accessibility = ApiAccessibility.Protected };

        var issues = new ApiComparer().Compare(
            Assembly(Type("Fluent.Ribbon", expectedMember)),
            Assembly(Type("Fluent.Ribbon", actualMember))).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Details, Does.Contain("accessibility expected Public, actual Protected"));
    }

    [Test]
    public void Compare_ReportsConcreteVirtualMemberBecomingAbstract()
    {
        var expectedMember = Method(
            "Fluent.Ribbon",
            "Execute",
            "System.Void") with
        {
            IsVirtual = true
        };
        var actualMember = expectedMember with
        {
            IsAbstract = true
        };

        var issues = new ApiComparer().Compare(
            Assembly(Type("Fluent.Ribbon", expectedMember)),
            Assembly(Type("Fluent.Ribbon", actualMember))).Issues;

        Assert.That(issues, Has.Count.EqualTo(1));
        Assert.That(issues[0].Details, Does.Contain("candidate member became abstract"));
    }

    private static ApiAssembly Assembly(params ApiType[] types)
        => new(types.ToDictionary(static type => type.Name, StringComparer.Ordinal));

    private static ApiType Type(
        string name,
        ApiMember? member = null,
        ApiMember? secondMember = null,
        IReadOnlySet<string>? interfaces = null,
        string? baseType = "System.Object")
    {
        var members = new[] { member, secondMember }
            .Where(static item => item is not null)
            .Cast<ApiMember>()
            .ToDictionary(static item => item.Identity, StringComparer.Ordinal);
        return new ApiType(
            name,
            ApiTypeKind.Class,
            ApiAccessibility.Public,
            0,
            baseType,
            interfaces ?? new HashSet<string>(StringComparer.Ordinal),
            false,
            false,
            members);
    }

    private static ApiMember Method(
        string typeName,
        string name,
        string returnType,
        ApiAccessibility accessibility = ApiAccessibility.Public,
        IReadOnlyList<ApiParameter>? parameters = null)
    {
        parameters ??= [];
        var identity = $"M:{typeName}::{name}({string.Join(",", parameters.Select(static parameter => parameter.Identity))})";
        return new ApiMember(
            identity,
            identity,
            ApiMemberKind.Method,
            accessibility,
            false,
            returnType,
            parameters);
    }

    private static ApiMember Field(
        string typeName,
        string name,
        string fieldType,
        ApiConstant literalValue)
    {
        var identity = $"F:{typeName}::{name}";
        return new ApiMember(
            identity,
            identity,
            ApiMemberKind.Field,
            ApiAccessibility.Public,
            true,
            fieldType,
            [],
            IsLiteral: true,
            LiteralValue: literalValue);
    }

    private static ApiMember Property(
        string typeName,
        string name,
        IReadOnlyDictionary<string, ApiAccessor> accessors)
    {
        var identity = $"P:{typeName}::{name}()";
        return new ApiMember(
            identity,
            identity,
            ApiMemberKind.Property,
            ApiAccessibility.Public,
            false,
            "System.Object",
            [],
            Accessors: accessors);
    }

    private static ApiMember Constructor(string typeName, params string[] parameterTypes)
    {
        var parameters = parameterTypes.Select(static type => new ApiParameter(type)).ToArray();
        var identity = $"M:{typeName}::.ctor({string.Join(",", parameterTypes)})";
        return new ApiMember(
            identity,
            identity,
            ApiMemberKind.Constructor,
            ApiAccessibility.Public,
            false,
            "System.Void",
            parameters);
    }
}
