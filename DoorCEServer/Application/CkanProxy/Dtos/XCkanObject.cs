using System.Text.Json.Serialization;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XCkanObject {
    [JsonPropertyName("id")]
    public string? CkanId { get; set; }
}