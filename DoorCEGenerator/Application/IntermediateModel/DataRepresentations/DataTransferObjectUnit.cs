using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public class DataTransferObjectUnit(CodeGenerationProfile codeGenerationProfile) : NamespaceUnit(codeGenerationProfile)
{
    // ATTRIBUTES
    public readonly List<DataTransferObject> Objects = [];
    public readonly List<ResultEnumeration> Enums = [];
}