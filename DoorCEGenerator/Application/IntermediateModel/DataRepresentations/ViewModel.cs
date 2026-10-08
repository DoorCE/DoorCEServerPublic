using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public class ViewModel(CodeGenerationProfile codeGenerationProfile) : DistinguishableCodeUnit(codeGenerationProfile)
{
    // ATTRIBUTES
    public readonly List<DataTransferObject> InputData = [];
    public readonly List<DataTransferObject> OutputData = [];
    public readonly List<DataTransferObject> ReferenceData = [];
}