namespace DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

public abstract class StructuralCodeElement(CodeGenerationProfile codeGenerationProfile) :
	CodeElement(codeGenerationProfile), NamedElement, StructuralElement
{
	// ***** From NamedElement ****************
	public required string Name { get; set; }
	// ***** End from NamedElement ***************
	
	// METHODS
	public string GetElemName()
	{
		return GenerationProfile.NamingConverter.GetElemName(this);
	}
}