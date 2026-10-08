using DoorCEGenerator.Application.IntermediateModel;
using DoorCEGenerator.Application.IntermediateModel.DataRepresentations;
using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;
using DoorCEGenerator.Application.IntermediateModel.StructuralUnitRepresentations;
using DoorCEModel.Utils.Extensions;

namespace DoorCEGenerator.Application.Domain.NamingConverters;

public class DefaultNamingConverter : INamingConverter
{
    public string GetElemName(StructuralElement element)
    {
        if (element is Value val) return val.Name.ToUpper();
        string name = GetName(element);
        return GetPrefix(element) + (element is Operation ? name.ToCamelCase() : name.ToPascalCase())
                                  + GetSuffix(element);
    }

    public string GetVarName(DistinguishableElement element)
    {
        string name = GetName(element);
        string prefix = GetPrefix(element).ToLower();
        return prefix + (0 == prefix.Length ? GetGenericVarName(name) : name.ToPascalCase())
                                  + GetSuffix(element);
    }

    public string GetGenericVarName(string name)
    {
        return name.ToCamelCase();
    }
    
    public string GetInterfaceName(FunctionalCodeUnit element)
    {
        string name = GetName(element);
        return "I" + (element is Service ? name.ToPascalCase() + GetSuffix(element, true) : GetElemName(element));
    }

    public string GetRoutingName(View element)
    {
        string name = GetName(element);
        return name.ToSnakeCase();
    }
    
    private string GetName(StructuralElement element)
    {
        return element switch {
            StructuralCodeElement sce => sce.Name,
            Primitive prim => prim.Value.ToString(),
            _ => throw new NotSupportedException(
                $"Element type '{element.GetType().Name}' is not supported by DefaultNamingConverter.")
        };
    }

    private string GetPrefix(StructuralElement element)
    {
        return element switch {
            Presenter => "P",
            Controller => "C",
            UseCase => "UC",
            View => "V",
            ViewModel => "VM",
            _ => string.Empty
        };
    }
    
    private string GetSuffix(StructuralElement element, bool forInterface = false)
    {
        return element switch {
            SimpleResultEnumeration sre => (ResultKind.Invoke == sre.Kind ? "Result" : string.Empty) + "Enum",
            ResultUnionEnumeration => "UnionEnum",
            Service when !forInterface => "Proxy",
            _ => string.Empty
        };
    }
}