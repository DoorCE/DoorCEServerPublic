using DoorCEServer.Application.VocabularyManager.Dtos;
using DoorCEServer.Application.VocabularyManager.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DoorCEServer.WebApi.Controllers;

[ApiController]
[Route("vocabulary")]
public class VocabularyController(VocabularyAPI api): ControllerBase
{
   /// <summary>
   /// Get a list of available languages
   /// </summary>
   /// <param name="languageCode">Optional ISO-639-1 (two-letter) or ISO-639-3 (three-letter) language code</param>
   /// <returns>List of languages with names in native languages or in the language of the provided language code</returns>
   [HttpGet("GetLanguages")]
   public IEnumerable<XLanguage> GetLanguages([FromQuery] string? languageCode = null)
   {
      return api.GetLanguages(languageCode);
   }
   
   /// <summary>
   /// Get a list of available file types (formats)
   /// </summary>
   /// <param name="mediaType">Optional MIME type</param>
   /// <returns>List of file types</returns>
   [HttpGet("GetFileTypes")]
   public IEnumerable<XFormat> GetFormats([FromQuery] string? mediaType = null)
   {
      return api.GetFileTypes(mediaType);
   }
   
   /// <summary>
   /// Get a list of available compression file types (formats)
   /// </summary>
   /// <returns></returns>
   [HttpGet("GetCompressionFormats")]
   public IEnumerable<XFormat> GetCompressionFormats()
   {
      return api.GetCompressionFormats();
   }
   
   /// <summary>
   /// Get a list of media types that correspond to a specified format (or any format)
   /// </summary>
   /// <param name="code">Optional authority code of a format</param>
   /// <returns>A list of media type strings</returns>
   [HttpGet("GetFormatMediaTypes")]
   public IEnumerable<string> GetFormatMediaTypes([FromQuery] string? code = null)
   {
      return api.GetFormatMediaTypes(code);
   }
   
   /// <summary>
   ///  Get a list of available media types that correspond to a specified type (or any type)
   /// </summary>
   /// <param name="type">Optional main media type (e.g. "video", "audio")</param>
   /// <returns>A list of media type strings</returns>
   [HttpGet("GetMediaTypes")]
   public IEnumerable<string> GetMediaTypes([FromQuery] string? type = null)
   {
      return api.GetMediaTypes(type);
   }
   
   /// <summary>
   /// Get a list of available licences
   /// </summary>
   /// <param name="languageCode">Optional ISO-639-1 (two-letter) or ISO-639-3 (three-letter) language code</param>
   /// <returns>List of languages with labels in English or in the language of the provided language code</returns>
   [HttpGet("GetLicences")]
   public IEnumerable<XLicence> GetLicences([FromQuery] string? languageCode = null)
   {
      return api.GetLicences(languageCode);
   }
   
   /// <summary>
   /// Get a list of available frequencies
   /// </summary>
   /// <param name="languageCode">Optional ISO-639-1 (two-letter) or ISO-639-3 (three-letter) language code</param>
   /// <returns>List of frequencies with labels in English or in the language of the provided language code</returns>
   [HttpGet("GetFrequencies")]
   public IEnumerable<XVocabularyEntry> GetFrequencies([FromQuery] string? languageCode = null)
   {
      return api.GetFrequencies(languageCode);
   }
   
   /// <summary>
   /// Get a list of available themes
   /// </summary>
   /// <param name="languageCode">Optional ISO-639-1 (two-letter) or ISO-639-3 (three-letter) language code</param>
   /// <returns>List of themes with labels in English or in the language of the provided language code</returns>
   [HttpGet("GetThemes")]
   public IEnumerable<XVocabularyEntry> GetThemes([FromQuery] string? languageCode = null)
   {
      return api.GetThemes(languageCode);
   }
   
   /// <summary>
   /// Get a list of available themes
   /// </summary>
   /// <param name="languageCode">Optional ISO-639-1 (two-letter) or ISO-639-3 (three-letter) language code</param>
   /// <returns>List of themes with labels in English or in the language of the provided language code</returns>
   [HttpGet("GetAccessRights")]
   public IEnumerable<XVocabularyEntry> GetAccessRights([FromQuery] string? languageCode = null)
   {
      return api.GetAccessRights(languageCode);
   }
   
   /// <summary>
   /// Get a list of available maturity statuses for distributions
   /// </summary>
   /// <param name="languageCode">Optional ISO-639-1 (two-letter) or ISO-639-3 (three-letter) language code</param>
   /// <returns>List of maturity statuses with labels in English or in the language of the provided language code</returns>
   [HttpGet("GetMaturityStatuses")]
   public IEnumerable<XVocabularyEntry> GetMaturityStatuses([FromQuery] string? languageCode = null)
   {
      return api.GetMaturityStatuses(languageCode);
   }
}