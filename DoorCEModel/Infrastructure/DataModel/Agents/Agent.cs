namespace DoorCEModel.Infrastructure.DataModel.Agents;

public abstract class Agent : IdentifiableElement
{
	// ***** From IdentifiableElement ************
	public int Id { get; set; }
	public required string Uri { get; set; } // TODO - assure uniqueness
	// ***** End from IdentifiableElement ************
		
	// ATTRIBUTES
	public string? Description { get; set; }
	public abstract string FullName { get; }
		
	// RELATIONSHIPS
	public ICollection<ContactData> Contacts { get; set; } = new List<ContactData>();
	public ICollection<ResourceEditorshipsLink> ResourceLink { get; set; } = new List<ResourceEditorshipsLink>();
	public ICollection<Editorship> EditorRoles { get; set; } = new List<Editorship>();
	public ICollection<OwnableResource> ManagedResources { get; set; } = new List<OwnableResource>();
	public IEnumerable<Membership> MemberRoles { get; set; } = new List<Membership>();
	
	// Used to determine if the agent is editable by an agent or a user account (not saved)
	public ICollection<ManagerRole>? UserRoles { get; set; }
    
	// OPERATIONS
	public bool HasMetadataRole() => UserRoles!.Contains(ManagerRole.MetadataManager);
	public bool HasOwnershipRole() => UserRoles!.Contains(ManagerRole.OwnershipManager);
	public bool HasAgentRole() => UserRoles!.Contains(ManagerRole.AgentManager);
	
	public bool Validate()
	{
		foreach (var contact in Contacts)
			if (!contact.Validate()) return false;
		return true;
	}
}