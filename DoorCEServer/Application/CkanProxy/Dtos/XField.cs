using System.Text.Json.Serialization;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XField {
    [JsonPropertyName("id")]
    public string? Name { get; init; }
    public string? Type { get; init; }
}