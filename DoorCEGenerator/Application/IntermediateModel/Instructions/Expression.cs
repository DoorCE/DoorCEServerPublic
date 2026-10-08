using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.Instructions;

public class Expression(CodeGenerationProfile codeGenerationProfile) : CodeElement(codeGenerationProfile)
{
	// ATTRIBUTES
	public DataTransferObject? ToBeChecked;
	public Value? Value;
}