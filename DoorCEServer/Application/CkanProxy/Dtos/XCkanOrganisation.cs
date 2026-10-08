using System.Text.Json.Serialization;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XCkanOrganisation : XCkanObject {
    [JsonPropertyName("name")]
    public string? CkanName { get; set; } // set only for package insert
    
    public string? Title { get; init; }
    public string State => "active";
}