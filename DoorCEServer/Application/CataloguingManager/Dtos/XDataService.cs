namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XDataService : XCataloguedResource
	{
		public ICollection<string> EndpointUrl { get; set; } = new List<string>();
		public ICollection<string> EndpointDescription { get; set; } = new List<string>();
		public ICollection<string> Documentation { get; set; } = new List<string>();
		public ICollection<string> Format { get; set; } = new List<string>();
		public required short Status { get; set; }
		public ICollection<string> StandardUris { get; set; } = new List<string>();
		public ICollection<string> StandardTitles { get; set; } = new List<string>();
		
	}
}