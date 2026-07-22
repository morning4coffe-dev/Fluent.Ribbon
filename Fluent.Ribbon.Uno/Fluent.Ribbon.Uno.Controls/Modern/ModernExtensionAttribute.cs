namespace Fluent.Modern;

/// <summary>
/// <para><b>Modern extension</b> — marks a type that is part of the Fluent.Ribbon.Uno
/// "modern" surface, i.e. functionality that goes BEYOND the original WPF Fluent.Ribbon.</para>
/// <para>Every public type in the <c>Fluent.Modern</c> namespace must carry this attribute.
/// It is verified at runtime by the Showcase modern auto-test guard.</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
[ModernExtension]
public sealed class ModernExtensionAttribute : Attribute
{
}
