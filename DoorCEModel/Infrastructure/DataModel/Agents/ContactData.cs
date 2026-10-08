namespace DoorCEModel.Infrastructure.DataModel.Agents;

public class ContactData
{
	// ***** From IdentifiableElement ************
	public int Id { get; set; }
	public required string Uri { get; set; } // TODO - assure uniqueness
	// ***** End from IdentifiableElement ************
		
	// ATTRIBUTES
	public string? Name { get; set; }
	public required string Contents { get; set; }
		
	// RELATIONSHIPS
	public Agent? Agent { get; set; }
	public ICollection<OwnableResource> Resources { get; set; } = new List<OwnableResource>();

	public bool Validate()
	{
		return null != Agent || null != Name;
	}
}