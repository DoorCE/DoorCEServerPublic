using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEModel.Infrastructure.DataModel.Applications
{
	public class ConceptAccess
	{
		public string? Query { get; set; }
		public ApiQueryType? Type { get; set; }
		public required IEnumerable<Concept> Concepts { get; set; }
		public ApiAccessSpec? ApiSpec { get; set; }
		public required Dataset Dataset { get; set; }
	}
}