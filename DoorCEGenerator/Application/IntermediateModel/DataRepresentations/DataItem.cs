using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public abstract class DataItem(CodeGenerationProfile codeGenerationProfile) :
	StructuralCodeElement(codeGenerationProfile), DistinguishableElement
{
	// ATTRIBUTES
	public bool Multiple { get; set; }
	public bool Optional { get; set; }
	public bool CanBeSimplified { get; set; }
	
	// DERIVED ATTRIBUTES
	public bool IsCollection => Multiple || Type is DataTransferObject { IsAuxiliaryCollection: true };
	public abstract bool IsReferenceField { get; }
	
	// RELATIONSHIPS
	public required DataItemType Type { get; set; }
	
	// METHODS
	public string GetVarName()
	{
		return GenerationProfile.NamingConverter.GetVarName(this);
	}
}