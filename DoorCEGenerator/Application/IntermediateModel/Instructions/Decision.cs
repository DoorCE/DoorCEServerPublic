namespace DoorCEGenerator.Application.IntermediateModel.Instructions;

public class Decision(CodeGenerationProfile codeGenerationProfile) : Instruction(codeGenerationProfile)
{
	// ATTRIBUTES
	public readonly List<Condition> Conditions = [];
}