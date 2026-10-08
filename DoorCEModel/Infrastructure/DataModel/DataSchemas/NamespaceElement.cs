using DoorCEModel.Utils.Extensions;

namespace DoorCEModel.Infrastructure.DataModel.DataSchemas;

public abstract class NamespaceElement
{
	// ATTRIBUTES
	public int Id { get; set; }
	public string? Uri => null != Namespace || null != GetSchema()?.DefaultNamespace
		? $"{(Namespace??GetSchema()!.DefaultNamespace)!.Iri.ToLowerInvariant()}:{Name.ToCamelCase().ToLowerInvariant()}"
		: null;
	public required string Name { get; set; }
	public string? Description { get; set; }
	
	// RELATIONSHIPS
	public Namespace? Namespace { get; set; }
	
	// METHODS
	protected abstract DataSchema? GetSchema();
}