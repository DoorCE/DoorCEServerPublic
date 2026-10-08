namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public class FileDistribution // TODO - consider refactoring (cf. Distribution)
	{
		/// <summary>
		/// Mapping between checksum algorithms and resulting values
		/// </summary>
		public required Dictionary<string, string> Checksum { get; set; }
		public string? CompressionFormat { get; set; }
		public string? DownloadUrl { get; set; }
		public int? FileId { get; set; }
	}
}