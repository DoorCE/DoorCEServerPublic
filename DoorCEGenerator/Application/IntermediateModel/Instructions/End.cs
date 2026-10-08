using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

namespace DoorCEGenerator.Application.IntermediateModel.Instructions;

public class End(CodeGenerationProfile codeGenerationProfile) : Instruction(codeGenerationProfile)
{
    // ATTRIBUTES
    public Value? Value;
}