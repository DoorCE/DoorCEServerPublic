namespace DoorCEServer.Common.Exceptions;

public class DatastorePublishException(string message, string? ckanResourceId = null, int? rowsUpserted = null) : Exception(message)
{
    public string? CkanResourceId { get; } = ckanResourceId;
    public int? RowsUpserted { get; } = rowsUpserted;
}