using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;
using DoorCEModel.Infrastructure.DataModel.CodeContents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;

namespace DoorCEGenerator.Application.IntermediateModel;

public interface ICodeGenerator
{
    public string GetCode(ResultUnionEnumeration element, int tabs = 0);
    public string GetCode(SimpleResultEnumeration element, int tabs = 0);
    public string GetCode(Controller element, int tabs = 0);
    public string GetCode(COperation element, int tabs = 0);
    public  string GetCode(DataTransferObjectUnit element, int tabs = 0);
    public string GetCode(DataTransferObject element, int tabs = 0);
    public string GetCode(POperation element, int tabs = 0);
    public string GetCode(Presenter element, int tabs = 0);
    public string GetCode(SOperation element, int tabs = 0);
    public string GetCode(Service element, int tabs = 0);
    public string GetCode(UCOperation element, int tabs = 0);
    public string GetCode(UseCase element, int tabs = 0);
    public string GetCode(View element, int tabs = 0);
    public string GetCode(ViewModelUnit element, int tabs = 0);
    public string GetCode(ViewModel element, int tabs = 0);
    public string GetCode(IntermediateRepresentation element);
    public string GetHeaderCode(Operation element, int tabs = 0);
    public string MapPrimitiveType(PrimitiveType type);
    public CodeFile? ToCodeFile(CodeUnit element, string basePath);
    public IEnumerable<CodeFile> GetAuxiliaryFiles();
    public string GetMainFileName();
}