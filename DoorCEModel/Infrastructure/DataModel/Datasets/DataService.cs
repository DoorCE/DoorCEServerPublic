namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public class DataService : CataloguedResource
	{
		// ATTRIBUTES
		public required ICollection<string> EndpointUrl { get; set; }
		public ICollection<string> EndpointDescription { get; set; } = new List<string>();
		public ICollection<string> Documentation { get; set; } = new List<string>();
		public ICollection<string> Format { get; set; } = new List<string>();
		public required AccessStatus Status { get; set; }
		
		// RELATIONSHIPS
		public ICollection<Standard> ConformsTo { get; set; } = new List<Standard>();
		public ICollection<Distribution> Distributions { get; set; } = new List<Distribution>();
	}
}