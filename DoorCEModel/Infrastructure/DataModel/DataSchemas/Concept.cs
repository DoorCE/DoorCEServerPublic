namespace DoorCEModel.Infrastructure.DataModel.DataSchemas;

public class Concept : NamespaceElement
{
	// RELATIONSHIPS
	public ICollection<Property> Properties { get; set; } = new List<Property>();
	
	// ATTRIBUTES
	public bool IsMain { get; set; }
	public DataSchema? Schema { get; set; }
	public Attribute? DefaultIdentifier => 
		Properties.FirstOrDefault(p => p is Attribute { IsDefaultIdentifier: true }) as Attribute;
	
	// METHODS
	protected override DataSchema? GetSchema()
	{
		return Schema;
	}

	public string GetDatastorePrimaryKeyFieldName() {
		return DefaultIdentifier?.Name ?? "id";
	}
	
	public string ToDScript(bool auxiliary = false)
	{
		string rsl = $"Concept{(auxiliary?"?":"")} {Name} " + "{\n";
		rsl += string.Join(",\n", Properties.Select(p => $"    {p.ToDScript()}")) + "\n";
		rsl += "}\n";
		return rsl;
	}
}