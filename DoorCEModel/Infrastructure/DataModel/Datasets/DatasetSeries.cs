namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public class DatasetSeries : DataResource
	{
		// RELATIONSHIPS
		public required ICollection<Dataset> Datasets { get; set; }
	}
}