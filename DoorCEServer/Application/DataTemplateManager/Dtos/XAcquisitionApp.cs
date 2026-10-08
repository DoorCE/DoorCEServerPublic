using DoorCEServer.Application.CataloguingManager.Dtos;

namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XAcquisitionApp : XManageableResource
{
    // ***** From XDescribableElement ************
    public required string Title { get; set; }
    public required string Description { get; set; }
    // ***** End from XDescribableElement ************
    
    public required string? TemplateUri { get; set; } // can be null for platform apps *only* 
    public string? TemplateTitle { get; set; }
    public required string? ActiveResourceUri { get; set; }
    public Dictionary<string,string>? ActiveResourceTitle { get; set; }
    public required IEnumerable<string> SourceResourceUris { get; set; }
    public Dictionary<string,Dictionary<string,string>>? SourceResourceTitles { get; set; }
    public required bool IsVisible { get; set; }
    public required bool IsEnabled { get; set; }
    public short? Status { get; set; }
}