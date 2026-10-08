namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public abstract class ResultEnumeration(CodeGenerationProfile codeGenerationProfile) : CustomType(codeGenerationProfile)
{
    // RELATIONSHIPS
    public readonly List<Value> Values = [];
}