using AutoMapper;
using System.Xml.Linq;
using DoorCEServer.Application.VocabularyManager.Dtos;
using DoorCEServer.Application.VocabularyManager.Interfaces;

namespace DoorCEServer.Application.VocabularyManager.Domain;

public class MVocabulary : VocabularyAPI
{
    private IEnumerable<Language> _languages = new List<Language>();
    private IEnumerable<Format> _formats = new List<Format>();
    private IEnumerable<MediaType> _mediaTypes = new List<MediaType>();
    private IEnumerable<Licence> _licences = new List<Licence>();
    private IEnumerable<Frequency> _frequencies = new List<Frequency>();
    private IEnumerable<Theme> _themes = new List<Theme>();
    private IEnumerable<Theme> _accessRights = new List<Theme>();
    private IEnumerable<Theme> _maturityStatuses = new List<Theme>();
    
    private readonly IMapper _mapper;
    public MVocabulary(IMapper mapper)
    {
        _mapper = mapper;
        LoadLanguages();
        LoadFormats();
        LoadMediaTypes();
        LoadLicences();
        LoadFrequencies();
        LoadThemes();
        LoadAccessRights();
        LoadMaturityStatuses();
    }
    
    public IEnumerable<XLanguage> GetLanguages(string? languageCode = null)
    {
        if (languageCode == null)
            return _mapper.Map<IEnumerable<Language>,ICollection<XLanguage>>(_languages);
        languageCode = RectifyLanguageCode(languageCode);
        return _languages.Select(l => new XLanguage() {
            Code = l.Code,
            CodeLong = l.CodeLong,
            Name = l.Labels.ContainsKey(languageCode) ? l.Labels[languageCode] : l.Name
        });
    }

    public IEnumerable<XFormat> GetFileTypes(string? mediaType = null)
    {
        if (null == mediaType)
            return _mapper.Map<IEnumerable<Format>,ICollection<XFormat>>(_formats);
        return _mapper.Map<IEnumerable<Format>,ICollection<XFormat>>(_formats.Where(ft => ft.MimeTypes.Contains(mediaType)));
    }

    public IEnumerable<XFormat> GetCompressionFormats()
    {
        return _mapper.Map<IEnumerable<Format>,ICollection<XFormat>>(_formats.Where(ft => ft.IsCompression));
    }

    public IEnumerable<string> GetFormatMediaTypes(string? code = null)
    {
        if (null == code)
            return _formats.SelectMany(f => f.MimeTypes).Distinct().OrderBy(m => m).ToList();
        return _formats.SingleOrDefault(f => code == f.Code)?.MimeTypes ?? new List<string>();
    }

    public IEnumerable<string> GetMediaTypes(string? type = null)
    {
        if (null == type)
            return _mediaTypes.Select(t => t.ToString());
        return _mediaTypes.Where(t => type == t.Type).Select(t => t.ToString());
    }

    public IEnumerable<XLicence> GetLicences(string? languageCode = null)
    {
        if (null == languageCode)
            languageCode = "eng";
        else languageCode = RectifyLanguageCode(languageCode);
        return _licences.Select(f => new XLicence
        {
            Code = f.Code,
            Description = f.Description,
            Acronym = f.Acronym,
            Url = f.Url,
            Label = f.Labels.ContainsKey(languageCode) ? f.Labels[languageCode] : f.Labels["eng"]
        });
    }

    public IEnumerable<XVocabularyEntry> GetFrequencies(string? languageCode = null)
    {
        if (null == languageCode)
            languageCode = "eng";
        else languageCode = RectifyLanguageCode(languageCode);
        return _frequencies.Select(f => new XVocabularyEntry()
        {
            Code = f.Code,
            Description = f.Description,
            Label = f.Labels.ContainsKey(languageCode) ? f.Labels[languageCode] : f.Labels["eng"]
        });
    }

    public IEnumerable<XVocabularyEntry> GetThemes(string? languageCode = null)
    {
        if (null == languageCode)
            languageCode = "eng";
        else languageCode = RectifyLanguageCode(languageCode);
        return _themes.Select(t => new XVocabularyEntry
        {
            Code = t.Code,
            Description = t.Descriptions.ContainsKey(languageCode) ? t.Descriptions[languageCode] : t.Descriptions["eng"],
            Label = t.Labels.ContainsKey(languageCode) ? t.Labels[languageCode] : t.Labels["eng"]
        });
    }

    public IEnumerable<XVocabularyEntry> GetAccessRights(string? languageCode = null)
    {
        if (null == languageCode)
            languageCode = "eng";
        else languageCode = RectifyLanguageCode(languageCode);
        return _accessRights.Select(t => new XVocabularyEntry
        {
            Code = t.Code,
            Description = t.Descriptions.ContainsKey(languageCode) ? t.Descriptions[languageCode] : t.Descriptions["eng"],
            Label = t.Labels.ContainsKey(languageCode) ? t.Labels[languageCode] : t.Labels["eng"]
        });
    }

    public IEnumerable<XVocabularyEntry> GetMaturityStatuses(string? languageCode = null)
    {
        if (null == languageCode)
            languageCode = "eng";
        else languageCode = RectifyLanguageCode(languageCode);
        return _maturityStatuses.Select(t => new XVocabularyEntry
        {
            Code = t.Code,
            Description = t.Descriptions.ContainsKey(languageCode) ? t.Descriptions[languageCode] : t.Descriptions["eng"],
            Label = t.Labels.ContainsKey(languageCode) ? t.Labels[languageCode] : t.Labels["eng"]
        });
    }

    private string RectifyLanguageCode(string languageCode)
    {
        languageCode = languageCode.ToLower();
        string? code;
        if (2 == languageCode.Length) {
            Language? lang = _languages.SingleOrDefault(l => languageCode == l.Code);
            if (lang is { CodeLong: null }) throw new Exception("Internal error - language table corrupted");
            code = lang?.CodeLong;
        } else if (3 == languageCode.Length)
            code = _languages.Any(l => languageCode == l.CodeLong) ? languageCode : null;
        else
            throw new Exception("Invalid language code format");
        if (null == code)
            throw new Exception("Language not found - invalid code");
        return code;
    }
    
    private void LoadLanguages()
    {
        XDocument languageVocab = XDocument.Load("vocab/languages.xml");
        _languages = languageVocab.Descendants("record")
            .Where(lang => lang.Attribute("language.classification")?.Value == "EU")
            .Select(lang => new Language
                {
                    CodeLong = lang.Element("iso-639-3")?.Value ?? "",
                    Code = lang.Element("iso-639-1")?.Value ?? "",
                    Name = String.Join(", ", lang.Element("name")?.Element("original.name")?
                        .Elements("lg.version")
                        .ToDictionary(l => (l.Attribute("lg")?.Value ?? "") + l.GetHashCode()
                            , l => l.Value)
                        .Values!),
                    Labels = lang.Descendants("label").Elements("lg.version")
                        .ToDictionary(l => l.Attribute("lg")?.Value ?? "", l => l.Value)
                });
    }

    private void LoadFormats()
    {
        XDocument filetypeVocab = XDocument.Load("vocab/file-types.xml");
        _formats = filetypeVocab.Descendants("record")
            .Where (ft => ft.Attribute("deprecated")?.Value == "false")
            .Select(ft => new Format
                {
                    Code = ft.Element("authority-code")?.Value ?? "",
                    Label = ft.Elements("label")
                        .Elements("lg.version")
                        .FirstOrDefault(l => "eng" == l.Attribute("lg")?.Value)?.Value ?? "",
                    LabelLong = ft.Elements("long.label")
                        .Elements("lg.version")
                        .FirstOrDefault(l => "eng" == l.Attribute("lg")?.Value)?.Value ?? "",
                    Description = ft.Element("sources")?.Elements("source")
                        .FirstOrDefault(l => "eng" == l.Attribute("lg")?.Value)?
                        .Element("description")?.Value ?? "",
                    MimeTypes = ft.Elements("internet-media-type").Select(mt => mt.Value).ToList(),
                    FileExtensions = ft.Elements("file-extension").Select(mt => mt.Value).ToList(),
                    IsCompression = ft.Element("is-compressedFormat")?.Value == "true"
                });
    }
    
    private void LoadMediaTypes()
    {
        XDocument mediaTypeVocab = XDocument.Load("vocab/media-types.xml");
        List<MediaType> mediaTypes = (List<MediaType>) _mediaTypes;
        // TODO - check if needed: IEnumerable<XElement> test = mediaTypeVocab.Descendants("record");
        foreach (XElement elem in mediaTypeVocab.Descendants("registry"))
        {
            string type = elem.Attribute("id")?.Value ?? "";
            mediaTypes.AddRange(elem.Elements("record")
                .Select(mt => new MediaType
                    {
                        Type = type,
                        Subtype = mt.Element("name")?.Value ?? ""
                    })
            );
        }
    }

    private void LoadLicences()
    {
        XDocument licenceVocab = XDocument.Load("vocab/licences.xml");
        _licences = licenceVocab.Descendants("record")
            .Where (lic => lic.Attribute("deprecated")?.Value == "false")
            .Select(lic => new Licence
                {
                    Code = lic.Element("authority-code")?.Value ?? "",
                    Acronym = lic.Element("acronym")?.Elements("lg.version")
                        .FirstOrDefault(a => "eng" == a.Attribute("lg")?.Value)?.Value ?? "",
                    Url = lic.Element("exactMatch")?.Value ?? "",
                    Description = lic.Descendants("source")
                        .SingleOrDefault(s => "eng" == s.Attribute("lg")?.Value)?
                        .Element("description")?
                        .Value ?? "",
                    Labels = lic.Element("label")?.Elements("lg.version")
                        .ToDictionary(l => l.Attribute("lg")?.Value ?? "", l => l.Value)
                             ?? new Dictionary<string,string>(),
                });
    }

    private void LoadFrequencies()
    {
        XDocument frequencyVocab = XDocument.Load("vocab/frequencies.xml");
        _frequencies = frequencyVocab.Descendants("record")
            .Where (f => f.Attribute("deprecated")?.Value == "false")
            .Select(f => new Frequency
                {
                    Code = f.Element("authority-code")?.Value ?? "",
                    Description = f.Descendants("source")
                        .SingleOrDefault(s => "eng" == s.Attribute("lg")?.Value)?
                        .Element("description")?
                        .Value ?? "",
                    Labels = f.Element("label")?.Elements("lg.version")
                                 .DistinctBy(e => e.Attribute("lg")?.Value)
                                 .ToDictionary(l => l.Attribute("lg")?.Value ?? "", l => l.Value) 
                             ?? new Dictionary<string,string>()
                });
    }

    private void LoadThemes()
    {
        XDocument themeVocab = XDocument.Load("vocab/data-theme.xml");
        _themes = themeVocab.Descendants("record")
            .Where (t => t.Attribute("deprecated")?.Value == "false")
            .Select(t => new Theme
                {
                    Code = t.Element("authority-code")?.Value ?? "",
                    Descriptions = t.Descendants("source")
                        .ToDictionary(d => d.Attribute("lg")?.Value ?? ""
                            , d => d.Element("description")?.Value ?? ""),
                    Labels = t.Element("label")?.Elements("lg.version")
                                 .DistinctBy(l => l.Attribute("lg")?.Value)
                                 .ToDictionary(l => l.Attribute("lg")?.Value ?? "", l => l.Value) 
                             ?? new Dictionary<string,string>()
                });
    }
    
    private void LoadAccessRights()
    {
        XDocument themeVocab = XDocument.Load("vocab/access-rights.xml");
        _accessRights = themeVocab.Descendants("record")
            .Where (t => t.Attribute("deprecated")?.Value == "false")
            .Select(t => new Theme
            {
                Code = t.Element("authority-code")?.Value ?? "",
                Descriptions = t.Descendants("source")
                    .ToDictionary(d => d.Attribute("lg")?.Value ?? ""
                        , d => d.Element("description")?.Value ?? ""),
                Labels = t.Element("label")?.Elements("lg.version")
                             .DistinctBy(l => l.Attribute("lg")?.Value)
                             .ToDictionary(l => l.Attribute("lg")?.Value ?? "", l => l.Value) 
                         ?? new Dictionary<string,string>()
            });
    }
    
    private void LoadMaturityStatuses()
    {
        XDocument statusVocab = XDocument.Load("vocab/dataset-statuses.xml");
        _maturityStatuses = statusVocab.Descendants("record")
            .Where (t => t.Attribute("deprecated")?.Value == "false")
            .Select(t => new Theme
            {
                Code = t.Element("authority-code")?.Value ?? "",
                Descriptions = t.Descendants("source")
                    .ToDictionary(d => d.Attribute("lg")?.Value ?? ""
                        , d => d.Element("description")?.Value ?? ""),
                Labels = t.Element("label")?.Elements("lg.version")
                             .DistinctBy(l => l.Attribute("lg")?.Value)
                             .ToDictionary(l => l.Attribute("lg")?.Value ?? "", l => l.Value) 
                         ?? new Dictionary<string,string>()
            });
    }
}