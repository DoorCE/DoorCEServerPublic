namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public class SimpleResultEnumeration(CodeGenerationProfile codeGenerationProfile) : ResultEnumeration(codeGenerationProfile)
{
	// ATTRIBUTES
	public required ResultKind Kind;
	
	// RELATIONSHIPS
	public readonly List<ResultUnionEnumeration> Unions = [];
}

public enum ResultKind
{
	Check,
	Invoke,
	Navigation
}