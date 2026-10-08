using DoorCEModel.Infrastructure.DataModel.Applications;

namespace DoorCEModel.Infrastructure.DataModel.CodeContents;

public class CodePackage : IdentifiableElement {
	public int Id { get; set; }

	// ***** From IdentifiableElement ************
	public required string Uri { get; set; }
	// ***** End from IdentifiableElement *********
	
	// ATTRIBUTES
	public required string CodeFramework{ get; set; }
	
	// RELATIONSHIPS
	public IEnumerable<CodeFile> Files { get; set; } = new List<CodeFile>();
	public required AppTemplate Template { get; set; }
}