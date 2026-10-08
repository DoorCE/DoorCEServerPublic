using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.SchemaManager.Labels;
using DoorCEServer.Tests.SchemaManager.MockDtos;

namespace DoorCEServer.Tests.CataloguingManager.MockDtos;

public class XDatasetMock
{
    private readonly Dictionary<DatasetLabel, List<Dictionary<VariantLabel, XDataset>>> _xDatasets;
    private readonly Dictionary<DatasetLabel, List<Dictionary<ContactsLabel, List<XContactData>>>> _xDatasetContacts;
    private readonly TestCommon _testCommon;
    
    private readonly XOrganisationMock _orgMock;
    private readonly XContactsMock _contactsMock;
    private readonly XPersonMock _personMock;
    private readonly XCatalogueMock _catMock;
    private readonly XDatasetSeriesMock _dseriesMock;
    private readonly XSchemaMock _schemaMock;
    
    public XDatasetMock()
    {
        _xDatasets = new Dictionary<DatasetLabel, List<Dictionary<VariantLabel, XDataset>>>();
        _xDatasetContacts = new Dictionary<DatasetLabel, List<Dictionary<ContactsLabel, List<XContactData>>>>();
        _testCommon = new TestCommon();
        
        _contactsMock = new XContactsMock();
        _orgMock = new XOrganisationMock();
        _personMock = new XPersonMock();
        _catMock = new XCatalogueMock();
        _dseriesMock = new XDatasetSeriesMock();
        _schemaMock =  new XSchemaMock();
        
        Setup();
    }
    
    public XDataset Get(DatasetLabel datasetLabel, VariantLabel variantLabel)
    {
        try
        {
            var datasetList = _xDatasets[datasetLabel];
            var dictWithKey = datasetList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock dataset not found: {datasetLabel}, {variantLabel}");
        }
    }
    
    public List<XContactData> GetContacts(DatasetLabel datasetLabel, ContactsLabel contactsLabel)
    {
        try
        {
            var contactsList = _xDatasetContacts[datasetLabel];
            var dictWithKey = contactsList.First(d => d.ContainsKey(contactsLabel));
            return dictWithKey[contactsLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock dataset contacts not found: {datasetLabel}, {contactsLabel}");
        }
    }
    
    private void Add(XDataset xDataset, DatasetLabel datasetLabel, VariantLabel variantLabel)
    {
        if (_xDatasets.TryGetValue(datasetLabel, out var dataset))
        {
            dataset.Add(new Dictionary<VariantLabel, XDataset>{{variantLabel, xDataset}});
        }
        else
        {
            _xDatasets.Add(datasetLabel, [new Dictionary<VariantLabel, XDataset>{{variantLabel, xDataset}}]);
        }
    }

    private void AddToContacts(List<XContactData> xContacts, DatasetLabel datasetLabel, ContactsLabel contactsLabel)
    {
        if (_xDatasetContacts.TryGetValue(datasetLabel, out var contact))
        {
            contact.Add(new Dictionary<ContactsLabel, List<XContactData>>
                { { contactsLabel, xContacts } });
        }
        else
        {
            _xDatasetContacts.Add(datasetLabel,
                [new Dictionary<ContactsLabel, List<XContactData>> { { contactsLabel, xContacts } }]);
        }
    }

    private void Setup()
    {
        // create contacts
        List<XContactData> basicContacts1 = _contactsMock.Get(ContactsLabel.BasicDatasetContacts1);
        List<string> basicContacts1List = basicContacts1.Select(c => c.Uri).ToList()!;

        List<XContactData> basicContacts2 = _contactsMock.Get(ContactsLabel.BasicDatasetContacts2);
        List<string> basicContacts2List = basicContacts2.Select(c => c.Uri).ToList()!;

        List<XContactData> basicContactsCombined = _contactsMock.Get(ContactsLabel.BasicDatasetContacts1);
        List<string> basicContactsCombinedList = basicContactsCombined.Select(c => c.Uri).ToList()!;
        basicContactsCombinedList.AddRange(basicContacts2List);
        
        List<XContactData> extraPersonContacts = _personMock.GetContacts(PersonLabel.ExtraPerson, ContactsLabel.Contacts1);
        List<string> extraPersonContactsList = extraPersonContacts.Select(c => c.Uri).ToList()!;
        
        List<XContactData> extraOrgContacts = _orgMock.GetContacts(OrgLabel.ExtraOrg, ContactsLabel.Contacts1);
        List<string> extraOrgContactsList = extraOrgContacts.Select(c => c.Uri).ToList()!;

        // create organisations, persons, catalogues, series, schemas
        XOrganisation basicOrg = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.AfterUpsert);
        XOrganisation basicOrgModified = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.ModifiedAfterUpsert);
        XOrganisation extraOrg = _orgMock.Get(OrgLabel.ExtraOrg, VariantLabel.AfterUpsert);
        
        XPerson basicPerson = _personMock.Get(PersonLabel.PersonWithOrg, VariantLabel.AfterUpsert);
        XPerson basicPersonModified = _personMock.Get(PersonLabel.PersonWithOrg, VariantLabel.ModifiedAfterUpsert);
        XPerson extraPerson = _personMock.Get(PersonLabel.ExtraPerson, VariantLabel.AfterUpsert);
        
        XCatalogue basicCat = _catMock.Get(CatLabel.BasicCat, VariantLabel.AfterUpsert);
        XCatalogue basicCatModified = _catMock.Get(CatLabel.BasicCat, VariantLabel.ModifiedAfterUpsert);
        
        XDatasetSeries basicDSeries = _dseriesMock.Get(DSeriesLabel.DSeriesWithCat, VariantLabel.AfterUpsert);
        XDatasetSeries basicDSeriesModified = _dseriesMock.Get(DSeriesLabel.DSeriesWithCat, VariantLabel.ModifiedAfterUpsert);

        XDataSchema basicSchema = _schemaMock.Get(SchemaLabel.BasicSchema, VariantLabel.AfterUpsert);

        // create datasets and add them to dict
        AddToContacts(basicContacts1, DatasetLabel.BasicDataset, ContactsLabel.Contacts1);
        AddToContacts(basicContacts1, DatasetLabel.DatasetWithSeries, ContactsLabel.Contacts1);
        AddToContacts(basicContacts1, DatasetLabel.BasicDatasetWithSchema, ContactsLabel.Contacts1);
        AddToContacts(basicContacts1, DatasetLabel.BasicDatasetIndependent, ContactsLabel.Contacts1);
        AddToContacts(basicContacts2, DatasetLabel.BasicDataset, ContactsLabel.Contacts2);
        AddToContacts(basicContacts2, DatasetLabel.DatasetWithSeries, ContactsLabel.Contacts2);
        AddToContacts(basicContacts2, DatasetLabel.BasicDatasetWithSchema, ContactsLabel.Contacts2);
        AddToContacts(basicContacts2, DatasetLabel.BasicDatasetIndependent, ContactsLabel.Contacts2);
        
        // basic dataset
        XDataset basicDatasetBeforeUpsert = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string> { { "en", "New dataset" }, { "pl", "Nowy zestaw danych" }, { "it", "Nuovo set di dati" } },
            basicCat.Uri!,
            basicOrg.Uri,
            basicPerson.Uri
        );
        Add(basicDatasetBeforeUpsert, DatasetLabel.BasicDataset, VariantLabel.BeforeUpsert);

        XDataset basicDatasetWithSchema = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string>
                { { "en", "New dataset" }, { "pl", "Nowy zestaw danych" }, { "it", "Nuovo set di dati" } },
            basicCat.Uri!,
            basicOrg.Uri,
            null,
            null,
            basicSchema.Uri
        );
        Add(basicDatasetWithSchema, DatasetLabel.BasicDatasetWithSchema, VariantLabel.BeforeUpsert);
        Add(basicDatasetWithSchema, DatasetLabel.BasicDatasetWithSchema, VariantLabel.AfterUpsert);

        XDataset basicDatasetIndependent = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string>
                { { "en", "New dataset" }, { "pl", "Nowy zestaw danych" }, { "it", "Nuovo set di dati" } },
            basicCat.Uri!,
            basicOrg.Uri,
            null,
            null,
            basicSchema.Uri
        );
        basicDatasetIndependent.Status = (int)DatasetStatus.Independent;
        Add(basicDatasetIndependent, DatasetLabel.BasicDatasetIndependent, VariantLabel.BeforeUpsert);
        Add(basicDatasetIndependent, DatasetLabel.BasicDatasetIndependent, VariantLabel.AfterUpsert);
        
        var basicDatasetAfterUpsert = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string> { { "en", "New dataset" }, { "pl", "Nowy zestaw danych" }, { "it", "Nuovo set di dati" } },
            basicCat.Uri!,
            basicOrg.Uri,
            basicPerson.Uri
        );
        basicDatasetAfterUpsert.TemporalCoverage = basicDatasetBeforeUpsert.TemporalCoverage;
        basicDatasetAfterUpsert.ContactsUris = basicContacts1List;
        basicDatasetAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPerson.GivenNames)} {basicPerson.FamilyName}";
        basicDatasetAfterUpsert.ResponsibleOrganisationName = basicOrg.Name;
        basicDatasetAfterUpsert.CatalogueTitle = basicCat.Title;
        Add(basicDatasetAfterUpsert, DatasetLabel.BasicDataset, VariantLabel.AfterUpsert);
        
        var basicDatasetModifiedBeforeUpsert = BasicDatasetModifiedBeforeUpsert(
            basicOrgModified, basicPersonModified, basicCatModified, basicContacts1List);
        Add(basicDatasetModifiedBeforeUpsert, DatasetLabel.BasicDataset, VariantLabel.ModifiedBeforeUpsert);

        var basicDatasetModifiedAfterUpsert = BasicDatasetModifiedBeforeUpsert(
            basicOrgModified, basicPersonModified, basicCatModified, basicContactsCombinedList);
        basicDatasetModifiedAfterUpsert.TemporalCoverage =
            basicDatasetModifiedBeforeUpsert.TemporalCoverage;
        basicDatasetModifiedAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPersonModified.GivenNames)} {basicPersonModified.FamilyName}";
        basicDatasetModifiedAfterUpsert.ResponsibleOrganisationName = basicOrgModified.Name;
        basicDatasetModifiedAfterUpsert.CatalogueTitle = basicCatModified.Title;
        Add(basicDatasetModifiedAfterUpsert, DatasetLabel.BasicDataset, VariantLabel.ModifiedAfterUpsert);
        
        // basic dataset with person contacts
        
        XDataset basicDatasetWithPersonContactsBeforeUpsert = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string> { { "en", "New dataset" }, { "pl", "Nowy zestaw danych" }, { "it", "Nuovo set di dati" } },
            basicCat.Uri!,
            basicOrg.Uri,
            basicPerson.Uri
        );
        basicDatasetWithPersonContactsBeforeUpsert.ContactsUris = extraPersonContactsList;
        basicDatasetWithPersonContactsBeforeUpsert.EditorsUris = [extraPerson.Uri!];
        basicDatasetWithPersonContactsBeforeUpsert.EditorsRoles = new Dictionary<string, ICollection<EditorRole>>()
        {
            { extraPerson.Uri!, [EditorRole.MetadataEditor] }
        };
        Add(basicDatasetWithPersonContactsBeforeUpsert, DatasetLabel.BasicDataset, VariantLabel.WithExtraPersonContacts);
        
        // basic dataset with organisation contacts
        XDataset basicDatasetWithOrgContactsBeforeUpsert = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string> { { "en", "New dataset" }, { "pl", "Nowy zestaw danych" }, { "it", "Nuovo set di dati" } },
            basicCat.Uri!,
            basicOrg.Uri,
            basicPerson.Uri
        );
        basicDatasetWithOrgContactsBeforeUpsert.ContactsUris = extraOrgContactsList;
        basicDatasetWithOrgContactsBeforeUpsert.EditorsUris = [extraOrg.Uri!];
        basicDatasetWithOrgContactsBeforeUpsert.EditorsRoles = new Dictionary<string, ICollection<EditorRole>>()
        {
            { extraOrg.Uri!, [EditorRole.MetadataEditor] }
        };
        Add(basicDatasetWithOrgContactsBeforeUpsert, DatasetLabel.BasicDataset, VariantLabel.WithExtraOrgContacts);
        
        // dataset with series
        XDataset basicDatasetWithSeriesBeforeUpsert = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string> { { "en", "New dataset" }, { "pl", "Nowy zestaw danych" }, { "it", "Nuovo set di dati" } },
            basicCat.Uri!,
            basicOrg.Uri,
            basicPerson.Uri,
            new List<string> {basicDSeries.Uri!}
        );
        Add(basicDatasetWithSeriesBeforeUpsert, DatasetLabel.DatasetWithSeries, VariantLabel.BeforeUpsert);

        var basicDatasetWithSeriesAfterUpsert = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string> { { "en", "New dataset" }, { "pl", "Nowy zestaw danych" }, { "it", "Nuovo set di dati" } },
            basicCat.Uri!,
            basicOrg.Uri,
            basicPerson.Uri,
            new List<string> {basicDSeries.Uri!}
        );
        basicDatasetWithSeriesAfterUpsert.TemporalCoverage = basicDatasetWithSeriesBeforeUpsert.TemporalCoverage;
        basicDatasetWithSeriesAfterUpsert.ContactsUris = basicContacts1List;
        basicDatasetWithSeriesAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPerson.GivenNames)} {basicPerson.FamilyName}";
        basicDatasetWithSeriesAfterUpsert.ResponsibleOrganisationName = basicOrg.Name;
        basicDatasetWithSeriesAfterUpsert.CatalogueTitle = basicCat.Title;
        basicDatasetWithSeriesAfterUpsert.SeriesTitles = new Dictionary<string, Dictionary<string, string>>()
        {
            { basicDSeries.Uri!, basicDSeries.Title }
        };
        Add(basicDatasetWithSeriesAfterUpsert, DatasetLabel.DatasetWithSeries, VariantLabel.AfterUpsert);

        var basicDatasetWithSeriesModifiedBeforeUpsert = BasicDatasetModifiedBeforeUpsert(
            basicOrgModified, basicPersonModified, basicCatModified, basicContacts1List, [basicDSeriesModified.Uri!]);
        Add(basicDatasetWithSeriesModifiedBeforeUpsert, DatasetLabel.DatasetWithSeries, VariantLabel.ModifiedBeforeUpsert);

        var basicDatasetWithSeriesModifiedAfterUpsert = BasicDatasetModifiedBeforeUpsert(
            basicOrgModified, basicPersonModified, basicCatModified, basicContactsCombinedList, [basicDSeriesModified.Uri!]);
        basicDatasetWithSeriesModifiedAfterUpsert.TemporalCoverage =
            basicDatasetWithSeriesModifiedBeforeUpsert.TemporalCoverage;
        basicDatasetWithSeriesModifiedAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPersonModified.GivenNames)} {basicPersonModified.FamilyName}";
        basicDatasetWithSeriesModifiedAfterUpsert.ResponsibleOrganisationName = basicOrgModified.Name;
        basicDatasetWithSeriesModifiedAfterUpsert.CatalogueTitle = basicCatModified.Title;
        basicDatasetWithSeriesModifiedAfterUpsert.SeriesTitles = new Dictionary<string, Dictionary<string, string>>()
        {
            { basicDSeriesModified.Uri!, basicDSeriesModified.Title }
        };
        Add(basicDatasetWithSeriesModifiedAfterUpsert, DatasetLabel.DatasetWithSeries, VariantLabel.ModifiedAfterUpsert);
    }

    private XDataset BasicDatasetModifiedBeforeUpsert(XOrganisation org, XPerson person, 
        XCatalogue cat, List<string> contacts, List<string>? seriesIds=null)
    {
        XDataset basicDatasetWithSeriesModifiedBeforeUpsert = _testCommon.GetMockXDataset(
            "doorce/trees_in_parks/1.0",
            new Dictionary<string, string> { { "en", "New dataset (upd)" }, { "pl", "Nowy zestaw danych (upd)" }, { "it", "Nuovo set di dati (upd)" } },
            cat.Uri!,
            org.Uri,
            person.Uri,
            seriesIds
        );
            
        basicDatasetWithSeriesModifiedBeforeUpsert.Description = new Dictionary<string, string>()
        {
            { "en", "English description (upd)" },
            { "pl", "Polish description (upd)" }
        };
        basicDatasetWithSeriesModifiedBeforeUpsert.ContactsUris = contacts;
        basicDatasetWithSeriesModifiedBeforeUpsert.AccessRights = (short)AccessRightsType.Public;
        basicDatasetWithSeriesModifiedBeforeUpsert.Keywords = new List<string>() { "trees", "parks", "nature" };
        
        basicDatasetWithSeriesModifiedBeforeUpsert.Frequency = "MONTHLY";
        basicDatasetWithSeriesModifiedBeforeUpsert.TemporalCoverage = new List<(DateTime, DateTime)>()
        {
            (DateTime.Today, DateTime.Now),
            (DateTime.Today - TimeSpan.FromDays(3), DateTime.Now - TimeSpan.FromDays(4)),
        };
        basicDatasetWithSeriesModifiedBeforeUpsert.ReleaseDate = DateTime.Today.AddDays(4).ToUniversalTime();

        basicDatasetWithSeriesModifiedBeforeUpsert.Status = (short)DatasetStatus.Draft;

        return basicDatasetWithSeriesModifiedBeforeUpsert;
    }
}