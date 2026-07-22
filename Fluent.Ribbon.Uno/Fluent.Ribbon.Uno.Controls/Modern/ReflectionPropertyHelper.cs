namespace Fluent.Modern;

using System.Reflection;

internal static class ReflectionPropertyHelper
{
    internal static PropertyInfo? GetReadableProperty(Type type, string propertyName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var property = current.GetProperty(
                propertyName,
                BindingFlags.Instance
                | BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly);
            if (property?.GetMethod is not null)
            {
                return property;
            }
        }

        return null;
    }
}
