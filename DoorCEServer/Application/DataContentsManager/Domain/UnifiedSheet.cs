using DoorCEModel.Infrastructure.DataModel.DataSchemas;

namespace DoorCEServer.Application.DataContentsManager.Domain;

public class UnifiedSheet
{
    public required string Name { get; set; }
    public Concept? Concept { get; set; }
    public List<Dictionary<string, object>> Rows { get; init; } = new();
    public string? IdFieldName { get; set; }
    public List<Property> ReferenceProperties { get; set; } = new();
    public List<Property> UniqueProperties { get; set; } = new();
}