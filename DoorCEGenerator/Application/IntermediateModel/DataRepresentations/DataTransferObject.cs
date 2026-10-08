namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
public class DataTransferObject(CodeGenerationProfile codeGenerationProfile) : CustomType(codeGenerationProfile)
{
    // ATTRIBUTES
    public bool Auxiliary { get; set; }
    
    // DERIVED ATTRIBUTES
    public bool IsAuxiliaryCollection => Auxiliary && Fields is [{ Multiple: true, Type: DataTransferObject }];
    public bool IsSimple => Auxiliary && Fields is [{ Multiple: false, Type: Primitive or ResultEnumeration }];
    public bool CanBeMap => Auxiliary && Fields is [{ CanBeSimplified: true, Multiple: true, Type: DataTransferObject }];
    public List<Field> ActualFields => Auxiliary && Fields is [{Type: DataTransferObject dto}] ? dto.ActualFields : Fields;
    
    // RELATIONSHIPS
    public readonly List<Field> Fields = [];
    public SimpleResultEnumeration? Enumeration { get; set; }
}