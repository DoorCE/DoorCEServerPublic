using DoorCEModel.Infrastructure.DataModel.DataSchemas;

namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public class Primitive(CodeGenerationProfile codeGenerationProfile) : DataItemType
{
    // ATTRIBUTES
    public required PrimitiveType Value { get; set; }
    
    // METHODS
    public string GetElemName()
    {
        return codeGenerationProfile.Generator.MapPrimitiveType(Value);
    }

    public string GetVarName()
    {
        return codeGenerationProfile.NamingConverter.GetVarName(this);
    }
}