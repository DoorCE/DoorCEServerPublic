using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;
using DoorCEModel.Infrastructure.DataModel.CodeContents;

namespace DoorCEGenerator.Application.IntermediateModel;

public class IntermediateRepresentation(IntermediateRepresentationFactory factory, string appName)
{
    public string AppName { get; } = appName;
    public List<View> Views {get; set;} = [];
    public List<Controller> Controllers {get; set;} = [];
    public List<UseCase> UseCases {get; set;} = [];
    public List<Presenter> Presenters {get; set;} = [];
    public List<Service> Services {get; set;} = [];
    public DataTransferObjectUnit DataTransferObjectUnit {get; set;} = factory.GetDataTransferObjectUnit("Dto");
    
    public ViewModelUnit ViewModelUnit {get; set;} = factory.GetViewModelUnit("ViewModel");
    
    public CodeFile ToMainCodeFile(){
        ICodeGenerator gen = factory.GenerationProfile.Generator;
        return new CodeFile{
            Path = @"/" + gen.GetMainFileName(),
            CodeContents = gen.GetCode(this),
        };
    }
}

public enum PredicateType {
    None,
    Show,
    Read,
    Update,
    Delete,
    Check,
    CheckExisting,
    Execute,
    Select,
    Enter,
    Invoke,
    Repetition
}