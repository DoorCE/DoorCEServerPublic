namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XDistribution : XIdentifiableElement
	{
		// ***** From XMultiDescriptionElement ************
		public required Dictionary<string, string> Title { get; set; }
		public required Dictionary<string, string> Description { get; set; }
		// ***** End from XMultiDescriptionElement ************
		public string? Status { get; set; }
		public required string DatasetUri { get; set; }
		public Dictionary<string, string> DatasetTitle { get; set; } = new Dictionary<string, string>();
		public string? DataServiceUri { get; set; }
		public Dictionary<string, string>? DataServiceTitle { get; set; } = new Dictionary<string, string>();
		public string? SchemaUri { get; set; }
		public string? SchemaTitle { get; set; }
		public ICollection<string> AccessUrl { get; set; } = new List<string>();
		public string? Format { get; set; }
		public short? AccessStatus { get; set; }
		public uint? ByteSize { get; set; }
		public ICollection<string> Languages { get; set; } = new List<string>();
		public string? MediaType { get; set; }
		public DateTime? ReleaseDate { get; set; }
		public string? DatasetIconUri { get; set; }
		public Dictionary<string, string> Checksum { get; set; } = new();
		public string? CompressionFormat { get; set; }
		public string? DownloadUrl { get; set; }
		public int? FileId { get; set; }
		public string? FileName { get; set; }
		public bool IsUserEditable { get; set; } = true;
	}
}