using System.Net;
using System.Text.Json;
using DoorCEServer.Application.CkanProxy.Common.Serialization;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XCkanResponse {
    public bool IsSuccess {get; init;}
    public HttpStatusCode StatusCode {get; init;} // TODO - remove?
    public JsonElement? Details { get; init;}
    public string? ErrorMessage {get; init;}
    
    public static XCkanResponse Success(JsonElement? data)
        => new() {
            IsSuccess = true, 
            StatusCode = HttpStatusCode.OK, 
            Details = data, 
            ErrorMessage = null
        };

    public static XCkanResponse Failure(HttpStatusCode statusCode, JsonElement? data, string? errorMessage)
        => new() {
            IsSuccess = false,
            StatusCode = statusCode,
            Details = data,
            ErrorMessage = errorMessage
        };
    
    public string? ExtractObjectId() {
        JsonElement result = Details ?? JsonSerializer.SerializeToElement(new { });

        if (result.TryGetProperty("resource_id", out var resourceId))
            return resourceId.GetString();

        if (result.TryGetProperty("id", out var id))
            return id.GetString();

        if (result.TryGetProperty("meta", out var meta)
            && meta.TryGetProperty("id", out var metaId))
            return metaId.GetString();
        
        throw new ArgumentException($"Cannot extract id from Ckan response: {result}");

    }

    public ICollection<XCkanResource>? ExtractResources(bool withDistributions = false) {
        JsonElement result = Details ?? JsonSerializer.SerializeToElement(new { });

        if (!result.TryGetProperty("resources", out var resources))
            return null;
        
        JsonSerializerOptions options = new JsonSerializerOptions() {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new TypeResolver(),
        };
        
        List<XCkanResource> dataResources;
        if (!withDistributions) {
            dataResources = resources.EnumerateArray()
                .Select(r => r.Deserialize<XCkanResource>(options) 
                             ?? throw new InvalidOperationException("Cannot deserialize XCkan resource response"))
                .Where(r => r.UdasType != "distribution").ToList();
        } else {
            dataResources = resources
                .EnumerateArray()
                .Select(r => r.Deserialize<XCkanResource>(options)
                             ?? throw new InvalidOperationException("Cannot deserialize XCkan resource response"))
                .ToList();
        }

        return dataResources;
    }
}