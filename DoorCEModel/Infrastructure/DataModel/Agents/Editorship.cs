namespace DoorCEModel.Infrastructure.DataModel.Agents;

public class Editorship
{
    public ICollection<EditorRole> Roles { get; set; } = new List<EditorRole>();
    public Agent? Agent { get; set; }
    public required ResourceEditorshipsLink ResourceLink { get; set; }
}