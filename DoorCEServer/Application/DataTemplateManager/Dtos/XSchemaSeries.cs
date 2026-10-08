using DoorCEServer.Application.CataloguingManager.Dtos;

namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XSchemaSeries : XManageableResource
{
    // ***** From XDescribableElement ************
    public required string Title { get; set; }
    public required string Description { get; set; }
    // ***** End from XDescribableElement ************
    public required ICollection<short> Type { get; set; }
        
    public string? CurrentSchemaUri;
    public string? CurrentSchemaTitle;
}