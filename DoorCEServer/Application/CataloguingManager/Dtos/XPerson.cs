using DoorCEModel.Infrastructure.DataModel.Agents;

namespace DoorCEServer.Application.CataloguingManager.Dtos
{
	public class XPerson : XAgent
	{
		public required ICollection<string> GivenNames { get; set; }
		public required string FamilyName { get; set; }
		public ICollection<string> OrganisationsUris { get; set; } = new List<string>();
		public Dictionary<string,string> OrganisationsNames { get; set; } = new();
		public Dictionary<string, ICollection<ManagerRole>> RolesInOrganisations { get; set; } = new();
		public string? UserId { get; set; }
		public string? UserEmail { get; set; }
		public string? UserPassword { get; set; }
		
		public override bool IsOrganisation => false;
	}
}