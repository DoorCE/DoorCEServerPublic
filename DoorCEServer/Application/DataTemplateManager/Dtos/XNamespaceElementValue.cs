namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XNamespaceElementValue
{
    public string Type { get; set; } = "object";
    public string? Description { get; set; }
    public string? NamespacePrefix { get; set; }
    public string? Uri { get; set; }
}