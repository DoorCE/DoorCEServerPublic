namespace DoorCEModel.Infrastructure.DataModel.Agents;

public interface ManageableResource : IdentifiableElement
{
    // RELATIONSHIPS
    public ResourceEditorshipsLink EditorshipsLink { get; set; }
    public ICollection<Agent> Editors { get; }
    public ICollection<Editorship> EditorRoles { get; }
    
    // Used to determine if the resource is editable by an agent or a user account (not saved)
    public ICollection<EditorRole>? UserRoles { get; set; }
}

public static class ManageableResourceExtensions
{
    public static bool HasMetadataRole(this ManageableResource manageableResource) =>
        manageableResource.UserRoles!.Contains(EditorRole.MetadataEditor);

    public static bool HasOwnershipRole(this ManageableResource manageableResource) =>
        manageableResource.UserRoles!.Contains(EditorRole.OwnershipEditor);

    public static bool HasDistributionRole(this ManageableResource manageableResource) =>
        manageableResource.UserRoles!.Contains(EditorRole.DistributionEditor);
}