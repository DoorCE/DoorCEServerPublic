namespace DoorCEModel.Infrastructure.DataModel.Agents;

public class Person : Agent
{
	// ATTRIBUTES
	public required ICollection<string> GivenNames { get; set; }
	public required string FamilyName { get; set; }
	public override string FullName => $"{string.Join(" ", GivenNames)} {FamilyName}";

	// RELATIONSHIPS
	public UserAccount? Account { get; set; }
	public IEnumerable<Organisation> Organisations { get; set; } = new List<Organisation>();
}