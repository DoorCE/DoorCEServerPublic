namespace DoorCEModel.Infrastructure.DataModel.DataSchemas;

public class Namespace
{
    // ATTRIBUTES
    public int Id { get; set; }
    public required string Iri { get; set; }
    public required string Prefix { get; set; }
}