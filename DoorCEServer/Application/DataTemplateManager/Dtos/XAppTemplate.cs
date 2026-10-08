using DoorCEServer.Application.CataloguingManager.Dtos;

namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XAppTemplate : XManageableResource
{
    // ***** From XDescribableElement ************
    public required string Title { get; set; }
    public required string Description { get; set; }
    // ***** End from XDescribableElement ************
    
    public required string Language { get; set; }
    public required bool IsReady { get; set; }
    public required string SchemaUri { get; set; }
    public string? SchemaTitle { get; set; }
    public Dictionary<string, string> UseCaseScenarios { get; set; } = new();
    public string? AppDataSpecification { get; set; }
    public ICollection<XConcept> AuxiliaryConcepts { get; set; } = [];
    public string? VersionOfUri { get; set; }
    public string? VersionOfName { get; set; }
}