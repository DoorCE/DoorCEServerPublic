namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XPropertyValue : XNamespaceElementValue
// Represents the "Property" class without its "Name" attribute
// Contains two potentially redundant attributes due to typeless notation of JSON
{
    // If the property type is "array", this is set to represent the "items" key, e.g.: "items": { "type": "string" }
    // Otherwise it has to be null
    public XPropertyValue? Items { get; set; }

    // If the property type is "reference", this is set to represent the "target" key, e.g.: "target": "species"
    // Otherwise it has to be null 
    public string? Target { get; set; }
}