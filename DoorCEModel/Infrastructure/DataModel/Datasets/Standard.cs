namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public class Standard : IdentifiableElement
	{
		// ***** From DescribableElement **************
		public int Id { get; set; }
		public required string Uri { get; set; } // TODO -assure uniqueness
		public required string Title { get; set; }
		public required string Description { get; set; }
		// ***** End from DescribableElement ***********

		// ATTRIBUTES
		public DateTime? ReleaseDate;
		
		// RELATIONSHIPS
		public ICollection<Dataset> Datasets { get; set; } = new List<Dataset>();
		public ICollection<DataService> DataServices { get; set; } = new List<DataService>();
	}
}