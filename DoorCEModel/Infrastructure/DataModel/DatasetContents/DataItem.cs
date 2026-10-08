using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEModel.Infrastructure.DataModel.DatasetContents
{
	public class DataItem
	{
		public long Id { get; set; }
		public required string Identifier { get; set; }
		public required Concept Concept { get; set; }
		public string? ConceptUri => Concept.Uri;
		public Dictionary<string, object> Values { get; set; } = new();
		public required Dataset Dataset { get; set; }
		public string DatasetUri => Dataset.Uri;
		public Dataset? Source { get; set; }
	}
}