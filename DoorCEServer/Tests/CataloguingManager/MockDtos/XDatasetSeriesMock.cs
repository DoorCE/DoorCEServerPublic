using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.Labels;

namespace DoorCEServer.Tests.CataloguingManager.MockDtos;

public class XDatasetSeriesMock
{
    private readonly Dictionary<DSeriesLabel, List<Dictionary<VariantLabel,XDatasetSeries>>> _xDSeries;
    private readonly Dictionary<DSeriesLabel, List<Dictionary<ContactsLabel, List<XContactData>>>> _xDSeriesContacts;
    private readonly TestCommon _testCommon;
    
    private readonly XOrganisationMock _orgMock;
    private readonly XContactsMock _contactsMock;
    private readonly XPersonMock _personMock;
    private readonly XCatalogueMock _catMock;
    
    public XDatasetSeriesMock()
    {
        _xDSeries = new Dictionary<DSeriesLabel, List<Dictionary<VariantLabel,XDatasetSeries>>>();
        _xDSeriesContacts = new Dictionary<DSeriesLabel, List<Dictionary<ContactsLabel, List<XContactData>>>>();
        _testCommon = new TestCommon();
        
        _contactsMock = new XContactsMock();
        _orgMock = new XOrganisationMock();
        _personMock = new XPersonMock();
        _catMock = new XCatalogueMock();
        
        Setup();
    }
    
    public XDatasetSeries Get(DSeriesLabel seriesLabel, VariantLabel variantLabel)
    {
        try
        {
            var seriesList = _xDSeries[seriesLabel];
            var dictWithKey = seriesList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock dataset series not found: {seriesLabel}, {variantLabel}");
        }
    }
    
    public List<XContactData> GetContacts(DSeriesLabel seriesLabel, ContactsLabel contactsLabel)
    {
        try
        {
            var contactsList = _xDSeriesContacts[seriesLabel];
            var dictWithKey = contactsList.First(d => d.ContainsKey(contactsLabel));
            return dictWithKey[contactsLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock dataset series contacts not found: {seriesLabel}, {contactsLabel}");
        }
    }
    
    private void Add(XDatasetSeries xSeries, DSeriesLabel dseriesLabel, VariantLabel variantLabel)
    {
        if (_xDSeries.ContainsKey(dseriesLabel))
        {
            _xDSeries[dseriesLabel].Add(new Dictionary<VariantLabel, XDatasetSeries>{{variantLabel, xSeries}});
        }
        else
        {
            _xDSeries.Add(dseriesLabel, [new Dictionary<VariantLabel, XDatasetSeries>{{variantLabel, xSeries}}]);
        }
    }

    private void AddToContacts(List<XContactData> xContacts, DSeriesLabel dseriesLabel, ContactsLabel contactsLabel)
    {
        if (_xDSeriesContacts.ContainsKey(dseriesLabel))
        {
            _xDSeriesContacts[dseriesLabel].Add(new Dictionary<ContactsLabel, List<XContactData>>
                { { contactsLabel, xContacts } });
        }
        else
        {
            _xDSeriesContacts.Add(dseriesLabel,
                [new Dictionary<ContactsLabel, List<XContactData>> { { contactsLabel, xContacts } }]);
        }
    }

    private void Setup()
    {
        // create contacts
        List<XContactData> basicContacts1 = _contactsMock.Get(ContactsLabel.BasicDSeriesContacts1);
        List<string> basicContacts1List = basicContacts1.Select(c => c.Uri).ToList()!;

        List<XContactData> basicContacts2 = _contactsMock.Get(ContactsLabel.BasicDSeriesContacts2);
        List<string> basicContacts2List = basicContacts2.Select(c => c.Uri).ToList()!;

        List<XContactData> basicContactsCombined = _contactsMock.Get(ContactsLabel.BasicDSeriesContacts1);
        List<string> basicContactsCombinedList = basicContactsCombined.Select(c => c.Uri).ToList()!;
        basicContactsCombinedList.AddRange(basicContacts2List);

        // create organisations, persons and catalogues
        XOrganisation basicOrg = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.AfterUpsert);
        XOrganisation basicOrgModified = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.ModifiedAfterUpsert);
        
        XPerson basicPerson = _personMock.Get(PersonLabel.PersonWithOrg, VariantLabel.AfterUpsert);
        XPerson basicPersonModified = _personMock.Get(PersonLabel.PersonWithOrg, VariantLabel.ModifiedAfterUpsert);
        
        XCatalogue basicCat = _catMock.Get(CatLabel.BasicCat, VariantLabel.AfterUpsert);
        XCatalogue basicCatModified = _catMock.Get(CatLabel.BasicCat, VariantLabel.ModifiedAfterUpsert);

        // create series and add them to dict
        AddToContacts(basicContacts1, DSeriesLabel.DSeriesWithCat, ContactsLabel.Contacts1);
        AddToContacts(basicContacts2, DSeriesLabel.DSeriesWithCat, ContactsLabel.Contacts2);
        
        XDatasetSeries basicDSeriesWithCatBeforeUpsert = _testCommon.GetMockXSeries(
            "doorce/tree_series",
            new Dictionary<string, string> { { "en", "New dataset series" }, { "pl", "Nowa seria danych" }, { "it", "Nuova serie di dati" } },
            basicOrg.Uri!,
            basicPerson.Uri!,
            basicCat.Uri
        );
        Add(basicDSeriesWithCatBeforeUpsert, DSeriesLabel.DSeriesWithCat, VariantLabel.BeforeUpsert);

        var basicDSeriesWithCatAfterUpsert = _testCommon.GetMockXSeries(
            "doorce/tree_series",
            new Dictionary<string, string> { { "en", "New dataset series" }, { "pl", "Nowa seria danych" }, { "it", "Nuova serie di dati" } },
            basicOrg.Uri!,
            basicPerson.Uri!,
            basicCat.Uri
        );
        basicDSeriesWithCatAfterUpsert.ContactsUris = basicContacts1List;
        basicDSeriesWithCatAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPerson.GivenNames)} {basicPerson.FamilyName}";
        basicDSeriesWithCatAfterUpsert.ResponsibleOrganisationName = basicOrg.Name;
        basicDSeriesWithCatAfterUpsert.CatalogueTitle = basicCat.Title;
        Add(basicDSeriesWithCatAfterUpsert, DSeriesLabel.DSeriesWithCat, VariantLabel.AfterUpsert);

        var basicDSeriesWithCatModifiedBeforeUpsert = BasicDSeriesWithCatModifiedBeforeUpsert(
            basicOrgModified, basicPersonModified, basicCatModified, basicContacts1List);
        Add(basicDSeriesWithCatModifiedBeforeUpsert, DSeriesLabel.DSeriesWithCat, VariantLabel.ModifiedBeforeUpsert);

        var basicDSeriesWithCatModifiedAfterUpsert = BasicDSeriesWithCatModifiedBeforeUpsert(
            basicOrgModified, basicPersonModified, basicCatModified, basicContactsCombinedList);
        basicDSeriesWithCatModifiedAfterUpsert.TemporalCoverage =
            basicDSeriesWithCatModifiedBeforeUpsert.TemporalCoverage;
        basicDSeriesWithCatModifiedAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPersonModified.GivenNames)} {basicPersonModified.FamilyName}";
        basicDSeriesWithCatModifiedAfterUpsert.ResponsibleOrganisationName = basicOrgModified.Name;
        basicDSeriesWithCatModifiedAfterUpsert.CatalogueTitle = basicCatModified.Title;
        Add(basicDSeriesWithCatModifiedAfterUpsert, DSeriesLabel.DSeriesWithCat, VariantLabel.ModifiedAfterUpsert);
    }

    private XDatasetSeries BasicDSeriesWithCatModifiedBeforeUpsert(XOrganisation org, XPerson person, XCatalogue cat, List<string> contacts)
    {
        XDatasetSeries basicDSeriesWithCatModifiedBeforeUpsert = _testCommon.GetMockXSeries(
            "doorce/tree_series",
            new Dictionary<string, string> { { "en", "New dataset series (upd)" }, { "pl", "Nowa seria danych (upd)" }, { "it", "Nuova serie di dati (upd)" } },
            org.Uri!,
            person.Uri!,
            cat.Uri
        );
        basicDSeriesWithCatModifiedBeforeUpsert.ContactsUris = contacts;
        basicDSeriesWithCatModifiedBeforeUpsert.AccessRights = (short)AccessRightsType.Restricted;
        basicDSeriesWithCatModifiedBeforeUpsert.Languages = new List<string>() { "en", "pl", "it", "fr" };
        basicDSeriesWithCatModifiedBeforeUpsert.Frequency = "DAILY";
        basicDSeriesWithCatModifiedBeforeUpsert.TemporalCoverage = new List<(DateTime, DateTime)>()
        {
            (DateTime.Today, DateTime.Now),
            (DateTime.Today - TimeSpan.FromDays(2), DateTime.Now - TimeSpan.FromDays(1)),
        };
        basicDSeriesWithCatModifiedBeforeUpsert.ReleaseDate = DateTime.Today.AddDays(2).ToUniversalTime();

        return basicDSeriesWithCatModifiedBeforeUpsert;
    }
}