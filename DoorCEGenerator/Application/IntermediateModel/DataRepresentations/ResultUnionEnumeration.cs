namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
public class ResultUnionEnumeration(CodeGenerationProfile codeGenerationProfile) : ResultEnumeration(codeGenerationProfile)
{
	// ATTRIBUTES
	public readonly List<SimpleResultEnumeration> Members = [];
}