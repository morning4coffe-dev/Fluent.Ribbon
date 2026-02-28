namespace Fluent.Internal;

/// <summary>
/// Provides reflection utility methods for type checking.
/// </summary>
/// <remarks>
/// Ported from WPF Fluent.Ribbon. Framework-agnostic.
/// </remarks>
public static class TypeHelper
{
    /// <summary>
    /// Checks whether <paramref name="type"/> inherits from a type with <paramref name="typeName"/>
    /// by walking the base type chain.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="typeName">The name of the base type to find.</param>
    /// <returns><c>true</c> if <paramref name="type"/> has <paramref name="typeName"/> in its hierarchy.</returns>
    public static bool InheritsFrom(Type type, string typeName)
    {
        var currentType = type;

        while (currentType is not null)
        {
            if (currentType.Name == typeName)
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }
}
