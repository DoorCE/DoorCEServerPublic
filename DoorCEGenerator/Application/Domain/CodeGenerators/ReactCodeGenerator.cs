using DoorCEGenerator.Application.IntermediateModel;
using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;
using DoorCEGenerator.Application.IntermediateModel.Instructions;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;
using DoorCEModel.Infrastructure.DataModel.CodeContents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;

namespace DoorCEGenerator.Application.Domain.CodeGenerators;

public class ReactCodeGenerator : ICodeGenerator
{
    public string GetCode(ResultUnionEnumeration element, int tabs = 0)
    {
        return GetBaseCode(element, tabs);
    }
        
    public string GetCode(SimpleResultEnumeration element, int tabs = 0)
    {
        return GetBaseCode(element, tabs);
    }

    public string GetCode(Controller element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = GetImports(element);
        // export function CClientListForm(
        code += ts + "export function " + element.GetElemName() + "(\n";
        //   state: ClientListFormData,
        code += ts + "\tstate: " + Common.Utils.ToPascalCase(element.Name) + "State,\n";
        //   showClientList: UCShowClientList
        code += ts + "\t" + element.UseCase!.GetVarName() + ": " + element.UseCase.GetElemName();
        // ) {
        code += "\n) {\n" + ts;
        //   >>>sub-functions<<<
        code += string.Join("\n" + ts, element.Operations.Select(x => x.ToCode(tabs + 1)));
        //   return [selectClose, selectFindClient, invokeCheckFindClient];
        code += "\n\t" + ts + "return [" + string.Join(", ", 
            element.Operations.Select(x => x.GetElemName())) + "];\n" + ts + "}";
        return code;
    }

    public string GetCode(COperation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        bool isCondition = null != element.ReturnType;
        // function invokeCheckFindClient(): boolean {
        string code = ts + "function " + element.GetElemName() + "()";
        if (isCondition) code += ": " + element.ReturnType;
        code += " {\n";
        // let role: Role = Object.create(state.role);
        foreach (DataTransferObject dto in element.TransferredData){
            code += "\t" + ts + "let " + dto.GetVarName() + ": " + dto.GetElemName() + 
                    " = Object.create(state." + dto.GetVarName() + ");\n";
        }
        // return findClient.PreconditionCheck(role);
        code += ts + "\t" + (isCondition ? "return " : "") + element.Invoked!.Uc!.GetVarName() + "." +
                element.Invoked.GetElemName();
        code += "(" + string.Join(", ", element.TransferredData.Select(da => da.GetVarName()));
        if (null != element.ReturnTo)
            code += (0 == element.TransferredData.Count ? "" : ", ") + element.ReturnTo.Uc!.GetVarName() + "." +
                    element.ReturnTo.GetElemName();
        code += ");\n";
        code += ts + "}\n";
        return code;
    }

    public string GetCode(DataTransferObjectUnit element, int tabs = 0)
    {
        // TODO ? - string ts = global::Utils.GetTabString(tabs);
        // Generate code for Enumerations
        string code = string.Join("", element.Enums.Select(en => en.ToCode(tabs) + "\n"));
        // Generate code for DTOs (except for auxiliary collections)
        code += string.Join("", element.Objects.Where(dto => dto is { IsAuxiliaryCollection: false, IsSimple: false })
            .Select(dto => dto.ToCode(tabs) + "\n"));
        return code;
    }

    public string GetCode(DataTransferObject element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "export class " + element.GetElemName() + " {\n";
        code += string.Join("", element.Fields.Select(di => GetCode(di, tabs+1) + "\n"));
        code += ts + "}\n";
        return code;
    }

    public string GetCode(POperation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + element.GetElemName() + GetParametersCode(element) + "{\n";
        foreach (Parameter par in element.Parameters) {
            DataTransferObject? dtob = null;
            if (par.IsInputOutput)
                dtob = par.Type as DataTransferObject;
            
            bool hasMultipleItems = null != dtob && dtob.Fields
                .Exists(di => di.Type is not Primitive && di.Multiple);
            code += ts + "\tthis.state." + (hasMultipleItems ? "base" + GetTypeName(par) : ToVarCode(par)) +
                    " = " + ToVarCode(par) + ";\n";
            if (hasMultipleItems) {
                code +=  ts + "\tthis.state." + ToVarCode(par) + " = new " + GetTypeName(par) + "();\n";
                foreach (Field di in dtob!.Fields)
                    if (di.Type is not Primitive && di.Multiple)
                        code += ts + "\tthis.state." + di.Type.GetVarName() + " = undefined;\n";
                    else
                        code += ts + "\tthis.state." + ToVarCode(par) + "." + di.GetVarName() + " = " +
                                ToVarCode(par) + "." + di.GetVarName() + ";\n";
            }
        }
        code += ts + "\tthis.gUpdateView?.(ScreenId." + element.Pres!.GetElemName().ToUpper().Substring(1) +
                ");\n" + ts + "}";
        return code;
    }

    public string GetCode(Presenter element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = GetImports(element);
        // export function updateClwView(state: ClientListWndData, action: string) {
        code += ts + "export function update" + Common.Utils.ToPascalCase(element.Name) + "(";
        code += "state: " + Common.Utils.ToPascalCase(element.Name) + "State, action: string) {\n";
        //   let newState = { ...state };
        code += ts + "\tlet newState = { ...state };\n";
        //   return newState;
        code += ts + "\treturn newState;\n" + ts + "}\n\n";
        // }

        // CODE: export class PClientListWnd extends PresentationDispatcher {
        code += ts + "export class " + element.GetElemName() + " extends PresentationDispatcher {\n";
        //   CODE: state!: ClientListWndData;
        code += ts + "\tstate!: " + Common.Utils.ToPascalCase(element.Name) + "State;\n";
        //   CODE: updateView!: Dispatch<string>;
        code += ts + "\tupdateView!: Dispatch<string>;\n\n";

        code += ts + "\tinjectStateHandle(state: " + Common.Utils.ToPascalCase(element.Name) +
                "State, updateView: Dispatch<string>) {\n";
        code += ts + "\t\tthis.state = state;\n";
        code += ts + "\t\tthis.updateView = updateView;\n";
        code += ts + "\t}\n\n";

        code += string.Join("\n", element.Operations.Select(m => m.ToCode(tabs + 1)+"\n"));

        code += ts + "}";

        return code;
    }

    public string GetCode(SOperation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string ret = element.ReturnType!.GetElemName();
        if (element.ReturnType is CustomType and not DataTransferObject) ret += "[0];";
        else if (element.ReturnType is Primitive prim) {
            switch (prim.Value) {
                case PrimitiveType.Integer:
                    ret = "BigInt(0);";
                    break;
                case PrimitiveType.Number:
                case PrimitiveType.Float:
                    ret = "0;";
                    break;
                case PrimitiveType.String:
                    ret = "\"\";";
                    break;
                case PrimitiveType.Boolean:
                    ret = "false;";
                    break;
                case PrimitiveType.Time:
                case PrimitiveType.Date:
                    ret = "new Date();";
                    break;
                default:
                    throw new NotImplementedException($"The primitive type {prim.Value} is not implemented.");
            }
        }
        else ret = "new " + ret + "();";
        string code = element.ToHeaderCode(tabs) + " {\n" + ts + "\treturn " + ret + "\n" + ts + "}\n";
        return code;
    }

    public string GetCode(Service element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = GetImports(element);
        code += ts + "export interface " + element.GetInterfaceName() + " {\n";
        code += string.Join("", element.Operations.Select(x => x.ToHeaderCode(tabs + 1) + ";\n"));
        code += ts + "}\n\n";

        code += ts + "export class " + element.GetElemName() + " implements " + element.GetInterfaceName() + "{\n\n";
        code += string.Join("", element.Operations.Select(x => x.ToCode(tabs + 1)));
        code += ts + "}\n\n";
        return code;
    }

    public string GetCode(UCOperation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        // CODE: showClientListSelected(...) {
        string code = ts + element.GetElemName() + GetParametersCode(element);
        
        code += (null != element.ReturnType ? ": " + element.ReturnType : "") + " {\n";
        if (element.Initial){
            //   CODE: if (null != returnTo) this.returnTo = returnTo;
            code += ts + "\tif (undefined != returnTo) { this.returnTo = returnTo; this.returnUc = returnUc; }\n";
        }
        //   CODE: this.clientType = clientType;
        code += string.Join("", element.Parameters.Select( p => 
            p.Type is ResultUnionEnumeration ? "" :
                ts + "\t" + ToVarCode(p) + " = " + GetCode(p,0,true) + ";\n" ));

        //           >>>instructions<<<
        if (null == element.ReturnType)
            foreach (Instruction instr in element.Instructions)
                code += GetCode((dynamic) instr,tabs + 1) + "\n";
        else if (element.ReturnType is Primitive { Value: PrimitiveType.Boolean }) {
            try {
                code += ts + "\treturn " + string.Join(" && ", 
                    element.Instructions.Select(i => ToVarCode((Call) i))) + ";\n";
            } catch(InvalidCastException) {
                throw new Exception("Critical compilation error");
            }
        } else throw new Exception("Critical compilation error");
        // CODE: }
        code += ts + "}\n";
        return code;
    }

    public string GetCode(UseCase element, int tabs = 0)
    { 
        string ts = Common.Utils.GetTabString(tabs);
        string code = GetImports(element);
        // CODE: export class UCShowClientList {
        code += ts + "export class " + element.GetElemName() + "{\n";
        //   CODE: pClientListWindow: PClientListWnd;
        code += 0 == element.Presenters.Count ? "" : 
            string.Join("", element.Presenters.Select(p => "\t" + ts + p.GetVarName() + ": " +
                                                           p.GetElemName() + ";\n")) + "\n";
        //   CODE: iCl: IClients;
        code += 0 == element.Services.Count ? "" :
            string.Join("", element.Services.Select(s => "\t" + ts + s.GetVarName() + ": " +
                                                         s.GetElemName() + ";\n")) + "\n";
        code += 0 == element.Invoked.Count ? "" :
            string.Join("", element.Invoked.Select(s => "\t" + ts + s.GetVarName() + ": " +
                                                        s.GetElemName() + " | undefined;\n")) + "\n";
        //   CODE: returnTo: Function = new Function();
        code += ts + "\treturnTo?: Function;\n";
        code += ts + "\treturnUc?: any;\n\n";
        //   CODE: clientType: ClientType;
        code += 0 == element.State.Count ? "" :
            string.Join("", element.State.Select(a => "\t" + ts + a.GetVarName() + ": " + a.GetElemName() + 
                                                      " = new " + a.GetElemName() + "();\n")) + "\n";
        //   CODE: constructor(clw: PClientListWnd, mm: PMainMenu, cl: IClients) {
        code += "\t" + ts + "constructor(";
        code += string.Join(", " + ts, element.Presenters.Select(p => p.GetVarName() + ": " + p.GetElemName()));
        if (element.Services.Count > 0)
            code += ", " + string.Join(", " + ts, element.Services.Select(s => s.GetVarName() + ": " + s.GetElemName()));
        code += ") {\n";
        //   CODE: this.pCLW = clw;
        code += string.Join("", element.Presenters.Select(p => ts + "\t\tthis." + p.GetVarName() + " = " +
                                                               p.GetVarName() + ";\n"));
        code += string.Join("", element.Services.Select(s => ts + "\t\tthis." + s.GetVarName() + " = " +
                                                             s.GetVarName() + ";\n"));
        //   CODE: }
        code += "\t" + ts + "}\n\n";
        code += string.Join("\n", element.Operations.Select(m => m.ToCode(tabs + 1)));
        // CODE: }
        code += ts + "}";
        return code;
    }

    public string GetCode(View element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = GetImports(element);
        // CODE: export default function VClientListWnd(
        code += ts + "export default function " + element.GetElemName() + "(\n";
        //   CODE: isActive: boolean,
        code += ts + "\tisActive: boolean,\n";
        //   CODE:   pCLW: PClientListWnd,
        code += ts + "\t" + element.Presenter.GetVarName() + ": " + element.Presenter.GetElemName() + ",\n";
        //   ucSCL: UCShowClientList
        code += ts + "\t" + element.Controller.UseCase!.GetVarName() + ": " +
                element.Controller.UseCase.GetElemName() + "\n";
        // CODE: ) {
        code += ts + ") {\n";
        //   CODE: const emptyState: ClientListWndState = new ClientListWndState();
        string windowName = element.GetElemName().Substring(1);
        code += ts + "\tconst emptyState: " + windowName + "State = new " + windowName + "State();\n";
        //   CODE: const [viewState, viewUpdate] = useReducer(updateClientListWnd, emptyState);
        code += ts + "\tconst [viewState, updateView] = useReducer(update" + windowName + ", emptyState);\n\n";
        //   CODE: pCLW.injectDataHandles(clwData, clwUpdateView);
        code += ts + "\t" + element.Presenter.GetVarName() + ".injectStateHandle(viewState, updateView);\n\n"; 
        //   CODE: if (!isActive) return;
        code += ts + "\tif (!isActive) return;\n\n";
        //   CODE: const [selectAdd, selectBack] = CClientListWnd(viewState, ucSCL);
        code += ts + "\tconst [" + string.Join(", ", element.Controller.Operations
                    .Select(f => f.GetElemName())) + "] = " +
                element.Controller.GetElemName() + 
                "(viewState, " + element.Controller.UseCase.GetVarName() + ");\n";
        //   CODE: return (
        code += ts + "\treturn (\n";
        //     CODE: <div className="ClientListWnd">
        code += ts + "\t\t<div className=\"" + windowName + "\">\n";
        //       CODE: <h2>Client list</h2>
        if (!string.IsNullOrEmpty(element.Label)) code += ts + "\t\t\t<h2>" + element.Label + "</h2>\n";
            
        code += element.ViewModel.OutputData.Count == 0 ? "" : string.Join("", element.ViewModel.OutputData
            .Select(da => ToHtml(da,element.ViewModel.InputData.Contains(da), tabs + 3)
                          + "\n"));
        code += element.ViewModel.InputData.Count == 0 ? "" : string.Join("", element.ViewModel.InputData
            .Where(da => !element.ViewModel.OutputData.Contains(da))
            .Select(da => ToHtml(da,true, tabs + 3) + "\n"));
        
        code += element.Triggers.Count == 0 ? "" : string.Join("", element.Triggers.Select(tr => GetCode(tr,tabs + 3) + "\n"));

        //     CODE: </div>
        code += ts + "\t\t</div>\n";
        code += ts + "\t);\n";
        code += ts + "}";
        return code;
    }

    public string GetCode(ViewModelUnit element, int tabs = 0)
    {
        string code = GetImports(element);
        return code + string.Join("", element.Models.Select(vm => vm.ToCode(tabs) + "\n"));
    }
        
    public string GetCode(ViewModel element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "export class " + element.GetElemName() + " {\n";
        code += string.Join("", element.OutputData.Where(dto => !element.InputData.Contains(dto))
            .Select(dto => ToVarCode(dto, tabs+1) + "\n"));
        code += string.Join("", element.OutputData.Where(dto => element.InputData.Contains(dto))
            .Select(dto => ToVarCode(dto, tabs+1, true) + "\n"));
        code += string.Join("", element.InputData.Where(dto => !element.OutputData.Contains(dto))
            .Select(dto => ToVarCode(dto, tabs+1) + "\n"));
        code += ts + "}\n";
        return code;
    }
    
    public string GetCode(IntermediateRepresentation element)
    {
        string code = GetImports(element);
        code += string.Join("", element.Presenters.Select(pc => "const " + pc.GetVarName() + ": " +
                                                                pc.GetElemName() + " = new " + pc.GetElemName() +
                                                                "();\n")) + "\n";
        code += string.Join("", element.Services.Select(si => "const " + si.GetVarName() + ": " +
                                                              si.GetInterfaceName() + " = new " + si.GetElemName() +
                                                              "();\n")) + "\n";

        UseCase? startClass;
        foreach (UseCase ucc in element.UseCases){
            if ("Start" == ucc.Name) startClass = ucc;
            code += "const " + ucc.GetVarName() + ": " + ucc.GetElemName() + " = new " + ucc.GetElemName() + "(";
            code += GetParams(ucc) + ");\n";
        }

        foreach (UseCase ucc in element.UseCases)
        foreach (UseCase inv in ucc.Invoked)
            code += ucc.GetVarName() + "." + inv.GetVarName() + " = " + inv.GetVarName() + ";\n";

        code += "\nfunction switchView(state: AppState, action: ScreenId) {\n";
        code += "\tlet newState = { ...state };\n";
        code += "\tswitch (action) {\n";
        ResultEnumeration? screenId = element.DataTransferObjectUnit.Enums
            .Find(id => id is SimpleResultEnumeration { Kind: ResultKind.Navigation } 
                        && "screen id" == id.Name);
        if (null == screenId) throw new Exception("Critical error - no ScreenId defined");
        foreach (Value id in screenId.Values){
            code += "\t\tcase ScreenId." + id.GetElemName() + ":\n";
            code += "\t\t\tnewState.screen = ScreenId." + id.GetElemName() + ";\n";
            code += "\t\t\tbreak;\n";
        }
        code += "\t}\n\treturn newState;\n}\n";

        code += "export default function App() {\n";
        code += "\tconst [state, globalUpdateView] = useReducer(switchView, {\n";
        code += "\t\tscreen: ScreenId.START,\n\t});\n\n";

        code += string.Join("", element.Presenters.Select(pc => "\t" + pc.GetVarName() +
                                                                ".injectGlobalUpdateView(globalUpdateView);\n")) + "\n";

        UCOperation? startUCOperation = element.UseCases
            .Find(ucc => "Start" == ucc.Name)?.Operations
            .Find(m => m.Initial);

        code += "\tif (state.screen === ScreenId.START) start." +
                (null != startUCOperation ? startUCOperation.GetElemName() : "selectApplication") + "();\n\n";

        code += "\treturn (\n";
        code += "\t\t<div className=\"App\">\n";
        foreach (View vf in element.Views){
            code += "\t\t\t{" + vf.GetElemName() + "(state.screen === ScreenId." + vf.GetElemName().ToUpper().Substring(1) + ", ";
            code += vf.Presenter.GetVarName();
            if (null != vf.Controller.UseCase)
                code += ", " + vf.Controller.UseCase.GetVarName();
            code += ")}\n";
        }
        
        code += "\t\t</div>\n\t);\n}\n";
        
        return null == startUCOperation ? throw new Exception("Missing \"Start\" use case") : code;
    }

    public string GetHeaderCode(Operation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + element.GetElemName() + GetParametersCode(element) +
                      (null == element.ReturnType ? "" : ": " + GetTypeName(element.ReturnType));
        return code;
    }

    public string MapPrimitiveType(PrimitiveType type)
    {
        return type switch {
            PrimitiveType.Integer => "bigint",
            PrimitiveType.Number or PrimitiveType.Float => "number",
            PrimitiveType.String => "string",
            PrimitiveType.Boolean => "boolean",
            PrimitiveType.Time or PrimitiveType.Date => "Date",
            _ => ""
        };
    }

    public CodeFile? ToCodeFile(CodeUnit element, string basePath)
    {
        if (element is DataTransferObject or ViewModel or ResultEnumeration)
            return null;
        return new CodeFile{
            Path = basePath + "\\" + GetFileName(element) + ".tsx",
            CodeContents = element.ToCode()
        };
    }

    public IEnumerable<CodeFile> GetAuxiliaryFiles()
    {
        List<CodeFile> files = [];
        string code = $"import {{ ScreenId }} from \"../../viewmodel/{GetGenericFileName(typeof(DataTransferObjectUnit))}\";\n";
        code += "import { Dispatch } from \"react\";\n\n";
        code += "export class PresentationDispatcher {\n";
        code += "\tgUpdateView!: Dispatch<ScreenId>;\n";
        code += "\tinjectGlobalUpdateView(guv: Dispatch<ScreenId>) {\n";
        code += "\t\tthis.gUpdateView = guv;\n";
        code += "\t}\n}\n";
        files.Add(new CodeFile{
            Path = @"view\presenters\PPresentationDispatcher.tsx",
            CodeContents = code 
        });
        return files;
    }

    public string GetMainFileName()
    {
        return "App.tsx";
    }

    // ==== PRIVATE METHODS =============
    
    private string GetCode(Trigger element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "<button\n"; // TODO - handle different types of buttons (links, etc.)
        if (null != element.Condition) code += ts + "\tdisabled={!Boolean(" + element.Condition.GetElemName() + "())}\n";
        code += ts + "\tonClick={" + element.Action!.GetElemName() + "}\n" + ts + ">\n";
        code += ts + "\t" + Common.Utils.ToTitleCase(element.Name) + "\n" + ts + "</button>"; // TODO - as above
        return code;
    }

    private string GetBaseCode(ResultEnumeration element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "export enum " + element.GetElemName() + " {\n";
        code += string.Join(",\n", element.Values.Select(v => ts + "\t" + v.GetElemName() + " = \"" +
                                                              v.GetElemName() + "\"")) + "\n";
        code += ts + "}\n";
        return code;
    }

    private string GetCode(DataItem element, int tabs = 0, bool forceSimple = false){
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + (forceSimple ? element.Type.GetVarName() : element.GetVarName()) + ": ";
        string typeString = GetTypeName(element, forceSimple);
        code += typeString + " = ";
        if (element.Type is Primitive || element is { Multiple: false, Type: DataTransferObject { IsSimple: true } dto }
            && dto.Fields[0].Type is Primitive)
        {
            Primitive prim = element.Type as Primitive ?? (Primitive)((DataTransferObject) element.Type).Fields[0].Type;
            if (!element.Multiple || forceSimple)
                switch (prim.Value) {
                    case PrimitiveType.Integer: code += "BigInt(0)";
                        break;
                    case PrimitiveType.Number:
                    case PrimitiveType.Float: code += "0";
                        break;
                    case PrimitiveType.String: code += "\"\"";
                        break;
                    case PrimitiveType.Boolean: code += "false";
                        break;
                    case PrimitiveType.Time:
                    case PrimitiveType.Date: code += "new Date()";
                        break; 
                }
            else code += "[]";
        } else {
            code += element is { Multiple: false, Type: not DataTransferObject { IsAuxiliaryCollection: true } } 
                    || forceSimple ? 
                "new " + typeString + "()" 
                : "[]";
        }
        return code + ";";
    }

    // Returns type declaration for the given DataItem (considers collections)
    private string GetTypeName(DataItem element, bool forceSingle = false)
    {
        string baseTypeName = element.Type is DataTransferObject dto
            ? GetTypeName(dto, forceSingle)
            : element.Type.GetElemName();
        if (!element.Multiple || forceSingle)
            return baseTypeName;
        return baseTypeName + "[]";
    }
    
    // Returns type declaration for the given DataItemType (considers collections)
    private string GetTypeName(DataItemType element, bool forceSingle = false)
    {
        if (element is DataTransferObject dto) {
            if (dto.IsAuxiliaryCollection)
                return dto.Fields[0].Type.GetElemName() + (forceSingle ? "" : "[]");
            if (dto.IsSimple)
                return dto.Fields[0].Type.GetElemName();
        }
        return element.GetElemName();
    }

    private string GetTypeNameForConstructor(DataItem element, bool forceSingle = false)
    {
        string name = GetTypeName(element,forceSingle);
        return name.Substring(0, 1).ToUpper() + name.Substring(1);
    }


    private string GetCode(Parameter element, int i = 0, bool var = false){
        string code;
        if (element.Type is ResultEnumeration) code = "result";
        else code = element.GetVarName() + (i > 0 ? i.ToString() : "");
        if (!var) code += ": " + GetTypeName(element);
        return code;
    }
    
    private string GetCode(Call element, int tabs = 0)
    {
        return ToFullCode(element, tabs);
    }

    private string GetCode(Decision element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + string.Join(" else ", element.Conditions.Select(c => GetCode(c,tabs)));
        return code;
    }

    private string GetCode(Condition element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        // CODE: if (ClientEnumCheck.Valid == clientEnumCheck) {
        string code = "if (" + string.Join(" && ", element.Expressions.Select(GetCode)) + ") {\n";
        foreach (Instruction instr in element.Instructions)
            code += GetCode((dynamic)instr, tabs + 1) + "\n";
        code += ts + "}";
        return code;
    }

    private string GetCode(Expression element)
    {
        // CODE: ClientEnumCheck.Valid == clientEnumCheck
        return element.ToBeChecked == null ? "result == \"" + element.Value!.GetElemName() + "\""
            : element.ToBeChecked.Enumeration!.GetElemName() + "." + element.Value!.GetElemName() +
              " == " + element.ToBeChecked.Enumeration.GetVarName();
    }

    private string GetCode(End element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "if (undefined != this.returnTo)\n";
        code += ts + "\tthis.returnTo.call(this.returnUc, " + element.Value!.Parent!.GetElemName() + "." + element.Value.GetElemName() + ");";
        return code;
    }

    // ==== HELPERS =============

    private string ToFullCode(Call element, int tabs = 0, bool var = false){
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts;
        // CODE: this.pCLW.showUpdatedClientListWnd(list);
        if (element.CalledOperation is SOperation op) {
            if (!var && op.Type is PredicateType.Read or PredicateType.Check)
                code += /* op.GetReturnTypeElemName() + */ "let " + op.ReturnType!.GetVarName() + " = "; // TODO - repeated return variable names
            code += "this." + op.Service!.GetVarName();
        } else if (element.CalledOperation is POperation pop)
            code += "this." + pop.Pres!.GetVarName();
        else if (element.CalledOperation is UCOperation ucop)
            code += "this." + ucop.Uc!.GetVarName() + "?";
        else throw new Exception("Critical compilation failure");

        code += "." + element.CalledOperation.GetElemName() + GetParametersCode(element.CalledOperation, true);
        if (var) code += null == element.Value ? "" : " == " + element.Value.Parent!.GetElemName() + "." + 
                                                      element.Value.GetElemName();
        else code += ";";
        return code;
    }
    
    private string ToHtml(DataItem element, string parentPath, bool editable, int tabs = 0, int hLevel = 4){
        if (6 < hLevel) throw new Exception("To many levels of indentation in " + parentPath);
        string ts = Common.Utils.GetTabString(tabs);
        string code;
        if (element.Type is Primitive prim) {
            string itemType = "string"; // TODO switch (other item types)
            bool isNotString = PrimitiveType.String != prim.Value;
            code = ts + "<label>" + element.Name + "</label>\n";
            code += ts + "<input\n";
            code += ts + "\ttype=\"" + (element.Multiple ? "string" : itemType) + "\"\n";
            code += ts + "\tvalue={viewState." + parentPath + "." + element.GetVarName() +
                    (element.Multiple ? ".join(\",\")" : (isNotString ? ".toString()" : "")) + "}\n";
            code += ts + "\tonChange={(e) => {\n";
            code += ts + "\t\tviewState." + parentPath + "." + element.GetVarName() + " = " + 
                    (isNotString ? 
                        GetTypeNameForConstructor(element) + "("
                        : "") + "e.target.value" + (element.Multiple ? ".split(\",\")" : "")
                    + (isNotString ? ")" : "") + ";\n";
            code += ts + "\t\tupdateView(\"" + parentPath.Replace(".", "_") + "_" + element.Name + "\")\n";
            code += ts + "\t}}\n";
            code += ts + "/> <br />\n";
        } else {
            if (!element.Multiple) {
                code = ts + "<h" + hLevel + ">" + element.Name + "</h" + hLevel + ">\n";
                code += ToHtml((CustomType)element.Type, editable, tabs, hLevel,
                    parentPath + "." + element.GetVarName());
            } else
                code = ToHtmlTable((CustomType)element.Type, editable, 
                    parentPath + "." + element.GetVarName(), tabs);
        }
        return code;
    }

    private string ToHtml(CustomType type, bool editable, int tabs = 0, int hLevel = 3, string? parentPath = null){
        if (type is DataTransferObject dto)
        {
            string ts = Common.Utils.GetTabString(tabs);
            string varName = parentPath ?? dto.GetVarName();
            string code = "";
            if (3 >= hLevel)
                code += ts + "<h" + hLevel + ">" + dto.Name + "</h" + hLevel + ">\n";
            foreach (Field item in dto.Fields)
                code += ToHtml(item, varName, editable, tabs + 1, hLevel + 1);
            return code;
        }
        throw new NotImplementedException();
    }

    private string ToHtmlTable(CustomType type, bool editable, string parentPath, int tabs = 0) {
        if (type is DataTransferObject dto) {
            string ts = Common.Utils.GetTabString(tabs);
            string code = ts + "<table className=\"table table-striped table-bordered\">\n";
            code += ts + "\t<thead>\n" + ts + "\t\t<tr>\n";
            if (editable) code += ts + "\t\t\t<th></th>\n";
            code += string.Join("",
                dto.Fields.Select(f =>
                    f.Type is Primitive ? ts + "\t\t\t<th>" + f.Name + "</th>\n" : ""));
            code += ts + "\t\t</tr>\n" + ts + "\t</thead>\n";
            code += ts + "\t<tbody>\n";
            string baseParentPath = editable ? "base" + parentPath[..1].ToUpper() + parentPath[1..] : parentPath;
            code += ts + "\t\t{viewState." + baseParentPath + " &&\n";
            code += ts + "\t\t viewState." + baseParentPath + ".map((value,index) => (\n";
            code += ts + "\t\t\t<tr key={index}>\n";
            if (editable)
            {
                string nts = Common.Utils.GetTabString(tabs + 4);
                code += nts + "<td>\n" + nts + "\t<input\n";
                code += nts + "\t\ttype=\"checkbox\"\n";
                code += nts + "\t\tid={index.toString()}\n";
                code += nts + "\t\tonChange={(e) => {\n";
                nts = Common.Utils.GetTabString(tabs + 7);
                code += nts + "e.target.checked\n";
                code += nts + "\t? viewState." + parentPath + ".push(\n";
                code += nts + "\t\t\tviewState." + baseParentPath + "[index]\n";
                code += nts + "\t\t)\n";
                code += nts + "\t: viewState." + parentPath + ".splice(\n";
                code += nts + "\t\t\tviewState." + parentPath + ".indexOf(\n";
                code += nts + "\t\t\t\tviewState." + baseParentPath + "[index]\n";
                code += nts + "\t\t\t), 1\n";
                code += nts + "\t\t);\n";
                code += nts + "viewState." + dto.GetVarName() + " =\n";
                code += nts + "\t1 == viewState." + parentPath + ".length\n";
                code += nts + "\t\t? viewState." + baseParentPath + "[0]\n";
                code += nts + "\t\t: undefined;\n";
                nts = Common.Utils.GetTabString(tabs + 4);
                code += nts + "\t\t}}\n";
                code += nts + "\t/>\n";
                code += nts + "</td>\n";
            }

            code += string.Join("", dto.Fields.Select(f => f.Type is Primitive prim
                ? ts + "\t\t\t\t<td>{value." + f.Name +
                  (f.Multiple ? ".join(\",\")" : PrimitiveType.String != prim.Value ? ".toString()" : "") + "}</td>\n"
                : ""));
            code += ts + "\t\t\t</tr>\n" + ts + "\t\t))}\n";
            code += ts + "\t</tbody>\n";
            code += ts + "</table>\n";
            return code;
        }
        throw new NotImplementedException();
    }
    
    private string ToVarCode(Call element, int tabs = 0){
        return ToFullCode(element, tabs, true);
    }

    private string ToVarCode(Parameter element, int i = 0){
        string code = (element.HasAttribute) ? "this." : "";
        return code + GetCode(element, i, true);
    }
    
    private string ToVarCode(DataTransferObject element, int tabs = 0, bool isUpdateable = false){
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + element.GetVarName() + ": ";

        string typeString = element.IsSimple ? GetTypeName(element.Fields[0]) : GetTypeName(element);
        code += typeString + " = ";
        // Example: screen: ScreenIdEnum = ScreenIdEnum.START
        if ("ScreenIdEnum" == typeString) code += typeString + ".START";
        else code += element.IsAuxiliaryCollection  ? "[]" : "new " + typeString + "()";
        
        if (isUpdateable && element.Fields.Exists(di => di.Multiple)) {
            // Example: baseOrderList: OrderList = new OrderList();
            // Example: order?: Order = undefined;
            // This code is for storing user selection(s) on the list
            code += ";\n" + code.Replace(Common.Utils.ToCamelCase(element.Name),
                Common.Utils.ToCamelCase("base " + element.Name));
            foreach (Field di in element.Fields)
                if (di.Multiple) {
                    string codeFrg = GetCode(di, tabs, true).Replace(":", "?:");
                    code += ";\n" + codeFrg[..(codeFrg.IndexOf("=", StringComparison.Ordinal) + 2)] + "undefined";
                }
        }
        return code + ";";
    }
    
    private string GetImports(Controller element){
        string state = element.GetElemName().Substring(1) + "State";
        string code = "import { " + state;
        code += $" }} from \"../../viewmodel/{GetGenericFileName(typeof(ViewModelUnit))}\";\n";
        List<string> dataObjects = [];
        foreach (COperation cop in element.Operations){
            foreach (DataTransferObject dto in cop.TransferredData){
                string elemName = dto.GetElemName();
                if (!dataObjects.Contains(elemName)) dataObjects.Add(elemName);
            }
            //foreach (Parameter par in cop.parameters) {
            //    string name = par.ToTypeCode();
            //    if (!dataObjects.Contains(name)) dataObjects.Add(name);
            //}
        }

        if (0 != dataObjects.Count) {
            code += "import { " + string.Join(", ", dataObjects);
            code += $" }} from \"../../viewmodel/{GetGenericFileName(typeof(DataTransferObjectUnit))}\";\n";
        }

        code += "import { " + element.UseCase!.GetElemName() +
                " } from \"../../usecases/" + element.UseCase.GetElemName() + "\";\n";
        return code + "\n";
    }
    
    private string GetImports(Presenter element){
        string code = "import { Dispatch } from \"react\";\n";
        code += "import { PresentationDispatcher } from \"./PresentationDispatcher\";\n";
        
        string state = element.GetElemName().Substring(1) + "State";
        code += "import { " + state;
        code += $" }} from \"../../viewmodel/{GetGenericFileName(typeof(ViewModelUnit))}\";\n";

        code += "import { ";
        List<string> dataObjects = ["ScreenId"];
        foreach (POperation cop in element.Operations){
            if (null != cop.ReturnType && cop.ReturnType is not Primitive { Value: PrimitiveType.Boolean }) 
                dataObjects.Add(GetTypeName(cop.ReturnType));
            foreach (Parameter par in cop.Parameters) {
                string name = GetTypeName(par, true);
                if (!dataObjects.Contains(name)) dataObjects.Add(name);
                if (par is { IsInputOutput: true, Type: DataTransferObject dto }
                    && dto.Fields.Exists(di => di is { Multiple: true, Type: not Primitive }))
                    foreach (Field di in dto.Fields)
                        if (di is { Multiple: true, Type: not Primitive }) {
                            name = di.Type.GetElemName();
                            if (!dataObjects.Contains(name)) dataObjects.Add(name);
                        }
            }
        }
        code += string.Join(", ", dataObjects);
        code += $" }} from \"../../viewmodel/{GetGenericFileName(typeof(DataTransferObjectUnit))}\";\n";
            
        return code + "\n";
    }
    
    private string GetImports(Service element){
        string code = "import { ";
        List<string> dataObjects = [];
        foreach (SOperation sig in element.Operations){
            if (null != sig.ReturnType && sig.ReturnType is not Primitive)
                dataObjects.Add(GetTypeName(sig.ReturnType, true));
            foreach (Parameter par in sig.Parameters) {
                string name = GetTypeName(par);
                if (!dataObjects.Contains(name)) dataObjects.Add(name);
            }
        }
        code += string.Join(", ", dataObjects);
        code += $" }} from \"../viewmodel/{GetGenericFileName(typeof(DataTransferObjectUnit))}\";\n";
            
        return code + "\n";
    }
    
    private string GetImports(UseCase element){
        List<string> dataObjects = [];
        foreach (UCOperation ucop in element.Operations){
            foreach (Instruction instr in ucop.Instructions){
                if (instr is Call { CalledOperation.ReturnType: not null } call) {
                    if (call.CalledOperation.ReturnType is not Primitive) {
                        string name = GetTypeName(call.CalledOperation.ReturnType, true);
                        if (!dataObjects.Contains(name)) dataObjects.Add(name);
                    }
                } else if (instr is End) {
                    string resultEnum = Common.Utils.ToPascalCase(element.Name) + "ResultEnum";
                    if (!dataObjects.Contains(resultEnum)) dataObjects.Add(resultEnum);
                }
            }
            foreach (Parameter par in ucop.Parameters) 
                if (par.Type is not Primitive) {
                    string name = GetTypeName(par, true);
                    if (!dataObjects.Contains(name)) dataObjects.Add(name);
                }
            foreach (DataTransferObject dto in element.State) {
                string name = dto.GetElemName();
                if (!dataObjects.Contains(name)) dataObjects.Add(name);
            }
        }
        string code = "import { " + string.Join(", ", dataObjects);
        code += $" }} from \"../viewmodel/{GetGenericFileName(typeof(DataTransferObjectUnit))}\";\n";

        foreach (Presenter presenter in element.Presenters)
            code += "import { " + presenter.GetElemName() + " } from \"../view/presenters/" + presenter.GetElemName() + "\";\n";
        foreach (Service service in element.Services)
            code += "import { " + service.GetInterfaceName() + " } from \"../services/" + service.GetElemName() + "\";\n";
        foreach (UseCase inv in element.Invoked)
            code += "import { " + inv.GetElemName() + " } from \"./" + inv.GetElemName() + "\";\n";
        return code + "\n";
    }
    
    private string GetImports(IntermediateRepresentation element){
        string code = "import React from \"react\";\n";
        code += "import { useReducer } from \"react\";\n";
        code += $"import {{ AppState, ScreenId }} from \"./viewmodel/{GetGenericFileName(typeof(DataTransferObjectUnit))}\";\n";
        foreach (View vf in element.Views)
            code += "import " + vf.GetElemName() + " from \"./view/" + vf.GetElemName() + "\";\n";
        foreach (Presenter pc in element.Presenters)
            code += "import { " + pc.GetElemName() + " } from \"./view/presenters/" + pc.GetElemName() + "\";\n";
        foreach (UseCase ucc in element.UseCases)
            code += "import { " + ucc.GetElemName() + " } from \"./usecases/" + ucc.GetElemName() + "\";\n";
        foreach (Service si in element.Services)
            code += "import { " + si.GetElemName() + ", " + si.GetElemName().Substring(1) + "Proxy } from \"./services/" + si.GetElemName() + "\";\n";
        return code + "\n";
    }
    
    private string GetImports(View element){
        string code = "import React from \"react\";\n";
        code += "import { useReducer } from \"react\";\n";
        code += "import { " + element.GetElemName().Substring(1) + 
                $"State }} from \"../viewmodel/{GetGenericFileName(typeof(ViewModelUnit))}\";\n";
        code += "import { " + element.Controller.GetElemName() + " } from \"./controllers/" +
                element.Controller.GetElemName() + "\";\n";
        code += "import { " + element.Controller.UseCase!.GetElemName() + " } from \"../usecases/" +
                element.Controller.UseCase.GetElemName() + "\";\n";
        code += "import { " + element.Presenter.GetElemName() + ", update" + 
                element.Presenter.GetElemName().Substring(1) + 
                " } from \"./presenters/" + element.Presenter.GetElemName() + "\";\n\n";
        return code;
    }
    
    private string GetImports(ViewModelUnit element)
    {
        return $"import * from \"../viewmodel/{GetGenericFileName(typeof(DataTransferObjectUnit))}\";\n";
    }
    
    private string GetParametersCode(Operation element, bool var = false, bool bare = false){
        IEnumerable<string> pars = var ? element.Parameters.Select(p => ToVarCode(p)) :
            element.Parameters.Select(p => GetCode(p));
        
        string prefix = "", suffix = "";
        if (element is UCOperation ucop) {
            if (ucop.Returning) prefix += "this" + (var ? "" : ": any") + (0 != element.Parameters.Count ? ", " : "");
            if ((ucop.Initial || ucop.Invoking) && null != ucop.Uc!.Enumeration)
                suffix = (0 != element.Parameters.Count || 0 != prefix.Length ? ", " : "") +
                         "returnTo" + (var ? "" : ": Function")
                            + (ucop.Invoking ? "" : ", " + (var ? "this" : "returnUc: any")); // NOTE: replace "this" to "returnUc" if used anywhere else than in ToFullCode(Call)
        }
        
        return (bare ? "" : "(") + prefix + string.Join(", ", pars) + suffix + (bare ? "" : ")");
    }
    
    private string GetParams(UseCase element){
        return string.Join(", ", 
            element.Presenters.Concat<DistinguishableCodeUnit>(element.Services).
                ToList().Select(p => (p is Presenter pr) ? pr.GetVarName() : ((Service) p).GetVarName()));
    }

    private string GetFileName(CodeUnit element)
    {
        switch (element) {
            case DataTransferObjectUnit:
                return GetGenericFileName(typeof(DataTransferObjectUnit));
            case ViewModelUnit:
                return GetGenericFileName(typeof(ViewModelUnit));
            case View:
            case Controller:
            case Presenter:
            case Service:
            case UseCase:
                return element.GetElemName();
        }
        throw new Exception("Critical error - unknown code unit type");
    }

    private string GetGenericFileName(Type elementType)
    {
        if (typeof(DataTransferObjectUnit) == elementType)
            return "DataModel";
        if (typeof(ViewModelUnit) == elementType)
            return "ViewModel";
        throw new Exception("Critical error - type has no generic file name");
    }
}