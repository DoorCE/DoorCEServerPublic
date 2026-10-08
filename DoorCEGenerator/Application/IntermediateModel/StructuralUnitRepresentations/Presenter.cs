using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class Presenter(CodeGenerationProfile codeGenerationProfile) : FunctionalCodeUnit(codeGenerationProfile)
{
    // RELATIONSHIPS
    public readonly List<POperation> Operations = [];
    public View? View;
}