namespace Fluent.Helpers;

using System.Reflection;

internal static class PropertyValueHelper
{
    internal static object? GetPublicPropertyValue(
        object source,
        string propertyName)
    {
        for (var type = source.GetType(); type is not null; type = type.BaseType)
        {
            var property = type
                .GetProperties(
                    BindingFlags.Instance
                    | BindingFlags.Public
                    | BindingFlags.DeclaredOnly)
                .FirstOrDefault(
                    candidate => string.Equals(
                        candidate.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase));
            if (property is not null)
            {
                return property.GetValue(source);
            }
        }

        return null;
    }
}
