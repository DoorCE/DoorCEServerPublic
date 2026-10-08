using DoorCEGenerator.Application.IntermediateModel;
using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;
using DoorCEGenerator.Application.IntermediateModel.Instructions;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;
using DoorCEModel.Infrastructure.DataModel.CodeContents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Utils.Extensions;

namespace DoorCEGenerator.Application.Domain.CodeGenerators;

public class FlutterCodeGenerator(INamingConverter namingConverter) : ICodeGenerator
{
    // ==== PUBLIC GetCode() METHODS (used by ToCode() dynamic methods) =============
    
    public string GetCode(ResultUnionEnumeration element, int tabs = 0)
    {
        string mappingCode = "class " + GetTypeName(element) + "Ext {\n";
        mappingCode += GetUnionMapOperation(element, tabs+1) + "\n";
        mappingCode += "}\n";
        return GetImports(element) + GetBaseCode(element, tabs) + "\n" + mappingCode;
    }

    public string GetCode(SimpleResultEnumeration element, int tabs = 0)
    {
        return GetBaseCode(element, tabs);
    }

    public string GetCode(Controller element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        // Example: import ...
        string code = GetImports(element);
        // Example: class CUpsertCatalogueWindow {
        code += ts + "class " + element.GetElemName() + " {\n";
        // Example:   final VMUpsertCatalogueWindow? _model;
        code += ts + "\tfinal " + element.View!.ViewModel.GetElemName() + "? _model;\n";
        // Example:   const CUpsertCatalogueWindow(this._model);
        code += ts + "\tconst " + element.GetElemName() + "(this._model);\n\n";
        //            >>>methods<<<
        code += string.Join("\n" + ts, element.Operations.Select(x => x.ToCode(tabs + 1)));
        // Example: }
        code += "\n" + ts + "}";
        return code;
    }

    public string GetCode(COperation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        bool hasTransferredData = 0 != element.TransferredData.Count;
        // Example: Future<void> saveSelected(BuildContext context) async {
        string code = element.ToHeaderCode(tabs) + " async {\n";
        // Example:   return await context.read<IUCUpsertCatalogue>().upsertCatalogueSelected(context,
        code += ts + "\treturn await context.read<" + element.Invoked!.Uc!.GetInterfaceName() + ">()." +
                element.Invoked.GetElemName() + "(context" + 
                (hasTransferredData || null != element.ReturnTo? "," : ");\n");
        // Example:      _model.catalogue, _model.newContacts, _model.isInsert
        // Example:      catalogue, newContacts, isInsert
        if (hasTransferredData)
            code += "\n" + ts + "\t\t" + string.Join(", ",
                element.TransferredData.Select(da => 
                    (element.Invoked!.Parameters.Where(p => p.CanBeIdentifier)
                    .Any(p => p.Type == da) ? "" : "_model!.") + da.GetVarName()));

        // Example:      , (BuildContext context, UpsertCatalogueAtA1UnionEnum result) => context.read<IUCUpsertCatalogue>().invokedAtA1(context, result)
        if (null != element.ReturnTo) {
            code += (hasTransferredData ? ",\n " : "\n");
            code += ts + "\t\t";
            code += "(BuildContext context, " 
                       + GetTypeName(((element.Invoked.Instructions[0] as Call ?? throw new Exception("Critical error"))
                           .CalledOperation as UCOperation ?? throw new Exception("Critical Error")).Uc!.Enumeration!) + " result)";
            code += " => context.read<" + element.ReturnTo.Uc!.GetInterfaceName() + ">()." +
                element.ReturnTo.GetElemName() + "("
                + GetParametersCode(element.ReturnTo, true, true)
                    .Replace("result", element.ReturnTo.Parameters[0].Type is ResultUnionEnumeration ?
                        GetTypeName(element.ReturnTo.Parameters[0].Type) + "Ext.map(result)" : "result") + ")";
            // TODO - dont' make this mapping if not a union
        }
        // Example:    );
        if (hasTransferredData || null != element.ReturnTo)
            code += "\n" + ts + "\t);\n";
        // Example: }
        code += ts + "}\n";
        return code;
    }

    public string GetCode(DataTransferObjectUnit element, int tabs = 0)
    {
        throw new Exception("Data transfer object unit in Flutter generator not needed ");
    }

    public string GetCode(DataTransferObject element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        
        // TODO - move imports
        string code = ts + "import 'package:doorce_style/doorce_style.dart';\n";
        
        // Example: class Tree {
        code += ts + "class " + element.GetElemName() + " {\n";
        //            >>>fields<<<
        code += string.Join("", element.Fields.Select(di => GetCode(di, tabs+1) + "\n"));

        code += "\n" + GetFromJson(element, tabs + 1) + "\n";
        code += GetToJson(element, tabs + 1) + "\n";
        code += GetToLabel(element, tabs + 1) + "\n";
        
        // Example: }
        code += ts + "}\n";
        return code;
    }

    public string GetCode(POperation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        
        // Example: show(BuildContext context, XDataset dataset) {
        string code = ts + element.ToHeaderCode() + " {\n";

        // Example:   _model = VMUpsertDatasetWindow();
        code += ts + "\t_model = " + element.Pres!.View!.ViewModel.GetElemName() + "();\n";
        
        // Example:   _model.init(dataset);
        if (0 != element.Parameters.Count)
            code += ts + "\t_model.init" + GetParametersCode(element, true, false, true) + ";\n";
        
        // Example:   _viewKey = GlobalKey<VMUpsertDatasetWindow>();
        code += ts + "\t_viewKey = GlobalKey<" + element.Pres.View!.GetElemName() + "State>();\n";

        if (0 != element.Parameters.Count &&
            (element.Parameters[0].Multiple || element.Parameters[0].Type is DataTransferObject { IsAuxiliaryCollection: true, CanBeMap: false })) 
            code += ts + "\tLogger.root.fine(\"[" + element.Name + "] running with ${"
                + element.Parameters[0].GetVarName() + ".length} items (first: ${"
                + element.Parameters[0].GetVarName() + ".isNotEmpty ? "
                + element.Parameters[0].GetVarName() + "[0].toLabel(false) : \"none\"})\");\n";

        // Example:   pushWindow(context, _vUpsertDatasetWindowFactory.get(_viewKey, _model));
        code += ts + "\tpushWindow(context, " +
                "_v" + Common.Utils.ToPascalCase(element.Pres!.Name) + "Factory.get(_viewKey, _model));\n";
        
        // Example: }
        code += ts + "}\n";
        return code;
    }

    public string GetCode(Presenter element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = GetImports(element);
        // Example: abstract interface class IPUpsertDatasetWindow {
        code += ts + "abstract interface class " + element.GetInterfaceName() + " {\n";
        //            >>>operations<<<
        code += string.Join("\n", element.Operations.Select(m => ts + "\t" + m.ToHeaderCode() + ";\n"));
        // Example: }
        code += ts + "}\n\n";
        
        // Example: class PUpsertDatasetWindow extends AbstractPresenter implements IPUpsertDatasetWindow {
        code += ts + "class " + element.GetElemName() + " extends AbstractPresenter implements " 
                + element.GetInterfaceName() + " {\n";
        // Example:   final VUpsertDatasetWindowFactory _vUpsertDatasetWindowFactory;
        code += ts + "\t" + "final " + element.View!.GetElemName() + "Factory _" + element.View!.GetVarName() + "Factory;\n";
        // Example:   GlobalKey<VUpsertDatasetWindowState>? _viewKey;
        code += ts + "\t" + "GlobalKey<" + element.View.GetElemName() + "State>? _viewKey;\n";
        // Example:   late VMUpsertDatasetWindow _model;
        code += ts + "\t" + "late " + element.View!.ViewModel.GetElemName() + " _model;\n\n";
        // Example:   PUpsertDatasetWindow(this._vUpsertDatasetWindowFactory) : super(RoutingPrefixes.upsertDatasetWindow);
        code += ts + "\t" + element.GetElemName() + "(this._" + element.View!.GetVarName() +
                "Factory) : super(\"/" + namingConverter.GetRoutingName(element.View) + "\");\n\n";
        
        // TODO - implement: String getRoutingName(){...}
        
        //            >>>methods<<<
        code += string.Join("\n", element.Operations.Select(m => ts + "\t@override\n" + m.ToCode(tabs + 1)+"\n"));
        
        // Example: }
        code += ts + "}";
        return code;
    }

    public string GetCode(SOperation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        // Example: @override
        // Example: Future<XDataset> getDataset(String identifier) async {
        string code = ts + "@override\n" 
                         + element.ToHeaderCode(tabs) + " async {\n";

        string conceptUri = element.OperationTarget;
        
        switch (element.Type) {
            case PredicateType.Read:
                if (element.ReturnType is DataTransferObject {IsSimple: false} rdto)
                    foreach (Field field in rdto.ActualFields.Where(f => f.Type is DataTransferObject { IsSimple: false }))
                        code += ts + "\tMap<String,String> " + field.Type.GetVarName()
                                + "Options = await " + field.Type.GetVarName()
                                + "OptionsProxy.read" + field.Type.GetElemName() +
                                "Options();\n";
                
                code += ts + "\tfinal response = await httpGet('acquisition/";
                DataTransferObject dto = element.ReturnType as DataTransferObject 
                                         ?? throw new Exception("Critical error - return type of a read operation should be a DTO");
                if (dto.IsAuxiliaryCollection)
                    code += "GetDatasetItemList?";
                else
                    code += "GetDataItem?identifier=$identifier&";
                code += "datasetUri=$datasetUri&conceptUri=" + conceptUri.Replace("#", "%23") + "');\n";
                // TODO - add Criteria
                code += GetResponseParsingCode(dto, tabs+1);
                break;
            case PredicateType.Update:
                code += ts + "\tfinal values = " + element.Parameters[0].GetVarName() + ".toJson();\n";
                code += ts + "\tfinal body = jsonEncode({\n" + 
                        ts + "\t\t\"identifier\": " + element.Parameters[0].GetVarName() + ".identifier,\n" +
                        ts + "\t\t\"datasetUri\": datasetUri,\n" +
                        ts + "\t\t\"conceptUri\": \"" + conceptUri + "\",\n" + ts + "\t\t\"values\": values\n" +
                        ts + "\t});\n";
                code += ts + "\tfinal response = await httpPost('acquisition/UpsertDataItem', body);\n";
                code += ts + "\tif (response.statusCode != 200) {\n";
                code += ts + "\tthrow Exception('Error updating " + element.Parameters[0].GetElemName() +
                        " with identifier: ${"  + element.Parameters[0].GetVarName() + 
                        ".identifier} - status code: ${response.statusCode}');\n";
                code += ts + "\t}\n";
                break;
            case PredicateType.Delete:
                code += ts + "\tfinal response = await httpDelete('acquisition/DeleteDataItem?identifier=$identifier&";
                code += "datasetUri=$datasetUri&conceptUri=" + conceptUri.Replace("#", "%23") + "');\n";
                code += ts + "\tif (response.statusCode != 200) {\n";
                code += ts + "\tthrow Exception('Error deleting " + element.Parameters[0].GetElemName() +
                        " with identifier: $identifier - status code: ${response.statusCode}');\n";
                code += ts + "\t}\n";
                break;
            case PredicateType.Check:
                code += ts + "\tfinal values = " + element.Parameters[0].GetVarName() + ".toJson();\n";
                code += ts + "\tfinal body = jsonEncode({\n" + 
                        ts + "\t\t\"identifier\": " + element.Parameters[0].GetVarName() + ".identifier,\n" +
                        ts + "\t\t\"datasetUri\": datasetUri,\n" +
                        ts + "\t\t\"conceptUri\": \"" + conceptUri + "\",\n" + ts + "\t\t\"values\": values\n" +
                        ts + "\t});\n";
                code += ts + "\tfinal response = await httpPost('acquisition/CheckDataItem', body);\n";
                code += ts + "\tif (response.statusCode == 200) {\n";
                code += ts + "\t\tint result = jsonDecode(response.body);\n";
                code += ts + "\t\treturn " + GetTypeName(element.ReturnType!) + ".values[result];\n";
                code += ts + "\t} else {\n";
                code += ts + "\t\tthrow Exception('Error checking " + element.Parameters[0].GetElemName() +
                        " - status code: ${response.statusCode}');\n";
                code += ts + "\t}\n";
                break;
            case PredicateType.CheckExisting:
                code += ts + "\tfinal response = await httpGet('acquisition/CheckExistingDataItem?identifier=$identifier&";
                code += "datasetUri=$datasetUri&conceptUri=" + conceptUri + "');\n";
                code += ts + "\tif (response.statusCode == 200) {\n";
                code += ts + "\t\tint result = jsonDecode(response.body);\n";
                code += ts + "\t\treturn " + GetTypeName(element.ReturnType!) + ".values[result];\n";
                code += ts + "\t} else {\n";
                code += ts + "\t\tthrow Exception('Error checking " + element.Parameters[0].GetElemName() +
                        " - status code: ${response.statusCode}');\n";
                code += ts + "\t}\n";
                break;
            case PredicateType.Execute:
                if ("submit" == element.OperationTarget.ToLower()) {
                    code += ts + "\tawait httpPost('acquisition/SubmitSourceDatasetContents?datasetUri=$datasetUri', null);\n";
                }
                break;
            default:
                throw new Exception("Critical error - unexpected predicate type");
        }
        return code + ts + "}\n";
    }

    private string GetResponseParsingCode(DataTransferObject returnType, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "if (response.statusCode == 200) {\n";
        string typeName = GetTypeName(returnType), varName = returnType.GetVarName();
        
        if (returnType.IsAuxiliaryCollection) {
            code += ts + "\tfinal List<dynamic> jsonList = jsonDecode(response.body) as List<dynamic>;\n";
            code += ts + "\t" + typeName + " " + varName + "= jsonList.map((json) {\n";
            string simpleTypeName = GetTypeName(returnType, true);
            code += ts + "\t\t" + simpleTypeName + " item = " + simpleTypeName + "();\n";
            code += ts + "\t\tMap<String,dynamic> values = (json as Map<String, dynamic>)['values'] as Map<String, dynamic>;\n";
            code += ts + "\t\titem.fromJson(values";
            foreach (Field field in returnType.ActualFields.Where(f => f.Type is DataTransferObject {IsSimple: false}))
                code += ", " + field.Type.GetVarName() + "Options";
            code += ");\n";
            code += ts + "\t\titem.identifier = (json)['identifier'] as String;\n";
            code += ts + "\t\treturn item;\n";
            code += ts + "\t}).toList();\n";
            if (returnType.CanBeMap) {
                code += ts + "\tfinal Map<String,String> labelsMap = {\n";
                code += ts + "\t\tfor (final item in " + varName + ")\n";
                code += ts + "\t\t\titem.identifier!: item.toLabel(false),\n";
                code += ts + "\t};\n";
                varName = "labelsMap";
            }
        } else {
            code += ts + "\tfinal Map<String, dynamic> jsonData = jsonDecode(response.body);\n";
            code += ts + "\t" + typeName + " " + varName + " = " + typeName + "();\n";
            code += ts + "\tMap<String,dynamic> values = jsonData['values'] as Map<String, dynamic>;\n";
            code += ts + "\t" + varName + ".fromJson(values";
            foreach (Field field in returnType.ActualFields.Where(f => f.Type is DataTransferObject {IsSimple: false}))
                code += ", " + field.Type.GetVarName() + "Options";
            code += ");\n";
            code += ts + "\t" + varName + ".identifier = jsonData['identifier'] as String;\n";
        }
            
        code += ts + "\treturn " + varName + ";\n";

        code += ts + "} else {\n";
        code += ts + "\tthrow Exception('Error getting " + returnType.GetElemName() + 
                (returnType.IsAuxiliaryCollection ? "" : " with identifier: $identifier") 
                + " - status code: ${response.statusCode}');\n";
        code += ts + "}\n";
        return code;
    }

    public string GetCode(Service element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        
        List<DataTransferObject> uniqueReturnTypes = GetUniqueReturnTypes(element);
        string code = GetImports(element, uniqueReturnTypes);
        // Example: abstract interface class IDatasetManagement {
        code += ts + "abstract interface class " + element.GetInterfaceName() + " {\n";
        //            >>>operations<<<
        code += string.Join("", element.Operations.Select(x => x.ToHeaderCode(tabs + 1) + ";\n"));
        // Example: }
        code += ts + "}\n\n";

        // Example: class DatasetManagementProxy extends Api implements IDatasetManagement {
        code += ts + "class " + element.GetElemName() + " extends Api implements " + element.GetInterfaceName() + " {\n\n";
        // Example:   final String datasetUri;
        code += ts + "\tfinal String datasetUri;\n";
        
        // Example: final ITreeSpeciesOptions treeSpeciesOptionsProxy;
        foreach (DataTransferObject dto in uniqueReturnTypes)
            code += ts + "\tfinal I" + dto.GetElemName() + "Options " + dto.GetVarName() + "OptionsProxy;\n";
        
        // Example:   DatasetManagementProxy(TokenAPI tokenAPI, ... ) : super(...);
        code += "\n" + ts + "\t" + element.GetElemName()
                + "(TokenAPI tokenAPI, String backendPath, this.datasetUri";
        foreach (DataTransferObject dto in uniqueReturnTypes)
            code += ", this." + dto.GetVarName() + "OptionsProxy";
        code += ") : super(tokenAPI: tokenAPI, backendPath: backendPath);\n\n";
        //              >>>methods<<<
        code += string.Join("\n", element.Operations.Select(x => x.ToCode(tabs + 1))) + "\n";
        // Example: }
        code += ts + "}\n\n";
        return code;
    }

    public string GetCode(UCOperation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        // Example: showClientListSelected(...) {
        string code = element.ToHeaderCode(tabs);
        
        code += " async {\n";
        if (element.Initial && null != element.Uc!.Enumeration){ 
            // Example:   if (null != returnTo) this.returnTo = returnTo;
            code += ts + "\tif (null != returnFunction) _returnFunction = returnFunction;\n";
        }
        // Example:   this.clientType = clientType;
        code += string.Join("", element.Parameters.Select( p => 
            p.Type is ResultEnumeration ? "" : // skip parameters that are only for returning results from the use case (they are not needed to be stored in state)
                ts + "\t" + (p.HasAttribute ? "this." : "") + ToVarCode(p) + " = " + GetCode(p, false, true) + ";\n" ));
        // TODO - skip only Enums that are not needed (that will not be used in other UCOperations of the same UC); do not skip "check" result Enums

        // Example:   this.speciesOptions = await treeSpeciesOptionsProxy.readTreeSpeciesOptions();
        foreach (Parameter par in element.Instructions.OfType<Call>()
                     .SelectMany(c => c.CalledOperation.Parameters)
                     .Where(p => p.CanBeSimplified).Distinct()) {
            SOperation optionsReadOperation = element.Uc!.Services.SelectMany(s => s.Operations)
                                                  .FirstOrDefault(o => o.ReturnType is DataTransferObject { CanBeMap: true } rdto
                                                                       && rdto.Name.NamingEquals(((DataTransferObject)par.Type).Name)) 
                                               ?? throw new Exception("Critical error");
            code += ts + "\t" + ToVarCode(par) + " = await "
                    + optionsReadOperation.Service!.GetVarName() + "." 
                    + optionsReadOperation.GetElemName() + GetParametersCode(optionsReadOperation, true)
            + ";\n";
        }


        //            >>>instructions<<<
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
        // Example: }
        code += ts + "}\n";
        return code;
    }

    public string GetCode(UseCase element, int tabs = 0)
    { 
        string ts = Common.Utils.GetTabString(tabs);
        string code = GetImports(element);
        
        // Example: abstract interface class IUCShowClientList {
        code += ts + "abstract interface class " + element.GetInterfaceName() + " {\n";
        //            >>>operations<<<
        code += string.Join("", element.Operations.Select(o => o.ToHeaderCode(tabs + 1) + ";\n"));
        // Example: }
        code += ts + "}\n\n";
        
        // Example: class UCShowClientList implements IUCShowClientList {
        code += ts + "class " + element.GetElemName() + " implements " + element.GetInterfaceName() + " {\n";
        // Example:   IPClientListWindow pClientListWindow;
        code += 0 == element.Presenters.Count ? "" : 
            string.Join("", element.Presenters.Select(p => ts + "\tfinal " + p.GetInterfaceName() + " " +
                                                           p.GetVarName() + ";\n")) + "\n";
        // Example:   IClients iClients;
        code += 0 == element.Services.Count ? "" :
            string.Join("", element.Services.Select(s => ts + "\tfinal " + s.GetInterfaceName() + " " +
                                                         s.GetVarName() + ";\n")) + "\n";
        
        // Example:   Function(BuildContext, ShowClientListResultEnum)? _returnFunction;
        if (null != element.Enumeration)
            code += ts + "\tFunction(BuildContext, " + GetTypeName(element.Enumeration!) + ")? _returnFunction;\n\n";
        // Example:   List<Client> clientList = {};
        code += 0 == element.State.Count ? "" :
            string.Join("", element.State.Select(a =>
                // TODO - this code should be optimised (State vs. ReferenceState)
                ToVarCode(a, tabs+1, true, element.ReferenceState.Contains(a)) + "\n"))
            + "\n";
        // TODO - create additional state items (isUpdateable == true) only when needed (do it in free time!)
        // Example: const UCShowClientList(this.ClientListWindow, this.iClients);
        code += "\t" + ts + (null == element.Enumeration ? "const " : "") + element.GetElemName() + "(";
        code += string.Join(", " + ts, element.Presenters.Select(p => "this." + p.GetVarName()));
        if (0 < element.Services.Count)
            code += (0 < element.Presenters.Count ? ", " : "") + 
                    string.Join(", " + ts, element.Services.Select(s => "this." + s.GetVarName()));
        code += ");\n\n";
        //            >>>operations<<<
        code += string.Join("\n", element.Operations.Select(o => o.ToCode(tabs + 1)));
        // Example: }
        code += ts + "}";
        return code;
    }

    public string GetCode(View element, int tabs = 0)
    {
        string code = GetImports(element) + "\n";
        bool isEditable = 0 != element.ViewModel.InputData.Count;
        code += "class " + element.GetElemName() + " extends StatefulWidget {\n";
        code += "\tfinal " + element.Controller.GetElemName() + " _controller;\n";
        code += "\tfinal " + element.ViewModel.GetElemName() + " _model;\n";
        code += "\tconst " + element.GetElemName() + "(GlobalKey? key, this._controller, this._model) : super(key: key);\n\n";
        code += "\t@override\n";
        code += "\tState<" + element.GetElemName() + "> createState() => " + element.GetElemName() + "State();\n";
        code += "}\n\n";
        code += "class " + element.GetElemName() + "State extends State<" + element.GetElemName() + "> {\n";
        if (isEditable) code += "\tfinal _formKey = GlobalKey<FormState>();\n\n";
        code += "\t@override\n";
        code += "\tvoid initState() {\n";
        code += "\t\tsuper.initState();\n";
        code += "\t}\n\n";
        code += "\tvoid refresh() { setState(() {}); }\n\n";
        code += GetViewHelperMethods(1) + "\n\n";
        
        if (isEditable)
            code += GetViewModelHelperMethods(1) + "\n\n";
        
        code += "\t@override\n";
        code += "\tWidget build(BuildContext context) {\n";

        code += "\t\treturn Material(child: SafeArea(\n";
        code += "\t\t\tchild: SizedBox.expand(\n";
        code += "\t\t\t\tchild: Column(\n";
        code += "\t\t\t\t\tchildren: [\n";
        code += "\t\t\t\t\t\tExpanded(\n";
        code += "\t\t\t\t\t\t\tchild: ";
        if (0 == element.ViewModel.InputData.Count && 0 == element.ViewModel.OutputData.Count && "root" != element.Type)
            code += Widget("Center", startAlignment: false, tabs: 8, child:
                DoorText("\"" + element.Name + "\"", "DoorTextStyle.titleBlack", 10)) + "\n";
        else if (isEditable) {
            code += "Form(\n" + "\t\t\t\t\t\t\t\tkey: _formKey,\n";
            code += "\t\t\t\t\t\t\t\t\tchild: " + GenerateColumn(element, /*true,*/ tabs + 9);
            code += "\t\t\t\t\t\t\t)\n";
        } else code += GenerateColumn(element, /*false,*/ tabs + 8);
        code += "\t\t\t\t\t\t),\n";
        
        List<Trigger> triggers = element.Triggers
            .Where(t => !t.Action!.TransferredData
                .Any(td => element.ViewModel.OutputData.Any(od => od.Fields[0].Type == td)))
            .ToList();

        bool isCollection = element.ViewModel.InputData.All(od => od.IsAuxiliaryCollection);

        if (0 != triggers.Count && (!isEditable || isCollection)) {
            code += "\t\t\t\t\t\t" + Widget("Container", startAlignment: false, tabs: tabs + 6, padding: "const EdgeInsets.all(16)", child:
                Row(tabs: tabs + 1, children: [
                        Widget("IntrinsicWidth", startAlignment: false, child:
                            GetButtons(null, triggers)
                        )
                    ]
                )

            );
            code += "\n";
        }
        code += "\t\t\t\t\t]\n";
        code += "\t\t\t\t)\n";
        code += "\t\t\t)\n";
        code += "\t\t));\n";
        code += "\t}\n";
        code += "}\n\n";
        code += GetViewFactoryCode(element, tabs);
        return code;
    }

    public string GetCode(ViewModelUnit element, int tabs = 0)
    {
        throw new Exception("View model unit in Flutter generator not needed ");
    }

    public string GetCode(ViewModel element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = GetImports(element);
        // Example: class VMTreeForm {
        code += ts + "class " + element.GetElemName() + " {\n";
        //            >>>fields<<<
        code += string.Join("", element.OutputData.Where(dto => !element.InputData.Contains(dto))
            .Select(dto => ToVarCode(dto, tabs+1) + "\n"));
        code += string.Join("", element.OutputData.Where(dto => element.InputData.Contains(dto))
            .Select(dto => ToVarCode(dto, tabs+1, true) + "\n"));
        code += string.Join("", element.InputData.Where(dto => !element.OutputData.Contains(dto))
            .Select(dto => ToVarCode(dto, tabs+1) + "\n"));
        code += string.Join("", element.ReferenceData.Select(dto => ToVarCode(dto, tabs+1, false, true) + "\n"));
        code += "\n";
        //            >>>init function<<<
        code += ts + "\tvoid init(" + string.Join(", ", element.OutputData.Select(dto => GetTypeName(dto) + " " + dto.GetVarName()));
        if (0 != element.OutputData.Count && 0 != element.ReferenceData.Count) code += ", ";
        code += string.Join(", ", element.ReferenceData.Select(dto => GetTypeName(dto, false, true) + " " + dto.GetVarName())) + ") {\n";
        code += string.Join("\n", element.OutputData.Select(dto => ts + "\t\tthis." + dto.GetVarName() + " = " + dto.GetVarName() + ";"));
        if (0 != element.OutputData.Count && 0 != element.ReferenceData.Count) code += "\n";
        code += string.Join("\n", element.ReferenceData.Select(dto => ts + "\t\tthis." + dto.GetVarName() + " = " + dto.GetVarName() + ";")) + "\n";
        code += ts + "\t}\n";
        
        // Example: }
        code += ts + "}\n";
        return code;
    }

    public string GetCode(IntermediateRepresentation element)
    {
        string code = GetImports(element);
        code += "void main() async {\n";
        code += "\tWidgetsFlutterBinding.ensureInitialized();\n";

        code += "\tfinal logLevelStr = const String.fromEnvironment('DEBUG', defaultValue: 'TRUE').toUpperCase();\n";
        code += "\tLogger.root.level = logLevelStr == \"TRUE\" ? Level.ALL : Level.CONFIG;\n";
        code += "\tLogger.root.onRecord.listen((record) {\n";
        code += "\t\tprint('${record.level.name}: ${record.time}: ACQ-APP: ${record.message}');\n";
        code += "\t});\n";
        
        code += "\trunApp(const MainAppBootstrap());\n";
        code += "}\n\n";
        code += GetMainAppBootstrapCode();
        code += GetSetupDependenciesCode(element);
        code += GetMainAppCode(element.AppName);
        code += GetMainRouterCode();
        code += GetUseCaseWidgetCode();
        
        return code;
    }

    private string GetSetupDependenciesCode(IntermediateRepresentation element)
    {
        string code = "MultiProvider setupDependencies(String datasetUri, TokenAPI tokenHandler) {\n";
        code += "\tList<SingleChildWidget> providerList = List<SingleChildWidget>.empty(growable: true);\n\n";

        // REGISTER PROVIDERS FOR SERVICES
        code += "\tproviderList.add(Provider<TokenAPI>(create: (context) => tokenHandler));\n\n";
        List<Service> services = element.Services.
            Where(s => s.Operations
                is [{ Type: PredicateType.Read, ReturnType: DataTransferObject { CanBeMap: true } }]).ToList();
        services.AddRange(element.Services
            .Where(s => s.Operations is not 
                [{ Type: PredicateType.Read, ReturnType: DataTransferObject { CanBeMap: true } }]).ToList());
        foreach (Service service in services) {
            code += "\tproviderList.add(Provider<" + service.GetInterfaceName() + ">(create: (context) => "
                    + service.GetElemName()
                    + "(context.read(), const String.fromEnvironment(\"UDAS_SERVER_URL\"), datasetUri";
            foreach (DataTransferObject _ in GetUniqueReturnTypes(service))
                code += ", context.read()";
            code += ")));\n";
        }

        // REGISTER PROVIDERS FOR VIEW FACTORIES
        foreach (View v in element.Views)
            code += "\tproviderList.add(Provider(create: (context) => " + v.GetElemName() + "Factory()));\n";

        // REGISTER PROVIDERS FOR PRESENTERS
        foreach (Presenter p in element.Presenters)
            code += "\tproviderList.add(Provider<" + p. GetInterfaceName() + ">(create: (context) => "
                    + p.GetElemName() + "(context.read())));\n";
        code += "\n";
  
        // REGISTER PROVIDERS FOR USE CASES
        foreach (UseCase uc in element.UseCases)
            code += "\tproviderList.add(Provider<" + uc.GetInterfaceName() + ">(create: (context) => "
                    + uc.GetElemName() + "(" +
                    String.Join(", ", Enumerable.Repeat("context.read()", uc.Presenters.Count + uc.Services.Count)) + 
                    ")));\n";
        code += "\n";

        code += "\treturn MultiProvider(providers: providerList, child: const MainApp() );\n"; 
        code += "}\n\n";
        return code;
    }

    private string GetMainAppBootstrapCode()
    {
        string code = "class MainAppBootstrap extends StatefulWidget {\n";
        code += "\tconst MainAppBootstrap({super.key});\n";
        code += "\t@override\n";
        code += "\t\tState<MainAppBootstrap> createState() => _MainAppBootstrapState();\n";
        code += "}\n\n";

        code += "class _MainAppBootstrapState extends State<MainAppBootstrap> {\n";
        code += "\tlate Future<MultiProvider> _init;\n";
        code += "\t@override\n";
        code += "\tvoid initState() {\n";
        code += "\t\tsuper.initState();\n";
        code += "\t\t_init = _initialize();\n";
        code += "\t}\n\n";

        code += "\tFuture<MultiProvider> _initialize() async {\n";
        code += "\t\tconst String platformUrl = String.fromEnvironment(\"UDAS_PLATFORM_URL\", defaultValue: \"http://localhost:8080\");\n";
        code += "\t\tMessageHandler messageHandler = MessageHandler(\n";
        code += "\t\t\tplatformUrl,\n";
        code += "\t\t\treadyFromAppType: IFrameTexts.readyFromApp,\n";
        code += "\t\t\treadyFromPlatformType: IFrameTexts.readyFromPlatform,\n";
        code += "\t\t);\n\n";
        
        code += "\t\tLogger.root.fine(\"[_initialize] Waiting for message from platform '$platformUrl'\");\n\n";

        code += "\t\tfinal messageMap = await messageHandler.getMessageFromPlatform();\n";
        code += "\t\tfinal messageFromPlatform = XIFrameMessage.fromJson(messageMap);\n\n";

        code += "\t\tLogger.root.fine(\"[_initialize] Received message from platform." +
            " Token: ${messageFromPlatform.token}, DatasetUri: ${messageFromPlatform.datasetUri}\");\n\n";

        code += "\t\tfinal tokenHandler = KeycloakTokenService(\n";
        code += "\t\t\tkeycloakUrl: const String.fromEnvironment(\"KEYCLOAK_PATH\"),\n";
        code += "\t\t\trealm: const String.fromEnvironment(\"KEYCLOAK_REALM\"),\n";
        code += "\t\t\tclientId: const String.fromEnvironment(\"KEYCLOAK_CLIENT\"),\n";
        code += "\t\t\tclientSecret: const String.fromEnvironment(\"KEYCLOAK_CLIENT_SECRET\"),\n";
        code += "\t\t);\n\n";

        code += "\t\tawait tokenHandler.setRefreshToken(messageFromPlatform.token);\n\n";
        
        code += "\t\tLogger.root.fine(\"[_initialize] Refresh token set in token handler." +
                " New refresh token: ${await tokenHandler.getRefreshToken()}\");\n\n";
        
        code += "\t\treturn setupDependencies(messageFromPlatform.datasetUri, tokenHandler);\n";
        code += "\t}\n\n";

        code += "\t@override\n";
        code += "\tWidget build(BuildContext context) {\n";
        code += "\t\treturn FutureBuilder<MultiProvider>(\n";
        code += "\t\t\tfuture: _init,\n";
        code += "\t\t\tbuilder: (context, snapshot) {\n";
        code += "\t\t\t\tif (!snapshot.hasData) {\n";
        code += "\t\t\t\t\treturn const MaterialApp(\n";
        code += "\t\t\t\t\t\thome: Scaffold(\n";
        code += "\t\t\t\t\t\t\tbody: Center(child: Text('Loading...')),\n";
        code += "\t\t\t\t\t\t),\n";
        code += "\t\t\t\t\t);\n";
        code += "\t\t\t\t}\n";
        code += "\t\t\t\treturn snapshot.data!;\n";
        code += "\t\t\t},\n";
        code += "\t\t);\n";
        code += "\t}\n";
        code += "}\n\n";

        return code;
    }

    private string GetMainAppCode(string title)
    {
        string code = "class MainApp extends StatefulWidget {\n";
        code += "\tconst MainApp({super.key});\n\n";
        code += "\t@override\n";
        code += "\tState<MainApp> createState() => _MainAppState();\n";
        code += "}\n\n";

        code += "class _MainAppState extends State<MainApp> {\n";
        code += "\t@override\n";
        code += "\tWidget build(BuildContext context) {\n";
        code += "\t\treturn MaterialApp(\n";
        code += "\t\t\tinitialRoute: '/main_menu',\n";
        code += "\t\t\tonGenerateInitialRoutes: (initialRoute) =>"
                + " [MainRouter.route(RouteSettings(name: initialRoute))!],\n";
        code += "\t\t\tonGenerateRoute: MainRouter.route,\n";
        code += "\t\t\ttitle: '" + title + "'\n";
        code += "\t\t);\n";
        code += "\t}\n";
        code += "}\n\n";
        return code;
    }

    private string GetMainRouterCode()
    {
        string code = "class MainRouter {\n";
        code += "\tstatic Route<dynamic>? route (RouteSettings routeSettings) {\n";
        code += "\t\treturn MaterialPageRoute<void>(\n";
        code += "\t\t\tsettings: routeSettings,\n";
        code += "\t\t\tbuilder: (context) => _UseCaseWidget(\n";
        code += "\t\t\t\t(context) async{ return context.read<IUCStart>().selectApplication(context);},\n";
        code += "\t\t\t),\n";
        code += "\t\t);\n";
        code += "\t}\n";
        code += "}\n\n";
        return code;
    }

    private string GetUseCaseWidgetCode()
    {
        string code = "class _UseCaseWidget extends StatefulWidget {\n";
        code += "\tfinal void Function(BuildContext) useCaseCall;\n";
        code += "\tconst _UseCaseWidget(this.useCaseCall, {super.key});\n";
        code += "\t@override\n";
        code += "\tState<_UseCaseWidget> createState() => _UseCaseWidgetState();\n";
        code += "}\n\n";
        
        code += "class _UseCaseWidgetState extends State<_UseCaseWidget> {\n";
        code += "\tlate Future<void> _callFirst;\n";
        code += "\t@override\n";
        code += "\tvoid initState() {\n";
        code += "\t\tsuper.initState();\n";
        code += "\t\t_callFirst = _initAsync();\n";
        code += "\t}\n\n";
        
        code += "\tFuture<void> _initAsync() async {\n";
        code += "\t\tWidgetsBinding.instance.addPostFrameCallback((_) {\n";
        code += "\t\t\tLogger.root.fine(\"[postFrameCallback] Calling 'start' use case...\");\n";
        code += "\t\t\twidget.useCaseCall(context);\n";
        code += "\t\t});\n";
        code += "\t}\n\n";
        
        code += "\t@override\n";
        code += "\tWidget build(BuildContext context) {\n";
        code += "\t\treturn FutureBuilder(\n";
        code += "\t\t\tfuture: _callFirst,\n";
        code += "\t\t\tbuilder: (context, snapshot) => Scaffold(\n";
        code += "\t\t\t\tbody: Align(\n";
        code += "\t\t\t\t\talignment: Alignment.center,\n";
        code += "\t\t\t\t\tchild: const Text('Loading...',\n";
        code += "\t\t\t\t\t\toverflow: TextOverflow.ellipsis,\n";
        code += "\t\t\t\t\t),\n";
        code += "\t\t\t\t),\n";
        code += "\t\t\t),\n";
        code += "\t\t);\n";
        code += "\t}\n";
        code += "}\n\n";

        return code;
    }

    public string GetHeaderCode(Operation element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        bool hasFuture = element is not POperation;
        // Example: Future<XDataset> getDataset(String identifier)
        string code = ts + (hasFuture ? "Future<" : "") +
                      (null == element.ReturnType ? hasFuture ? "void" : "" 
                          : GetTypeName(element.ReturnType,false,
                              element.ReturnType is DataTransferObject { CanBeMap: true })) +
                      (hasFuture ? "> " : "") + element.GetElemName() + GetParametersCode(element);
        return code;
    }

    public CodeFile? ToCodeFile(CodeUnit element, string basePath)
    {
        if (element is DataTransferObjectUnit or ViewModelUnit or DataTransferObject { IsAuxiliaryCollection: true }
            or DataTransferObject { IsSimple: true } or DataTransferObject {CanBeMap: true})
            return null;
        return new CodeFile{
            Path = GetRootPath(true) + @"/" + basePath + @"/" + GetFileName(element) + ".dart",
            CodeContents = element.ToCode()
        };
    }
    
    public string MapPrimitiveType(PrimitiveType type)
    {
        return type switch {
            PrimitiveType.Integer => "int",
            PrimitiveType.Number or PrimitiveType.Float => "double",
            PrimitiveType.String => "String",
            PrimitiveType.Boolean => "bool",
            PrimitiveType.Time or PrimitiveType.Date or PrimitiveType.DateTime => "DateTime",
            PrimitiveType.Location => "(double,double)",
            _ => ""
        };
    }

    public IEnumerable<CodeFile> GetAuxiliaryFiles()
    {
        List<CodeFile> files = [];
        string code = "import 'package:flutter/material.dart';\n";
        code += "import 'package:provider/provider.dart';\n\n";
        code += "abstract class AbstractPresenter {\n\n";
        code += "\tfinal String routingNamePrefix;\n";
        code += "\tString getRoutingName() => routingNamePrefix;\n\n";
        code += "\tconst AbstractPresenter(this.routingNamePrefix);\n\n";
        code += "\tpushWindow(BuildContext context, Widget body) {\n";
        code += "\t\tNavigator.push(\n";
        code += "\t\t\t\tcontext,\n";
        code += "\t\t\t\tMaterialPageRoute(\n";
        code += "\t\t\t\t\tbuilder: (context) => body,\n";
        code += "\t\t\t\t\tsettings: RouteSettings(\n";
        code += "\t\t\t\t\t\tname: getRoutingName(),\n";
        code += "\t\t\t\t\t)),\n";
        code += "\t\t);\n";
        code += "\t}\n\n";
        code += "\tpopWindow(BuildContext context) => Navigator.pop(context);\n";
        code += "}\n";
        
        files.Add(new CodeFile{
            Path = $@"{GetRootPath(true)}/view/presenters/AbstractPresenter.dart",
            CodeContents = code 
        });
        return files;
    }

    public string GetMainFileName()
    {
        return "main.dart";
    }

    // ==== PRIVATE METHODS ====================================================================
    // =========================================================================================
    
    private string GetRootPath(bool forPath = false)
    {
        return ""; // throw new NotImplementedException();
    }
    
    private string GetBaseCode(ResultEnumeration element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        // Example: enum ClientEnumCheck {
        string code = ts + "enum " + element.GetElemName() + " {\n";
        //            >>>values<<<
        Value? okValue = element.Values.FirstOrDefault(v => "OK" == v.Name.ToUpper());
        if (null != okValue) code += ts + "\t" + okValue.GetElemName() + (1 < element.Values.Count ? "," : "") + "\n";
        code += string.Join(",\n", element.Values.Where(v => "OK" != v.Name.ToUpper())
            .OrderBy(v => v.Name)
            .Select(v => ts + "\t" + v.GetElemName())) + "\n";
        // Example: }
        code += ts + "}\n";
        return code;
    }

    private string GetCode(DataItem element, int tabs = 0, bool forStoringSelection = false)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts;
        bool isDTOReferenceField = element.IsReferenceField;
        bool isMultiple = !forStoringSelection && element.IsCollection;
        string typeString = GetTypeName(element, forStoringSelection);
        string varString = forStoringSelection ? element.Type.GetVarName() : element.GetVarName();
        code += typeString + (element.Optional || forStoringSelection ? "? " : " ") + varString 
                + (isDTOReferenceField ? isMultiple ? "Map" : "Id" : "") + " = ";
        if (forStoringSelection) code += "null";
        else if (element.Type is Primitive || element is { Multiple: false, Type: DataTransferObject { IsSimple: true } dto }
                && dto.Fields[0].Type is Primitive) {
            Primitive prim = element.Type as Primitive ?? (Primitive)((DataTransferObject) element.Type).Fields[0].Type;
            // Example 1: String name = ""
            // Example 2: List<String> names = {}
            if (!element.Multiple || forStoringSelection)
                switch (prim.Value) {
                    case PrimitiveType.Integer:
                        code += "0";
                        break;
                    case PrimitiveType.Number:
                    case PrimitiveType.Float:
                        code += "0";
                        break;
                    case PrimitiveType.String:
                        code += "\"\"";
                        break;
                    case PrimitiveType.Boolean:
                        code += "false";
                        break;
                    case PrimitiveType.Time:
                    case PrimitiveType.Date:
                    case PrimitiveType.DateTime:
                        code += "null";
                        break;
                    case PrimitiveType.Location:
                        code += "(0,0)";
                        break;
                }
            else code += element.CanBeSimplified ? "{}" : "[]";
        } else if (isDTOReferenceField) code += isMultiple ? "{}" : "\"\"";
        // Example 1: User user = User()
        // Example 2: List<User> users = {}
        else code += element is { Multiple: false, Type: not DataTransferObject { IsAuxiliaryCollection: true } } || forStoringSelection 
                ? typeString + "()"
                : element.CanBeSimplified ? "{}" : "[]";
        
        if (!forStoringSelection && isDTOReferenceField && !isMultiple) 
            code += ";\n" + ts + typeString + (element.Optional ? "? " : " ") + varString + "Label = \"\"";
        return code + ";";
    }

    private string GetCode(Parameter element, bool useIdentifier = false, bool var = false, int i = 0){
        string stringDesignation = MapPrimitiveType(PrimitiveType.String);
        string code = var 
            ? "" 
            : element.CanBeSimplified ? "Map<" + stringDesignation +  ", " + stringDesignation + "> " 
                : (element.CanBeIdentifier && useIdentifier ? stringDesignation : GetTypeName(element)) + " ";
        if (element.Type is ResultEnumeration) code += "result";
        else code += (element.CanBeIdentifier && useIdentifier && !var 
            ? namingConverter.GetGenericVarName("identifier") 
            : element.GetVarName()) + (i > 0 ? i.ToString() : "");
        if (element.CanBeIdentifier && useIdentifier && var)
            code += "." + namingConverter.GetGenericVarName("identifier") + "!";
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
        // Example: if (ClientEnumCheck.Valid == clientEnumCheck) {
        string code = "if (" + string.Join(" && ", element.Expressions.Select(GetCode)) + ") {\n";
        //            >>>instructions<<<
        foreach (Instruction instr in element.Instructions)
            code += GetCode((dynamic)instr, tabs + 1) + "\n";
        // Example: }
        code += ts + "}";
        return code;
    }
    
    private string GetCode(Expression element)
    {
        return element.ToBeChecked == null ? 
            // Example: ClientEnum.OK == result
            GetTypeName(element.Value!.Parent!) + "." + element.Value!.GetElemName() + " == " +  "result"
            // Example: ClientEnumCheck.Valid == clientEnumCheck
            : GetTypeName(element.ToBeChecked.Enumeration!) + "." + element.Value!.GetElemName() +
              " == " + element.ToBeChecked.Enumeration!.GetVarName();
    }

    private string GetCode(End element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        // Example: if (null != _returnFunction)
        string code = ts + "if (null != _returnFunction)\n";
        // Example:   _returnFunction(context, ShowClientListResultEnum.OK);
        code += ts + "\t_returnFunction!(context, " + element.Value!.Parent!.GetElemName() + "." + element.Value.GetElemName() + ");";
        return code;
    }
    
    // Return type declaration for the given DataItem (considers collections)
    // forceSingle - forces to return single type name even if the element is "Multiple"
    private string GetTypeName(DataItem element, bool forceSingle = false)
    {
        string baseTypeName = element.Type is DataTransferObject dto
            ? GetTypeName(dto, forceSingle, element is Field)
            : element.Type.GetElemName();
        if (!element.Multiple || forceSingle)
            return baseTypeName;
        return element is Field && element.Type is DataTransferObject { IsSimple: false }
            ? "Map<" + baseTypeName + ", " + baseTypeName + ">"
            : "List<" + baseTypeName + ">";
    }
    
    // Return type declaration for the given DataItemType (considers collections)
    // forceSingle - forces to return single type name even if it is collection
    // canBeMap - indicates that the type can be simplified to a map if it is a non-simple DTO
    private string GetTypeName(DataItemType element, bool forceSingle = false, bool canBeMap = false)
    {
        if (element is not DataTransferObject dto) return element.GetElemName();
        if (dto.IsSimple)
            return dto.Fields[0].Type.GetElemName();
        if (canBeMap) {
            string stringDesignation = MapPrimitiveType(PrimitiveType.String);
            // TODO - recursively check in case it is a collection
            return dto.IsAuxiliaryCollection ? "Map<" + stringDesignation + ", " + stringDesignation + ">" : stringDesignation;
        }
        if (dto.IsAuxiliaryCollection)
            return (forceSingle ? "" : "List<") + dto.Fields[0].Type.GetElemName() + (forceSingle ? "" : ">");
        return element.GetElemName();
    }

    private string GetViewFactoryCode(View view, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        // Example: class VUpsertDatasetWindowFactory {
        string code = ts + "class " + view.GetElemName() + "Factory {\n";
        // Example:    const VUpsertDatasetWindowFactory();
        code += ts + "\tconst " + view.GetElemName() + "Factory();\n";
        // Example:    VUpsertDatasetWindow get(key, model) {
        code += ts + "\t" + view.GetElemName() + " get(key, model) {\n";
        // Example:        return VUpsertDatasetWindow(key, new CUpsertDatasetWindow(model), model);
        code += ts + "\t\treturn " + view.GetElemName() + "(key, new " + view.Controller.GetElemName() + "(model), model);\n";
        // Example:     }
        code += ts + "\t}\n";
        // Example: }
        code += ts + "}\n";
        return code;
    }
    
    // ==== HELPERS ===================================================================================
    // ================================================================================================
    
    private string ToFullCode(Call element, int tabs = 0, bool var = false){
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts;
        // Example: this.pClientListWindow.showUpdatedClientListWnd(list);
        if (element.CalledOperation is SOperation op) {
            if (!var && op.Type is PredicateType.Read or PredicateType.Check) {
                code += element.EnclosingOperation.Uc!.State.Exists(
                    dto => op.ReturnType is DataTransferObject rdto && dto.Name.NamingEquals(rdto.Name))
                    ? "this." 
                    : GetTypeName(op.ReturnType!) + " ";
                code += op.ReturnType!.GetVarName() + " = "; // TODO - repeated return variable names
            }
            code += "await " + op.Service!.GetVarName();
        } else if (element.CalledOperation is POperation pop)
            code += pop.Pres!.GetVarName();
        else if (element.CalledOperation is UCOperation ucop)
            code += "await context.read<" + ucop.Uc!.GetInterfaceName() + ">()";
        else throw new Exception("Critical compilation failure");
        code += "." + element.CalledOperation.GetElemName() + GetParametersCode(element.CalledOperation, true);
        if (var) code += null == element.Value ? "" : " == " + element.Value.Parent!.GetElemName() + "." + 
                                                      element.Value.GetElemName();
        else code += ";";
        return code;
    }
    
    private string ToVarCode(Call element, int tabs = 0)
    {
        return ToFullCode(element, tabs, true);
    }
    
    private string ToVarCode(Parameter element, bool useIdentifier = false, int i = 0)
    {
        // string code = element.HasAttribute && (!element.CanBeIdentifier || !useIdentifier) ? "this." : "";

        string code = element is { HasAttribute: true, CanBeIdentifier: false } && useIdentifier ? "this." : "";
        return code + GetCode(element, useIdentifier, true, i);
    }
    
    private string ToVarCode(DataTransferObject element, int tabs = 0, bool isUpdateable = false, bool isReference = false)
    {
        string ts = Common.Utils.GetTabString(tabs);

        string typeString = element.IsSimple 
            ? GetTypeName(element.Fields[0]) 
            : GetTypeName(element, false, isReference);
        string code = ts + typeString + " " + element.GetVarName() + " = ";
        // Example: ScreenIdEnum screen = ScreenIdEnum.START
        if ("ScreenIdEnum" == typeString) code += typeString + ".START";
        else code += element.IsAuxiliaryCollection  ? (element.CanBeMap ? "{}" : "[]") : typeString + "()";
        code += ";";
        
        if (isUpdateable && element.Fields.Exists(di => di.Multiple))
            // This code is for storing user selection(s) on the list
            foreach (Field di in element.Fields)
                if (di is { CanBeSimplified: false, Multiple: true, Type: DataTransferObject })
                    code += "\n" + GetCode(di, tabs, true);
        return code;
    }

    private string GetUnionMapOperation(ResultUnionEnumeration unionEnum, int tabs)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "static " + GetTypeName(unionEnum) + " map(Object value) {\n";
        code += ts + "\tswitch(value) {\n";
        foreach (SimpleResultEnumeration senum in unionEnum.Members) {
            code += ts + "\t\tcase " + GetTypeName(senum) + " ivalue:\n";
            code += ts + "\t\t\tswitch(ivalue) {\n";
            foreach (Value v in senum.Values)
                code += ts + "\t\t\t\tcase " + GetTypeName(senum) + "." + v.GetElemName() + ":\n" + 
                        ts + "\t\t\t\t\treturn " + GetTypeName(unionEnum) + "." + v.GetElemName() + ";\n";
            code += ts + "\t\t\t}\n";
        }
        code += ts + "\t}\n";
        code += ts + "\tthrow ArgumentError('Unsupported type');\n";
        code += ts + "}";
        return code;
    }

    // Build the list of unique return types of a Service
    private List<DataTransferObject> GetUniqueReturnTypes(Service service)
    {
        List<DataTransferObject> uniqueReturnTypes = [];
        foreach (DataTransferObject dto in service.Operations
                     .Select(o => o.ReturnType).OfType<DataTransferObject>()) {
            foreach (DataTransferObject fdto in dto.ActualFields
                         .Select(f => f.Type).OfType<DataTransferObject>()
                         .Where(f => !f.IsSimple))
                if (!uniqueReturnTypes.Any(t => t.Name.NamingEquals(fdto.Name)))
                    uniqueReturnTypes.Add(fdto);
        }
        return uniqueReturnTypes;
    }

    // === VIEW HELPERS =======================================================================================
    
    // NOTE #1: when applying indent inside indent (or widget inside widget), inner element(s) should have '0' tabs
    // NOTE #2: you may use Indent for indenting lists and other iterable elements
    private string Indent(string text, int level)
    {
        var prefix = new string('\t', level);
        return string.Join("\n", text.Split('\n').Select(line => prefix + line));
    }
    
    private string Widget(string name, bool startAlignment = true, bool mainAxisCenterAlignment = false, 
        int tabs = 0, string[]? children = null, string? child = null, string? mainAxisSize=null, string? padding=null,
        string? additionalProperties=null)
    // TODO - make additionalProperties more structured (e.g. dictionary)
        => name + "(\n" 
           + Indent((null != padding ? "\tpadding: " + padding + ",\n" : null)
                    + (startAlignment ? "\tcrossAxisAlignment: CrossAxisAlignment.start,\n" : null)
                    + (mainAxisCenterAlignment ? "\tmainAxisAlignment: MainAxisAlignment.center,\n" : null)
                    + (null != mainAxisSize ? "\tmainAxisSize: MainAxisSize." + mainAxisSize + ",\n" : null)
                    + (null != additionalProperties ? additionalProperties + ",\n" : null)
                    + (null != children
                        ? "\tchildren: [\n" +
                        $"{Indent(string.Join(",\n", children), 2)}\n" +
                        "\t]\n)"
                        : null != child 
                            ? "\tchild: " + child + "\n)" 
                            : throw new ArgumentException("Either children or child should be provided for a widget")), 
               tabs);

    private string Column(bool startAlignment = true, int tabs = 0, string? mainAxisSize=null, params string[] children)
        => Widget("Column", startAlignment, false, tabs, children, mainAxisSize: mainAxisSize);

    private string Row(int tabs = 0, bool mainAxisCenterAlignment = false, params string[] children)
        => Widget("Row", true, mainAxisCenterAlignment, tabs, children);

    private string DoorText(string text, string style, int tabs = 0)
        => "DoorText(\n"
           + $"\t{text},\n"
           + $"\t{style}\n" 
           + ")";

    private string DoorTextFormField(string onChanged, bool isEnabled = true, 
        string? initialValue = null, string typeValidated = "String", string? key = null, string? autovalidateMode = null,
        bool? isLocation=false)
        => $"DoorTextFormField<{(typeValidated is "string" ? typeValidated.ToPascalCase() : typeValidated)}>(\n"
           + (null != key ? $"\tkey: {key},\n" : null)
           + (null != autovalidateMode ? $"\tautovalidateMode: AutovalidateMode.{autovalidateMode},\n" : null)
           + (isLocation == true ? $"\tisLocation: true,\n" : null)
           + $"\tenabled: {isEnabled.ToString().ToLowerInvariant()},\n"
           + $"\tonChanged: {onChanged},\n"
           + (null != initialValue ? $"\tinitialValue: {initialValue}\n" : null)
           + ")";

    private string SizedBox(int? width = null, int? height = null, string? child = null)
        => "SizedBox(\n"
           + (width  != null ? $"\twidth: {width},\n"  : "")
           + (height != null ? $"\theight: {height},\n" : "")
           + (child  != null ? $"\tchild: {child}\n" : "")
           + ")";
    
    private string DoorMultiDropdown(string items, string onSelectionChange, string type = "String", 
        bool singleSelect = false, bool enabled = true, string? validator = null)
        => $"DoorMultiDropdown<{type}>(\n"
           + $"\titems: {items},\n"
           + $"\tsingleSelect: {singleSelect.ToString().ToLowerInvariant()},\n"
           + $"\tenabled: {enabled.ToString().ToLowerInvariant()},\n"
           + $"\tonSelectionChange: {onSelectionChange},\n"
           + (null != validator ? $"\tvalidator: {validator}\n" : null)
           + ")";

    private string DoorTextFormFieldWithChips(string initialItems, string onChipAddedOrRemoved,
        bool isEnabled = true, string typeValidated = "String", string? key = null, bool? isLocation=false) {

        string type = typeValidated is "string" ? typeValidated.ToPascalCase() : typeValidated;
        
        return $"DoorTextFormFieldWithChips<{type}>(\n"
               + (null != key ? $"\tkey: {key},\n" : null)
               + (isLocation == true ? $"\tisLocation: true,\n" : null)
               + $"\titems: {initialItems},\n"
               + $"\tonChipAddedOrRemoved: {onChipAddedOrRemoved},\n"
               + $"\tenabled: {isEnabled.ToString().ToLowerInvariant()},\n"
               + ")";
    } 
    
    private string DoorLocationPicker(string userAgent, string onLocationPicked, bool isEnabled = true)
        => "HeroMode(\n"
           + "\tenabled: false,\n"
           + "\tchild: DoorLocationPicker(\n"
           + $"\t\tuserAgent: '{userAgent}',\n"
           + $"\t\tonLocationPicked: {onLocationPicked},\n"
           + $"\t\tisEnabled: {isEnabled.ToString().ToLowerInvariant()}\n"
           + "\t)\n"
           + ")";
    
    private string DoorCheckbox(string value, string onChanged, bool isEnabled = true)
        => "DoorCheckbox(\n"
           + $"\tvalue: {value},\n"
           + $"\tonChanged: {onChanged},\n"
           + $"\tisEnabled: {isEnabled.ToString().ToLowerInvariant()}\n"
           + ")";
    
    // mode is either "date", "time" or "dateTime"
    private string DoorDateTimePicker(string mode, string initialValue, string onChanged, bool isEnabled = true)
        => "DoorDateTimePicker(\n"
           + $"\tmode: DoorDateTimeMode.{mode},\n"
           + $"\tinitialValue: {initialValue},\n"
           + $"\tonChanged: {onChanged},\n"
           + $"\tisEnabled: {isEnabled.ToString().ToLowerInvariant()}\n"
           + ")";

    private string GetViewHelperMethods(int tabs = 0)
    {
        var ts = Common.Utils.GetTabString(tabs);
        var code = ts + "String capitalize(String s) =>\n";
        code += ts + "\ts.isEmpty ? s : s[0].toUpperCase() + s.substring(1);";
        code += ts + "\n\n";
        code += ts + "String humanizeKey(String key) {\n";
        code += ts + "\tkey = key.replaceAll('_', ' ');\n";
        code += ts + "\tkey = key.replaceAllMapped(\n";
        code += ts + "\t\tRegExp(r'([a-z])([A-Z])'),\n";
        code += ts + "\t\t(m) => '${m.group(1)} ${m.group(2)}',\n";
        code += ts + "\t);\n";
        code += ts + "\treturn capitalize(key);\n";
        code += ts + "}";
        return code;
    }

    private string GetButtons(String? itemName, IEnumerable<Trigger> buttons, int tabs = 0)
    {
        // List<(string button name, function, bool is enabled)>
        List<List<string>> buttonList = buttons
            .Select(b 
                => new List<string> {"'" + (b.Label ?? b.Name) + "'", 
                    $"() => widget._controller." +
                    $"{b.Action!.GetElemName()}(context{(null != itemName ? ", " + itemName : "")})", 
                    null != b.Condition 
                        ? $"widget._controller." +
                          $"{b.Condition.GetElemName()}(context{(null != itemName ? ", " + itemName : "")})"
                        : "true"})
            .ToList();
        
        var ts = Common.Utils.GetTabString(tabs);
        
        string code = "DoorButtonColumn([\n";
        code += ts + "\t(\n\t";
        code += Indent(string.Join("\n),\n(\n\t", buttonList.Select(t 
            => Indent(string.Join(",\n\t", t), 0))), tabs+1) + "\n";
        code += ts + "\t)\n";
        code += ts + "])";
        return code;
    }
    
    private string GetListBuilderCode(string listVarName, /*DataTransferObject listItemDto,*/
        IEnumerable<Trigger> buttons, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = "Expanded(child: ListView.builder(\n";
        code += ts + "\tpadding: const EdgeInsets.only(top: 16, left: 16, right: 16, bottom: 80),\n";
        code += ts + $"\titemCount: widget._model.{listVarName}.length,\n";
        code += ts + "\titemBuilder: (context, index) {\n";
        code += ts + "\t\tvar item = widget._model." + listVarName + "[index];\n";
        code += ts + "\t\tLogger.root.fine(\"[itemBuilder] Building list item: ${item.toLabel(false)}\");\n";

        code += ts +
                "\t\tvar summary = item.toLabel(true);\n";

        code += ts + "\t\tLogger.root.info(\"[itemBuilder] Item.toJson(): ${item.toJson()}\");\n";
        code += ts + "\t\tLogger.root.info(\"[itemBuilder] Item.toJson().entries: ${item.toJson().entries}\");\n";
        code += ts + "\t\tLogger.root.info(\"[itemBuilder] Item.toJson() humanizeKey: ${item.toJson().entries.map((e) => humanizeKey(e.key)).join(' | ')}\");\n";
        
        code += ts + "\t\tLogger.root.fine(\"[itemBuilder] Built list item summary: ${summary}\");\n";

        code += ts + "\t\tvar rightButtons = Center(\n";
        code += ts + $"\t\t\tchild: {GetButtons("item", buttons, tabs + 3)}\n";
        code += ts + "\t\t);\n";
        
        code += ts + $"\t\treturn {Column(startAlignment: false, tabs:tabs+2, children:[
            Row(tabs: 0, children:[
                Widget("Expanded", false, child:
                        DoorText("summary", "DoorTextStyle.emph")
                ), // Expanded
                "rightButtons"
            ]), // Row
            "const Divider()"
        ])};\n"; // Column

        code += ts + "\t},\n"; //itemBuilder
        code += ts + "))"; //ListView.builder //Expanded
        
        return code;
    }
    
    private string GenerateColumn(View element, /*bool isEditable,*/ int tabs)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = "Column(\n";
        code += ts + "\tspacing: 16,\n";
        code += ts + "\tcrossAxisAlignment: CrossAxisAlignment.start,\n";
        code += ts + "\tchildren: [\n";

        // Code for the content of the view (based on the view model and triggers)
        foreach (DataTransferObject dto in element.ViewModel.OutputData)
            if (dto.IsAuxiliaryCollection) {
                string listVarName = dto.GetVarName();
                code += ts + "\t\t" + GetListBuilderCode(listVarName,
                    /*dto.Fields[0].Type as DataTransferObject ?? throw new Exception("Critical error"),*/ 
                    element.Triggers.Where(t => t.Action!.TransferredData
                        .Exists(td => dto.Fields[0].Type == td)),
                    tabs + 2) + ",\n";
            } else if (element.ViewModel.InputData.All(d => d != dto))
                code += ts + "\t\t" + GetFormFields(dto, 
                    element.Triggers, // TODO 
                    /*false,*/ true, tabs + 2);
        
        foreach (DataTransferObject dto in element.ViewModel.InputData)
            if (!dto.IsAuxiliaryCollection)
                code += ts + "\t\t" + GetFormFields(dto, element.Triggers, // TODO
                    /*element.ViewModel.OutputData.All(d => d != dto),*/ false, // TODO
                    tabs + 2) + "\n";
        
        code += ts + "\t],\n";
        code += ts + ")\n";
        return code;
    }

    private string GetFormFields(DataTransferObject dto, IEnumerable<Trigger> buttons, /*bool isInsert,*/ bool readOnly, 
        int tabs = 0)
    {
        //if (isInsert && readOnly) throw new Exception("Critical error: form cannot be both insert and read-only");
        
        // create form title
        var ts = Common.Utils.GetTabString(tabs);

        var title = Row(
            children:
            [
                DoorText($"humanizeKey('{dto.GetVarName()}')", "DoorTextStyle.titleBlack"),
            ]
        );
        
        // create form fields
        var formFields = dto.Fields
            .Where(f => !f.IsItemIdentifier)
            .Select(f =>
            Row(
                children:
                [
                    Widget("Flexible", false, child:
                        Column(tabs:1, children: [
                            DoorText($"humanizeKey('{f.GetVarName()}')", "DoorTextStyle.label"),
                            GetFormField(dto, f, readOnly/*, isInsert*/),
                            SizedBox(height: 12)
                            ]
                        )
                    )
                ]
            )
        ).ToArray();
        
        var formChildren = new List<string>();
        formChildren.Add(title);
        formChildren.Add(SizedBox(height: 12));
        formChildren.AddRange(formFields);
        
        var code = Widget("Expanded", false, tabs: tabs, child:
            Widget("ListView", false, 
                padding: "const EdgeInsets.only(bottom: 80)",
                tabs: 1,
                children: formChildren.ToArray())) + ",\n";
        
        // create buttons
        var buttonBoxes = buttons.Select(b
            => SizedBox(
                196,
                48,
                "DoorButtonLoading(\n" +
                "\t\t'" + (b.Label ?? b.Name) + "',\n" +
                "\t\t() async {\n" +
                "\t\t\tif (_formKey.currentState!.validate()) {\n" +
                "\t\t\t\tawait widget._controller." + $"{b.Action!.GetElemName()}(context);\n" +
                "\t\t\t} else {\n" +
                "\t\t\t\tSnackMessage().show(context, true, text: 'Errors in the form - check above');\n" +
                "\t\t\t}\n" +
                "\t\t},\n" +
                "\t\tenabled: " + (readOnly 
                    ? (!readOnly).ToString().ToLowerInvariant() 
                    : null != b.Condition
                        ? $"widget._controller." +
                          $"{b.Condition.GetElemName()}(context)"
                        : "true") + ",\n" +
                "\t)"
                )
            ).ToArray();
        
        code += ts + $"Center(\n";
        code += ts + $"\tchild: ";
        code += Row(tabs + 1, true, buttonBoxes) + "\n";
        code += ts + ")";

        return code;
    }
    
    private string GetFormField(DataTransferObject dto, Field field, bool readOnly/*, bool isInsert*/) {
        return field.Type switch
        {
            DataTransferObject { IsSimple: false } => HandleDtoFormField(dto, field, readOnly), 
            DataTransferObject { IsSimple: true } d => 
                HandlePrimitiveFormField((Primitive)d.Fields[0].Type, dto, field, readOnly/*, isInsert*/),
            Primitive p => HandlePrimitiveFormField(p, dto, field, readOnly/*, isInsert*/),
            _ => ""
        };
    }

    private string HandleDtoFormField(DataTransferObject dto, Field field, bool readOnly) {
        var model = GetModelField(dto, field);
        var options = $"widget._model.{field.Type.GetVarName()}Options";

        return !field.IsCollection
            ? DoorMultiDropdown(
                items: $"getDropdownOptionsSingle({options},\n\t\t{model}Id)",
                singleSelect: true,
                onSelectionChange:
                    "(values) {\n" +
                    "\t\tsetState(() {\n" +
                    $"\t\t\t{model}Id = values.firstOrNull ?? '';\n" +
                    "\t\t});\n" +
                    "\t}",
                enabled: !readOnly
            )
            : DoorMultiDropdown(
                items: $"getDropdownOptionsCollection({options},\n\t\t{model}Map)",
                singleSelect: false,
                onSelectionChange:
                    "(values) {\n" +
                    "\t\tsetState(() {\n" +
                    $"\t\t\t{model}Map = Map.fromEntries({options}.entries.where((o) => values.contains(o.key)));\n" +
                    "\t\t});\n" +
                    "\t}",
                enabled: !readOnly
            );
    }

    private string HandlePrimitiveFormField(Primitive p, DataTransferObject dto, Field field, bool readOnly/*, bool isInsert*/) {
        var parts = new List<string>();
        var modelField = GetModelField(dto, field);

        if (field.Multiple) {
                parts.Add(DoorTextFormFieldWithChips(
                    key: p.Value == PrimitiveType.Location ? $"ValueKey({modelField}?.map((loc) => \"${{loc.$1}},${{loc.$2}}\").join(\";\"))" : null,
                    initialItems: modelField,
                    onChipAddedOrRemoved: GetOnChanged(dto, field, textField: true),
                    isEnabled: !readOnly,
                    typeValidated: GetTypeName(field.Type, forceSingle: true),
                    isLocation: p.Value == PrimitiveType.Location
                ));
        } else if (p.Value != PrimitiveType.Boolean && p.Value != PrimitiveType.DateTime && p.Value != PrimitiveType.Date && p.Value != PrimitiveType.Time) {
            parts.Add(DoorTextFormField(
                    key: p.Value == PrimitiveType.Location ? $"ValueKey({modelField}.toString())" : null,
                    autovalidateMode: "onUserInteraction",
                    onChanged: GetOnChanged(dto, field, textField: true),
                    initialValue: modelField,
                    isEnabled: !readOnly,
                    typeValidated: GetTypeName(field.Type, forceSingle: true),
                    isLocation: p.Value == PrimitiveType.Location));
        }
            
        parts.Add(SizedBox(height: 8));

        if (field.Multiple && (p.Value == PrimitiveType.Boolean || p.Value == PrimitiveType.DateTime || p.Value == PrimitiveType.Date || p.Value == PrimitiveType.Time)) {
            return string.Join(",\n", parts.Where(part => !string.IsNullOrEmpty(part)));
        }
        
        parts.Add(p.Value switch
            {
                PrimitiveType.Location => DoorLocationPicker(
                            userAgent: "UdasApp/1.0.0 (michal.smialek@pw.edu.pl)",
                            onLocationPicked: GetOnChanged(dto, field),
                            isEnabled: !readOnly
                            ),
            
                PrimitiveType.Boolean => DoorCheckbox(
                    value: modelField,
                    onChanged: GetOnChanged(dto, field),
                    isEnabled: !readOnly
                ),

                PrimitiveType.Date or PrimitiveType.DateTime or PrimitiveType.Time
                    => DoorDateTimePicker(
                        mode: GetDatePickerMode(p.Value),
                        initialValue: field.Multiple ? "null" : modelField,
                        onChanged: GetOnChanged(dto, field),
                        isEnabled: !readOnly
                    ),
                _ => ""
            });
        return string.Join(",\n", parts.Where(part => !string.IsNullOrEmpty(part)));
    }

    private static string GetDatePickerMode(PrimitiveType type) => type switch
    {
        PrimitiveType.Date => "date",
        PrimitiveType.Time => "time",
        PrimitiveType.DateTime => "dateTime",
        _ => throw new Exception("Critical error")
    };

    private static string GetModelField(DataTransferObject dto, Field field) =>
        $"widget._model.{dto.GetVarName()}.{field.GetVarName()}";
    
    private string GetOnChanged(DataTransferObject dto, Field field, bool textField = false)
    {
        if (field.Type is not Primitive p)
            throw new ArgumentException("Type validation is only supported for primitive types");

        var modelField = GetModelField(dto, field);

        if (p.Value == PrimitiveType.Location && !textField) {
            if (field.Multiple)
            {
                return "(picked) {\n" +
                       $"\t\tif (null == {modelField}) {{\n" +
                       $"\t\t\t{modelField} = [];\n" +
                       "\t\t}\n" +
                       "\t\tsetState(() {\n" +
                       $"\t\t\t{modelField}!.add((picked.latLong.latitude, picked.latLong.longitude));\n" +
                       "\t\t});\n" +
                       "\t}";
            }

            return "(picked) {\n" +
                   "\t\tsetState(() {\n" +
                   $"\t\t\t{modelField} = (picked.latLong.latitude, picked.latLong.longitude);\n" +
                   "\t\t});\n" +
                   "\t}";
        } 
        
        return "(value) {\n" +
               "\t\tsetState(() {\n" +
               $"\t\t\t{modelField} = value;\n" +
               "\t\t});\n" +
               "\t}";
        
    }
    
    private string GetViewModelHelperMethods(int tabs = 0)
    {
        var ts = Common.Utils.GetTabString(tabs);

        var code = ts + "List<DropdownItem<String>> getDropdownOptionsCollection(" + "Map<String, String> optionMap,\n";
        code += ts + "\tMap<String,String> dtoFieldMap) {\n";
        code += ts + "\treturn optionMap.entries.map((idLabel) {\n";
        code += ts + "\t\treturn DropdownItem<String>(\n";
        code += ts + "\t\t\tvalue: idLabel.key ?? '',\n";
        code += ts + "\t\t\tlabel: idLabel.value ?? '',\n";
        code += ts + "\t\t\tselected: dtoFieldMap.isNotEmpty ? dtoFieldMap.keys.contains(idLabel.key) : false\n";
        code += ts + "\t\t);\n";
        code += ts + "\t}).toList();\n";
        code += ts + "}\n\n";
        
        code += ts + "List<DropdownItem<String>> getDropdownOptionsSingle(" + "Map<String, String> optionMap,\n";
        code += ts + "\tString? selectedId) {\n";
        code += ts + "\treturn optionMap.entries.map((idLabel) {\n";
        code += ts + "\t\treturn DropdownItem<String>(\n";
        code += ts + "\t\t\tvalue: idLabel.key ?? '',\n";
        code += ts + "\t\t\tlabel: idLabel.value ?? '',\n";
        code += ts + "\t\t\tselected: idLabel.key == selectedId\n";
        code += ts + "\t\t);\n";
        code += ts + "\t}).toList();\n";
        code += ts + "}";
        return code;
    }
    
    // === IMPORT HELPERS =====================================================================================
    
    private string GetImports(Controller element)
    {
        string code = "import 'package:flutter/material.dart';\n";
        code += "import 'package:provider/provider.dart';\n";
        code += "import '../viewmodel/" + element.View!.ViewModel.GetElemName() + ".dart';\n";
        
        // Import dtos and enumerations used in operations of the controller (including those used in invoked use cases)
        List<string> dataObjects = [];
        foreach (COperation cop in element.Operations){
            // DTOs
            // TODO - check if this is needed
            //foreach (DataTransferObject dto in cop.TransferredData){
            //    string elemName = dto.GetElemName();
            //    if (!dataObjects.Contains(elemName)) dataObjects.Add(elemName);
            //}
            
            foreach (DataTransferObject dto in cop.Invoked!.Parameters
                         .Where(p => p.CanBeIdentifier)
                         .Select(p => (DataTransferObject)p.Type)) {
                string elemName = dto.GetElemName();
                if (!dataObjects.Contains(elemName)) dataObjects.Add(elemName);
            }
            
            // Enumerations based on invoked use cases
            UCOperation? ucop;
            if (null != (ucop = cop.Invoked)) { 
                if (ucop.Invoking) {
                    string elemName = GetTypeName(((ucop.Instructions[0] as Call ?? throw new Exception("Critical error"))
                        .CalledOperation as UCOperation ?? throw new Exception("Critical Error")).Uc!.Enumeration!, true);
                    if (!dataObjects.Contains(elemName)) dataObjects.Add(elemName);
                }
                if (ucop.Returning) {
                    string elemName = GetTypeName(ucop.ReturnType!, true);
                    if (!dataObjects.Contains(elemName)) dataObjects.Add(elemName);
                }
            }
            // Enumeration unions based on the returnTo operation of the controller operation
            if (null != cop.ReturnTo && 0 != cop.ReturnTo.Parameters.Count) {
                string elemName = GetTypeName(cop.ReturnTo.Parameters[0].Type, true);
                if (!dataObjects.Contains(elemName)) dataObjects.Add(elemName);
            }
            // DTOs based on the return type of the controller operation (if any)
            if (null != cop.ReturnType && cop.ReturnType is not Primitive) {
                string elemName = GetTypeName(cop.ReturnType, true);
                if (!dataObjects.Contains(elemName)) dataObjects.Add(elemName);
            }
        }
        if (0 != dataObjects.Count)
            code += "import '../../dtos/" + string.Join(".dart';\nimport '../../dtos/", dataObjects) + ".dart';\n";
        
        // Import use case invoked by the controller
        code += "import '../../usecases/" + element.UseCase!.GetElemName() + ".dart';\n";
        return code + "\n";
    }
 
    private string GetImports(Presenter element)
    {
        string code = "import 'package:flutter/material.dart';\n";
        code += "import './AbstractPresenter.dart';\n";
        code += "import '../viewmodel/" + element.View!.ViewModel.GetElemName() + ".dart';\n";
        
        List<string> dataObjects = ["ScreenIdEnum"];
        foreach (POperation cop in element.Operations){
            if (null != cop.ReturnType && cop.ReturnType is not Primitive) 
                dataObjects.Add(GetTypeName(cop.ReturnType, true));
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
        foreach (string obj in dataObjects)
            code += "import '../../dtos/" + obj + ".dart';\n";
        
        // Import the associated view
        code += "import '../" + element.View.GetElemName() + ".dart';\n";
        
        code += "import 'package:logging/logging.dart';\n";
        
        return code + "\n";
    }
    
    private string GetImports(Service element, List<DataTransferObject> uniqueReturnTypes)
    {
        string code = "import 'dart:convert';\n" + 
                      "import 'package:doorce_api_client/doorce_api_client.dart';\n" + 
                      "import 'package:doorce_keycloak_token_handler/doorce_keycloak_token_handler.dart';\n";
        List<string> dataObjects = [];
        foreach (SOperation sop in element.Operations) {
            string name;
            if (null != sop.ReturnType && sop.ReturnType is not Primitive && 
                    !dataObjects.Contains(name = GetTypeName(sop.ReturnType, true)))
                dataObjects.Add(name);
            foreach (Parameter par in sop.Parameters) {
                name = GetTypeName(par, true);
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
        foreach (string obj in dataObjects)
            code += "import '../../dtos/" + obj + ".dart';\n";
        foreach (DataTransferObject dto in uniqueReturnTypes)
            code += "import '" + dto.GetElemName() + "OptionsProxy.dart';\n";
        
        return code + "\n";
    }
    
    private string GetImports(UseCase element)
    {
        List<string> dataObjects = [];
        if (null != element.Enumeration) dataObjects.Add(element.Enumeration!.GetElemName());
        foreach (UCOperation ucop in element.Operations){
            foreach (Instruction instr in ucop.Instructions){
                if (instr is Call { CalledOperation.ReturnType: not null } call) {
                    if (call.CalledOperation.ReturnType is not Primitive) {
                        string name = GetTypeName(call.CalledOperation.ReturnType, true);
                        if (!dataObjects.Contains(name)) dataObjects.Add(name);
                    }
                } else if (instr is End endInstr) {
                    string resultEnum = endInstr.Value!.Parent!.GetElemName();
                    if (!dataObjects.Contains(resultEnum)) dataObjects.Add(resultEnum);
                }
            }
            foreach (Parameter par in ucop.Parameters) 
                if (par.Type is not Primitive) {
                    string name = GetTypeName(par, true);
                    if (!dataObjects.Contains(name)) dataObjects.Add(name);
                }
            foreach (DataTransferObject dto in element.State.Where(e => !e.CanBeMap)) {
                string name = GetTypeName(dto, true);
                if (!dataObjects.Contains(name)) dataObjects.Add(name);
            }
        }
        foreach (UseCase inv in element.Invoked) {
            string name = inv.Enumeration!.GetElemName();
            if (!dataObjects.Contains(name)) dataObjects.Add(name);
        }

        string code = "import 'package:flutter/material.dart';\n";
        if (0 != element.Invoked.Count) code += "import 'package:provider/provider.dart';\n";
        
        foreach (string obj in dataObjects)
            code += "import '../../dtos/" + obj + ".dart';\n";

        foreach (Presenter presenter in element.Presenters)
            code += "import '../view/presenters/" + presenter.GetElemName() + ".dart';\n";
        foreach (Service service in element.Services)
            code += "import '../services/" + service.GetElemName() + ".dart';\n";
        foreach (UseCase inv in element.Invoked)
            code += "import './" + inv.GetElemName() + ".dart';\n";
        return code + "\n";
    }
    
    private string GetImports(IntermediateRepresentation element)
    {
        string code = "import 'package:doorce_message_handler/doorce_message_handler.dart';\n";
        code += "import 'package:flutter/material.dart';\n";
        code += "import 'package:doorce_keycloak_token_handler/doorce_keycloak_token_handler.dart';\n";
        code += "import 'package:provider/provider.dart';\n";
        code += "import 'package:provider/single_child_widget.dart';\n";
        code += "import 'package:logging/logging.dart';\n\n";
        
        // IMPORTS FOR SERVICES
        foreach (Service service in element.Services)
            code += "import './services/" + service.GetElemName() + ".dart';\n";
            
        // IMPORTS FOR VIEW FACTORIES
        foreach (View v in element.Views)
            code += "import './view/" + v.GetElemName() + ".dart';\n";
        code += "\n";

        // IMPORTS FOR PRESENTERS
        foreach (Presenter p in element.Presenters)
            code += "import './view/presenters/" + p.GetElemName() + ".dart';\n";
        code += "\n";
  
        // REGISTER PROVIDERS FOR USE CASES
        foreach (UseCase uc in element.UseCases)
            code += "import './usecases/" + uc.GetElemName() + ".dart';\n";
        
        return code + "\n";
    }
    
    private string GetImports(View element)
    {
        string code = "import 'package:doorce_style/doorce_style.dart';\n";
        code += "import 'package:flutter/material.dart';\n";
        // TODO - optimise for usage of multidropdowns
        if (0 != element.ViewModel.InputData.Count)
            code += "import 'package:multi_dropdown/multi_dropdown.dart';\n";
        code += "import './controllers/" + element.Controller.GetElemName() + ".dart';\n";
        code += "import './viewmodel/" + element.ViewModel.GetElemName() + ".dart';\n";
        code += "import 'package:logging/logging.dart';\n";
        return code + "\n";
    }
    
    private string GetImports(ResultUnionEnumeration element)
    {
        string code = "";
        foreach (SimpleResultEnumeration enumeration in element.Members)
            code += "import './" + GetTypeName(enumeration) + ".dart';\n";
        return code + "\n";
    }
    
    private string GetImports(ViewModel element)
    {
        string code = "";
        List<string> dataObjects = [];
        string name;
        foreach (DataTransferObject dto in element.OutputData)
            if (!dataObjects.Contains(name = GetTypeName(dto, true))) dataObjects.Add(name);
        foreach (DataTransferObject dto in element.InputData.Where(dto => !element.OutputData.Contains(dto)))
            if (!dataObjects.Contains(name = GetTypeName(dto, true))) dataObjects.Add(name);
        foreach (string obj in dataObjects)
            code += "import '../../dtos/" + obj + ".dart';\n";
        return code + "\n";
    }
    
    private string GetFromJson(DataTransferObject element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "fromJson(Map<String, dynamic> json";
        foreach (Field field in element.ActualFields.Where(f => f.Type is DataTransferObject {IsSimple: false}))
            code += ", Map<String, String> " + field.Type.GetVarName() + "Options";
        code += ") {\n";
        foreach (Field field in element.ActualFields.Where(f => f.Type is DataTransferObject {IsSimple: false}))
            code += ts + "\tList<String> " + field.GetVarName() + 
                    "Selection = List<String>.from(json['" + field.GetVarName() + "Ids'] ?? []);\n";
        code += string.Join("\n",
            element.Fields
                .Where(f => !f.IsItemIdentifier) 
                .Select(f => ts + GetFromJson(f, tabs))) + "\n";
        code += ts + "}\n";
        return code;
    }
    
    private string GetFromJson(Field field, int tabs = 0){
        string code = "";
        var ts = Common.Utils.GetTabString(tabs);
        
        if (field.Type is Primitive || field is { Multiple: false, Type: DataTransferObject { IsSimple: true } dto }
            && dto.Fields[0].Type is Primitive) 
        {
            code += "\t" + field.GetVarName() + " = ";
            if (field is { Multiple: false })
            {
                if (field.Type is Primitive p && p.Value == PrimitiveType.Location) {
                    code += $"DoorCommon().tryParsePoint(json['{field.GetVarName()}'] ?? '') ?? (0,0);";
                } else if (field.Type is Primitive pr && (pr.Value == PrimitiveType.DateTime 
                                                          || pr.Value == PrimitiveType.Date || pr.Value == PrimitiveType.Time)) {
                    code += $"json['{field.GetVarName()}'] != null ? DateTime.parse(json['{field.GetVarName()}'] as String) : null;";
                } else {
                    code += "json['" + field.GetVarName() + "'] ?? " + GetEmptyType(field);
                }
            } else {
                
                code += "List.from(json['" + field.GetVarName() + "'] ?? [])";
                
                if (field.Type is Primitive p && p.Value == PrimitiveType.Location) {
                    code += $".map((loc) => DoorCommon().tryParsePoint(loc) ?? {GetEmptyType(field)}).cast<(double, double)>().toList()";
                } else if (field.Type is Primitive pr && (pr.Value == PrimitiveType.DateTime 
                                                          || pr.Value == PrimitiveType.Date || pr.Value == PrimitiveType.Time)) {
                    code += ".map((dt) => DateTime.parse(dt as String)).toList()";
                }
            }
        } else {
            if (field is { Multiple: false, Type: not DataTransferObject { IsAuxiliaryCollection: true } }) {
                code += "\t" + field.GetVarName() + "Id = json['" + field.GetVarName() + "Id'] ?? \"\";\n";
                code += ts + "\t" + field.GetVarName() + "Label = "
                        + field.Type.GetVarName() + "Options[" + field.GetVarName() + "Id] ?? \"\"";
            } else {
                code += "\t" + field.GetVarName() + "Map = Map.fromEntries(" 
                        + field.Type.GetVarName() + "Options.entries.where((o) => "
                        + field.GetVarName() + "Selection.contains(o.key)))";
            }
        }
        return code + ";";
    }

    private string GetEmptyType(Field field)
    {
        if (field.Type is Primitive type) {
            Primitive prim = type ?? throw new Exception("Critical compilation error");
            // Example 1: String name = ""
            // Example 2: List<String> names = []
            if (!field.Multiple)
                switch (prim.Value) {
                    case PrimitiveType.Integer:
                        return "0";
                    case PrimitiveType.Number:
                    case PrimitiveType.Float:
                        return "0";
                    case PrimitiveType.String:
                        return "\"\"";
                    case PrimitiveType.Boolean:
                        return "false";
                    case PrimitiveType.Time:
                    case PrimitiveType.Date:
                    case PrimitiveType.DateTime:
                        return "null";
                    case PrimitiveType.Location:
                        return "(0,0)";
                }
            else return field.CanBeSimplified ? "{}" : "[]";
        } else {
            // Example 1: User user = User()
            // Example 2: List<User> users = []
            return field is { Multiple: false, Type: DataTransferObject { IsAuxiliaryCollection: false } }
                ? GetTypeName(field.Type, true) + "()"
                : field.CanBeSimplified ? "{}" : "[]";
        }

        return "";

    }
    
    private string GetToJson(DataTransferObject element, int tabs = 0)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "Map<String, dynamic> toJson() {\n";
        code += ts + "\tvar jsonData = <String, dynamic>{\n";
        code += string.Join(",\n", element.Fields
            .Where(f => !f.IsItemIdentifier)
            .Select(f => ts + GetToJson(f, tabs))) + "\n";
        code += ts + "\t};\n";
        code += ts + "\tjsonData.removeWhere((key, value) => value == null || value == \"\");\n";
        code += ts + "\treturn jsonData;\n";
        code += ts + "}\n";
        return code;
    }
    
    private string GetToJson(Field field, int tabs = 0){
        var ts = Common.Utils.GetTabString(tabs);
        string code = "";
        
        if (field.Type is Primitive p) {
            if (p.Value == PrimitiveType.Location) {
                if (field.Multiple) {
                    code += "\t\t'" + field.GetVarName() + "': " + field.GetVarName() +
                            "?.map((point) => point.toString()).toList()";
                } else {
                    code += "\t\t'" + field.GetVarName() + "': " + field.GetVarName() + "?.toString()";
                }
            } else if (p.Value == PrimitiveType.DateTime || p.Value == PrimitiveType.Date ||
                       p.Value == PrimitiveType.Time) {
                if (field.Multiple) {
                    code += "\t\t'" + field.GetVarName() + "': " + field.GetVarName() +
                            "?.map((dt) => dt.toIso8601String()).toList()";
                } else {
                    code += "\t\t'" + field.GetVarName() + "': " + field.GetVarName() + "?.toIso8601String()";
                }
            } else {
                code += "\t\t'" + field.GetVarName() + "': " + field.GetVarName();
            }
        } else {
            if (field is { Multiple: false, Type: not DataTransferObject { IsAuxiliaryCollection: true } })
                code += "\t\t'" + field.GetVarName() + "Id': " + field.GetVarName() + "Id\n";
            else {
                code += "\t\t'" + field.GetVarName() + "Ids': " + field.GetVarName() + "Map.keys.toList()";
            }
        }

        return code;
    }
    
    private string GetToLabel(DataTransferObject element, int tabs)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string code = ts + "String toLabel(bool fullLabel) {\n";
        List<(string,string)> labelParts = [];
        foreach (Field f in element.ActualFields.Where(f => !f.IsItemIdentifier)) {
            bool isMultiple = f.Multiple || f.Type is DataTransferObject { IsAuxiliaryCollection: true };
            if (f.Type is DataTransferObject { IsSimple: false })
                labelParts.Add((f.Name, f.GetVarName() + (isMultiple ? "Map" : "Label")));
            else labelParts.Add((f.Name, f.GetVarName()));
        }

        code += ts + "\tif (fullLabel) {\n";
        code += ts + "\t\treturn \"" + 
                (0 != labelParts.Count ? string.Join(" | ", labelParts.Select(lp => lp.Item1 + ": $" + lp.Item2)) : "$identifier") +
                "\";\n";
        code += ts + "\t}\n";
        code += ts + "\treturn \"$" + 
                (0 != labelParts.Count ? string.Join(" | $", labelParts.Select(lp => lp.Item2)) : "identifier") +
                "\";\n";
        code += ts + "}\n";
        return code;
    }
    
    private string GetParametersCode(Operation element, bool var = false, bool bare = false, bool ignoreContext = false)
    {
        UCOperation? ucop = element as UCOperation;
        if (ucop is { Invoking: true })
            return ucop.Instructions[0] is Call call ? GetParametersCode(call.CalledOperation, var, bare)
                : throw new Exception("Critical compilation error");
        List<string> pars = [];
        if (element is not COperation) {
            bool useIdentifier = element is SOperation;
            pars = var ? element.Parameters.Select(p => ToVarCode(p, useIdentifier)).ToList() :
            element.Parameters.Select(p => GetCode(p, useIdentifier)).ToList();
        } else if (element is COperation { Invoked: not null } cop)
                pars = cop.Invoked!.Parameters.Where(p => p.CanBeIdentifier)
                    .Select(p => var ? ToVarCode(p) : GetCode(p)).ToList();

        string prefix = "", suffix = "";
        if (element is not SOperation && !ignoreContext)
            prefix += (var ? "" : "BuildContext ") +"context" + (0 != pars.Count() ? ", " : "");
        if (ucop is { Initial: true } && null != ucop.Uc!.Enumeration)
            suffix = (0 != element.Parameters.Count || 0 != prefix.Length ? ", " : "") + 
                     (var ? "" : "Function(BuildContext, " + GetTypeName(ucop.Uc!.Enumeration!) + ")? ") 
                     + "returnFunction";

        return (bare ? "" : "(") + prefix + string.Join(", ", pars) + suffix + (bare ? "" : ")");
    }
    
    // TODO - parameterise main generator and use depending on parameters
    private string ToMockBodyCode(SOperation element, int tabs)
    {
        string ts = Common.Utils.GetTabString(tabs);
        string ret = GetTypeName(element.ReturnType!);
        if (ret.Contains("Enum")) ret += "[0];";
        else if ("int" == ret) ret = "0;";
        else if ("bool" == ret) ret = "false";
        else ret += "();";
        string code = "{\n" + ts + "\treturn " + ret + "\n" + ts + "}\n";
        return code;
    }

    private string GetFileName(CodeUnit element)
    {
        switch (element) {
            case DataTransferObject:
            case ViewModel:
            case ResultEnumeration:
            case View:
            case Controller:
            case Presenter:
            case Service:
            case UseCase:
                return element.GetElemName();
        }
        throw new Exception("Critical error - unknown code unit type");
    }
}