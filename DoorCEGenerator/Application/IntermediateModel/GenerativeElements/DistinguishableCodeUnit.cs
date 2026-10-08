namespace DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

public abstract class DistinguishableCodeUnit(CodeGenerationProfile codeGenerationProfile):
	CodeUnit(codeGenerationProfile), DistinguishableElement
{
	// METHODS
	public string GetVarName()
	{
		return GenerationProfile.NamingConverter.GetVarName(this);
	}
}