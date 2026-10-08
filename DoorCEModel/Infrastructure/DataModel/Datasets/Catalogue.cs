using DoorCEModel.Infrastructure.DataModel.Agents;

namespace DoorCEModel.Infrastructure.DataModel.Datasets
{
	public class Catalogue : OwnableResource
	{
		public override Catalogue? GetParent()
		{
			return PartOf;
		}
		
		// RELATIONSHIPS
		public ICollection<CataloguedResource> Resources { get; set; } = new List<CataloguedResource>();
		public ICollection<Catalogue> Parts { get; set; } = new List<Catalogue>();
		public Catalogue? PartOf { get; set; }
	}
}