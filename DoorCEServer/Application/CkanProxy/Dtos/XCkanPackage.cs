using System.Text.Json.Serialization;
using DoorCEServer.Application.CkanProxy.Common.Serialization;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XCkanPackage : XCkanObject {
    [JsonPropertyName("name")]
    public string? CkanName { get; set; } // set only for package insert

    [JsonPropertyName("title")] 
    public string? MainTitle { get; init; } // different attribute name for mapping

    public string? Notes { get; init; }

    public List<Dictionary<string, string>>? Tags { get; init; }

    public string? Version { get; init; }

    public string? State { get; init; } = "active";

    [JsonConverter(typeof(XExtrasConverter))]
    public XExtras? Extras { get; init; }

    public List<XCkanResource> Resources { get; set; } = [];

    [JsonPropertyName("owner_org")] 
    public string? OrganisationId { get; set; }
}