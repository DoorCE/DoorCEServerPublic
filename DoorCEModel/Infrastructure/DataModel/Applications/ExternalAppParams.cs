namespace DoorCEModel.Infrastructure.DataModel.Applications
{
	public class ExternalAppParams
	{
		public IEnumerable<ConceptAccess> AccessSpecs { get; set; } = new List<ConceptAccess>();
		public IEnumerable<ApiAccessSpec> ApiSpecs { get; set; } = new List<ApiAccessSpec>();
		public required AppTemplate Template { get; set; }
	}
}