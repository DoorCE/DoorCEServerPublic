namespace DoorCEModel.Infrastructure.DataModel.Agents;

public class ResourceEditorshipsLink
{
    public int Id { get; set; }
    
    // RELATIONSHIPS
    public ICollection<Agent> Editors { get; set;  } = new List<Agent>();
    public ICollection<Editorship> EditorRoles { get; set; } = new List<Editorship>();
    
    public bool Validate(bool isAdmin)
    {
        return isAdmin || EditorRoles.Any(e => e.Roles.Contains(EditorRole.OwnershipEditor));
    }
}