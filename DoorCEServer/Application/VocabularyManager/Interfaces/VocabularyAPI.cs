using DoorCEServer.Application.VocabularyManager.Dtos;

namespace DoorCEServer.Application.VocabularyManager.Interfaces;

public interface VocabularyAPI
{
    IEnumerable<XLanguage> GetLanguages(string? languageCode = null);
    IEnumerable<XFormat> GetFileTypes(string? mediaType = null);
    IEnumerable<XFormat> GetCompressionFormats();
    IEnumerable<string> GetFormatMediaTypes(string? code = null);
    IEnumerable<string> GetMediaTypes(string? type = null);
    IEnumerable<XLicence> GetLicences(string? languageCode = null);
    IEnumerable<XVocabularyEntry> GetFrequencies(string? languageCode = null);
    IEnumerable<XVocabularyEntry> GetThemes(string? languageCode = null);
    IEnumerable<XVocabularyEntry> GetAccessRights(string? languageCode = null);
    IEnumerable<XVocabularyEntry> GetMaturityStatuses(string? languageCode = null);
}