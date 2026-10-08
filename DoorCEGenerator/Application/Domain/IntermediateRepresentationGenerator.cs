using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using DoorCEGenerator.Application.IntermediateModel;
using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.Instructions;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;
using DoorCEGenerator.WebApi.Controllers;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Utils.Extensions;

namespace DoorCEGenerator.Application.Domain;

public class IntermediateRepresentationGenerator(ILogger<AppGenerationController> logger, IntermediateRepresentationFactory factory,
        Dictionary<string,string> conceptUriMap, string appName) : DScriptBaseVisitor<IntermediateRepresentation> {
    private readonly IntermediateRepresentation _result = new(factory, appName);
    // Auxiliary structures
    UseCase? CurrentUC {get; set;}
    View? CurrentView {get; set;}
    UCOperation? CurrentUCO {get; set;}
    UCOperation? PreviousUCO {get; set;}
    Condition? CurrentCondition {get; set;}
    COperation? ConditionCO {get; set;}
    POperation? RecentPOP {get; set;}
    List<DataTransferObject> InheritedDTOD {get; set;} = []; // Inherited DTOs for data processing
    List<DataTransferObject> CurrentDTOP {get; set;} = []; // Current DTOs for presentation
    List<DataTransferObject> CurrentDTOD {get; set;} = []; // Current DTOs for data processing
    string? CurrentLabel {get; set;}
    PredicateType LastPredicateType {get; set;} = PredicateType.None;
    PredicateType LastNonInvokePT {get; set;} = PredicateType.None;
    private ResultEnumeration? LastInvokeEnumeration { get; set; }
    bool FirstSentence {get; set;}
    bool ConditionPossible {get; set;}
    Dictionary<string, List<int>> UndeclaredConceptUsageLines {get; set;} = new();
    Dictionary<string,View?> LabelToView {get; set;} = new();
    Dictionary<string,Trigger> UcNameToTrigger {get; set;} = new();
    Dictionary<string,List<View>> UcNameToView {get; set;} = new();
    Dictionary<(string,string),UCOperation> UCViewToUCOperation {get; set;} = new();
    SimpleResultEnumeration? ScreenIdEnum {get; set;}

    // Configuration

    public bool Verbose {get; set;}

    private static string ObtainName(ParserRuleContext context){
        IList<IParseTree> strings;
        switch (context)
        {
            case DScriptParser.NameContext:
            case DScriptParser.NotionContext:
            case DScriptParser.ValueContext:
            case DScriptParser.UilabelContext:
                strings = context.children;
                break;
            case DScriptParser.DatatypeContext:
                return context.GetText();
            case DScriptParser.MultnotionContext:
                return context.GetText();
            case DScriptParser.TriggertypeContext:
                return context.GetText();
            case DScriptParser.ViewtypeContext:
                return context.GetText();
            default:
                throw new Exception("Unknown context type");
        }     
        return string.Join(" ", strings.ToList().Select(x => x.GetText()));
    }

    public override IntermediateRepresentation VisitStart([NotNull] DScriptParser.StartContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug,"Starting processing RSL specification");
        ScreenIdEnum = factory.GetSimpleResultEnumeration("screen id", ResultKind.Navigation);
        _result.DataTransferObjectUnit.Enums.Add(ScreenIdEnum);
        Value start = factory.GetValue("start", ScreenIdEnum);
        ScreenIdEnum.Values.Add(start);
        ViewModel appState = factory.GetViewModel("app state");
        DataTransferObject appStateDto = factory.GetDataTransferObject("screen", true);
        Field screenId = factory.GetConceptField("screen id", ScreenIdEnum);
        appStateDto.Fields.Add(screenId);
        appState.OutputData.Add(appStateDto);
        _result.ViewModelUnit.Models.Add(appState);
        base.VisitStart(context);
        if (0 != UndeclaredConceptUsageLines.Count) {
            string msg = "Undeclared concept(s) used in the specification:\n";
            foreach (var entry in UndeclaredConceptUsageLines)
                msg += $"- Concept '{entry.Key}' used at line(s): {string.Join(", ", entry.Value)}\n";
            throw new Exception(msg);
        }
        if (0 != UcNameToView.Count) {
            string msg = "Undeclared invoked use case(s) used in the specification:\n";
            foreach (var entry in UcNameToView)
                msg += $"- Use case '{entry.Key}' invoked from view(s): " 
                       + $"{string.Join(", ", entry.Value.Select(v => v.Name))}\n";
            throw new Exception(msg);
        }
        PostProcess();
        return _result;
    }

    //*****************************************************************************************************
    // 1. USE CASE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitUsecase([NotNull] DScriptParser.UsecaseContext context){
        // 1. Create ‘UseCaseClass’ based on ‘name’ and set as ‘CurrentUCC’
        string name = ObtainName(context.name());
        if (Verbose) logger.Log(LogLevel.Debug, "Parsing Use Case: " + name);
        UseCase ucC = factory.GetUseCase(name);
        _result.UseCases.Add(ucC);
        CurrentUC = ucC;

        // 2. If not "Start" use case -> create ‘Enumeration’ (if it does not exist) based on ‘name’;
        // add it to ‘DataTransferObjectUnit’; set it as ‘CurrentUCC.Enumeration’
        if (!"Start".NamingEquals(name)) {
            SimpleResultEnumeration? en = (SimpleResultEnumeration?)_result.DataTransferObjectUnit.Enums
                .Find(x => x is SimpleResultEnumeration { Kind: ResultKind.Invoke }
                           && name.NamingEquals(x.Name));
            if (null == en) {
                en = factory.GetSimpleResultEnumeration(CurrentUC!.Name, ResultKind.Invoke);
                _result.DataTransferObjectUnit.Enums.Add(en);
            }
            ucC.Enumeration = en;
        }

        // 3. Reset auxiliary structures for the new use case
        CleanupUseCaseGenerator();
        return base.VisitUsecase(context);
    }

    private void CleanupUseCaseGenerator(){
        CurrentView = null;
        CurrentUCO = null;
        CurrentCondition = null;
        ConditionCO = null;
        InheritedDTOD = [];
        CurrentDTOP = [];
        CurrentDTOD = [];
        CurrentLabel = null;
        LastPredicateType = PredicateType.None;
        LastNonInvokePT = PredicateType.None;
        FirstSentence = true;
        ConditionPossible = false;
        LabelToView = new Dictionary<string, View?>();
    }

    //*****************************************************************************************************
    // 2. PRECONDITION
    //*****************************************************************************************************  

    public override IntermediateRepresentation VisitUcconditions([NotNull] DScriptParser.UcconditionsContext context)
    {
        try {
            if (Verbose) logger.Log(LogLevel.Debug, "Precondition sentence: " + context.GetText());
            ProcessPreconditions(context.conditions());
        } catch (Exception e) {
            WriteErrorMessage(e);
        }
        return _result;
    }

    private void ProcessPreconditions(DScriptParser.ConditionsContext? context)
    {
        if (null == context) return;
        DScriptParser.ContextconditionContext cCondition = context.condition().contextcondition();
        DScriptParser.ValueconditionContext vCondition = context.condition().valuecondition();
        // 1. For each ‘condition’ create ‘DataTransferObject’ (if it does not exist) based on ‘notion’
        //    add it to ‘ViewModel’
        string notionName = null != cCondition ? ObtainName(cCondition.notion()) :
            ObtainName(vCondition.notion());
        DataTransferObject? dto = _result.DataTransferObjectUnit.Objects
            .Find(x => notionName.NamingEquals(x.Name));
        if (null == dto) {
            dto = factory.GetDataTransferObject(notionName);
            _result.DataTransferObjectUnit.Objects.Add(dto);
        }
        int line = null != cCondition ? cCondition.notion().Start.Line : vCondition.notion().Start.Line;
        string? key = UndeclaredConceptUsageLines.Keys
            .FirstOrDefault(k => k.NamingEquals(notionName));
        if (null != key)
            UndeclaredConceptUsageLines[key].Add(line);
        else UndeclaredConceptUsageLines.Add(notionName, [line]);
        if (null != cCondition){
            // 2. if ‘condition’ is ‘contextcondition’ -> add ‘DataTransferObject’ to ‘InheritedDTOD’ and ‘CurrentUCC.attrs’
            InheritedDTOD.Add(dto); // TODO - replace by CurrentUCC.attrs
            CurrentUC!.LegacyState.Add(dto);
        } else {
            // 3. If any ‘valuecondition’ exists -> create ‘COperation’; set it as ‘ConditionCO’;
            //    create ‘UCOperation’; add it to ‘CurrentUCC’; attach it to ‘COperation’;
            //    set ‘UCOperation.returnType’ as ‘boolean’
            if (null == ConditionCO)
            {
                UCOperation ucop = factory.GetUCOperation("precondition check ", CurrentUC!,
                    PrimitiveType.Boolean);
                CurrentUC!.Operations.Add(ucop);
                COperation cop = factory.GetCOperation("invoke check " + CurrentUC.Name, ucop, 
                    PrimitiveType.Boolean);
                ConditionCO = cop;
            }
            // 4. For each ‘valuecondition’ -> Algorithm for value condition
            ProcessValueCondition(vCondition, dto);
        }
        ProcessPreconditions(context.conditions());
    }

    private void ProcessValueCondition(DScriptParser.ValueconditionContext context, DataTransferObject dto){
        string notionName = ObtainName(context.notion());
        // 1. Create 'Parameter' with ‘DataItem’ (type as ‘notion’);
        // add ‘Parameter’ to ‘ConditionCO.invoked’ ('UCOperation');
        // attach ‘DataTransferObject’ to ‘ConditionCO’ (as ‘data’)
        Parameter par = factory.GetIdentifierParameter(dto, false, notionName);
        if (CurrentUC!.State.Contains(dto)) par.HasAttribute = true;
        ConditionCO!.Invoked!.Parameters.Add(par);
        PropagateIdFromServiceToUc(dto, ConditionCO.Invoked);
        ConditionCO.TransferredData.Add(dto);
        // 2. Create ‘Service’ (if it does not exist) based on ‘notion’; attach it to ‘CurrentUCC’
        Service? service = _result.Services
            .Find(x => notionName.NamingEquals(x.Name));
        SOperation? sop = service?.Operations
            .Find(x => ("check existing " + notionName).NamingEquals(x.Name) && PredicateType.CheckExisting == x.Type);
        if (null == service) {
            service = factory.GetService(notionName);
            _result.Services.Add(service);
        }
        if (!CurrentUC.Services.Contains(service)) CurrentUC.Services.Add(service);
        // 3. Create ‘Enumeration’ (if it does not exist) based on ‘notion’; add it to ‘DataTransferObjectUnit’
        SimpleResultEnumeration? en = (SimpleResultEnumeration?)_result.DataTransferObjectUnit.Enums
            .Find(x => x is SimpleResultEnumeration { Kind: ResultKind.Check } 
                       && notionName.NamingEquals(x.Name));
        string valueName = ObtainName(context.value());
        Value? value = en?.Values.Find(x => valueName.NamingEquals(x.Name));
        if (null == en) {
            en = factory.GetSimpleResultEnumeration(notionName, ResultKind.Check);
            _result.DataTransferObjectUnit.Enums.Add(en);
            dto.Enumeration = en;
        }
        // 4. Create ‘Value’ based on ‘value’; add it to ‘Enumeration’
        if (null == value) {
            value = factory.GetValue(valueName, en);
            en.Values.Add(value);
        }
        // 5. Create ‘SOperation’ (if it does not exist); add 'Parameter' to ‘SOperation’;
        // add ‘SOperation’ to 'Service'
        // set ‘SOperation.returnType’ to ‘Enumeration.name’
        if (null == sop) {
            sop = factory.GetSOperation("check existing " + notionName, GetConceptUri(notionName), 
                PredicateType.CheckExisting, service, en);
            sop.Parameters.Add(par);
            service.Operations.Add(sop);
        }
        // 6. Create ‘Call’; append it to ‘UCOperation’; attach ‘SOperation’ to ‘Call’
        Call call = factory.GetCall(sop, ConditionCO.Invoked, value);
        ConditionCO.Invoked.Instructions.Add(call);
    }

    private string GetConceptUri(string notionName)
    {
        string key = conceptUriMap.Keys.FirstOrDefault(k => k.NamingEquals(notionName)) 
                     ?? throw new Exception("Unexpected concept name: " + notionName);
        return conceptUriMap[key];
    }

    //*****************************************************************************************************
    // 3. ANY SENTENCE
    //*****************************************************************************************************  

    public override IntermediateRepresentation VisitSvosentence([NotNull] DScriptParser.SvosentenceContext context)
    {
        CurrentLabel = context.label().NUMBER().GetText();
        return base.VisitSvosentence(context);
    }

    public override IntermediateRepresentation VisitAltsvosentence([NotNull] DScriptParser.AltsvosentenceContext context)
    {
        CurrentLabel = context.altlabel().CHAR().GetText() + context.altlabel().NUMBER().GetText();
        return base.VisitAltsvosentence(context);
    }

    private void CreateCall(UCCallableOperation uop){
        // 1. Create ‘Call’; attach ‘SOperation’ to it; ; set ‘Call.label’ as CurrentLabel
        Call call = factory.GetCall(uop, CurrentUCO!);
        call.Label = CurrentLabel!;
        // 2. If ‘CurrentCondition’ empty ->  append ‘Call’ to ‘CurrentUCO’ else append ‘Call’ to ‘CurrentCondition’ 
        if (null == CurrentCondition) CurrentUCO!.Instructions.Add(call);
        else CurrentCondition.Instructions.Add(call);
    }

    private void SetLastPredicateTypes(PredicateType ptype){
        LastPredicateType = ptype;
        LastNonInvokePT = ptype;
    }

    //*****************************************************************************************************
    // 4. SYSTEM-TO-DATA (READ) SENTENCE
    //*****************************************************************************************************  

    public override IntermediateRepresentation VisitReadpredicate([NotNull] DScriptParser.ReadpredicateContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": System-to-Data (read) predicate: " + context.GetText());
        ConditionPossible = true;
        // 1. Create ‘DataTransferObject’ (if it does not exist) based on ‘notion’; add it to ‘CurrentDTOP’;
        // attach it to ‘DatTransferObjectUnit’
        string notionName = ObtainName(context.notion());
        DataTransferObject? dto = _result.DataTransferObjectUnit.Objects
            .Find(x => notionName.NamingEquals(x.Name));
        if (null == dto) {
            dto = factory.GetDataTransferObject(notionName);
            _result.DataTransferObjectUnit.Objects.Add(dto);
        }
        int line = context.notion().Start.Line;
        string? key = UndeclaredConceptUsageLines.Keys
            .FirstOrDefault(k => k.NamingEquals(notionName));
        if (null != key)
            UndeclaredConceptUsageLines[key].Add(line);
        else UndeclaredConceptUsageLines.Add(notionName, [line]);
        CurrentDTOP.Add(dto); // TODO - error if already exists in CurrentDTOP
        // 2.- 4. ...
        SOperation sop = CreateReadServiceOperation(notionName, dto, CurrentUC!, CurrentUCO!, CurrentDTOD, InheritedDTOD);
        // 5. Create ‘Call’ etc.
        CreateCall(sop);

        LabelToView.Add(CurrentLabel!,null);
        SetLastPredicateTypes(PredicateType.Read);
        return _result;
    }

    private SOperation CreateReadServiceOperation(String notionName, DataTransferObject dto, UseCase uc,
        UCOperation? ucop, List<DataTransferObject> currentDTOD, List<DataTransferObject> inheritedDTOD)
    {
        // 2. Create ‘Service’ (if it does not exist) based on ‘notion’; attach it to ‘CurrentUCC’
        // TODO - assure single service for "normal" and "list" DTOs based on the same persistent concept 
        Service? service = _result.Services
            .Find(x => notionName.NamingEquals(x.Name));
        SOperation? sop = service?.Operations
            .Find(x => ("read " + notionName).NamingEquals(x.Name));
        if (null == service) {
            service = factory.GetService(notionName);
            _result.Services.Add(service);
        }
        if (!uc.Services.Contains(service)) uc.Services.Add(service);
        // 3. Create ‘SOperation’ (if it does not exist) based on ‘notion’;
        // set ‘returnType’ based on ‘DataTransferObject’;
        // add ‘SOperation’ to ‘Service’
        if (null == sop) { // TODO - handle overloaded methods
            string conceptUri = !dto.CanBeMap
                ? GetConceptUri(notionName)
                : GetConceptUri(((DataTransferObject) dto.Fields[0].Type).Name);
            sop = factory.GetSOperation("read " + notionName, conceptUri, 
                PredicateType.Read, service, dto);
            // 4. For each 'DataTransferObject' in 'CurrentDTOD' + 'InheritedDTOD' create a 'Parameter' with ‘DataItem’
            //    (‘DataTransferObject’ as its base type); add the ‘Parameters’ to the ‘SOperation’
            foreach (DataTransferObject cdto in currentDTOD)
                // Create an 'identifier' parameter if the 'DataTransferObject' is the same as the 'SOperation' return type
                if (cdto.Name.NamingEquals(dto.Name)) {
                    if (!sop.Parameters.Exists(p => cdto.Name == p.Name)) {
                        Parameter par = factory.GetIdentifierParameter(cdto);
                        if (uc.State.Contains(cdto)) par.HasAttribute = true;
                        sop.Parameters.Add(par);
                        if (null != ucop) PropagateIdFromServiceToUc(dto, ucop);
                    }
                } else if (!sop.Parameters.Exists(p => cdto.Name == p.Name)) {
                    Parameter par = factory.GetParameter(cdto);
                    if (uc.State.Contains(cdto)) par.HasAttribute = true;
                    sop.Parameters.Add(par);
                }
            foreach (DataTransferObject idto in inheritedDTOD) {
                // Create an 'identifier' parameter if the 'DataTransferObject' is the same as the 'SOperation' return type
                if (idto.Name.NamingEquals(dto.Name)) {
                    if (!sop.Parameters.Exists(p => idto.Name == p.Name)) {
                        Parameter par = factory.GetIdentifierParameter(idto, true);
                        sop.Parameters.Add(par);
                        if (null != ucop) PropagateIdFromServiceToUc(dto, ucop);
                    }
                } else if (!sop.Parameters.Exists(p => idto.Name == p.Name)) {
                    Parameter par = factory.GetParameter(idto, true);
                    sop.Parameters.Add(par);
                }
            }
            service.Operations.Add(sop);
        }
        return sop;
    }

    //Propagate the 'identifier' designation from a service parameter to the corresponding parameter in 'CurrentUCO' if it exists
    private void PropagateIdFromServiceToUc(DataTransferObject dto, UCOperation ucop)
    {
        Parameter? ipar;
        if (null != (ipar = ucop.Parameters.FirstOrDefault(p => p.Type == dto && !p.Multiple)))
            ipar.CanBeIdentifier = true;
        else if (null != ucop.Previous) PropagateIdFromServiceToUc(dto, ucop.Previous);
    }

    //*****************************************************************************************************
    // 5. SYSTEM-TO-VIEW SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitShowpredicate([NotNull] DScriptParser.ShowpredicateContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": System-to-View predicate: " + context.GetText());
        ConditionPossible = false;
        // 1. Create ‘View’ (if it does not exist) based on ‘notion’; set it as ‘CurrentView’
        string notionName = ObtainName(context.notion());
        View? view = _result.Views
            .Find(v => notionName.NamingEquals(v.Name));
        if (null == view) {
            // 2. If ('currentView' did not exist) -> create ‘Controller’;
            // attach it to ‘currentView’; attach ‘CurrentUC’ to the ‘Controller’
            Controller controller = factory.GetController(notionName, CurrentUC!);
            // 3. If ('currentView' did not exist) -> create ‘Presenter’; attach it to ‘View’; attach it to the ‘CurrentUC’
            Presenter presenter = factory.GetPresenter(notionName);
            CurrentUC!.Presenters.Add(presenter);
            // 4. If ('currentView' did not exist) -> create ‘ViewModel’; attach it to ‘View’; attach it to the ‘ViewModelUnit’
            ViewModel viewModel = factory.GetViewModel(notionName);
            _result.ViewModelUnit.Models.Add(viewModel);
            view = factory.GetView(notionName, controller, presenter, viewModel);
            if (null == CurrentUC.Enumeration)
                view.Type = "root";
            controller.View = view;
            presenter.View = view;
            ScreenIdEnum!.Values.Add(factory.GetValue(Common.Utils.ToUpperCase(notionName), ScreenIdEnum));
            // 5. If ('currentView' did not exist) -> Attach all 'DataTransferObject's in 'CurrentDTOP' to the 'View'
            view.ViewModel.OutputData.AddRange(CurrentDTOP);
            
            _result.Views.Add(view);
            _result.Presenters.Add(presenter);
            _result.Controllers.Add(controller);
        }
        CurrentView = view;
        // 6. Create ‘POperation’ based on ‘notion’; add it to ‘Presenter’
        RecentPOP = factory.GetPOperation("show " + notionName, view.Presenter);
        // 7. For each ‘DataTransferObject’ in ‘CurrentDTOP’ add a ‘DataItem’ (‘parameter’; type as ‘DataTransferObject’ name);
        // attach the ‘DataItems’ to the ‘POperation’
        foreach (DataTransferObject dto in CurrentDTOP) {
            Parameter par = factory.GetParameter(dto);
            if (CurrentUC!.State.Contains(dto)) par.HasAttribute = true;
            RecentPOP.Parameters.Add(par);
        }
        POperation? ppop = view.Presenter.Operations
            .Find(x => RecentPOP.Equals(x)); // TODO - implement POperation.Equals
        if (null != ppop) RecentPOP = ppop;
        else view.Presenter.Operations.Add(RecentPOP);
        // 8., 9. Create ‘Call’ etc.
        CreateCall(RecentPOP);
        // 10. Reset ‘CurrentDTOD’ and ‘CurrentDTOP’ (empty the lists); reset 'CurrentUCO'
        CurrentDTOD.Clear();
        CurrentDTOP.Clear();
        PreviousUCO = CurrentUCO ?? PreviousUCO;
        CurrentUCO = null; // TODO - asynchronous "execute" (reset only before the first Actor-to-... sentence)
        CurrentCondition = null;
        // 11. Append ‘label’-to-‘CurrentVF’ to ‘LabelToVF’
        LabelToView.Add(CurrentLabel!,view);

        SetLastPredicateTypes(PredicateType.Show);
        return _result;
    }

    //*****************************************************************************************************
    // 6. ACTOR-TO-DATA SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitEnterpredicate(
        [NotNull] DScriptParser.EnterpredicateContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug,CurrentLabel + ": Actor-to-Data predicate: " + context.GetText());
        ConditionPossible = false;
        if (null == CurrentView)
            throw new Exception("Unexpected <enter> sentence");
        if (null == RecentPOP)
            throw new Exception("Critical error");
        // 1. Create ‘DataTransferObject’ (if it does not exist) based on ‘notion’; add it to ‘CurrentDTOD’; 
        // attach it to ‘DataTransferObjectUnit’, ‘ViewModel’ and ‘CurrentUCC.state’
        string notionName = ObtainName(context.notion());
        DataTransferObject? dto = _result.DataTransferObjectUnit.Objects
            .Find(x => notionName.NamingEquals(x.Name));
        if (null == dto) {
            dto = factory.GetDataTransferObject(notionName);
            _result.DataTransferObjectUnit.Objects.Add(dto);
        }

        int line = context.notion().Start.Line;
        string? key = UndeclaredConceptUsageLines.Keys
            .FirstOrDefault(k => k.NamingEquals(notionName));
        if (null != key)
            UndeclaredConceptUsageLines[key].Add(line);
        else UndeclaredConceptUsageLines.Add(notionName, [line]);
        CurrentDTOD.Add(dto);
        if (!CurrentUC!.OwnState.Contains(dto))
            CurrentUC.OwnState.Add(dto);
        if (!CurrentView.ViewModel.InputData.Contains(dto))
            CurrentView.ViewModel.InputData.Add(dto);
        // Mark the corresponding ‘Parameter’ in ‘RecentPOP’ as ‘isInput’
        if (CurrentView.ViewModel.OutputData.Contains(dto)) {
            Parameter? inputPar = RecentPOP!.Parameters
                    .Find(p => p.Type == dto);
            if (null != inputPar) inputPar.IsInputOutput = true;
            else throw new Exception("Critical error");
        }
        // 2. Append 'label'-to-'CurrentView' to 'LabelToView'
        LabelToView.Add(CurrentLabel!,CurrentView);

        SetLastPredicateTypes(PredicateType.Enter);
        return _result;
    }

    //*****************************************************************************************************
    // 7. ACTOR-INVOKE SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitInvoke([NotNull] DScriptParser.InvokeContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": Actor-Invoke sentence: " + context.GetText());
        ConditionPossible = true;
        ResultUnionEnumeration? eu = null;
        if (null != context.names().names()) { // multiple invocations - create EnumUnion
            eu = factory.GetResultUnionEnumeration(CurrentUC!.Name + " at " + CurrentLabel);
            _result.DataTransferObjectUnit.Enums.Add(eu);
        }
        LastInvokeEnumeration = eu;
        ProcessUserInvoke(context.names(), eu);
        LabelToView.Add(CurrentLabel!,null);
        LastPredicateType = PredicateType.Invoke;
        return _result;
    }

    private void ProcessUserInvoke(DScriptParser.NamesContext? context, ResultUnionEnumeration? eu, UCOperation? returnTo = null){
        if (null == context) return;
        string ucName = ObtainName(context.name());
        // 1. Create ‘Enumeration’ (if it does not exist) based on ‘name’; add it to ‘DataTransferObjectUnit’
        SimpleResultEnumeration? en = (SimpleResultEnumeration?)_result.DataTransferObjectUnit.Enums
            .Find(x => x is SimpleResultEnumeration { Kind: ResultKind.Invoke } 
                       && ucName.NamingEquals(x.Name));
        if (null == en) {
            en = factory.GetSimpleResultEnumeration(ucName, ResultKind.Invoke);
            _result.DataTransferObjectUnit.Enums.Add(en);
        }
        LastInvokeEnumeration ??= en;
        // 2. Create ‘UCOperation’ based on ‘name’ and ‘CurrentView.name’ (‘invoked…’);
        // add it to ‘CurrentUCC’; set it as ‘CurrentUCO’
        UCOperation ucop;
        if (null == returnTo){
            // ucop = factory.GetUCOperation("invoked " + ucName + " @ " + CurrentVF.name, CurrentUCC);
            ucop = factory.GetUCOperation("invoked at " + CurrentLabel, CurrentUC!);
            CurrentUC!.Operations.Add(ucop);
            PreviousUCO = CurrentUCO ?? PreviousUCO;
            CurrentUCO = ucop;
            ucop.Previous ??= PreviousUCO;
            if (null != eu) AddUnionMembers(en, eu);
            // 2.1. Mark the operation as "returning"
            ucop.Returning = true;
            // 3. Create 'Parameter' with 'DataItem' (type as 'Enumeration'); add it to 'UCOperation'
            ucop.Parameters.Add(factory.GetParameter(null == eu ? en : eu,
                false, null == eu ? en.Name : eu.Name));
        } else {
            ucop = returnTo;
            AddUnionMembers(en, eu!);
        }
        // 4. If ‘name’ is in ‘UcNameToTrigger’  -> 
        if (UcNameToTrigger.ContainsKey(ucName)){
            // add ‘Trigger.action’ (copy and attach if necessary) to ‘Controller’ attached to ‘CurrentView’;
            Trigger trg = UcNameToTrigger[ucName];
            COperation cop = trg.Action!;
            UCOperation baseUcop = cop.Invoked!;
            UCOperation proxyUcop = factory.GetUCOperation(baseUcop.Name, CurrentUC!);
            proxyUcop.Invoking = true;
            foreach (DataTransferObject dto in cop.TransferredData)
                proxyUcop.Parameters.Add(factory.GetParameter(dto));
            CurrentUC!.Operations.Add(proxyUcop);
            Call call = factory.GetCall(baseUcop, proxyUcop);
            proxyUcop.Instructions.Add(call);
            COperation newcop = factory.GetCOperation(cop.Name, proxyUcop);
            newcop.TransferredData.AddRange(cop.TransferredData);
            newcop.Controller = CurrentView!.Controller;
            CurrentView!.Controller.Operations.Add(newcop);
            // add ‘Trigger.condition’ (if not empty, copy and attach if necessary) to ‘Controller’ attached to ‘CurrentView’;
            COperation? condcop = trg.Condition;
            COperation? newCondcop = null;
            if (null != condcop){
                UCOperation baseCUcop = condcop.Invoked!;
                UCOperation proxyCUcop = factory.GetUCOperation(baseCUcop.Name + " " + ucName, 
                    CurrentUC, PrimitiveType.Boolean);
                proxyCUcop.CheckProxy = true;
                foreach (DataTransferObject dto in cop.TransferredData)
                    proxyCUcop.Parameters.Add(factory.GetParameter(dto));
                CurrentUC.Operations.Add(proxyCUcop);
                Call condCall = factory.GetCall(baseCUcop, proxyCUcop);
                proxyCUcop.Instructions.Add(condCall);
                newCondcop = factory.GetCOperation(condcop.Name, proxyCUcop);
                newCondcop.ReturnType = condcop.ReturnType;
                newCondcop.TransferredData.AddRange(condcop.TransferredData);
                newCondcop.Controller = CurrentView.Controller;
                CurrentView.Controller.Operations.Add(newCondcop);
            }
            // add the ‘Trigger’ (copy if necessary) from the map entry to ‘CurrentView’;
            Trigger newtrg = factory.GetTrigger(trg.Name, newcop, newCondcop);
            CurrentView.Triggers.Add(newtrg);
            // attach ‘Trigger.action.invoked.uc’ to ‘ControllerFuntion’ attached to ‘CurrentVF’ (if necessary);
            if (!CurrentView.Controller.UseCase!.Invoked.Contains(cop.Invoked!.Uc!))
                CurrentView.Controller.UseCase.Invoked.Add(cop.Invoked!.Uc!);
            // attach ‘UCOperation’ to ‘Trigger.action’ as ‘returnTo’
            newcop.ReturnTo = ucop; 
        // 5. else -> add use case 'name'-to-'CurrentView' to 'UcNameToView';
        } else {
            if (!UcNameToView.ContainsKey(ucName)) UcNameToView.Add(ucName, []);
            if (!UcNameToView[ucName].Contains(CurrentView!)) {
                UcNameToView[ucName].Add(CurrentView!);
                // add use case 'name'&'CurrentView.name'-to-'UCOperation' to 'UcViewToUCOperation'
                UCViewToUCOperation.Add((ucName, CurrentView!.Name), ucop);
            }
            else throw new Exception("Duplicate use case invocation");
        }
        ProcessUserInvoke(context.names(), eu, ucop);
    }

    private void AddUnionMembers(SimpleResultEnumeration en, ResultUnionEnumeration eu)
    {
        eu.Members.Add(en);
        en.Unions.Add(eu);
        foreach (Value val in en.Values)
            if (!eu.Values.Exists(v => v.Name.NamingEquals(val.Name)))
                eu.Values.Add(factory.GetValue(val.Name,eu));
    }

    //*****************************************************************************************************
    // 8. ACTOR-TO-TRIGGER SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitSelectpredicate([NotNull] DScriptParser.SelectpredicateContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": Actor-to-Trigger predicate: " + context.GetText());
        ConditionPossible = false;
        // 1. Create a ‘Trigger’ based on ‘notion’; If ‘CurrentView’ exists -> add it to ‘CurrentView’
        Trigger trg = factory.GetTrigger(ObtainName(context.notion()));
        if (null != CurrentView) CurrentView.Triggers.Add(trg);
        // 2. Create a ‘COperation’ based on ‘notion’ ; attach it to the ‘Trigger’ as ‘action’
        COperation cop = factory.GetCOperation("select " + ObtainName(context.notion()));
        trg.Action = cop;
        // 3. Attach all 'DataTransferObject's in 'CurrentDTOD' to the 'COperation'
        cop.TransferredData.AddRange(CurrentDTOD);
        // 4. Create 'UCOperation' based on 'COperation'
        int counter = 0;
        string ucopName = cop.Name;
        while (CurrentUC!.Operations.Exists(x => ucopName == x.Name)){
            ucopName = cop.Name + (0 == counter ? "" : " "+counter);
            counter++;
        }
        UCOperation ucop = factory.GetUCOperation(ucopName, CurrentUC);
        // 5. For each ‘DataTransferObject’ in ‘CurrentDTOD’ create a 'Parameter' with ‘DataItem’
        //    ('DataTransferObject' as its base type);
        //    add the ‘Parameter’ to the ‘UCOperation’
        foreach (DataTransferObject dto in CurrentDTOD) {
            Parameter par = factory.GetParameter(dto);
            if (CurrentUC.State.Contains(dto)) par.HasAttribute = true;
            ucop.Parameters.Add(par);
        }
        // 6. Add ‘UCOperation’ to the ‘CurrentUCC’; attach it to ‘COperation’ (as ‘invoked’)
        CurrentUC.Operations.Add(ucop); cop.Invoked = ucop;
        // 7. Set ‘UCOperation’ as ‘CurrentUCO’
        PreviousUCO = CurrentUCO ?? PreviousUCO;
        CurrentUCO = ucop;
        ucop.Previous ??= PreviousUCO;
        // 8. If ‘CurrentUCO’ empty -> Algorithm for initial trigger sentence
        if (FirstSentence) {
            ProcessInitialSentence(trg);
            FirstSentence = false;
        // 9. else -> Add the ‘COperation’ to ‘Controller’ attached to ‘CurrentView’; attach ‘CurrentUCC’ to ‘Controller’
        } else
        {
            cop.Controller = CurrentView!.Controller;
            CurrentView!.Controller.Operations.Add(cop);
            // if (!CurrentView.controller.useCase.invoked.Contains(CurrentUCC))
            // CurrentView.controller.useCase.invoked.Add(CurrentUCC);
        }

        SetLastPredicateTypes(PredicateType.Select);
        return _result;
    }

    private void ProcessInitialSentence(Trigger trg){
        CurrentUCO!.Initial = true;
        // 1. Attach all 'DataTransferObject's in 'InheritedDTOD' to 'COperation' and 'UCOperation' 
        COperation cop = trg.Action!;
        cop.TransferredData.AddRange(InheritedDTOD);
        foreach (DataTransferObject dto in InheritedDTOD)
            CurrentUCO.Parameters.Add(factory.GetParameter(dto, true));
        // 2. If ‘ConditionCO’ not empty -> attach ‘ConditionCO’ to ‘Trigger’ as ‘condition’
        if (null != ConditionCO) trg.Condition = ConditionCO;
        // 3. If ‘CurrentUCC.name’ is in ‘UcNameToView’ -> for each matching ‘View’ in ‘UcNameToView’ ->
        if (UcNameToView.ContainsKey(CurrentUC!.Name)) foreach (View vf in UcNameToView[CurrentUC.Name]){
            // add ‘COperation’ (copy and attach if necessary) and ‘ConditionCO’ (if not empty, copy and attach if necessary)
            //    to ‘Controller’ attached to ‘View’;
            UseCase invokingUC = vf.Controller.UseCase!;
            UCOperation baseUcop = cop.Invoked!;
            UCOperation proxyUcop = factory.GetUCOperation(baseUcop.Name, invokingUC);
            proxyUcop.Invoking = true;
            foreach (DataTransferObject dto in cop.TransferredData) {
                Parameter par = factory.GetParameter(dto);
                if (invokingUC.State.Contains(dto)) par.HasAttribute = true;
                proxyUcop.Parameters.Add(par);
            }
            invokingUC.Operations.Add(proxyUcop);
            Call call = factory.GetCall(baseUcop, proxyUcop);
            proxyUcop.Instructions.Add(call);
            COperation newcop = factory.GetCOperation(cop.Name, proxyUcop);
            newcop.TransferredData.AddRange(cop.TransferredData);
            newcop.Controller = vf.Controller;
            vf.Controller.Operations.Add(newcop);
            COperation? condcop = null;
            if (null != ConditionCO){
                UCOperation baseCUcop = ConditionCO.Invoked!;
                UCOperation proxyCUcop = factory.GetUCOperation(baseCUcop.Name + " " + CurrentUC.Name,
                    invokingUC, PrimitiveType.Boolean);
                proxyCUcop.CheckProxy = true;
                foreach (DataTransferObject dto in cop.TransferredData) {
                    Parameter par = factory.GetParameter(dto);
                    if (invokingUC.State.Contains(dto)) par.HasAttribute = true;
                    proxyCUcop.Parameters.Add(par);
                }
                invokingUC.Operations.Add(proxyCUcop);
                Call condCall = factory.GetCall(baseCUcop, proxyCUcop);
                proxyCUcop.Instructions.Add(condCall);
                condcop = factory.GetCOperation(ConditionCO.Name, proxyCUcop);
                condcop.ReturnType = ConditionCO.ReturnType;
                condcop.TransferredData.AddRange(ConditionCO.TransferredData);
                condcop.Controller = vf.Controller;
                vf.Controller.Operations.Add(condcop);
            }
            // add ‘Trigger’ (copy if necessary);
            Trigger newTrg = factory.GetTrigger(trg.Name, newcop, condcop);
            vf.Triggers.Add(newTrg);
            // attach ‘CurrentUCC’ to ‘Controller’ attached to ‘View’ (if necessary);
            if (!invokingUC.Invoked.Contains(CurrentUC)) invokingUC.Invoked.Add(CurrentUC);
            // attach matching (‘CurrentUCC.name’ & ‘View.name’) ‘UCOperation’ from ‘UCViewToUCOperation’ to ‘COperation’ as ‘return’;
            UCViewToUCOperation.TryGetValue((CurrentUC.Name, vf.Name), out newcop.ReturnTo);
            // remove ‘UCNameToView’ and ‘UCViewToUCOperation’ entries
            UcNameToView.Remove(CurrentUC.Name);
            UCViewToUCOperation.Remove((CurrentUC.Name, vf.Name));
        }
        // 4. Add ‘CurrentUCC.name’ -to-‘Trigger’ to ‘UcNameToTrigger’
        UcNameToTrigger.Add(CurrentUC.Name,trg);
        // 5. Clear ‘ConditionCO’
        ConditionCO = null;
    }

    //*****************************************************************************************************
    // 9. SYSTEM-TO-DATA (CUD/CHECK) SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitUpdatepredicate([NotNull] DScriptParser.UpdatepredicateContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": System-to-Data (update) predicate: " + context.GetText());
        ConditionPossible = true;
        ProcessDataSentence("update", PredicateType.Update, context.notion());
        LabelToView.Add(CurrentLabel!,null);
        SetLastPredicateTypes(PredicateType.Update);
        return _result;
    }

    public override IntermediateRepresentation VisitDeletepredicate([NotNull] DScriptParser.DeletepredicateContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": System-to-Data (delete) predicate: " + context.GetText());
        ConditionPossible = true;
        ProcessDataSentence("delete", PredicateType.Delete, context.notion());
        LabelToView.Add(CurrentLabel!,null);
        SetLastPredicateTypes(PredicateType.Delete);
        return _result;
    }

    public override IntermediateRepresentation VisitCheckpredicate([NotNull] DScriptParser.CheckpredicateContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": System-to-Data (check) predicate: " + context.GetText());
        ConditionPossible = true;
        ProcessDataSentence("check", PredicateType.Check, context.notion());
        LabelToView.Add(CurrentLabel!,null);
        SetLastPredicateTypes(PredicateType.Check);
        return _result;
    }

    private void ProcessDataSentence(string verb, PredicateType type, DScriptParser.NotionContext notion){
        string notionName = ObtainName(notion);
        // 1. Create ‘Service’ (if it does not exist) based on ‘notion’; attach it to ‘CurrentUCC’
        Service? service = _result.Services
            .Find(x => notionName.NamingEquals(x.Name));
        SOperation? sop = service?.Operations
            .Find(x => (verb +" " + notionName).NamingEquals(x.Name)
                       && ("check" != verb || PredicateType.Check == x.Type));
        if (null == service) {
            service = factory.GetService(notionName);
            _result.Services.Add(service);
        }
        if (!CurrentUC!.Services.Contains(service)) CurrentUC.Services.Add(service);
        // 2. Create ‘SOperation’ (if it does not exist) based on ‘notion’; add ‘SOperation’ to ‘Service’
        if (null == sop) { // TODO - handle overloaded methods
            sop = factory.GetSOperation(verb + " " + notionName,
                PredicateType.Execute != type ? GetConceptUri(notionName) : notionName, type, service);
            // 3. Create 'Parameter' with ‘DataItem’ based on ‘notion’; add it to ‘SOperation’
            Parameter par;
            if (PredicateType.Execute != type) {
                DataTransferObject dto = GetKnownDTO(notionName);
                if (PredicateType.Delete != type)
                    par = factory.GetParameter(dto);
                else {
                    par = factory.GetIdentifierParameter(dto);
                    PropagateIdFromServiceToUc(dto, CurrentUCO!);
                }
                if (null != CurrentUC.State
                        .Find(s => notionName.NamingEquals(s.Name))) par.HasAttribute = true;
                sop.Parameters.Add(par);
                // 4. For each ‘DataTransferObject’ in 'CurrentDTOD' + ‘InheritedDTOD’
                //    create a 'Parameter' with ‘DataItem’ (‘DataTransferObject’ as its base type);
                //    add the ‘Parameters’ to the ‘SOperation’
                foreach (DataTransferObject cdto in CurrentDTOD)
                    if (!notionName.NamingEquals(cdto.Name) 
                            && !sop.Parameters.Exists(p => cdto.Name == p.Name)) {
                        par = factory.GetParameter(cdto);
                        if (CurrentUC.State.Contains(cdto)) par.HasAttribute = true;
                        sop.Parameters.Add(par);
                    }
            }

            foreach (DataTransferObject dto in InheritedDTOD)
                if (!notionName.NamingEquals(dto.Name) 
                        && !sop.Parameters.Exists(p => dto.Name == p.Name)){
                    par = factory.GetParameter(dto, true);
                    sop.Parameters.Add(par);
                }
            service.Operations.Add(sop);
        }
        // 5. If a sentence has a ‘checkpredicate’ (“check” sentence) -> create ‘Enumeration’ based on ‘notion’;
        //    add it to ‘DataTransferObjectUnit’;
        //    set ‘SOperation.returnType’ to “short” or ‘Enumeration.name’
        if ("check" == verb) {
            SimpleResultEnumeration? en = (SimpleResultEnumeration?)_result.DataTransferObjectUnit.Enums
                .Find(x => x is SimpleResultEnumeration { Kind: ResultKind.Check } 
                           && notionName.NamingEquals(x.Name));
            if (null == en) {
                en = factory.GetSimpleResultEnumeration(notionName, ResultKind.Check);
                _result.DataTransferObjectUnit.Enums.Add(en);
                DataTransferObject? dto = _result.DataTransferObjectUnit.Objects
                    .Find(x => notionName.NamingEquals(x.Name));
                if (null == dto) throw new Exception("Check for a non-existent notion");
                dto.Enumeration = en;
            }
            sop.ReturnType = en;
        }
        // 6. Create ‘Call’ etc.
        CreateCall(sop);
    }
    
    // Helper method to get a known (already used previously in the current UC context) DataTransferObject
    private DataTransferObject GetKnownDTO(string name){
        DataTransferObject? dto = CurrentDTOD.Find(kdto => name.NamingEquals(kdto.Name)) ??
                                  InheritedDTOD.Find(kdto => name.NamingEquals(kdto.Name)) ?? 
                                  CurrentDTOP.Find(kdto => name.NamingEquals(kdto.Name));
        return dto ?? throw new Exception("DataTransferObject not provided yet: " + name);
    }

    //*****************************************************************************************************
    // 10. SYSTEM-TO-DATA (EXECUTE) SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitExecutepredicate([NotNull] DScriptParser.ExecutepredicateContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": System-to-Data (execute) predicate: " + context.GetText());
        ConditionPossible = true;
        ProcessDataSentence("execute", PredicateType.Execute, context.notion());
        LabelToView.Add(CurrentLabel!,null);
        SetLastPredicateTypes(PredicateType.Execute);
        return _result;
    }

    //*****************************************************************************************************
    // 11. REPETITION SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitRepsentence([NotNull] DScriptParser.RepsentenceContext context)
    {
        try {
            CurrentLabel = null != context.label() ?
                context.label().NUMBER().GetText() :
                context.altlabel().CHAR().GetText() + context.altlabel().NUMBER().GetText();
            if (Verbose) logger.Log(LogLevel.Debug, CurrentLabel + ": repetition sentence");
            // 1. Set ‘ConditionPossible’ and reset 'CurrentUCO'
            ConditionPossible = CurrentUC!.Operations.SelectMany(x => x.Instructions)
                .ToList().Exists(x => x is Decision && CurrentLabel == x.Label);
            // TODO - determine PreviousUCO
            CurrentUCO = null;
            // 2. Clear ‘CurrentCondition’
            CurrentCondition = null;
            // 3. Search for ‘label’ in ‘LabelToView’; if ‘label’ found -> set ‘CurrentView’ to associated ‘View’
            if (LabelToView.ContainsKey(CurrentLabel)) CurrentView = LabelToView[CurrentLabel];
            else throw new Exception("Incorrect repetition sentence label");
            SetLastPredicateTypes(PredicateType.Repetition);
        } catch (Exception e) {
            WriteErrorMessage(e);
        }
        return _result;
    }

    //*****************************************************************************************************
    // 12. CONDITION SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitCondsentence([NotNull] DScriptParser.CondsentenceContext context)
    {
        try {
            if (Verbose) logger.Log(LogLevel.Debug, "Condition sentence: " + context.GetText());
            if (ConditionPossible == false) throw new Exception("Unexpected condition"); // ERROR HANDLING
            // 1. Create ‘Decision’ (if it does not exist; set ‘Decision.label’ as ‘CurrentLabel’);
            Decision? dec = null != CurrentLabel ? (Decision?) CurrentUC!.Operations
                .SelectMany(x => x.Instructions).ToList()
                .Find(x => x is Decision && CurrentLabel.NamingEquals(x.Label)) : null;
            if (null == dec) { // not start of alternative scenario
                dec = factory.GetDecision(CurrentUCO!, CurrentLabel!);
                // 2. If ‘CurrentCondition’ empty ->  append ‘Decision’ to ‘CurrentUCO’ else append ‘Decision’ to ‘CurrentCondition’
                if (null == CurrentCondition) CurrentUCO!.Instructions.Add(dec);
                else CurrentCondition.Instructions.Add(dec);
                ConditionPossible = false;
            }
            // 3. Create ‘Condition’; add it to ‘Decision’
            Condition cond = factory.GetCondition();
            dec.Conditions.Add(cond);
            // 4. Set ‘Condition’ as ‘CurrentCondition’
            CurrentCondition = cond;
            ProcessConditions(context.conditions());
        } catch (Exception e) {
            WriteErrorMessage(e);
        }
        return _result;
    }

    private void ProcessConditions(DScriptParser.ConditionsContext? context){
        // 5. For each ‘condition’ -> create ‘Expression’; add it to ‘Condition’;
        //    attach it to ‘DataTransferObject’ based on ‘notion’; if ‘condition’ is ‘valuecondition’ -> create ‘Value’ based on ‘value’;
        //    add it to ‘Enumeration’ based on ‘notion’; attach it to ‘Expression’
        if (null == context) return;
        Expression expr = factory.GetExpression();
        CurrentCondition!.Expressions.Add(expr);

        DScriptParser.ContextconditionContext ccondition = context.condition().contextcondition();
        DScriptParser.ValueconditionContext vcondition = context.condition().valuecondition();

        string notionName = null != ccondition ? ObtainName(ccondition.notion()) : ObtainName(vcondition.notion());
        
        // TODO - add "ended" to the DScript grammar
        if ("ended" != notionName || null != ccondition) {       
            DataTransferObject? dto = _result.DataTransferObjectUnit.Objects
                .Find(x => notionName.NamingEquals(x.Name));
            if (null != dto) expr.ToBeChecked = dto;
            // ERROR HANDLING
            else throw new Exception("Notion not found");
        } else expr.ToBeChecked = null;

        if (null != vcondition) {
            ResultEnumeration? en;
            if ("ended" == notionName) {
                en = LastInvokeEnumeration ?? 
                     throw new Exception("Unexpected 'ended' condition - cannot be used without an invoke");
            } else {
                en = _result.DataTransferObjectUnit.Enums
                    .Find(x => x is SimpleResultEnumeration { Kind: ResultKind.Check }
                               && notionName.NamingEquals(x.Name));
                if (null == en)
                    throw new Exception("Notion not checked");
            }
            string valueName = ObtainName(vcondition.value());
            Value? val = en.Values.Find(x => valueName.NamingEquals(x.Name));
            if (null == val){
                val = factory.GetValue(valueName, en);
                en.Values.Add(val);
                if (en is SimpleResultEnumeration sre && 0 != sre.Unions.Count) 
                    // if the enumeration is part of any union, add the value to the union as well
                    foreach (ResultUnionEnumeration rue in sre.Unions
                                 .Where(rue => !rue.Values.Exists(v => valueName.NamingEquals(v.Name))))
                        rue.Values.Add(factory.GetValue(val.Name, rue));
            }
            expr.Value = val;
        }
        ProcessConditions(context.conditions());
    }

    //*****************************************************************************************************
    // 13. END SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitResultsentence([NotNull] DScriptParser.ResultsentenceContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, "End sentence ");
        if (null == CurrentUCO && null == CurrentCondition) throw new Exception("Unexpected end of scenario");
        // 1. Create ‘End’; append it to ‘CurrentUCO.instructions’ or ‘CurrentCondition.instructions’
        End end = factory.GetEnd(CurrentUCO!);
        if (null == CurrentCondition) CurrentUCO!.Instructions.Add(end);
        else CurrentCondition.Instructions.Add(end);
        // 2. Create ‘Enumeration’ (if it does not exist) based on ‘CUCC.name’; add it to ‘DataTransferObjectUnit’
        SimpleResultEnumeration? en = (SimpleResultEnumeration?)_result.DataTransferObjectUnit.Enums
            .Find(x => x is SimpleResultEnumeration { Kind: ResultKind.Invoke } 
                       && CurrentUC!.Name.NamingEquals(x.Name));
        if (null == en) throw new Exception("Start".NamingEquals(CurrentUC!.Name) ? 
            "Start use case cannot have end sentences" : 
            "Critical error: no result enumeration found for use case " + CurrentUC.Name);
        string valueName = ObtainName(context.value());
        Value? value = en.Values.Find(x => valueName.NamingEquals(x.Name));
        // 3. Create ‘Value’ based on ‘value’; add it to ‘Enumeration’
        if (null == value) {
            value = factory.GetValue(valueName, en);
            en.Values.Add(value);
            foreach (ResultUnionEnumeration rue in en.Unions
                         .Where(rue => !rue.Values.Exists(v => valueName.NamingEquals(v.Name))))
                rue.Values.Add(factory.GetValue(value.Name, rue));
        }
        end.Value = value;
        return _result;
    }

    //*****************************************************************************************************
    // 14. REJOIN SENTENCE
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitRejoinsentence([NotNull] DScriptParser.RejoinsentenceContext context)
    {
        // TODO - consider other than current (incompatible) Auxiliary variables state
        // (rejoin to <read> vs. rejoin to <show> vs. rejoin to <execute>)
        if (Verbose) logger.Log(LogLevel.Debug, "Rejoin sentence ");
        // Error In 1: ‘LastPredicateType’ empty or ‘System-to-Screen’ or ‘Actor-to-Data’(TODO) or ‘Repetition sentence’
        if (new List<PredicateType>{PredicateType.Show,PredicateType.Enter,PredicateType.Repetition}.Contains(LastPredicateType))
            throw new Exception("Unexpected rejoin sentence");
        // 1. Find ‘Call’ with ‘Call.label’ == ‘label’; if not found -> finish
        string rejoinLabel = context.labelref().GetText();
        List<Instruction>? rejoinList = null;
        foreach (UCOperation ucop in CurrentUC!.Operations){
            rejoinList = GetRejoinedInstructionList(ucop.Instructions,rejoinLabel);
            if (null != rejoinList) break;
        }
        if (null == rejoinList){
            if (LabelToView.ContainsKey(rejoinLabel)) return _result;
            else throw new Exception("Incorrect rejoin label");
        }
        Call call = (Call) rejoinList.Find(x => x is Call && rejoinLabel.NamingEquals(x.Label))!;
        // 2. For each further ‘Instruction’ in ‘Call.parent.instructions’ following and including the current ‘Call’ ->
        //    { If ‘CurrentCondition’ empty ->  append further ‘Instruction’ to ‘CurrentUCO’ 
        //      else append further ‘Instruction’ to ‘CurrentCondition’ }
        List<Instruction> instructions = null == CurrentCondition ? CurrentUCO!.Instructions : CurrentCondition.Instructions;
        for (int i = rejoinList.IndexOf(call); i < rejoinList.Count(); i++)
            instructions.Add(rejoinList[i]);
        return _result;
    }
    
    private List<Instruction>? GetRejoinedInstructionList(List<Instruction> instructions, string rejoinLabel){
        foreach (Instruction instr in instructions){
            if (instr is Call && rejoinLabel == instr.Label) return instructions;
            else if (instr is Decision dec) 
                foreach (Condition cond in dec.Conditions){
                    List<Instruction>? decList = GetRejoinedInstructionList(cond.Instructions, rejoinLabel);
                    if (null != decList) return decList;
                }
        }
        return null;
    }

    //*****************************************************************************************************
    // 15. DATA NOTION
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitDatanotion([NotNull] DScriptParser.DatanotionContext context)
    {
        if (Verbose) logger.Log(LogLevel.Debug, "Data notion: " + context.name().GetText());
        bool auxiliary = null != context.auxiliary();
        string notionName = ObtainName(context.name());
        DataTransferObject? dto = _result.DataTransferObjectUnit.Objects
            .Find(dto => notionName.NamingEquals(dto.Name));
        if (null == dto) {
            dto = factory.GetDataTransferObject(notionName, auxiliary);
            _result.DataTransferObjectUnit.Objects.Add(dto);
        } else {
            dto.Auxiliary = auxiliary;
            string? key = UndeclaredConceptUsageLines.Keys.FirstOrDefault(k => k.NamingEquals(notionName));
            if (null != key) UndeclaredConceptUsageLines.Remove(key);
            else throw new Exception($"Duplicate data notion declaration: {notionName}");
        }

        AddDataItems(dto,context.attributes());
        // Add standard identifier field for persistent notions if not already present
        if (!auxiliary && !dto.Fields.Exists(f => f.Name.NamingEquals("identifier")))
            dto.Fields.Add(factory.GetPrimitiveField("identifier", PrimitiveType.String, false, true, true));
        return _result;
    }

    private void AddDataItems(DataTransferObject dto, DScriptParser.AttributesContext? context){
        if (null == context) return;
        string itemName = ObtainName(context.attribute().name());
        string typeName;
        bool isOptional = null == context.attribute().required();
        bool isMultiple = false;
        bool concept = false;
        // Determine the type name and category of the attribute
        if (null != context.attribute().datatype()) {
            typeName = ObtainName(context.attribute().datatype());
        } else if (null != context.attribute().notion()) {
            typeName = ObtainName(context.attribute().notion()); concept = true;
        } else if (null != context.attribute().multnotion()) {
            isMultiple = true;
            if (null != context.attribute().multnotion().notion()) {
                typeName = ObtainName(context.attribute().multnotion().notion()); concept=true;   
            } else if (null != context.attribute().multnotion().datatype()) {
                typeName = ObtainName(context.attribute().multnotion().datatype());
            } else throw new Exception("Critical error");
        } else throw new Exception("Critical error");

        // Special handling for 'identifier' data item - has to be of type "string" and cannot be a concept
        if (itemName.NamingEquals("identifier")) {
            if (concept) throw new Exception("Identifier cannot refer to a concept");
            if ("string" != typeName)
                throw new Exception("Identifier has to be of type string");
        }
        Field di;
        if (concept) {
            DataTransferObject? baseType = _result.DataTransferObjectUnit.Objects
                .Find(t => typeName.NamingEquals(t.Name));
            string? key = UndeclaredConceptUsageLines.Keys
                .FirstOrDefault(k => k.NamingEquals(typeName));
            if (null == baseType || null != key) { // this concept not declared yet
                int line = context.attribute().name().Start.Line;
                if (null != key) // already in the undeclared dictionary?
                    UndeclaredConceptUsageLines[key].Add(line); // add new line number
                else {
                    UndeclaredConceptUsageLines.Add(typeName, [line]); // create new list with line number
                    baseType = factory.GetDataTransferObject(typeName); // create a placeholder concept
                    _result.DataTransferObjectUnit.Objects.Add(baseType);
                }
            }
            di = factory.GetConceptField(itemName, baseType!, isMultiple, isOptional);
        } else
            di = factory.GetPrimitiveField(itemName, PrimitiveTypeExtensions.FromSchemaType(typeName),
                isMultiple, isOptional);
        dto.Fields.Add(di);
        AddDataItems(dto,context.attributes());
    }

    //*****************************************************************************************************
    // 15. TRIGGER NOTION
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitTriggernotion([NotNull] DScriptParser.TriggernotionContext context)
    {
        string trgType = ObtainName(context.triggertype());
        SetTriggerDetails(trgType, context.namesandlabels());
        return _result;
    }

    private void SetTriggerDetails(string? trgType, DScriptParser.NamesandlabelsContext? context){
        if (null == context) return;
        string trgName = ObtainName(context.name());
        Trigger? trg = _result.Views.SelectMany(vf => vf.Triggers).ToList()
            .Find(t => trgName.NamingEquals(t.Name));
        if (null == trg) throw new Exception("Unexpected trigger definition");
        if (null != trgType) trg.Type = trgType;
        if (null != context.uilabel()) trg.Label = ObtainName(context.uilabel());
        SetTriggerDetails(trgType, context.namesandlabels());
    }

    //*****************************************************************************************************
    // 15. VIEW NOTION
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitViewnotion([NotNull] DScriptParser.ViewnotionContext context)
    {
        string viewType = ObtainName(context.viewtype());
        SetViewnotionDetails(viewType, context.namesandlabels());
        return _result;   
    }

    private void SetViewnotionDetails(string? viewType, DScriptParser.NamesandlabelsContext? context){
        if (null == context) return;
        string viewName = ObtainName(context.name());
        View? view = _result.Views.Find(vf => viewName.NamingEquals(vf.Name));
        if (null == view) throw new Exception("Unexpected view notion definition");
        if (null != viewType) view.Type = viewType;
        if (null != context.uilabel()) view.Label = ObtainName(context.uilabel());
        SetViewnotionDetails(viewType, context.namesandlabels());
    }
    
    //*****************************************************************************************************
    // POSTPROCESSING
    //*****************************************************************************************************

    private void PostProcess(){
        foreach (UseCase uc in _result.UseCases) {
            
            // Add to view models elements needed to pass into views options for populating dropdown lists
            foreach (ViewModel vm in uc.Presenters.Select(p => p.View!.ViewModel))
                foreach(Field f in vm.InputData.Where(d => !d.IsAuxiliaryCollection)
                            .SelectMany(d => d.Fields)
                            .Where(f => f.Type is DataTransferObject)) {
                    DataTransferObject? odto = _result.DataTransferObjectUnit.Objects
                        .FirstOrDefault(dto => dto.CanBeMap && ((DataTransferObject)f.Type).Name.NamingEquals(dto.Name));
                    
                    if (null == odto) {
                        odto = factory.GetOptionsDataTransferObject(((DataTransferObject)f.Type).Name,
                            (DataTransferObject)f.Type);
                        _result.DataTransferObjectUnit.Objects.Add(odto);
                    }
                    if (!vm.ReferenceData.Contains(odto)) {
                        vm.ReferenceData.Add(odto);
                        CreateReadServiceOperation(odto.Name, odto, uc, null, [], []);
                    }
                }
            
            // Add to use case state elements needed to pass into views options for populating dropdown lists
            foreach (Field field in uc.OwnState.Where(d => !d.IsAuxiliaryCollection)
                         .SelectMany(d => d.Fields).ToList())
                if (field.Type is DataTransferObject dto) {
                    DataTransferObject? odto = _result.DataTransferObjectUnit.Objects
                        .FirstOrDefault(d => d.CanBeMap && dto.Name.NamingEquals(d.Name));
                    if (null == odto) {
                        odto = factory.GetOptionsDataTransferObject(dto.Name, dto);
                        _result.DataTransferObjectUnit.Objects.Add(odto);
                    }

                    if (!uc.State.Contains(odto)) uc.ReferenceState.Add(odto);
                }
            foreach (Presenter pres in uc.Presenters)  
                foreach (POperation pop in pres.Operations)
                    foreach (DataTransferObject rd in pres.View!.ViewModel.ReferenceData) {
                        Parameter spar = factory.GetParameter(rd, true);
                        spar.CanBeSimplified = true;
                        if (!pop.Parameters.Contains(spar)) pop.Parameters.Add(spar);
                    }
            //Propagate 'canBeIdentifier' designation to parameters of invoking operations
            foreach (UCOperation iucop in uc.Operations.Where(o => o.Invoking || o.CheckProxy)) {
                foreach (Parameter ipar in (iucop.Instructions[0] as Call
                                       ?? throw new Exception("Critical error")
                                       ).CalledOperation.Parameters.Where(p => p.CanBeIdentifier)){
                    Parameter? cpar;
                    if (null!=(cpar=iucop.Parameters.FirstOrDefault(p => !p.Multiple && p.Type == ipar.Type)))
                        cpar.CanBeIdentifier = true;
                }
            }
        }
    }
    
    //*****************************************************************************************************
    // ERROR HANDLING
    //*****************************************************************************************************

    public override IntermediateRepresentation VisitSentence([NotNull] DScriptParser.SentenceContext context)
    {
        try {
            return base.VisitSentence(context);
        } catch (Exception e) {
            WriteErrorMessage(e);
            return _result;
        }
    }

    public override IntermediateRepresentation VisitAltsentence([NotNull] DScriptParser.AltsentenceContext context)
    {
        try {
            return base.VisitAltsentence(context);
        } catch (Exception e) {
            WriteErrorMessage(e);
            return _result;
        }
    }

    public override IntermediateRepresentation VisitEndsentence([NotNull] DScriptParser.EndsentenceContext context)
    {
        try {
            return base.VisitEndsentence(context);
        } catch (Exception e) {
            WriteErrorMessage(e, "after " + CurrentLabel);
            return _result;
        }
    }

    public override IntermediateRepresentation VisitSystemstep([NotNull] DScriptParser.SystemstepContext context)
    {
        if (null == CurrentUCO && null == CurrentCondition)
            throw new Exception(null != CurrentView ? "\'Actor-to\' sentence expected" : "Unexpected \'System-to\' sentence");
        return base.VisitSystemstep(context);
    }

    public override IntermediateRepresentation VisitUserstep([NotNull] DScriptParser.UserstepContext context)
    {
        if (!FirstSentence && null == CurrentView)
            throw new Exception(null == CurrentUCO || null == CurrentCondition ? "Unexpected \'Actor-to\' sentence" : "\'System-to\' sentence expected");
        return base.VisitUserstep(context);
    }

    private void WriteErrorMessage(Exception e, string? altlabel = null){
        string label = altlabel ?? CurrentLabel ?? "precondition";
        logger.Log(LogLevel.Debug, ">>>>> Error: " + e.Message + " in use case \"" + CurrentUC!.Name + "\", sentence - " + label);
        logger.Log(LogLevel.Debug, e.ToString());
    }
}