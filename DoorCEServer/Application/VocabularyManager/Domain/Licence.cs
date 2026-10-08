namespace DoorCEServer.Application.VocabularyManager.Domain;

public class Licence
{
    public required string Code { get; set; }
    public required string Acronym { get; set; }
    public required string Url { get; set; }
    public required string Description { get; set; }
    public required Dictionary<string,string> Labels { get; set; } = new Dictionary<string,string>();
}