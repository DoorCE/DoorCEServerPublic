using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.Labels;

namespace DoorCEServer.Tests.CataloguingManager.MockDtos;

public class XDataServiceMock
{
    private readonly Dictionary<ServiceLabel, List<Dictionary<VariantLabel, XDataService>>> _xServices;
    private readonly Dictionary<ServiceLabel, List<Dictionary<ContactsLabel, List<XContactData>>>> _xServiceContacts;
    private readonly TestCommon _testCommon;
    
    private readonly XOrganisationMock _orgMock;
    private readonly XContactsMock _contactsMock;
    private readonly XPersonMock _personMock;
    private readonly XCatalogueMock _catMock;
    
    public XDataServiceMock()
    {
        _xServices = new Dictionary<ServiceLabel, List<Dictionary<VariantLabel, XDataService>>>();
        _xServiceContacts = new Dictionary<ServiceLabel, List<Dictionary<ContactsLabel, List<XContactData>>>>();
        _testCommon = new TestCommon();
        
        _contactsMock = new XContactsMock();
        _orgMock = new XOrganisationMock();
        _personMock = new XPersonMock();
        _catMock = new XCatalogueMock();
        
        Setup();
    }
    
    public XDataService Get(ServiceLabel serviceLabel, VariantLabel variantLabel)
    {
        try
        {
            var serviceList = _xServices[serviceLabel];
            var dictWithKey = serviceList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock data service not found: {serviceLabel}, {variantLabel}");
        }
    }
    
    public List<XContactData> GetContacts(ServiceLabel serviceLabel, ContactsLabel contactsLabel)
    {
        try
        {
            var contactsList = _xServiceContacts[serviceLabel];
            var dictWithKey = contactsList.First(d => d.ContainsKey(contactsLabel));
            return dictWithKey[contactsLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock data service contacts not found: {serviceLabel}, {contactsLabel}");
        }
    }
    
    private void Add(XDataService xService, ServiceLabel serviceLabel, VariantLabel variantLabel)
    {
        if (_xServices.ContainsKey(serviceLabel))
        {
            _xServices[serviceLabel].Add(new Dictionary<VariantLabel, XDataService>{{variantLabel, xService}});
        }
        else
        {
            _xServices.Add(serviceLabel, [new Dictionary<VariantLabel, XDataService>{{variantLabel, xService}}]);
        }
    }
    
    private void AddToContacts(List<XContactData> xContacts, ServiceLabel serviceLabel, ContactsLabel contactsLabel)
    {
        if (_xServiceContacts.ContainsKey(serviceLabel))
        {
            _xServiceContacts[serviceLabel].Add(new Dictionary<ContactsLabel, List<XContactData>>{{contactsLabel, xContacts}});
        }
        else
        {
            _xServiceContacts.Add(serviceLabel, [new Dictionary<ContactsLabel, List<XContactData>>{{contactsLabel, xContacts}}]);
        }
    }

    private void Setup()
    {
        // create contacts
        List<XContactData> basicContacts1 = _contactsMock.Get(ContactsLabel.BasicServiceContacts1);
        List<string> basicContacts1List = basicContacts1.Select(c => c.Uri).ToList()!;

        List<XContactData> basicContacts2 = _contactsMock.Get(ContactsLabel.BasicServiceContacts2);
        List<string> basicContacts2List = basicContacts2.Select(c => c.Uri).ToList()!;

        List<XContactData> basicContactsCombined = _contactsMock.Get(ContactsLabel.BasicServiceContacts1);
        List<string> basicContactsCombinedList = basicContactsCombined.Select(c => c.Uri).ToList()!;
        basicContactsCombinedList.AddRange(basicContacts2List);

        // create organisations, persons and catalogues
        XOrganisation basicOrg = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.AfterUpsert);
        XOrganisation basicOrgModified = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.ModifiedAfterUpsert);
        
        XPerson basicPerson = _personMock.Get(PersonLabel.PersonWithOrg, VariantLabel.AfterUpsert);
        XPerson basicPersonModified = _personMock.Get(PersonLabel.PersonWithOrg, VariantLabel.ModifiedAfterUpsert);
        
        XCatalogue basicCat = _catMock.Get(CatLabel.BasicCat, VariantLabel.AfterUpsert);
        XCatalogue basicCatModified = _catMock.Get(CatLabel.BasicCat, VariantLabel.ModifiedAfterUpsert);

        // create data services and add them to dict
        XDataService basicServiceBeforeUpsert = _testCommon.GetMockXDataService(
            "doorce/data_service/1",
            new Dictionary<string, string> { { "en", "New data service" }, { "pl", "Nowy serwis danych" }, { "it", "Nuovo servizio di dati" } },
            basicCat.Uri!,
            basicOrg.Uri!,
            basicPerson.Uri!
        );
        Add(basicServiceBeforeUpsert, ServiceLabel.BasicService, VariantLabel.BeforeUpsert);
        AddToContacts(basicContacts1, ServiceLabel.BasicService, ContactsLabel.Contacts1);
        AddToContacts(basicContacts2, ServiceLabel.BasicService, ContactsLabel.Contacts2);

        var basicServiceAfterUpsert = _testCommon.GetMockXDataService(
            "doorce/data_service/1",
            new Dictionary<string, string> { { "en", "New data service" }, { "pl", "Nowy serwis danych" }, { "it", "Nuovo servizio di dati" } },
            basicCat.Uri!,
            basicOrg.Uri!,
            basicPerson.Uri!
        );
        basicServiceAfterUpsert.ContactsUris = basicContacts1List;
        basicServiceAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPerson.GivenNames)} {basicPerson.FamilyName}";
        basicServiceAfterUpsert.ResponsibleOrganisationName = basicOrg.Name;
        basicServiceAfterUpsert.CatalogueTitle = basicCat.Title;
        Add(basicServiceAfterUpsert, ServiceLabel.BasicService, VariantLabel.AfterUpsert);

        var basicServiceModifiedBeforeUpsert = BasicServiceModifiedBeforeUpsert(
            basicOrgModified, basicPersonModified, basicCatModified, basicContacts1List);
        Add(basicServiceModifiedBeforeUpsert, ServiceLabel.BasicService, VariantLabel.ModifiedBeforeUpsert);

        var basicServiceModifiedAfterUpsert = BasicServiceModifiedBeforeUpsert(
            basicOrgModified, basicPersonModified, basicCatModified, basicContactsCombinedList);
        basicServiceModifiedAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPersonModified.GivenNames)} {basicPersonModified.FamilyName}";
        basicServiceModifiedAfterUpsert.ResponsibleOrganisationName = basicOrgModified.Name;
        basicServiceModifiedAfterUpsert.CatalogueTitle = basicCatModified.Title;
        Add(basicServiceModifiedAfterUpsert, ServiceLabel.BasicService, VariantLabel.ModifiedAfterUpsert);
    }

    private XDataService BasicServiceModifiedBeforeUpsert(XOrganisation org, XPerson person, XCatalogue cat, List<string> contacts)
    {
        XDataService basicServiceModifiedBeforeUpsert = _testCommon.GetMockXDataService(
            "doorce/data_service/1",
            new Dictionary<string, string> { { "en", "New data service (upd)" }, { "pl", "Nowy serwis danych (upd)" }, { "it", "Nuovo servizio di dati (upd)" } },
            cat.Uri!,
            org.Uri!,
            person.Uri!
        );
        
        basicServiceModifiedBeforeUpsert.ContactsUris = contacts;
        basicServiceModifiedBeforeUpsert.AccessRights = (short)AccessRightsType.Restricted;
        basicServiceModifiedBeforeUpsert.Languages = new List<string>() { "en", "pl", "it", "fr" };
        basicServiceModifiedBeforeUpsert.Status = 1;

        return basicServiceModifiedBeforeUpsert;
    }
}