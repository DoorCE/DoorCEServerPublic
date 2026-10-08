using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class COperation(CodeGenerationProfile codeGenerationProfile) : Operation(codeGenerationProfile)
{
	// RELATIONSHIPS
	public readonly List<DataTransferObject> TransferredData = [];
	public UCOperation? Invoked;
	public UCOperation? ReturnTo;
	public Controller? Controller;
}