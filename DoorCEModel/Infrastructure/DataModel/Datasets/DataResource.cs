namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public abstract class DataResource : CataloguedResource
	{
		// ATTRIBUTES
		public ICollection<string> GeographicalCoverage { get; set; } = new List<string>();
		public string? Frequency { get; set; }
		public ICollection<(DateTime start, DateTime end)> TemporalCoverage { get; set; } =
			new List<(DateTime start, DateTime end)>();
		public DateTime? ReleaseDate { get; set; }
		public DateTime? ModificationDate { get; set; }
	}
}