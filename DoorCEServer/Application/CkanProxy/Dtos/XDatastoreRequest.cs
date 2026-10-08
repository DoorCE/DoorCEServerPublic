using System.Text.Json.Serialization;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XDatastoreRequest {
    [JsonPropertyName("resource_id")] 
    public string? ResourceId { get; set; }
    public bool? Force { get; set; }
    public List<Dictionary<string, object?>>? Records { get; set; }
    
}