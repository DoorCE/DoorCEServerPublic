namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class SOperation(CodeGenerationProfile codeGenerationProfile) : UCCallableOperation(codeGenerationProfile)
{
	// ATTRIBUTES
	public required string OperationTarget;
	public PredicateType Type = PredicateType.None;
	public Service? Service;
}