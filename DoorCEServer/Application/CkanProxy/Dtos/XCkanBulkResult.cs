using System.Net;
using System.Text.Json;

namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XCkanBulkResult : XCkanResponse {

    public ICollection<XCkanResponse> Results { get; init; }

    public Dictionary<string, int>? UpsertedRows { get; init; }

    public XCkanBulkResult(ICollection<XCkanResponse> results, string? parentObjectId = null) {
        Results = results;
        IsSuccess = results.Count == 0 || results.All(result => result.IsSuccess);
        StatusCode = !IsSuccess
            ? results.FirstOrDefault(result => !result.IsSuccess)!.StatusCode
            : HttpStatusCode.OK;
        ErrorMessage = results.FirstOrDefault(result => !result.IsSuccess)?.ErrorMessage;

        if (parentObjectId != null) {
            Dictionary<string, object?> idDict = new Dictionary<string, object?>() { { "id", parentObjectId } };
            Details = JsonSerializer.SerializeToElement(idDict);
        }

        var publishResults = results
            .OfType<XDatastorePublishResult>()
            .ToList();

        if (publishResults.Count == results.Count) {
            UpsertedRows = publishResults.ToDictionary(
                result => result.ConceptUri!,
                result => result.RowsUpserted);
        }
    }
}