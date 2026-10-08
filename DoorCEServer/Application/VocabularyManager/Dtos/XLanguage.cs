namespace DoorCEServer.Application.VocabularyManager.Dtos;

public class XLanguage
{
    public required string Code { get; set; }
    public required string CodeLong { get; set; } = "";
    public required string Name { get; set; }
}