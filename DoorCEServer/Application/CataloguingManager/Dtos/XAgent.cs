using DoorCEModel.Infrastructure.DataModel.Agents;

namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public abstract class XAgent : XIdentifiableElement
	{
		public ICollection<string> ContactsUris { get; set; } = new List<string>();
		public string? Description { get; set; }
		public abstract bool IsOrganisation { get; }
		
		// ==== Determines access to metadata and ownership ====
		public ICollection<ManagerRole>? UserRoles { get; set; }
			= [ManagerRole.MetadataManager, ManagerRole.OwnershipManager, ManagerRole.DistributionManager,
			ManagerRole.AgentManager];
	}
}