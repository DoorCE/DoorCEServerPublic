using DoorCEModel.Infrastructure.DataModel.Agents;

namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public enum XCatalogueElementType : short {
		Catalogue, Dataset, DatasetSeries, DataService
	}
	public class XCatalogueElement
	{
		// For XIdentifiableElement
		public required string Uri { get; set; }
		// For XMultiDescriptionElement
		public required Dictionary<string, string> Title { get; set; }
		public required Dictionary<string, string> Description { get; set; }
		// For XOwnableResource
		public Dictionary<string, string> Path { get; set; } = new();
		public string? IconUri { get; set; }
		public string? ResponsiblePersonUri { get; set; }
		public string? ResponsibleOrganisationUri { get; set; }
		// For XDataset
		public string? SchemaUri { get; set; }
		public string? SchemaTitle { get; set; }
		public short? Status { get; set; }
		public ICollection<string> SeriesUris { get; set; } = new List<string>();
		public Dictionary<string,Dictionary<string,string>> SeriesTitles { get; set; }
			= new(); // key: series uri, value: series titles
		public ICollection<short> Type { get; set; } =  new List<short>();
		public ICollection<string> Provenance { get; set; } =  new List<string>();
		// For XCataloguedResource
		public ICollection<string> Languages { get; set; } =  new List<string>();
		public ICollection<string> Keywords { get; set; } =  new List<string>();
		public ICollection<string> Themes { get; set; } =  new List<string>();
		public ICollection<string> Licenses { get; set; } =  new List<string>();
		public string? CatalogueUri { get; set; }
		// For XDataService
		public ICollection<string> Format { get; set; } = new List<string>();
		// Other
		public required XCatalogueElementType ElementType { get; set; }
		public ICollection<EditorRole> UserRoles { get; set; } = [];
	}
}