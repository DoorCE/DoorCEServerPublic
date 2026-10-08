using DoorCEModel.Infrastructure.DataModel.DataSchemas;

namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public class Distribution : IdentifiableElement
	{
		// ***** From IdentifiabelElement, MultiDescriptionElement ************
		public int Id { get; set; }
		public required string Uri { get; set; } // TODO - check uniqueness
		// Mapping of titles for different languages
		public required Dictionary<string, string> Title { get; set; }
		// Mapping of descriptions for different languages
		public required Dictionary<string, string> Description { get; set; }
		// ***** End from MultiDescriptionElement *********

		// ATTRIBUTES
		public MaturityStatus Status { get; set; } = MaturityStatus.Completed;
		public required ICollection<string> AccessUrl { get; set; }
		public string? Format { get; set; }
		public AccessStatus AccessStatus { get; set; } = AccessStatus.Available;
		public uint? ByteSize { get; set; }
		public ICollection<string> Languages { get; set; } = new List<string>();
		public string? MediaType { get; set; }
		public DateTime? ReleaseDate { get; set; }
		
		// RELATIONSHIPS
		public required Dataset Dataset { get; set; }
		public DataService? DataService { get; set; }
		public DataSchema? Schema { get; set; }
		
		// ***** From FileDistribution ************
		// Mapping between checksum algorithms and resulting values
		public required Dictionary<string, string> Checksum { get; set; } = new();
		public string? CompressionFormat { get; set; }
		public string? DownloadUrl { get; set; }
		public int? FileId { get; set; }
		// ***** End from FileDistribution ************

		public bool Validate()
		{
			if (0 == Title.Count) return false;
			return true;
		}
	}
}