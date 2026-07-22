using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace Fluent.Ribbon.ApiCompatibility;

public enum ApiMetadataRole
{
    Candidate,
    Reference
}

public sealed class MetadataApiReader
{
    private readonly FrameworkTypeNormalizer normalizer;

    public MetadataApiReader(FrameworkTypeNormalizer normalizer)
    {
        this.normalizer = normalizer;
    }

    public ApiAssembly Read(string assemblyPath, ApiMetadataRole role = ApiMetadataRole.Candidate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);

        using var stream = File.OpenRead(assemblyPath);
        using var peReader = new PEReader(stream);
        if (peReader.HasMetadata is false)
        {
            throw new BadImageFormatException($"'{assemblyPath}' does not contain managed metadata.");
        }

        var reader = peReader.GetMetadataReader();
        var provider = new ApiSignatureTypeProvider(
            reader,
            this.normalizer,
            normalizeFrameworkTypes: role is ApiMetadataRole.Reference);
        var types = new Dictionary<string, ApiType>(StringComparer.Ordinal);

        foreach (var handle in reader.TypeDefinitions)
        {
            var definition = reader.GetTypeDefinition(handle);
            if (IsVisibleType(reader, handle, definition) is false)
            {
                continue;
            }

            var name = provider.GetTypeFromDefinition(reader, handle, 0);
            if (name.Equals("Fluent", StringComparison.Ordinal) is false
                && name.StartsWith("Fluent.", StringComparison.Ordinal) is false
                && name.StartsWith("Fluent+", StringComparison.Ordinal) is false)
            {
                continue;
            }

            types.Add(name, ReadType(reader, provider, handle, definition, name));
        }

        return new ApiAssembly(types);
    }

    public ApiAssembly ReadReference(string assemblyPath) =>
        this.Read(assemblyPath, ApiMetadataRole.Reference);

    public ApiAssembly ReadCandidate(string assemblyPath) =>
        this.Read(assemblyPath, ApiMetadataRole.Candidate);

    private static ApiType ReadType(
        MetadataReader reader,
        ApiSignatureTypeProvider provider,
        TypeDefinitionHandle handle,
        TypeDefinition definition,
        string name)
    {
        var genericContext = new GenericContext(
            definition.GetGenericParameters().Count,
            0);
        var rawBaseType = ResolveType(reader, provider, definition.BaseType, genericContext);
        var kind = GetTypeKind(definition, rawBaseType);
        var interfaces = definition.GetInterfaceImplementations()
            .Select(interfaceHandle => reader.GetInterfaceImplementation(interfaceHandle))
            .Select(implementation => ResolveType(reader, provider, implementation.Interface, genericContext))
            .Where(static interfaceName => interfaceName is not null)
            .Select(static interfaceName => interfaceName!)
            .ToHashSet(StringComparer.Ordinal);

        return new ApiType(
            name,
            kind,
            GetTypeAccessibility(definition.Attributes),
            definition.GetGenericParameters().Count,
            kind is ApiTypeKind.Interface or ApiTypeKind.Enum or ApiTypeKind.Struct
                ? null
                : rawBaseType,
            interfaces,
            definition.Attributes.HasFlag(TypeAttributes.Abstract),
            definition.Attributes.HasFlag(TypeAttributes.Sealed),
            ReadMembers(reader, provider, handle, definition, name));
    }

    private static IReadOnlyDictionary<string, ApiMember> ReadMembers(
        MetadataReader reader,
        ApiSignatureTypeProvider provider,
        TypeDefinitionHandle typeHandle,
        TypeDefinition type,
        string typeName)
    {
        var members = new Dictionary<string, ApiMember>(StringComparer.Ordinal);
        var accessorMethods = new HashSet<MethodDefinitionHandle>();

        foreach (var propertyHandle in type.GetProperties())
        {
            var accessors = reader.GetPropertyDefinition(propertyHandle).GetAccessors();
            AddAccessor(accessorMethods, accessors.Getter);
            AddAccessor(accessorMethods, accessors.Setter);
            foreach (var other in accessors.Others)
            {
                AddAccessor(accessorMethods, other);
            }
        }

        foreach (var eventHandle in type.GetEvents())
        {
            var accessors = reader.GetEventDefinition(eventHandle).GetAccessors();
            AddAccessor(accessorMethods, accessors.Adder);
            AddAccessor(accessorMethods, accessors.Remover);
            AddAccessor(accessorMethods, accessors.Raiser);
            foreach (var other in accessors.Others)
            {
                AddAccessor(accessorMethods, other);
            }
        }

        foreach (var methodHandle in type.GetMethods())
        {
            if (accessorMethods.Contains(methodHandle))
            {
                continue;
            }

            var method = reader.GetMethodDefinition(methodHandle);
            var accessibility = GetMethodAccessibility(method.Attributes);
            if (accessibility is null)
            {
                continue;
            }

            var context = new GenericContext(type.GetGenericParameters().Count, method.GetGenericParameters().Count);
            var signature = method.DecodeSignature(provider, context);
            var parameters = ReadParameters(reader, method, signature.ParameterTypes);
            var methodName = reader.GetString(method.Name);
            var isConstructor = methodName is ".ctor" or ".cctor";
            var genericSuffix = signature.GenericParameterCount > 0 ? $"``{signature.GenericParameterCount}" : string.Empty;
            var identity = $"M:{typeName}::{methodName}{genericSuffix}({string.Join(",", parameters.Select(static parameter => parameter.Identity))})";

            members.Add(identity, new ApiMember(
                identity,
                identity,
                isConstructor ? ApiMemberKind.Constructor : ApiMemberKind.Method,
                accessibility.Value,
                method.Attributes.HasFlag(MethodAttributes.Static),
                signature.ReturnType,
                parameters,
                signature.GenericParameterCount,
                method.Attributes.HasFlag(MethodAttributes.Abstract),
                method.Attributes.HasFlag(MethodAttributes.Virtual),
                method.Attributes.HasFlag(MethodAttributes.Final)));
        }

        foreach (var fieldHandle in type.GetFields())
        {
            var field = reader.GetFieldDefinition(fieldHandle);
            var accessibility = GetFieldAccessibility(field.Attributes);
            if (accessibility is null)
            {
                continue;
            }

            var fieldName = reader.GetString(field.Name);
            var identity = $"F:{typeName}::{fieldName}";
            var fieldType = field.DecodeSignature(provider, new GenericContext(type.GetGenericParameters().Count, 0));
            members.Add(identity, new ApiMember(
                identity,
                identity,
                ApiMemberKind.Field,
                accessibility.Value,
                field.Attributes.HasFlag(FieldAttributes.Static),
                fieldType,
                [],
                IsReadOnly: field.Attributes.HasFlag(FieldAttributes.InitOnly),
                IsLiteral: field.Attributes.HasFlag(FieldAttributes.Literal),
                LiteralValue: ReadConstant(reader, field.GetDefaultValue())));
        }

        foreach (var propertyHandle in type.GetProperties())
        {
            var property = reader.GetPropertyDefinition(propertyHandle);
            var accessors = ReadPropertyAccessors(reader, property.GetAccessors());
            if (accessors.Count == 0)
            {
                continue;
            }

            var signature = property.DecodeSignature(provider, new GenericContext(type.GetGenericParameters().Count, 0));
            var propertyName = reader.GetString(property.Name);
            var parameters = signature.ParameterTypes.Select(static typeName => new ApiParameter(typeName)).ToArray();
            var identity = $"P:{typeName}::{propertyName}({string.Join(",", parameters.Select(static parameter => parameter.Identity))})";
            members.Add(identity, new ApiMember(
                identity,
                identity,
                ApiMemberKind.Property,
                GetWidestAccessibility(accessors.Values),
                accessors.Values.First().IsStatic,
                signature.ReturnType,
                parameters,
                Accessors: accessors));
        }

        foreach (var eventHandle in type.GetEvents())
        {
            var eventDefinition = reader.GetEventDefinition(eventHandle);
            var accessors = ReadEventAccessors(reader, eventDefinition.GetAccessors());
            if (accessors.Count == 0)
            {
                continue;
            }

            var eventName = reader.GetString(eventDefinition.Name);
            var identity = $"E:{typeName}::{eventName}";
            members.Add(identity, new ApiMember(
                identity,
                identity,
                ApiMemberKind.Event,
                GetWidestAccessibility(accessors.Values),
                accessors.Values.First().IsStatic,
                ResolveType(reader, provider, eventDefinition.Type, new GenericContext(type.GetGenericParameters().Count, 0))
                    ?? "<unknown>",
                [],
                Accessors: accessors));
        }

        return members;
    }

    private static IReadOnlyList<ApiParameter> ReadParameters(
        MetadataReader reader,
        MethodDefinition method,
        ImmutableArray<string> parameterTypes)
    {
        var metadata = method.GetParameters()
            .Select(handle => reader.GetParameter(handle))
            .Where(static parameter => parameter.SequenceNumber > 0)
            .ToDictionary(static parameter => parameter.SequenceNumber);
        var parameters = new ApiParameter[parameterTypes.Length];

        for (var index = 0; index < parameterTypes.Length; index++)
        {
            metadata.TryGetValue(index + 1, out var parameter);
            var modifier = parameter.Attributes.HasFlag(ParameterAttributes.Out)
                ? "out "
                : parameterTypes[index].EndsWith("&", StringComparison.Ordinal)
                    ? parameter.Attributes.HasFlag(ParameterAttributes.In) ? "in " : "ref "
                    : string.Empty;
            var type = modifier.Length == 0 ? parameterTypes[index] : parameterTypes[index].TrimEnd('&');
            parameters[index] = new ApiParameter(
                type,
                modifier,
                parameter.Attributes.HasFlag(ParameterAttributes.Optional),
                ReadConstant(reader, parameter.GetDefaultValue()));
        }

        return parameters;
    }

    private static ApiConstant? ReadConstant(MetadataReader reader, ConstantHandle handle)
    {
        if (handle.IsNil)
        {
            return null;
        }

        var constant = reader.GetConstant(handle);
        var valueReader = reader.GetBlobReader(constant.Value);
        var value = constant.TypeCode switch
        {
            ConstantTypeCode.Boolean => valueReader.ReadBoolean() ? "true" : "false",
            ConstantTypeCode.Char => ((int)valueReader.ReadUInt16()).ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.SByte => valueReader.ReadSByte().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Byte => valueReader.ReadByte().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int16 => valueReader.ReadInt16().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt16 => valueReader.ReadUInt16().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int32 => valueReader.ReadInt32().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt32 => valueReader.ReadUInt32().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Int64 => valueReader.ReadInt64().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.UInt64 => valueReader.ReadUInt64().ToString(CultureInfo.InvariantCulture),
            ConstantTypeCode.Single => valueReader.ReadSingle().ToString("R", CultureInfo.InvariantCulture),
            ConstantTypeCode.Double => valueReader.ReadDouble().ToString("R", CultureInfo.InvariantCulture),
            ConstantTypeCode.String => JsonSerializer.Serialize(valueReader.ReadUTF16(valueReader.Length)),
            ConstantTypeCode.NullReference => "null",
            _ => Convert.ToHexString(valueReader.ReadBytes(valueReader.Length))
        };

        return new ApiConstant(constant.TypeCode.ToString(), value);
    }

    private static IReadOnlyDictionary<string, ApiAccessor> ReadPropertyAccessors(
        MetadataReader reader,
        PropertyAccessors accessors)
    {
        var result = new Dictionary<string, ApiAccessor>(StringComparer.Ordinal);
        AddAccessor(reader, result, "get", accessors.Getter);
        AddAccessor(reader, result, "set", accessors.Setter);
        return result;
    }

    private static IReadOnlyDictionary<string, ApiAccessor> ReadEventAccessors(
        MetadataReader reader,
        EventAccessors accessors)
    {
        var result = new Dictionary<string, ApiAccessor>(StringComparer.Ordinal);
        AddAccessor(reader, result, "add", accessors.Adder);
        AddAccessor(reader, result, "remove", accessors.Remover);
        AddAccessor(reader, result, "raise", accessors.Raiser);
        return result;
    }

    private static void AddAccessor(
        MetadataReader reader,
        IDictionary<string, ApiAccessor> accessors,
        string name,
        MethodDefinitionHandle handle)
    {
        if (handle.IsNil)
        {
            return;
        }

        var method = reader.GetMethodDefinition(handle);
        var accessibility = GetMethodAccessibility(method.Attributes);
        if (accessibility is not null)
        {
            accessors.Add(name, new ApiAccessor(accessibility.Value, method.Attributes.HasFlag(MethodAttributes.Static)));
        }
    }

    private static void AddAccessor(ISet<MethodDefinitionHandle> accessors, MethodDefinitionHandle handle)
    {
        if (handle.IsNil is false)
        {
            accessors.Add(handle);
        }
    }

    private static ApiAccessibility GetWidestAccessibility(IEnumerable<ApiAccessor> accessors)
    {
        return accessors.Max(static accessor => accessor.Accessibility);
    }

    private static ApiTypeKind GetTypeKind(TypeDefinition definition, string? baseType)
    {
        if (definition.Attributes.HasFlag(TypeAttributes.Interface))
        {
            return ApiTypeKind.Interface;
        }

        return baseType switch
        {
            "System.Enum" => ApiTypeKind.Enum,
            "System.ValueType" => ApiTypeKind.Struct,
            "System.MulticastDelegate" or "System.Delegate" => ApiTypeKind.Delegate,
            _ => ApiTypeKind.Class
        };
    }

    private static bool IsVisibleType(
        MetadataReader reader,
        TypeDefinitionHandle handle,
        TypeDefinition definition)
    {
        var visibility = definition.Attributes & TypeAttributes.VisibilityMask;
        if (visibility == TypeAttributes.Public)
        {
            return true;
        }

        if (visibility is not (TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem))
        {
            return false;
        }

        var declaringType = definition.GetDeclaringType();
        return declaringType.IsNil is false
               && IsVisibleType(reader, declaringType, reader.GetTypeDefinition(declaringType));
    }

    private static ApiAccessibility GetTypeAccessibility(TypeAttributes attributes)
    {
        return (attributes & TypeAttributes.VisibilityMask) switch
        {
            TypeAttributes.NestedFamily => ApiAccessibility.Protected,
            TypeAttributes.NestedFamORAssem => ApiAccessibility.ProtectedInternal,
            _ => ApiAccessibility.Public
        };
    }

    private static ApiAccessibility? GetMethodAccessibility(MethodAttributes attributes)
    {
        return (attributes & MethodAttributes.MemberAccessMask) switch
        {
            MethodAttributes.Public => ApiAccessibility.Public,
            MethodAttributes.Family => ApiAccessibility.Protected,
            MethodAttributes.FamORAssem => ApiAccessibility.ProtectedInternal,
            _ => null
        };
    }

    private static ApiAccessibility? GetFieldAccessibility(FieldAttributes attributes)
    {
        return (attributes & FieldAttributes.FieldAccessMask) switch
        {
            FieldAttributes.Public => ApiAccessibility.Public,
            FieldAttributes.Family => ApiAccessibility.Protected,
            FieldAttributes.FamORAssem => ApiAccessibility.ProtectedInternal,
            _ => null
        };
    }

    private static string? ResolveType(
        MetadataReader reader,
        ApiSignatureTypeProvider provider,
        EntityHandle handle,
        GenericContext context)
    {
        if (handle.IsNil)
        {
            return null;
        }

        return handle.Kind switch
        {
            HandleKind.TypeDefinition => provider.GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0),
            HandleKind.TypeReference => provider.GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0),
            HandleKind.TypeSpecification => provider.GetTypeFromSpecification(reader, context, (TypeSpecificationHandle)handle, 0),
            _ => MetadataTokens.GetToken(handle).ToString("X8")
        };
    }

    private readonly record struct GenericContext(int TypeParameterCount, int MethodParameterCount);

    private sealed class ApiSignatureTypeProvider : ISignatureTypeProvider<string, GenericContext>
    {
        private readonly MetadataReader reader;
        private readonly FrameworkTypeNormalizer normalizer;
        private readonly bool normalizeFrameworkTypes;
        private readonly Dictionary<TypeDefinitionHandle, string> definitionNames = [];
        private readonly Dictionary<TypeReferenceHandle, string> referenceNames = [];

        public ApiSignatureTypeProvider(
            MetadataReader reader,
            FrameworkTypeNormalizer normalizer,
            bool normalizeFrameworkTypes)
        {
            this.reader = reader;
            this.normalizer = normalizer;
            this.normalizeFrameworkTypes = normalizeFrameworkTypes;
        }

        public string GetArrayType(string elementType, ArrayShape shape)
        {
            var commas = shape.Rank > 0 ? new string(',', shape.Rank - 1) : string.Empty;
            return $"{elementType}[{commas}]";
        }

        public string GetByReferenceType(string elementType) => $"{elementType}&";

        public string GetFunctionPointerType(MethodSignature<string> signature)
            => $"methodptr({string.Join(",", signature.ParameterTypes)})->{signature.ReturnType}";

        public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments)
            => $"{genericType}<{string.Join(",", typeArguments)}>";

        public string GetGenericMethodParameter(GenericContext genericContext, int index) => $"!!{index}";

        public string GetGenericTypeParameter(GenericContext genericContext, int index) => $"!{index}";

        public string GetModifiedType(string modifier, string unmodifiedType, bool isRequired)
            => isRequired ? $"modreq({modifier}) {unmodifiedType}" : unmodifiedType;

        public string GetPinnedType(string elementType) => elementType;

        public string GetPointerType(string elementType) => $"{elementType}*";

        public string GetPrimitiveType(PrimitiveTypeCode typeCode)
        {
            return typeCode switch
            {
                PrimitiveTypeCode.Boolean => "System.Boolean",
                PrimitiveTypeCode.Byte => "System.Byte",
                PrimitiveTypeCode.Char => "System.Char",
                PrimitiveTypeCode.Double => "System.Double",
                PrimitiveTypeCode.Int16 => "System.Int16",
                PrimitiveTypeCode.Int32 => "System.Int32",
                PrimitiveTypeCode.Int64 => "System.Int64",
                PrimitiveTypeCode.IntPtr => "System.IntPtr",
                PrimitiveTypeCode.Object => "System.Object",
                PrimitiveTypeCode.SByte => "System.SByte",
                PrimitiveTypeCode.Single => "System.Single",
                PrimitiveTypeCode.String => "System.String",
                PrimitiveTypeCode.TypedReference => "System.TypedReference",
                PrimitiveTypeCode.UInt16 => "System.UInt16",
                PrimitiveTypeCode.UInt32 => "System.UInt32",
                PrimitiveTypeCode.UInt64 => "System.UInt64",
                PrimitiveTypeCode.UIntPtr => "System.UIntPtr",
                PrimitiveTypeCode.Void => "System.Void",
                _ => typeCode.ToString()
            };
        }

        public string GetSZArrayType(string elementType) => $"{elementType}[]";

        public string GetTypeFromDefinition(
            MetadataReader metadataReader,
            TypeDefinitionHandle handle,
            byte rawTypeKind)
        {
            if (this.definitionNames.TryGetValue(handle, out var cached))
            {
                return cached;
            }

            var definition = metadataReader.GetTypeDefinition(handle);
            var name = metadataReader.GetString(definition.Name);
            var declaringType = definition.GetDeclaringType();
            var fullName = declaringType.IsNil
                ? JoinNamespace(metadataReader.GetString(definition.Namespace), name)
                : $"{this.GetTypeFromDefinition(metadataReader, declaringType, rawTypeKind)}+{name}";
            if (this.normalizeFrameworkTypes)
            {
                fullName = this.normalizer.Normalize(fullName);
            }
            this.definitionNames.Add(handle, fullName);
            return fullName;
        }

        public string GetTypeFromReference(
            MetadataReader metadataReader,
            TypeReferenceHandle handle,
            byte rawTypeKind)
        {
            if (this.referenceNames.TryGetValue(handle, out var cached))
            {
                return cached;
            }

            var reference = metadataReader.GetTypeReference(handle);
            var name = metadataReader.GetString(reference.Name);
            var fullName = reference.ResolutionScope.Kind == HandleKind.TypeReference
                ? $"{this.GetTypeFromReference(metadataReader, (TypeReferenceHandle)reference.ResolutionScope, rawTypeKind)}+{name}"
                : JoinNamespace(metadataReader.GetString(reference.Namespace), name);
            if (this.normalizeFrameworkTypes)
            {
                fullName = this.normalizer.Normalize(fullName);
            }
            this.referenceNames.Add(handle, fullName);
            return fullName;
        }

        public string GetTypeFromSpecification(
            MetadataReader metadataReader,
            GenericContext genericContext,
            TypeSpecificationHandle handle,
            byte rawTypeKind)
            => metadataReader.GetTypeSpecification(handle).DecodeSignature(this, genericContext);

        private static string JoinNamespace(string @namespace, string name)
            => string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
    }
}
