using DoorCEModel.Infrastructure.DataModel.Agents;

namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XOrganisation : XAgent
	{
		public required string Name { get; set; }
		public ICollection<string> MembersUris { get; set; } = new List<string>();
		public Dictionary<string, ICollection<ManagerRole>> MembersRoles { get; set; } = new();
		public override bool IsOrganisation => true;
	}
}