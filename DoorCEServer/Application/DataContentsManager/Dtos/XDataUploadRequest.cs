namespace DoorCEServer.Application.DataContentsManager.Dtos;

public class XDataUploadRequest
{
    public required string DatasetUri {get; set;}
    public required Dictionary<string, string> TableNameToConceptUriMap { get; set; } = new();
    public Dictionary<string, string> TableNameToIdFieldNameMap { get; set; } = new();
    public string? FileUrl { get; set; }
    public string? Extension { get; set; }
}