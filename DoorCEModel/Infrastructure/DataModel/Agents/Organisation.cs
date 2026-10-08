namespace DoorCEModel.Infrastructure.DataModel.Agents;

public class Organisation : Agent
{
	// ATTRIBUTES
	public required string Name { get; set; }
	public override string FullName => Name;
		
	// RELATIONSHIPS
	public IEnumerable<Person> Members { get; set; } = new List<Person>();
}