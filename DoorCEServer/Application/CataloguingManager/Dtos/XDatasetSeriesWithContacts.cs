namespace DoorCEServer.Application.CataloguingManager.Dtos;

public class XDatasetSeriesWithContacts
{
    public required XDatasetSeries DatasetSeries { get; set; }
    public required IEnumerable<XContactData> NewContacts { get; set; }
}