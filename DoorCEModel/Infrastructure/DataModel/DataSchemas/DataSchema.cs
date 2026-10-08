using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEModel.Infrastructure.DataModel.DataSchemas;

public class DataSchema : Standard
{
	// RELATIONSHIPS
	public required ICollection<Concept> Concepts { get; set; }
	public Concept? MainConcept => Concepts.SingleOrDefault(c => c.IsMain);
	public required ICollection<Namespace> UsedNamespaces { get; set; }
	public CustomNamespace? DefaultNamespace => (CustomNamespace?)UsedNamespaces.SingleOrDefault(
		n => n is CustomNamespace { IsDefault: true });
	public required SchemaSeries InSeries { get; set; }

	// METHODS
	public bool Validate()
	{
		if (null == DefaultNamespace && Concepts.Any(c => null == c.Namespace)) return false;
		return true;
	}
}