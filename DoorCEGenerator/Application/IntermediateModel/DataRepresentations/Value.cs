using DoorCEGenerator.Application.IntermediateModel.GenerativeElements;

namespace DoorCEGenerator.Application.IntermediateModel.DataRepresentations;

public class Value(INamingConverter namingConverter) : NamedElement, StructuralElement
{
	// ***** From NamedElement ****************
	public required string Name { get; set; }
	// ***** End from NamedElement ***************
	
	//ATTRIBUTES
	public ResultEnumeration? Parent;

	// METHODS
	public string GetElemName()
	{
		return namingConverter.GetElemName(this);
	}
}