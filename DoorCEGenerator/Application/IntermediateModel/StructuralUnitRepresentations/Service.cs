using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class Service(CodeGenerationProfile codeGenerationProfile) : FunctionalCodeUnit(codeGenerationProfile) 
{
    // ATTRIBUTES
    public readonly List<SOperation> Operations = [];
}