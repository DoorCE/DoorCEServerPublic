using System.Text;
using System.Text.Json;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.DataContentsManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Dtos;

namespace DoorCEServer.Tests;

public class TestCommon
{
    public XDataSchema GetMockXDataSchema(string id, string title, string seriesId, bool version2 = false)
    {
        XDataSchema xSchema = new XDataSchema
        {
            Uri = id,
            Title = title,
            Description = "This data schema defines the structure of datasets with trees.",
            SeriesUri = seriesId,
            MainConceptName = "Tree",
            UsedNamespaces = new List<XNamespace>()
            {
                new()
                {
                    Iri = "https://doorce.com/schemas/tree#",
                    Prefix = "tree"
                },
                new()
                {
                    Prefix = "xsd",
                    IsCustom = false
                }
            },
            DefaultNamespacePrefix = "tree",
            Concepts = new List<XConcept>()
            {
                new()
                {
                    Name = "Tree",
                    Description = "Representation of a specific tree.",
                    Properties = new Dictionary<string, XPropertyValue>()
                    {
                        { "height", new XPropertyValue { Type = "number" } },
                        { "species", new XPropertyValue { Type = "reference", Target = "Tree Species" } },
                        { "position", new XPropertyValue { Type = "array", Items = new XPropertyValue { Type = "string" } } }
                    },
                    Required = ["species", "position"],
                    Unique = ["position"]
                },
                new()
                {
                    Name = "Tree Species",
                    Description = "Representation of a specific tree species",
                    Properties = new Dictionary<string, XPropertyValue>
                    {
                        { "name", new XPropertyValue { Type = "string" } },
                        { "latin name", new XPropertyValue { Type = "string" } },
                    },
                    Unique = ["name", "latin name"]
                }
            }
        };
        if (version2)
            ((List<XConcept>)xSchema.Concepts)[1].Properties
                .Add("subspecies", new XPropertyValue { Type = "string" });
        return xSchema;
    }

    public XDataUploadRequest GetMockXDataUploadRequest(string datasetId,
        Dictionary<string, string> tableNameToConceptUriMap) 
    {
        return new XDataUploadRequest {
            DatasetUri = datasetId,
            TableNameToConceptUriMap = tableNameToConceptUriMap,
            TableNameToIdFieldNameMap = new Dictionary<string, string> {
                { "Tree Species", "name" },
                { "Tree", ""}
            }
        };
    }

    public Dictionary<string, dynamic?> GetMockJsonData() {
        Dictionary<string, dynamic?> jsonData = new() {
            { 
                "Tree Species", new List<Dictionary<string, dynamic?>> 
                {
                    new() {
                        { "name", "London plane" },
                        { "latin name", "Platanus acerifolia" }
                    },
                    new() {
                        { "name", "Wild cherry" },
                        { "latin name", "Prunus avium" }
                    },
                    new() {
                        { "name", "Lavallée's hawthorn" },
                        { "latin name", "Crataegus lavalleei" }
                    }
                }
            },
            {
                "Tree", new List<Dictionary<string, dynamic?>> 
                {
                    new() {
                        { "species", "London plane" },
                        { "height", 22.62},
                        { "position", new List<string> {"52.22977778, 21.01188889"}}
                    },
                    new() {
                        { "species", "Wild cherry"},
                        { "height", 15.7},
                        { "position", new List<string> {"52.23243333, 21.01019167"}}
                    },
                    new() {
                        { "species", "Wild cherry"},
                        {"height", 11.43},
                        {"position", new List<string> {"52.23289444, 21.01151944"}}
                    },
                    new() {
                        {"species", "Lavallée's hawthorn"},
                        {"height", 5.25},
                        {"position", new List<string> {"52.23304444, 21.01290833"}}
                    } 
                }
            }
        };

        return jsonData;
    }

    public IFormFile GetMockFile(Dictionary<string, dynamic?>  jsonData) {
        
        var json = JsonSerializer.Serialize(jsonData);
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);

        return new FormFile(
            baseStream: stream,
            baseStreamOffset: 0,
            length: stream.Length,
            name: "file",
            fileName: "data.json")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/json",
            ContentDisposition =
                $"form-data; name=\"file\"; filename=\"data.json\""
        };
    }
    
    public XSchemaSeries GetMockXSchemaSeries(string id, string title)
    {
        var xSchemaSeries = new XSchemaSeries
        {
            Uri = id,
            Title = title,
            Description = "This is our new schema series.",
            Type = new List<short>() {2}
        };
        return xSchemaSeries;
    }
    
    public XCatalogue GetMockXCatalogue(string id, Dictionary<string,string> titles, string? organisationId, 
        string? personId,  string? partOfId = null, List<string>? contactsUris = null)
    {
        var xCatalogue = new XCatalogue
        {
            Uri = id,
            Title = titles,
            Description = GetDescriptions(titles),
            PartOfUri = partOfId,
            ResponsibleOrganisationUri = organisationId,
            ResponsiblePersonUri = personId,
            ContactsUris = contactsUris ?? []
        };
        return xCatalogue;
    }

    public XDatasetSeries GetMockXSeries(string id, Dictionary<string, string> titles,
        string organisationId, string personId, string? catalogueId = null)
    {
        XDatasetSeries xSeries = new XDatasetSeries
        {
            Uri = id,
            Title = titles,
            Description = GetDescriptions(titles),
            AccessRights = (short)AccessRightsType.Provisional,
            CatalogueUri = catalogueId ?? "null",
            ResponsibleOrganisationUri = organisationId,
            ResponsiblePersonUri = personId,
        };
        return xSeries;
    }

    public XDataset GetMockXDataset(string id, Dictionary<string, string> titles, string catalogueId,
        string? organisationId = null, string? personId = null, ICollection<string>? series = null,
        string? schemaId = null, bool isSource = false)
    {
        XDataset xDataset = new XDataset()
        {
            Uri = id,
            Title = titles,
            Description = GetDescriptions(titles),
            CatalogueUri = catalogueId,
            Status = isSource ? (short)DatasetStatus.Source : (short)DatasetStatus.Active,
            Type = new List<short>(),
            AccessRights = (short)AccessRightsType.Provisional,
            SeriesUris = series ?? new List<string>(),
            Languages = new List<string>() { "en", "pl", "it", "sk", "sl", "de", "hr" },
            ReleaseDate = DateTime.Today.ToLocalTime(),
            Frequency = "DAILY",
            GeographicalCoverage = new List<string>(),
            TemporalCoverage = new List<(DateTime, DateTime)>()
            {
                (DateTime.Today, DateTime.Now),
                (DateTime.Today - TimeSpan.FromDays(1), DateTime.Now - TimeSpan.FromDays(1)),
            },
            ResponsibleOrganisationUri = organisationId,
            ResponsiblePersonUri = personId,
            Keywords = new List<string>() { "dataset", "example", "delete it" },
            Version = "1.0",
            SchemaUri = schemaId
        };

        //GeoCoordinate geoCoordinate = new GeoCoordinate(){ Longitude = 15.3, Latitude = 21.5 };
        //xDataset.GeographicalCoverage.Add(JsonConvert.SerializeObject(geoCoordinate));
        //geoCoordinate = new GeoCoordinate(){ Longitude = 14.3, Latitude = 21.6 };
        //xDataset.GeographicalCoverage.Add(JsonConvert.SerializeObject(geoCoordinate));

        return xDataset;
    }

    public XOrganisation GetMockXOrganisation(string id, string name, List<string> contactIds,
        List<string>? memberIds = null)
    {
        XOrganisation xOrganisation = new XOrganisation
        {
            Name = name,
            Uri = id,
            ContactsUris = contactIds,
            MembersUris = memberIds ?? new List<string>(),
        };
        return xOrganisation;
    }

    public XPerson GetMockXPerson(string id, string name, List<string> contactIds, List<string> organisationIds)
    {
        var possibleGivenNames = new List<List<string>>()
        {
            new List<string>() {"Adam", "Jan"},
            new List<string>() {"Alex", "Adrian"},
            new List<string>() {"Albert", "Oskar"},
        };

        int index = name[0] switch {
            'K' => 0,
            'S' => 1,
            _ => 2
        };

        XPerson xPerson = new XPerson()
        {
            FamilyName = name,
            GivenNames = possibleGivenNames[index],
            Uri = id,
            ContactsUris = contactIds,
            OrganisationsUris = organisationIds
        };
        return xPerson;
    }

    public List<XContactData> GetMockContacts(string id, List<string> contents)
    {
        List<XContactData> xContactDataList = new List<XContactData>();

        XContactData xContactData = new XContactData()
        {
            Uri = id + "1",
            Name = "Email",
            Contents = contents[0],
        };
        xContactDataList.Add(xContactData);

        xContactData = new XContactData()
        {
            Uri = id + "2",
            Name = "Tel",
            Contents = contents[1]
        };
        xContactDataList.Add(xContactData);

        return xContactDataList;
    }

    public XDistribution GetMockXDistribution(string id, Dictionary<string, string> names, string datasetId, string? serviceId = null)
    {
        XDistribution xDistribution = new XDistribution
        {
            Status = "DEPRECATED",
            Uri = id,
            Title = names,
            Description = GetDescriptions(names),
            DatasetUri = datasetId,
            AccessUrl = new List<string>() { "https://doorce.com/distributions/new-distrib" },
            Format = "application/json",
            MediaType = "application/json",
            AccessStatus = 0,
            DataServiceUri = serviceId
        };
        return xDistribution;
    }

    public XDataService GetMockXDataService(string id, Dictionary<string, string> names, string catalogueId, 
        string organisationId, string personId)
    {
        XDataService xDataService = new XDataService
        {
            Status = 0,
            Uri = id,
            Title = names,
            Description = GetDescriptions(names),
            AccessRights = 0,
            CatalogueUri = catalogueId,
            ResponsibleOrganisationUri = organisationId,
            ResponsiblePersonUri = personId
        };
        return xDataService;
    }
    
    public XAppTemplate GetMockXAppTemplate(string id, string name, string schemaId, string conceptName)
    {
        XDefaultAppTemplate defaultTemplate = XDefaultAppTemplateFactory.Get(conceptName, "en");
        XAppTemplate xAppTemplate = new XAppTemplate
        {
            Uri = id,
            Title = name,
            Description = "This is a template for an application that analyses some data.",
            SchemaUri = schemaId,
            Language = "en",
            IsReady = false,
            UseCaseScenarios = defaultTemplate.UseCaseScenarios,
            AuxiliaryConcepts = defaultTemplate.AuxiliaryConcepts
        };
        return xAppTemplate;
    }
    
    public XAppCreationRequest GetMockXApp(string id, string name, string templateId,
        string activeDatasetId, List<string> sourceDatasetIds)
    {
        XAppCreationRequest xApp = new XAppCreationRequest
        {
            Uri = id,
            Title = name,
            Description = "This application analyses some data from various datasets.",
            TemplateUri = templateId,
            ActiveResourceUri = activeDatasetId,
            SourceResourceUris = sourceDatasetIds,
            IsVisible = true,
            IsEnabled = false,
        };
        return xApp;
    }

    public XDataItem GetMockXDataItem(string conceptUri, string datasetUri, string id, Dictionary<string, object> values) {
        XDataItem xDataItem = new XDataItem {
            ConceptUri = conceptUri,
            DataSetUri = datasetUri,
            Identifier = id,
            Values = values
        };
        return xDataItem;
    }

    private Dictionary<string, string> GetDescriptions(Dictionary<string, string> titles)
    {
        Dictionary<string, string> descriptions = new Dictionary<string, string>();
        foreach (var title in titles)
        {
            string description = title.Key switch {
                "en" => $"This is the description of \"{title.Value}\".",
                "pl" => $"To jest opis \"{title.Value}\".",
                "it" => $"Questa è la descrizione di \"{title.Value}\".",
                "sk" => $"Toto je popis \"{title.Value}\".",
                "sl" => $"To je opis \"{title.Value}\".",
                "hr" => $"Ovo je opis \"{title.Value}\".",
                "de" => $"Dies ist die Beschreibung von \"{title.Value}\".",
                _ => $"...\"{title.Value}\"..."
            };
            descriptions.Add(title.Key, description);
        }
        return descriptions;
    }
    
    // ***** EQUALITY CHECKERS *****
    
    private bool CheckIdentifiableElementEquality(XIdentifiableElement x1, XIdentifiableElement x2)
    {
        return x1.Uri == x2.Uri;
    }
    
    private bool CheckManageableResourceEquality(XManageableResource x1, XManageableResource x2)
    {
        // check if parent properties are equal
        if (!CheckIdentifiableElementEquality(x1, x2))
        {
            return false;
        }
        
        // check if resources have the same editors
        if (x1.EditorsUris.Count != x2.EditorsUris.Count || x1.EditorsUris.Except(x2.EditorsUris).Any()) return false;

        // check if resources have the same editor roles
        if (x1.EditorsRoles.Count != x2.EditorsRoles.Count ||
            !x1.EditorsRoles.All(kv => x2.EditorsRoles.ContainsKey(kv.Key) 
                                       && !x2.EditorsRoles[kv.Key].Except(kv.Value).Any()))
            return false;
        
        // check if resources have the same user roles
        if (x1.UserRoles != null && x2.UserRoles != null )
        {
            return x1.UserRoles.Count == x2.UserRoles.Count || x1.UserRoles.All(role => x2.UserRoles.Contains(role));
        }

        return x2.UserRoles == null && x2.UserRoles == null;
            
    }
    
    private bool CheckOwnableResourceEquality(XOwnableResource x1, XOwnableResource x2)
    {
        // check if parent properties are equal
        if (!CheckManageableResourceEquality(x1, x2))
        {
            return false;
        }

        // check if resources have the same title and description
        if (x1.Title.Count != x2.Title.Count || 
            x1.Title.Any(kv => !x2.Title.TryGetValue(kv.Key, out var v) ||
                               v != kv.Value)) return false;
        if (x1.Description.Count != x2.Description.Count ||
            x1.Description.Any(kv => !x2.Description.TryGetValue(kv.Key, out var v) ||
                                     v != kv.Value)) return false;

        // check if resources have the same contacts
        if (x1.ContactsUris.Count != x2.ContactsUris.Count || x1.ContactsUris.Except(x2.ContactsUris).Any()) return false;

        // check if resources have the same values
        return x1.IconUri == x2.IconUri &&
               x1.ResponsiblePersonUri == x2.ResponsiblePersonUri &&
               x1.ResponsiblePersonName == x2.ResponsiblePersonName &&
               x1.ResponsibleOrganisationUri == x2.ResponsibleOrganisationUri &&
               x1.ResponsibleOrganisationName == x2.ResponsibleOrganisationName;
    }

    private bool CheckCataloguedResourceEquality(XCataloguedResource x1, XCataloguedResource x2)
    {
        // check if parent properties are equal
        if (!CheckOwnableResourceEquality(x1, x2))
        {
            return false;
        }

        // check if resources have the same languages, keywords, themes, applicable legislations, licenses
        if (x1.Languages.Count != x2.Languages.Count || x1.Languages.Except(x2.Languages).Any()) return false;
        if (x1.Keywords.Count != x2.Keywords.Count || x1.Keywords.Except(x2.Keywords).Any()) return false;
        if (x1.Themes.Count != x2.Themes.Count || x1.Themes.Except(x2.Themes).Any()) return false;
        if (x1.ApplicableLegislations.Count != x2.ApplicableLegislations.Count ||
            x1.ApplicableLegislations.Except(x2.ApplicableLegislations).Any()) return false;
        if (x1.Licences.Count != x2.Licences.Count || x1.Licences.Except(x2.Licences).Any()) return false;

        // check if resources are in the same catalogue
        if (x1.CatalogueUri != x2.CatalogueUri) return false;
        if (x1.CatalogueTitle.Count != x2.CatalogueTitle.Count ||
            x1.CatalogueTitle.Any(kv => !x2.CatalogueTitle.TryGetValue(kv.Key, out var v) ||
                                        v != kv.Value)) return false;

        // check if resources have the same access rights
        return x1.AccessRights == x2.AccessRights;
    }
    
    private bool CheckDataResourceEquality(XDataResource x1, XDataResource x2)
    {
        // check if parent properties are equal
        if (!CheckCataloguedResourceEquality(x1, x2))
        {
            return false;
        }

        // check if resources have the same geographical coverage
        if (x1.GeographicalCoverage.Count != x2.GeographicalCoverage.Count ||
            x1.GeographicalCoverage.Except(x2.GeographicalCoverage).Any()) return false;

        // check if resources have the same frequency
        if (x1.Frequency != x2.Frequency) return false;

        // check if resources have the same temporal coverage
        if (x1.TemporalCoverage.Count != x2.TemporalCoverage.Count ||
            x1.TemporalCoverage.Except(x2.TemporalCoverage).Any()) return false;

        // check if resources have the same release date
        
        return null == x1.ReleaseDate && null == x2.ReleaseDate ||
               null != x1.ReleaseDate && null != x2.ReleaseDate && ((DateTime) x1.ReleaseDate).ToUniversalTime() == ((DateTime) x2.ReleaseDate).ToUniversalTime();
    }

    public bool CheckDatasetSeriesEquality(XDatasetSeries x1, XDatasetSeries x2)
    {
        // check if parent properties are equal
        return CheckDataResourceEquality(x1, x2);
    }

    public bool CheckDatasetEquality(XDataset x1, XDataset x2)
    {
        // check if parent properties are equal
        if (!CheckDataResourceEquality(x1, x2))
        {
            return false;
        }
        
        // check if datasets have the same type, documentation, provenance, seriesuris
        if (x1.Type.Count != x2.Type.Count ||
            x1.Type.Except(x2.Type).Any()) return false;
        if (x1.Documentation.Count != x2.Documentation.Count ||
            x1.Documentation.Except(x2.Documentation).Any()) return false;
        if (x1.Provenance.Count != x2.Provenance.Count ||
            x1.Provenance.Except(x2.Provenance).Any()) return false;
        if (x1.SeriesUris.Count != x2.SeriesUris.Count ||
            x1.SeriesUris.Except(x2.SeriesUris).Any()) return false;
        
        // check if datasets are in the same series
        if (x1.SeriesTitles.Count != x2.SeriesTitles.Count || x1.SeriesTitles.Keys.Except(x2.SeriesTitles.Keys).Any()) return false;
        
        // check if series have the same titles
        foreach (var seriesUri in x1.SeriesTitles.Keys)
        {
            var titles1 = x1.SeriesTitles[seriesUri];
            var titles2 = x2.SeriesTitles[seriesUri];
            if (titles1.Count != titles2.Count ||
                titles1.Any(kv => !titles2.TryGetValue(kv.Key, out var v) ||
                                  v != kv.Value)) return false;
        }
        
        // check if datasets have the same values
        return x1.Status == x2.Status &&
               x1.Version == x2.Version &&
               x1.VersionNotes == x2.VersionNotes &&
               x1.SchemaUri == x2.SchemaUri &&
               x1.SchemaTitle == x2.SchemaTitle &&
               x1.TargetDatasetUri == x2.TargetDatasetUri;
    }
    
    public bool CheckCatalogueEquality(XCatalogue x1, XCatalogue x2)
    {
        // check if parent properties are equal
        if (!CheckOwnableResourceEquality(x1, x2))
        {
            return false;
        }
        
        // check if catalogues are in the same parent catalogue
        if (x1.PartOfTitle.Count != x2.PartOfTitle.Count ||
            x1.PartOfTitle.Any(kv => !x2.PartOfTitle.TryGetValue(kv.Key, out var v) ||
                                     v != kv.Value)) return false;

        return x1.PartOfUri == x2.PartOfUri;
    }
    
    public bool CheckDistributionEquality(XDistribution x1, XDistribution x2)
    {
        // check if parent properties are equal
        if (!CheckIdentifiableElementEquality(x1, x2))
        {
            return false;
        }
        
        // check if distributions have the same titles and descriptions
        if (x1.Title.Count != x2.Title.Count ||
            x1.Title.Any(kv => !x2.Title.TryGetValue(kv.Key, out var v) ||
                               v != kv.Value)) return false;
        if (x1.Description.Count != x2.Description.Count ||
            x1.Description.Any(kv => !x2.Description.TryGetValue(kv.Key, out var v) ||
                                     v != kv.Value)) return false;
        
        // check if distributions have the same dataset
        if (x1.DatasetUri != x2.DatasetUri) return false;
        if (x1.DatasetTitle.Count != x2.DatasetTitle.Count ||
            x1.DatasetTitle.Any(kv => !x2.DatasetTitle.TryGetValue(kv.Key, out var v) ||
                                      v != kv.Value)) return false;
        
        // check if distributions have the same data service
        if (x1.DataServiceUri != x2.DataServiceUri) return false;
        if (x1.DataServiceTitle?.Count != x2.DataServiceTitle?.Count ||
            x1.DataServiceTitle!.Any(kv => !x2.DataServiceTitle!.TryGetValue(kv.Key, out var v) ||
                                           v != kv.Value)) return false;
        
        // check if distributions have the same access urls, languages
        if (x1.AccessUrl.Count != x2.AccessUrl.Count || x1.AccessUrl.Except(x2.AccessUrl).Any()) return false;
        if (x1.Languages.Count != x2.Languages.Count || x1.Languages.Except(x2.Languages).Any()) return false;
        
        // check if distributions have the same values
        return x1.Status == x2.Status &&
               x1.SchemaUri == x2.SchemaUri &&
               x1.SchemaTitle == x2.SchemaTitle &&
               x1.Format == x2.Format &&
               x1.AccessStatus == x2.AccessStatus &&
               x1.ByteSize == x2.ByteSize &&
               x1.MediaType == x2.MediaType &&
               x1.ReleaseDate == x2.ReleaseDate &&
               x1.DatasetIconUri == x2.DatasetIconUri &&
               x1.FileId == x2.FileId &&
               x1.FileName == x2.FileName &&
               x1.IsUserEditable == x2.IsUserEditable;
    }
    
    private bool CheckAgentEquality(XAgent x1, XAgent x2)
    {
        // check if parent properties are equal
        if (!CheckIdentifiableElementEquality(x1, x2))
        {
            return false;
        }
        
        // check if agents have the same contacts
        if (x1.ContactsUris.Count != x2.ContactsUris.Count || x1.ContactsUris.Except(x2.ContactsUris).Any()) return false;
        
        // check if agents have the same user roles
        if (x1.UserRoles?.Count != x2.UserRoles?.Count || !x1.UserRoles!.All(role => x2.UserRoles!.Contains(role)))
            return false;
        
        // check if agents have the same values
        return x1.Description == x2.Description &&
               x1.IsOrganisation == x2.IsOrganisation;
    }
    
    public bool CheckOrganisationEquality(XOrganisation x1, XOrganisation x2)
    {
        // check if parent properties are equal
        if (!CheckAgentEquality(x1, x2))
        {
            return false;
        }
        
        // check if organisations have the same name and members
        if (x1.MembersUris.Count != x2.MembersUris.Count || x1.MembersUris.Except(x2.MembersUris).Any()) return false;
        if (x1.MembersRoles.Count != x2.MembersRoles.Count || 
            !x1.MembersRoles.All(kv => x2.MembersRoles.ContainsKey(kv.Key) 
                                       && x2.MembersRoles[kv.Key].Equals(kv.Value)))
            return false;

        return x1.Name == x2.Name;
    }
    
    public bool CheckPersonEquality(XPerson x1, XPerson x2)
    {
        // check if parent properties are equal
        if (!CheckAgentEquality(x1, x2))
        {
            return false;
        }
        
        // check if persons have the same family name, given names, organisations
        if (x1.FamilyName != x2.FamilyName) return false;
        
        if (x1.GivenNames.Count != x2.GivenNames.Count || x1.GivenNames.Except(x2.GivenNames).Any()) return false;
        
        if (x1.OrganisationsUris.Count != x2.OrganisationsUris.Count ||
            x1.OrganisationsUris.Except(x2.OrganisationsUris).Any()) return false;
        
        if (x1.OrganisationsNames.Count != x2.OrganisationsNames.Count ||
            x1.OrganisationsNames.Any(kv => !x2.OrganisationsNames.TryGetValue(kv.Key, out var v) ||
                                            v != kv.Value)) return false;
        
        if (x1.RolesInOrganisations.Count != x2.RolesInOrganisations.Count ||
            !x1.RolesInOrganisations.All(kv => x2.RolesInOrganisations.ContainsKey(kv.Key)
                && x2.RolesInOrganisations[kv.Key].Equals(kv.Value)))
            return false;
        
        return x1.UserId == x2.UserId;
    }
    
    public bool CheckDataServiceEquality(XDataService x1, XDataService x2)
    {
        // check if parent properties are equal
        if (!CheckCataloguedResourceEquality(x1, x2))
        {
            return false;
        }
        
        // check if data services have the same values
        if (x1.EndpointUrl.Count != x2.EndpointUrl.Count || x1.EndpointUrl.Except(x2.EndpointUrl).Any()) return false;
        if (x1.EndpointDescription.Count != x2.EndpointDescription.Count || x1.EndpointDescription.Except(x2.EndpointDescription).Any()) return false;
        if (x1.Documentation.Count != x2.Documentation.Count || x1.Documentation.Except(x2.Documentation).Any()) return false;
        if (x1.Format.Count != x2.Format.Count || x1.Format.Except(x2.Format).Any()) return false;
        if (x1.Status != x2.Status) return false;
        if (x1.StandardUris.Count != x2.StandardUris.Count || x1.StandardUris.Except(x2.StandardUris).Any()) return false;
        if (x1.StandardTitles.Count != x2.StandardTitles.Count || x1.StandardTitles.Except(x2.StandardTitles).Any()) return false;

        return true;
    }
    
    public bool CheckSchemaSeriesEquality(XSchemaSeries x1, XSchemaSeries x2)
    {
        // check if parent properties are equal
        if (!CheckManageableResourceEquality(x1, x2))
        {
            return false;
        }
        
        // check if elements have the same title and description
        if (x1.Title != x2.Title) return false;
        if (x1.Description != x2.Description) return false;
        
        // check if schema series have the same values
        if (x1.Type.Count != x2.Type.Count || x1.Type.Except(x2.Type).Any()) return false;
        if (x1.CurrentSchemaUri != x2.CurrentSchemaUri) return false;
        if (x1.CurrentSchemaTitle != x2.CurrentSchemaTitle) return false;

        return true;
    }
    
    public bool CheckXDataSchemaEquality(XDataSchema x1, XDataSchema x2)
    {
        // check if parent properties are equal
        if (!CheckIdentifiableElementEquality(x1, x2))
        {
            return false;
        }
        
        // check if elements have the same title and description
        if (x1.Title != x2.Title) return false;
        if (x1.Description != x2.Description) return false;
        
        // check if schemas have the same concepts
        if (x1.Concepts.Count != x2.Concepts.Count) return false;
        
        foreach (var concept in x1.Concepts)
        {
            if (!x2.Concepts.Any(c => CheckXConceptEquality(concept, c)))
            {
                return false;
            }
        }
        
        if (x1.MainConceptPrefix != x2.MainConceptPrefix) return false;
        if (x1.MainConceptName != x2.MainConceptName) return false;
        
        // check if schemas have the same namespaces
        if (x1.UsedNamespaces.Count != x2.UsedNamespaces.Count) return false;
        foreach (var n1 in x1.UsedNamespaces)
        {
            if (!x2.UsedNamespaces.Any(n2 => CheckXNamespaceEquality(n1, n2)))
            {
                return false;
            }
        }
        
        if (x1.DefaultNamespacePrefix != x2.DefaultNamespacePrefix) return false;
        if (x1.DefaultNamespaceIri != x2.DefaultNamespaceIri) return false;
        
        // check if schema series have the same values
        if (x1.SeriesUri != x2.SeriesUri) return false;
        if (x1.SeriesTitle != x2.SeriesTitle) return false;
        
        // check if schemas have the same isUserEditable property
        return x1.IsUserEditable == x2.IsUserEditable;
    }

    private bool CheckXNamespaceElementValueEquality(XNamespaceElementValue x1, XNamespaceElementValue x2)
    {
        // check if elements have the same type and description
        if (x1.Type != x2.Type) return false;
        if (x1.Description != x2.Description) return false;

        // check if elements have the same namespace prefix
        return x1.NamespacePrefix == x2.NamespacePrefix;
    }

    private bool CheckXConceptEquality(XConcept x1, XConcept x2)
    {
        // check if parent properties are equal
        if (!CheckXNamespaceElementValueEquality(x1, x2))
        {
            return false;
        }
        
        // check if concepts have the same name and description
        if (x1.Name != x2.Name) return false;

        // check if concepts have the same properties
        if (x1.Properties.Count != x2.Properties.Count || !x1.Properties.Keys.SequenceEqual(x2.Properties.Keys))
            return false;
        
        if (x1.Properties.Any(kv => !CheckXPropertyValueEquality(kv.Value, x2.Properties[kv.Key])))
        {
            return false;
        }

        // check if concepts have the same required and unique properties
        return x1.Required.SequenceEqual(x2.Required) && x1.Unique.SequenceEqual(x2.Unique);
    }

    private bool CheckXPropertyValueEquality(XPropertyValue x1, XPropertyValue x2)
    {
        // check if parent properties are equal
        if (!CheckXNamespaceElementValueEquality(x1, x2))
        {
            return false;
        }

        // check if property values have the same target
        if (x1.Target != x2.Target) return false;

        // check if property values have the same items
        if (x1.Items != null && x2.Items != null)
        {
            return CheckXPropertyValueEquality(x1.Items, x2.Items);
        }
        
        return x1.Items == null && x2.Items == null;
    }

    private bool CheckXNamespaceEquality(XNamespace x1, XNamespace x2)
    {
        // check if namespaces have the same values
        return x1.Iri == x2.Iri &&
               x1.Prefix == x2.Prefix &&
               x1.IsCustom == x2.IsCustom;
    }
}