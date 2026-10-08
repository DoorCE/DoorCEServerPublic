using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

namespace DoorCEGenerator.Application.IntermediateModel.Instructions;

public class Call(CodeGenerationProfile codeGenerationProfile) : Instruction(codeGenerationProfile)
{
	// ATTRIBUTES
	public required UCCallableOperation CalledOperation;
	public Value? Value;
}