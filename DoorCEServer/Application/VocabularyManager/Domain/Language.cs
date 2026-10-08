namespace DoorCEServer.Application.VocabularyManager.Domain;

public class Language
{
    public string Code { get; set; } = "";
    public string CodeLong { get; set; } = "";
    public string Name { get; set; } = "";
    public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
}