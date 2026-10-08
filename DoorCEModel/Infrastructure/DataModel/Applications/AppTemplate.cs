using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.CodeContents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;

namespace DoorCEModel.Infrastructure.DataModel.Applications
{
	public class AppTemplate : ManageableResource
	{
		public int Id { get; set; }
		
		// ***** From IdentifiableElement, ManageableResource, DescribableElement **************
		public required string Uri { get; set; }
		public required ResourceEditorshipsLink EditorshipsLink { get; set; }
		public ICollection<Agent> Editors => EditorshipsLink.Editors;
		public ICollection<Editorship> EditorRoles => EditorshipsLink.EditorRoles;
		public ICollection<EditorRole>? UserRoles { get; set; }
		public required string Title { get; set; }
		public required string Description { get; set; }
		// ***** End from DescribableElement ***********

		// ATTRIBUTES
		public string Language { get; set; } = "en";
		public bool IsReady { get; set; }
		
		// RELATIONSHIPS
		public IEnumerable<UseCaseScenarios> UseCases { get; set; } = new List<UseCaseScenarios>();
		public IEnumerable<AppTemplate> Versions { get; set; } = new List<AppTemplate>();
		public AppTemplate? VersionOf { get; set; }
		public required AppDataSpecification DataSpecification { get; set; }
		public IEnumerable<AcquisitionApp> Apps { get; set; } = new List<AcquisitionApp>();
		public required DataSchema Schema { get; set; }
		public IEnumerable<CodePackage> Packages { get; set; } = new List<CodePackage>();
		public ICollection<Concept> AuxiliaryConcepts { get; set; } = new List<Concept>();
		
		public bool Validate(bool isAdmin)
		{
			//TODO
			return EditorshipsLink.Validate(isAdmin);
		}

		public bool IsDefault()
		{
			return Apps.All(a => 1 == a.SourceResources.Count()
			                     && a.SourceResources.First().Uri == a.ActiveResource.Uri);
		}
	}
}