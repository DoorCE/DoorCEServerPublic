using DoorCEModel.Infrastructure.DataModel.Agents;

namespace DoorCEServer.Application.CataloguingManager.Dtos;

public class XManageableResource : XIdentifiableElement
{
    // ==== Ownership properties ====
    public ICollection<string> EditorsUris { get; set; } = new List<string>();
    public Dictionary<string, ICollection<EditorRole>> EditorsRoles { get; set; } = new();
    
    // ==== Determines access to metadata and ownership ====
    public ICollection<EditorRole>? UserRoles { get; set; }
        = [EditorRole.MetadataEditor, EditorRole.OwnershipEditor, EditorRole.DistributionEditor];
}