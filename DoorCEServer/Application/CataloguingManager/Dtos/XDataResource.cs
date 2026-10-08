namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public abstract class XDataResource : XCataloguedResource {
		public ICollection<string> GeographicalCoverage { get; set; } = new List<string>();
		public string? Frequency { get; set; }
		public ICollection<(DateTime start, DateTime end)> TemporalCoverage { get; set; } = new List<(DateTime, DateTime)>();
		public DateTime? ReleaseDate { get; set; }

	}
}