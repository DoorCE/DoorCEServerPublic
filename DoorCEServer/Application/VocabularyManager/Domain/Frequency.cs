namespace DoorCEServer.Application.VocabularyManager.Domain;

public class Frequency
{
    public required string Code { get; set; }
    public required string Description { get; set; }
    public required Dictionary<string,string> Labels { get; set; } = new();
}