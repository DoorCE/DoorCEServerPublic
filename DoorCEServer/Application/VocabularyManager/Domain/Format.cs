namespace DoorCEServer.Application.VocabularyManager.Domain;

public class Format
{
    public required string Code { get; set; }
    public required string Label { get; set; }
    public required string LabelLong { get; set; }
    public required string Description { get; set; }
    public required ICollection<string> MimeTypes { get; set; }
    public required ICollection<string> FileExtensions { get; set; }
    public required bool IsCompression { get; set; }
}