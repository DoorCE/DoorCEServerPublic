using DoorCEGenerator.Application.IntermediateModel.Instructions;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class UCOperation(CodeGenerationProfile codeGenerationProfile) : UCCallableOperation(codeGenerationProfile)
{
    // ATTRIBUTES
    public readonly List<Instruction> Instructions = [];
    public UseCase? Uc;
    public bool Initial = false; // Function that is called at the beginning of the use case execution, will be called from other use cases
    public bool Invoking = false; // Function passing control to another use case, will call other use cases (needed for certain technologies)
    public bool Returning = false; // Function returning control to the caller use case, will be called from other use cases
    public bool CheckProxy = false; // Function that is a proxy calling another use case to check if that use case can be executed
    
    // RELATIONSHIPS
    public UCOperation? Previous;
}