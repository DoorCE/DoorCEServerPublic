using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.Instructions;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;

namespace DoorCEGenerator.Application.IntermediateModel;

public class IntermediateRepresentationFactory(CodeGenerationProfile generationProfile)
{
    public readonly CodeGenerationProfile GenerationProfile = generationProfile;

    public ResultUnionEnumeration GetResultUnionEnumeration(string name)
        { return new ResultUnionEnumeration(GenerationProfile) {Name = name}; }
    public Controller GetController(string name, UseCase? useCase)
        { return new Controller(GenerationProfile) {Name = name, UseCase = useCase}; }
    public COperation GetCOperation(string name, UCOperation? invoked = null, DataItemType? returnType = null)
        { return new COperation(GenerationProfile) {Name = name, Invoked = invoked, ReturnType = returnType}; }
    public COperation GetCOperation(string name, UCOperation invoked, PrimitiveType returnPrim)
    { return new COperation(GenerationProfile) {Name = name, Invoked = invoked, ReturnType = GetPrimitive(returnPrim)}; }
    public DataTransferObjectUnit GetDataTransferObjectUnit(string name)
        { return new DataTransferObjectUnit(GenerationProfile){Name = name}; }
    public POperation GetPOperation(string name, Presenter? pres = null)
        { return new POperation(GenerationProfile){Name = name, Pres = pres}; }
    public Presenter GetPresenter(string name)
        { return new Presenter(GenerationProfile){Name = name}; }
    public SOperation GetSOperation(string name, string operationTarget, PredicateType type = PredicateType.None,
        Service? service = null, DataItemType? returnType = null)
        { return new SOperation(GenerationProfile){Name = name, OperationTarget = operationTarget, 
            Type = type, Service = service, ReturnType = returnType}; }
    public Service GetService(string name)
        { return new Service(GenerationProfile){Name = name}; }
    public UCOperation GetUCOperation(string name, UseCase? uc = null, DataItemType? returnType = null)
        { return new UCOperation(GenerationProfile){Name = name, Uc = uc, ReturnType = returnType}; }
    public UCOperation GetUCOperation(string name, UseCase uc, PrimitiveType returnPrim)
    { return new UCOperation(GenerationProfile){Name = name, Uc = uc,
        ReturnType = GetPrimitive(returnPrim)}; }
    public UseCase GetUseCase(string name)
        { return new UseCase(GenerationProfile) {Name = name}; }
    public View GetView(string name, Controller controller, Presenter presenter, ViewModel viewModel)
        { return new View(GenerationProfile) { Name = name, ViewModel = viewModel, Controller = controller, Presenter = presenter }; }
    public ViewModelUnit GetViewModelUnit(string name)
        { return new ViewModelUnit(GenerationProfile) { Name = name }; }
    public Trigger GetTrigger(string name, COperation? action = null, COperation? condition = null)
        { return new Trigger(GenerationProfile){ Name = name, Action = action, Condition = condition }; }
    public SimpleResultEnumeration GetSimpleResultEnumeration(string name, ResultKind kind)
        { return new SimpleResultEnumeration(GenerationProfile){ Name = name, Kind = kind }; }
    public DataTransferObject GetDataTransferObject(string name, bool auxiliary = false)
        { return new DataTransferObject(GenerationProfile) {Name = name, Auxiliary = auxiliary}; }
    public DataTransferObject GetOptionsDataTransferObject(String fieldName, DataTransferObject type) {
        DataTransferObject dto = new (GenerationProfile){Name = fieldName + " options", Auxiliary = true};
        Field optionsField = GetConceptField("options", type, true);
        optionsField.CanBeSimplified = true;
        dto.Fields.Add(optionsField);
        return dto;
    }
    public ViewModel GetViewModel(string name)
        { return new ViewModel(GenerationProfile){ Name = name }; }
    public Field GetPrimitiveField(string name, PrimitiveType type, bool multiple = false, bool optional = false,
        bool isIdentifier = false)
    {
        return new Field(GenerationProfile) {
            Name = name, Type = GetPrimitive(type), Multiple = multiple, Optional =  optional,
            IsItemIdentifier = isIdentifier };
    }
    public Field GetConceptField(string name, CustomType type, bool multiple = false, bool optional = false)
    {
        return new Field(GenerationProfile)
            { Name = name, Type = type, Multiple = multiple, Optional = optional };
    }
    public Parameter GetParameter(CustomType dit, bool isAttribute = false, string? name = null)
        { return new Parameter(GenerationProfile) { Name = name ?? dit.Name, Type = dit,
            HasAttribute = isAttribute }; }
    public Parameter GetIdentifierParameter(CustomType dit, bool isAttribute = false, string? name = null)
        { return new Parameter(GenerationProfile) {
            Name = name ?? dit.Name, Type = dit, HasAttribute = isAttribute, CanBeIdentifier = true
        }; }
    public Primitive GetPrimitive(PrimitiveType type)
        { return new Primitive(GenerationProfile){ Value = type }; }
    public Value GetValue(string name, ResultEnumeration? parent = null)
        {return new Value(GenerationProfile.NamingConverter){ Name = name, Parent = parent }; }
    public Call GetCall(UCCallableOperation calledOperation, UCOperation enclosingOperation, Value? value = null)
        { return new Call(GenerationProfile){ CalledOperation = calledOperation, Value = value,
            EnclosingOperation = enclosingOperation}; }
    public Decision GetDecision(UCOperation enclosingOperation, string? label = null)
        { return new Decision(GenerationProfile){ Label = label,
            EnclosingOperation = enclosingOperation }; }
    public Condition GetCondition()
        { return new Condition(GenerationProfile); }
    public Expression GetExpression()
        { return new Expression(GenerationProfile); }
    public End GetEnd(UCOperation enclosingOperation, Value? value = null)
        { return new End(GenerationProfile){ Value = value,
            EnclosingOperation = enclosingOperation }; }
}