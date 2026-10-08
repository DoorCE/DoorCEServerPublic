namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public abstract class XCataloguedResource : XOwnableResource
	{
		public required short AccessRights { get; set; }
		public ICollection<string> Languages { get; set; } = new List<string>();
		public ICollection<string> Keywords { get; set; } = new List<string>();
		public ICollection<string> Themes { get; set; } = new List<string>();
		public ICollection<string> ApplicableLegislations { get; set; } = new List<string>();
		public ICollection<string> Licences { get; set; } = new List<string>();
		public required string CatalogueUri { get; set; }
		public Dictionary<string,string> CatalogueTitle { get; set; } = new();
	}
}