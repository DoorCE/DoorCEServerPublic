using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;

namespace DoorCEGenerator.Application.IntermediateModel;

public interface INamingConverter
{
    public string GetElemName(StructuralElement element);
    public string GetVarName(DistinguishableElement element);
    string GetGenericVarName(string name);
    public string GetInterfaceName(FunctionalCodeUnit element);
    public string GetRoutingName(View element);
}