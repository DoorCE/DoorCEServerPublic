using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoorCEServer.Application.CkanProxy.Common.Serialization;
using DoorCEServer.Application.CkanProxy.Dtos;
using Serilog;

namespace DoorCEServer.Application.CkanProxy.Domain.Services;

public class CkanClient(IHttpClientFactory httpClientFactory) {
    private HttpClient Client => httpClientFactory.CreateClient("Ckan");
    
    // Post an HTTP request to CKAN: perform the 'action' with 'dto' as its body and return a response dto
    public async Task<XCkanResponse> SendAsync(string action, object? request = null) {
        try {
            // Prepare the request query
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                TypeInfoResolver = new TypeResolver(),
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(request, options);

            Log.Debug("CKAN request {Action}: {Request}", action, json);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, action);
            httpRequest.Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            // Post the request and get the response
            using HttpResponseMessage response = await Client.SendAsync(httpRequest);
            string content = await response.Content.ReadAsStringAsync();
            Log.Debug("CKAN response {Action} ({StatusCode}): {Body}",
                action, (int)response.StatusCode, content);

            // Prepare the response dto: extract the result dictionary from the response and add the status code
            return await _parseResponse(action, response);
        } catch (Exception ex) {
            // TODO - do not throw and return 'failure' response?
            Log.Error(ex, "CKAN request failed: {Action}", action);
            throw;
        }
    }

    private static async Task<XCkanResponse> _parseResponse(string action, HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        HttpStatusCode statusCode = response.StatusCode;
        
        if (string.IsNullOrWhiteSpace(body))
        {
            return XCkanResponse.Failure(
                statusCode,
                null,
                $"{action}: CKAN returned HTTP {(int)statusCode} with an empty body.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement.Clone();

            if (!response.IsSuccessStatusCode) {
                return XCkanResponse.Failure(statusCode, root, response.ReasonPhrase);
            }
            
            if (root.TryGetProperty("error", out var error)) {
                return XCkanResponse.Failure(statusCode, error, 
                    GetString(error, "message") ?? "CKAN request failed."); // TODO - check if this works
            }

            if (root.TryGetProperty("result", out var result)) {
                return XCkanResponse.Success(result);
            }

            if (root.TryGetProperty("success", out var success) &&
                success.ValueKind == JsonValueKind.True) {
                return XCkanResponse.Success(root);
            }

            return XCkanResponse.Failure(statusCode, root, $"{action}: CKAN request failed.");
        }
        catch (JsonException exception)
        {
            return XCkanResponse.Failure(statusCode, null, exception.Message);
        }
    }

    private static string? GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value)
            ? value.ToString()
            : null;
    }
}  