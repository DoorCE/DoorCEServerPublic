using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

namespace DoorCEGenerator.Application.IntermediateModel.Instructions;

public abstract class Instruction(CodeGenerationProfile codeGenerationProfile) : CodeElement(codeGenerationProfile)
{
	// ATTRIBUTES
	public string? Label;
	// RELATIONSHIPS
	public required UCOperation EnclosingOperation;
}
