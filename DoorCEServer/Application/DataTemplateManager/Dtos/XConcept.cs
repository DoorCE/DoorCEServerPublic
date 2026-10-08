namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XConcept : XNamespaceElementValue
{
    // e.g.: "name": "tree"
    public required string Name { get; set; }
    
    // e.g.: "properties": { "width": { ... }, ... }
    public Dictionary<string, XPropertyValue> Properties { get; set; } = new(); // Dictionary of "Properties" (name + type)
    
    //e.g.: "required": [ "width", "length" ]
    public IEnumerable<string> Required { get; set; } = [];
    //e.g.: "unique": [ "id" ]
    public IEnumerable<string> Unique { get; set; } = [];
    //e.g.: "defaultIdentifier": "id"
    public string? DefaultIdentifier { get; set; }
}