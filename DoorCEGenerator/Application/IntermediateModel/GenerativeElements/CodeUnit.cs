using DoorCEModel.Infrastructure.DataModel.CodeContents;

namespace DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

public abstract class CodeUnit(CodeGenerationProfile codeGenerationProfile) : StructuralCodeElement(codeGenerationProfile)
{
	// METHODS
	public CodeFile? ToCodeFile(string basePath)
	{
		return GenerationProfile.Generator.ToCodeFile((dynamic) this, basePath);
	}
}