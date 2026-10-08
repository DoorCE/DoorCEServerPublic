using DoorCEServer.Application.CataloguingManager.Dtos;

namespace DoorCEServer.Application.DataTemplateManager.Dtos;

public class XDataSchema : XIdentifiableElement
{
    // ***** From XDescribableElement ************
    public required string Title { get; set; }
    public required string Description { get; set; }
    // ***** End from XDescribableElement ************
    
    // e.g.: "concepts": [ ... ]
    public ICollection<XConcept> Concepts { get; set; } = new List<XConcept>();
    // e.g.: "mainConceptPrefix": "door"
    public string? MainConceptPrefix { get; set; }
    // e.g.: "mainConceptName": "tree"
    public required string MainConceptName { get; set; }
    
    // e.g.: "usedNamespaces": [ ... ]
    public ICollection<XNamespace> UsedNamespaces { get; set; } = new List<XNamespace>();
    // e.g.: "defaultNamespacePrefix": "door"
    public string? DefaultNamespacePrefix { get; set; }
    // e.g.: "defaultNamespaceIri": "http://door.ce/trees"
    public string? DefaultNamespaceIri { get; set; }
    
    public required string SeriesUri { get; set; }
    public string? SeriesTitle { get; set; }
    public bool IsUserEditable { get; set; } = true;
}