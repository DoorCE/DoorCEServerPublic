using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public abstract class Operation(CodeGenerationProfile codeGenerationProfile) : StructuralCodeElement(codeGenerationProfile)
{
	// ATTRIBUTES
	public DataItemType? ReturnType;
	public readonly List<Parameter> Parameters = [];
	
	// METHODS
	public string ToHeaderCode(int tabs = 0)
	{
		return GenerationProfile.Generator.GetHeaderCode(this, tabs);
	}
}