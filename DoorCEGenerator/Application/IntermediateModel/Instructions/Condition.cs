using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.Instructions;

public class Condition(CodeGenerationProfile codeGenerationProfile) : CodeElement(codeGenerationProfile)
{
	// ATTRIBUTES
	public readonly List<Instruction> Instructions = [];
	public readonly List<Expression> Expressions = [];
}