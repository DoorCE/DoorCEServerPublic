using DoorCEModel.Infrastructure.DataModel.Agents;

namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public abstract class CataloguedResource : OwnableResource
	{
		public override Catalogue GetParent()
		{
			return Catalogue;
		}
		
		// ATTRIBUTES
		public required AccessRightsType AccessRights { get; set; }
		// A list of two-letter codes based on ISO-639
		public ICollection<string> Languages { get; set; } = new List<string>();
		public ICollection<string> Keywords { get; set; } = new List<string>();
		public ICollection<string> Themes { get; set; } = new List<string>();
		public ICollection<string> ApplicableLegislations { get; set; } = new List<string>();
		public ICollection<string> Licences { get; set; } = new List<string>();
		
		// RELATIONSHIPS
		public required Catalogue Catalogue { get; set; }
	}
}