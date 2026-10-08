using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XCkanResource : XCkanObject {
    [JsonPropertyName("package_id")]
    public string? PackageId { get; set; } 
    
    [JsonPropertyName("name")]
    public string? CkanName { get; init; }
    
    [JsonPropertyName("title_translated")]
    public Dictionary<string, string>? Title { get; init; }
    
    [JsonPropertyName("notes_translated")]
    public Dictionary<string, string>? Description { get; init; }
    
    public string? Format {get; init;}
    
    [JsonPropertyName("size")]
    public uint? ByteSize { get; init; }
    
    [JsonPropertyName("language")]
    public ICollection<string>? Languages { get; init; }
    
    [JsonPropertyName("mimetype")]
    public string? MediaType { get; init; }

    public string? Created { get; init; }

    [JsonPropertyName("udas_type")] 
    public string? UdasType { get; init; }
    
    [JsonPropertyName("access_url")]
    public ICollection<string>? AccessUrl { get; init; }
    
    public Dictionary<string, string>? Checksum { get; init; }
    
    [JsonPropertyName("compress_format")]
    public string? CompressionFormat { get; init; }
    
    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }
    
    public string? Url { get; set; }
    
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalProperties { get; init; }
}