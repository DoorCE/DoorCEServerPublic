namespace DoorCEModel.Infrastructure.DataModel.CodeContents;

public class CodeFile {
	public int Id { get; set; }
		
	// ATTRIBUTES
	public required string Path{ get; set; }
	public required string CodeContents{ get; set; }
		
	// RELATIONSHIPS
	public CodePackage? Package { get; set; }
}