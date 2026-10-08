using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Utils.Extensions;

namespace DoorCEServer.Infrastructure.DataModel.DataSchemas;

public class Reference : Property
{
    // RELATIONSHIPS
    public required Concept Type { get; set; }
    
    // METHODS
    public override bool IsDefaultIdentifier { get => false; }

    protected override string GetTypeName()
    {
        return Type.Name;
    }
    
    // TODO - should "#" be preserved in datastore?
    public override string GetDatastoreTypeName()
    {
        return "text"; 
    }
}