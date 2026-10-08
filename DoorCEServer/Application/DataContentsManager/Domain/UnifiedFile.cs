using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEServer.Application.DataContentsManager.Domain;

public class UnifiedFile
{
    public List<UnifiedSheet> Sheets { get; init; } = new();
    public Dataset? Dataset { get; set; }
}