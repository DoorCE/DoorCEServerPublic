namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XCatalogueWithContacts
	{
		public required XCatalogue Catalogue { get; set; }
        public required IEnumerable<XContactData> NewContacts { get; set; }
	}
}