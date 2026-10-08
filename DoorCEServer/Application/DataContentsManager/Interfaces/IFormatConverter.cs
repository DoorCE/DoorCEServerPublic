using DoorCEServer.Application.DataContentsManager.Domain;

namespace DoorCEServer.Application.DataContentsManager.Interfaces;

public interface IFormatConverter
{
    UnifiedFile ReadFile(Stream fileStream, string? singleSheetName);
}