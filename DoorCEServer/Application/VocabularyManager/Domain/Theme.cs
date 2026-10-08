namespace DoorCEServer.Application.VocabularyManager.Domain;

public class Theme
{
    public required string Code { get; set; }
    public required Dictionary<string, string> Descriptions { get; set; } = new();
    public required Dictionary<string,string> Labels { get; set; } = new();
}