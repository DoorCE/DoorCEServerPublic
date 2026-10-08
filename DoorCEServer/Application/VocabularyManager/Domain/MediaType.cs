namespace DoorCEServer.Application.VocabularyManager.Domain;

public class MediaType
{
    public required string Type { get; set; } = "";
    public required string Subtype { get; set; } = "";
    public new string ToString() { return Type + "/" + Subtype; }
}