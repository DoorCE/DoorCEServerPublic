using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEModel.Infrastructure.DataModel.DataSchemas
{
	public class SchemaSeries : ManageableResource
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
		public required ICollection<DatasetType> Type { get; set; }
		
		// RELATIONSHIPS
		public ICollection<DataSchema> Schemas { get; set; } = new List<DataSchema>();
		public DataSchema? Current { get; set; }

		public bool Validate(bool isAdmin)
		{
			return EditorshipsLink.Validate(isAdmin);
		}
	}
}