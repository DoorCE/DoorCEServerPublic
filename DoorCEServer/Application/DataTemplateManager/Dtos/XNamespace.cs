namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XNamespace
{
    public string? Iri { get; set; }
    public required string Prefix { get; set; }

    public bool IsCustom { get; set; } = true;
}