namespace DoorCEServer.Application.CataloguingManager.Dtos
{
    public class XDataServiceWithContacts
    {
        public required XDataService DataService { get; set; }
        public required IEnumerable<XContactData> NewContacts { get; set; }
    }
}