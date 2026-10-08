namespace DoorCEModel.Infrastructure.DataModel.DataSchemas;

public class Attribute : Property
{
    private bool _defaultIdentifier;

    public required bool DefaultIdentifier
    {
        get => _defaultIdentifier;
        set {
            if (value) {
                if (!Unique)
                    throw new InvalidOperationException("An attribute must be unique to be set as the default identifier.");
                foreach (Attribute a in (Concept?.Properties.OfType<Attribute>() ?? Enumerable.Empty<Attribute>()))
                    if (a._defaultIdentifier)
                        a._defaultIdentifier = false;
            }
            _defaultIdentifier = value;
        }
    }

    // ATTRIBUTES
    public PrimitiveType Type { get; set; }

    // METHODS
    public override bool IsDefaultIdentifier => DefaultIdentifier;

    protected override string GetTypeName()
    {
        return Type.ToSchemaType();
    }
    
    public override string GetDatastoreTypeName()
    {
        var type = Type.ToPostgresType();
        
        if (Multiple)
        {
            // TODO - handle location and datetime arrays
            if (Type is PrimitiveType.Location or PrimitiveType.Date or PrimitiveType.DateTime or PrimitiveType.Time){
                return "text[]";
            }
            return $"{type}[]";
        }
        return type;
    }
}