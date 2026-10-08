using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEModel.Infrastructure.DataModel.Applications
{
	public class AcquisitionApp : ManageableResource
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
		
		public required AppTemplate Template { get; set; }
		public required DataResource ActiveResource { get; set; }
		public required IEnumerable<DataResource> SourceResources { get; set; }
		public bool IsVisible { get; set; }
		public bool IsEnabled { get; set; }
		public AppStatus Status { get; set; } = AppStatus.Created;
		public string Log { get; set; } = "";
		
		
		public bool Validate(bool isAdmin)
		{
			//TODO
			return EditorshipsLink.Validate(isAdmin);
		}
	}
}