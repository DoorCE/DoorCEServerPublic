using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

public class UseCase(CodeGenerationProfile codeGenerationProfile) : FunctionalCodeUnit(codeGenerationProfile)
{
    // RELATIONSHIPS
    public readonly List<UCOperation> Operations = [];
    public readonly List<Presenter> Presenters = [];
    public readonly List<Service> Services = [];
    public readonly List<DataTransferObject> OwnState = [];
    public readonly List<DataTransferObject> LegacyState = [];
    public readonly List<DataTransferObject> ReferenceState = [];
    public readonly List<UseCase> Invoked = [];
    public SimpleResultEnumeration? Enumeration { get; set; }
    
    // DERIVED
    public List<DataTransferObject> State
    {
        get { List<DataTransferObject> state = []; 
            state.AddRange(OwnState); state.AddRange(LegacyState); state.AddRange(ReferenceState);
            return state.Distinct().ToList(); }
    }
}