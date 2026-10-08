namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public class Parameter(CodeGenerationProfile codeGenerationProfile) : DataItem(codeGenerationProfile)
{
	// ATTRIBUTES
	public bool HasAttribute { get; set; }
	public bool IsInputOutput { get; set; }
	public bool CanBeIdentifier { get; set; }
	
	// DERIVED ATTRIBUTES
	public override bool IsReferenceField => false;
}