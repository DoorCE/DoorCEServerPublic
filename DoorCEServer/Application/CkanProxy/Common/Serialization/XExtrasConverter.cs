using System.Text.Json;
using System.Text.Json.Serialization;
using DoorCEServer.Application.CkanProxy.Dtos;
using System.Text.Encodings.Web;

namespace DoorCEServer.Application.CkanProxy.Common.Serialization;

public sealed class XExtrasConverter : JsonConverter<XExtras>
{
    public override XExtras Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(
        Utf8JsonWriter writer,
        XExtras value,
        JsonSerializerOptions options)
    {
        var element = JsonSerializer.SerializeToElement(
            value,
            CreateInnerOptions(options));

        writer.WriteStartArray();

        foreach (var property in element.EnumerateObject().Where(property => !IsEmpty(property.Value))) {
            writer.WriteStartObject();

            writer.WriteString("key", property.Name);

            writer.WritePropertyName("value");

            writer.WriteStringValue(property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.GetRawText());

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static JsonSerializerOptions CreateInnerOptions(
        JsonSerializerOptions options)
    {
        var innerOptions = new JsonSerializerOptions(options);

        for (var i = innerOptions.Converters.Count - 1; i >= 0; i--)
        {
            if (innerOptions.Converters[i] is XExtrasConverter)
                innerOptions.Converters.RemoveAt(i);
        }

        innerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;

        innerOptions.PropertyNamingPolicy =
            JsonNamingPolicy.SnakeCaseLower;
        
        innerOptions.Encoder =
            JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

        return innerOptions;
    }

    private static bool IsEmpty(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.Null or JsonValueKind.Undefined => true,

            JsonValueKind.String =>
                string.IsNullOrWhiteSpace(value.GetString()),

            JsonValueKind.Array =>
                value.GetArrayLength() == 0,

            JsonValueKind.Object =>
                !value.EnumerateObject().Any(),

            _ => false
        };
}