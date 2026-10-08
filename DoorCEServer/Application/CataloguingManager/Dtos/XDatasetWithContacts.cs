namespace DoorCEServer.Application.CataloguingManager.Dtos;

public class XDatasetWithContacts
{
    public required XDataset Dataset { get; set; }
    public required IEnumerable<XContactData> NewContacts { get; set; }
}