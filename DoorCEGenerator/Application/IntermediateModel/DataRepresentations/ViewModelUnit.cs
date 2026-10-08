using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public class ViewModelUnit(CodeGenerationProfile codeGenerationProfile) : NamespaceUnit(codeGenerationProfile)
{
    // ATTRIBUTES
    public readonly List<ViewModel> Models = [];
}