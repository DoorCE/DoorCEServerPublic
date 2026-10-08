namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public class Field(CodeGenerationProfile codeGenerationProfile) : DataItem(codeGenerationProfile)
{
    // ATTRIBUTES
    public bool IsItemIdentifier { get; set; }
    
    // DERIVED ATTRIBUTES
    public override bool IsReferenceField => Type is DataTransferObject { IsSimple: false };
}