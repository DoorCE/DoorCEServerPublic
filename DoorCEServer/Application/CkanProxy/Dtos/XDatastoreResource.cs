using System.Text.Json.Serialization;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XDatastoreResource : XDatastoreRequest {

    public required ICollection<string> Aliases { get; set; }

    public required List<XField> Fields { get; set; }

    [JsonPropertyName("primary_key")]
    public required ICollection<string> PrimaryKey { get; set; }
}