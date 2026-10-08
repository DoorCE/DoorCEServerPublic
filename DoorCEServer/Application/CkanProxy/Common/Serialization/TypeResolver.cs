using System.Collections;
using System.Text.Json.Serialization.Metadata;

namespace DoorCEServer.Application.CkanProxy.Common.Serialization;

public class TypeResolver : DefaultJsonTypeInfoResolver
{
    public TypeResolver()
    {
        Modifiers.Add(RemoveEmptyProperties);
    }

    private static void RemoveEmptyProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var property in typeInfo.Properties) {
            property.Name = property.Name.ToLower();
            property.ShouldSerialize = (_, value) => !IsNullOrEmpty(value);
        }
    }

    private static bool IsNullOrEmpty(object? value)
    {
        return value switch
        {
            null => true,
            string text => string.IsNullOrEmpty(text),
            ICollection collection => collection.Count == 0,
            IEnumerable enumerable => !HasAnyElement(enumerable),
            _ => false
        };
    }

    private static bool HasAnyElement(IEnumerable enumerable)
    {
        var enumerator = enumerable.GetEnumerator();

        try
        {
            return enumerator.MoveNext();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }
}