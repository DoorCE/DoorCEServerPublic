using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class Controller(CodeGenerationProfile codeGenerationProfile) : FunctionalCodeUnit(codeGenerationProfile)
{
    // RELATIONSHIPS
    public readonly List<COperation> Operations = [];
    public UseCase? UseCase;
    public View? View;
}