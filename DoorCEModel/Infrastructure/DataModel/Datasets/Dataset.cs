using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;

namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public class Dataset : DataResource
	{
		// ATTRIBUTES
		public required ICollection<DatasetType> Type { get; set; }
		public DatasetStatus Status { get; set; }
		// Links to documents about the dataset
		public ICollection<string> Documentation { get; set; } = new List<string>();
		public ICollection<string> Provenance { get; set; } = new List<string>();
		public required string Version { get; set; }
		public string? VersionNotes { get; set; }
		
		// RELATIONSHIPS
		public ICollection<Distribution> Distributions { get; set; } = new List<Distribution>();
		public Dataset? VersionOf { get; set; }
		public ICollection<DataItem> Items { get; set; } = new List<DataItem>();
		public DataSchema? Schema { get; set; }
		public ICollection<Standard> ConformsTo { get; set; } = new List<Standard>();
		public ICollection<Dataset> Source { get; set; } = new List<Dataset>();
		public Dataset? Target { get; set; }
		public ICollection<DatasetSeries> Series { get; set; } = new List<DatasetSeries>();

		public new bool Validate(bool isAdmin)
		{
			if (DatasetStatus.Source != Status && 0 == Contacts.Count) return false;
			return base.Validate(isAdmin);
		}

		public bool HasDataItems()
		{
			return Items.Count != 0;
		}
	}
}