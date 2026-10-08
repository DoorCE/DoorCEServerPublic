namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XCatalogue : XOwnableResource
	{
		public string? PartOfUri { get; set; }
		public Dictionary<string,string> PartOfTitle { get; set; } = new();
	}
}
