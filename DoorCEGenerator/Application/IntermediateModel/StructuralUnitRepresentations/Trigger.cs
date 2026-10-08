using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class Trigger(CodeGenerationProfile codeGenerationProfile) : CodeElement(codeGenerationProfile), NamedElement {
	// ***** From NamedElement ****************
	public required string Name { get; set; }
	// ***** End from NamedElement ***************
	
	// ATTRIBUTES
	public COperation? Action;
	public COperation? Condition;
	public string Type = "button";
	public string? Label;
}