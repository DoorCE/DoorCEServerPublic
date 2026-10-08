namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XDataset : XDataResource {
		public required ICollection<short> Type { get; set; }
		public required short Status { get; set; }
		public ICollection<string> Documentation { get; set; } = new List<string>();
		public ICollection<string> Provenance { get; set; } = new List<string>();
		public string? Version { get; set; }
		public string? VersionNotes { get; set; }
		public ICollection<string> SeriesUris { get; set; } = new List<string>();
		public Dictionary<string,Dictionary<string,string>> SeriesTitles { get; set; } 
			= new(); // key: series uri, value: series titles
		public string? SchemaUri { get; set; }
		public string? SchemaTitle { get; set; }
		public string? TargetDatasetUri { get; set; }
	}
}