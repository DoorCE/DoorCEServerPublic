namespace DoorCEModel.Infrastructure.DataModel.DataSchemas;

public enum PrimitiveType
{
    String,
    Boolean,
    Integer,
    Number, // decimal number
    Float,
    Double,
    Duration,
    DateTime,
    Time,
    Date,
    Location
}

public static class PrimitiveTypeExtensions
{
    public static string ToSchemaType(this PrimitiveType type)
    {
        return type switch
        {
            PrimitiveType.String => "string", 
            PrimitiveType.Boolean => "boolean",
            PrimitiveType.Integer => "integer",
            PrimitiveType.Number => "number",
            PrimitiveType.Float => "float",
            PrimitiveType.Double => "double",
            PrimitiveType.Duration => "duration",
            PrimitiveType.DateTime => "datetime",
            PrimitiveType.Time => "time",
            PrimitiveType.Date => "date",
            PrimitiveType.Location => "location",
            _ => throw new NotImplementedException($"Primitive type {type} is not implemented")
        };
    }
    
    public static string ToJsonSchemaType(this PrimitiveType type)
    // TODO - validate datetime and location
    {
        return type switch
        {
            PrimitiveType.String => "string", 
            PrimitiveType.Boolean => "boolean",
            PrimitiveType.Integer => "integer",
            PrimitiveType.Number => "number",
            PrimitiveType.Float => "number",
            PrimitiveType.Double => "number",
            PrimitiveType.Duration => "string",
            PrimitiveType.DateTime => "string",
            PrimitiveType.Time => "string",
            PrimitiveType.Date => "string",
            PrimitiveType.Location => "string",
            _ => throw new NotImplementedException($"Primitive type {type} is not implemented")
        };
    }
    
    public static PrimitiveType FromSchemaType(string type)
    {
        return type switch
        {
            "string" => PrimitiveType.String,
            "boolean" => PrimitiveType.Boolean,
            "integer" => PrimitiveType.Integer,
            "number" => PrimitiveType.Number,
            "float" => PrimitiveType.Float,
            "double" => PrimitiveType.Double,
            "duration" => PrimitiveType.Duration,
            "datetime" => PrimitiveType.DateTime,
            "time" => PrimitiveType.Time,
            "date" => PrimitiveType.Date,
            "location" => PrimitiveType.Location,
            _ => throw new NotImplementedException($"Primitive type {type} is not implemented")
        };
    }
    
    public static string ToPostgresType(this PrimitiveType type)
    // TODO - handle datetime and location
    {
        return type switch
        {
            PrimitiveType.String => "text",
            PrimitiveType.Boolean => "boolean",
            PrimitiveType.Integer => "integer",
            PrimitiveType.Number => "numeric",
            PrimitiveType.Float => "real",
            PrimitiveType.Double => "double precision",
            PrimitiveType.Duration => "interval", //TODO - confirm format in postgres (interval/text/bigint)
            PrimitiveType.DateTime => "timestamp",
            PrimitiveType.Date => "timestamp",
            PrimitiveType.Time => "timestamp",
            PrimitiveType.Location => "point",
            _ => throw new NotImplementedException($"Primitive type {type} is not implemented")
        };
    }
}