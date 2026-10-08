using DoorCEModel.Infrastructure.DataModel.Datasets;

namespace DoorCEModel.Infrastructure.DataModel.Agents;

public abstract class OwnableResource : ManageableResource
{
	public int Id { get; set; }

	// ***** From IdentifiableElement, ManageableResource, MultiDescriptionElement ************
	public required string Uri { get; set; }
	public required ResourceEditorshipsLink EditorshipsLink { get; set; }
	public ICollection<Agent> Editors => EditorshipsLink.Editors;
	public ICollection<Editorship> EditorRoles => EditorshipsLink.EditorRoles;
	public ICollection<EditorRole>? UserRoles { get; set; }
	public required Dictionary<string, string> Title { get; set; } // Mapping of titles for different languages
	public required Dictionary<string, string> Description { get; set; } 	// Mapping of descriptions for different languages
	// ***** End from MultiDescriptionElement etc. *********

	// Mapping of paths for different languages
	public Dictionary<string, string> GetPath()
	{
		var parent = GetParent();
		if (null == parent) return Title.Keys.ToDictionary(k => k, _ => "");
		var parentPath = parent.GetPath();
		var parentTitle = parent.Title;
		// If the parent path does not contain the current language, use English or first available language
		if (null == parent.PartOf) return Title.Keys.ToDictionary(k => k,
			k => parentPath.ContainsKey(k) ? parentTitle[k]
				: parentPath.ContainsKey("en") ? parentTitle["en"] :
				parentTitle.Values.First());
		return Title.Keys.ToDictionary(k => k,
			k => parentPath.ContainsKey(k) ? parentPath[k] + ">" + parentTitle[k]
				: parentPath.ContainsKey("en") ? parentPath["en"] + ">" + parentTitle["en"] :
				parentPath.Values.First() + ">" + parentTitle.Values.First());
	}
	
	public abstract Catalogue? GetParent();
	
	// ATTRIBUTES
	public string? IconUri { get; set; }
	
	// RELATIONSHIPS
	public Person? ResponsiblePerson { get; set; }
	public Organisation? ResponsibleOrganisation { get; set; }
	public ICollection<ContactData> Contacts { get; set; } = new List<ContactData>();

	public bool Validate(bool isAdmin)
	{
		if (0 == Title.Count) return false;
		if (!isAdmin && null == ResponsiblePerson && null == ResponsibleOrganisation) return false;
		if (ResponsiblePerson is { Contacts.Count: 0 }) return false;
		if (this is not Dataset && this is not Catalogue { PartOf: null } && 0 == Contacts.Count) return false;
		foreach (var contact in Contacts) {
			if (!contact.Validate()) return false;
			if (null!=contact.Agent && contact.Agent.Uri != ResponsiblePerson?.Uri &&
			    contact.Agent.Uri != ResponsibleOrganisation?.Uri
			    && Editors.All(e => e.Uri != contact.Agent.Uri))
				return false;
		}
		return EditorshipsLink.Validate(isAdmin);
	}
}
