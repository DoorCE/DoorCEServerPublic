namespace DoorCEServer.Application.DataContentsManager.Dtos;

public class XDataItem
{
    public string? Identifier { get; set; }
    public required string DataSetUri { get; set; }
    public string? ConceptUri { get; set; }
    public required Dictionary<string,object> Values { get; set; }
}