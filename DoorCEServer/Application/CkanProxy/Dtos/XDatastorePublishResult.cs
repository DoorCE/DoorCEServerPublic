namespace DoorCEServer.Application.CkanProxy.Dtos;

public class XDatastorePublishResult : XCkanResponse {
    public string? ConceptUri { get; init; }
    public int RowsUpserted { get; init; }

    public static XDatastorePublishResult FromXCkanResponse(string conceptUri, int rowsUpserted, XCkanResponse response) {
        return new XDatastorePublishResult {
            IsSuccess = response.IsSuccess,
            StatusCode = response.StatusCode,
            Details = response.Details,
            ErrorMessage = response.ErrorMessage,

            ConceptUri = conceptUri,
            RowsUpserted = rowsUpserted
        };
    }
}