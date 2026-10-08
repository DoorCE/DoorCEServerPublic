namespace DoorCEModel.Infrastructure.DataModel.DataSchemas;

public abstract class Property : NamespaceElement
{
	// ATTRIBUTES
	public bool Required { get; set; } 
	public bool Multiple { get; set; }
	public bool Unique { get; set; }

	public abstract bool IsDefaultIdentifier { get; }
	
	// RELATIONSHIPS
	public Concept? Concept { get; set; }
	
	// METHODS
	protected override DataSchema? GetSchema()
	{
		return Concept?.Schema;
	}

	protected abstract string GetTypeName();
	
	public abstract string GetDatastoreTypeName();

	public string ToDScript()
	{
		string type = GetTypeName();
		return $"{(Unique ? "#" : "")}{Name}: {(Multiple ? $"[{type}]" : type)}{(Required ? "!" : "")}";
	}
}