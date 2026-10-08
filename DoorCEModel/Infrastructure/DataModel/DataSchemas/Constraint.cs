namespace DoorCEModel.Infrastructure.DataModel.DataSchemas;

public class Constraint
{
	// ATTRIBUTES
	public required string ValidationRule { get; set; }
	
	// RELATIONSHIPS
	public IEnumerable<Concept> Subjects { get; set; } = new List<Concept>();
}