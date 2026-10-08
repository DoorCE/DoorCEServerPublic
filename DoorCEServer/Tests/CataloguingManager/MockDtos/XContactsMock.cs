using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.Labels;

namespace DoorCEServer.Tests.CataloguingManager.MockDtos;

public class XContactsMock
{
    private readonly Dictionary<ContactsLabel, List<XContactData>> _xContacts;
    private readonly TestCommon _testCommon;

    public XContactsMock()
    {
        _xContacts = new Dictionary<ContactsLabel, List<XContactData>>();
        _testCommon = new TestCommon();
        
        Setup();
    }

    public List<XContactData> Get(ContactsLabel contactsLabel)
    {
        try
        {
            return _xContacts[contactsLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock contacts not found: {contactsLabel}");
        }
    }

    public List<string> GetUriList(ContactsLabel contactsLabel)
    {
        List<XContactData> xContactDataList = Get(contactsLabel);
        return xContactDataList.Select(c => c.Uri).ToList()!;
    }

    private void Setup()
    {
        List<XContactData> basicOrgContacts1 = _testCommon.GetMockContacts("doorce/contact_org", ["org@org", "+48 123 456 789"]);
        _xContacts.Add(ContactsLabel.BasicOrgContacts1, basicOrgContacts1);
        
        List<XContactData> basicOrgContacts2 = _testCommon.GetMockContacts("doorce/contact_wut", ["org@org", "+48 123 456 789"]);
        _xContacts.Add(ContactsLabel.BasicOrgContacts2, basicOrgContacts2);
        
        List<XContactData> basicPersonContacts1 = _testCommon.GetMockContacts("doorce/contact_person", ["person@person", "+48 987 654 321"]);
        _xContacts.Add(ContactsLabel.BasicPersonContacts1, basicPersonContacts1);
        
        List<XContactData> basicPersonContacts2 = _testCommon.GetMockContacts("doorce/contact_per", ["per@per", "+48 987 654 321"]);
        _xContacts.Add(ContactsLabel.BasicPersonContacts2, basicPersonContacts2);
        
        List<XContactData> basicCatalogueContacts1 = _testCommon.GetMockContacts("doorce/contact_catalogue", ["Andrzej@Katalog", "+48 111 222 333"]);
        _xContacts.Add(ContactsLabel.BasicCatalogueContacts1, basicCatalogueContacts1);
        
        List<XContactData> basicCatalogueContacts2 = _testCommon.GetMockContacts("doorce/contact_cat", ["andrzej@katalog", "+48 111 222 333"]);
        _xContacts.Add(ContactsLabel.BasicCatalogueContacts2, basicCatalogueContacts2);
        
        List<XContactData> basicDSeriesContacts1 = _testCommon.GetMockContacts("doorce/contact_dseries", ["Anna Seria", "+48 444 555 666"]);
        _xContacts.Add(ContactsLabel.BasicDSeriesContacts1, basicDSeriesContacts1);
        
        List<XContactData> basicDSeriesContacts2 = _testCommon.GetMockContacts("doorce/contact_dser", ["anna@seria", "+48 444 555 666"]);
        _xContacts.Add(ContactsLabel.BasicDSeriesContacts2, basicDSeriesContacts2);
        
        List<XContactData> basicServiceContacts1 = _testCommon.GetMockContacts("doorce/contact_service", ["Anna@Serwis", "+48 333 444 555"]);
        _xContacts.Add(ContactsLabel.BasicServiceContacts1, basicServiceContacts1);
        
        List<XContactData> basicServiceContacts2 = _testCommon.GetMockContacts("doorce/contact_serv", ["anna@serwis", "+48 333 444 555"]);
        _xContacts.Add(ContactsLabel.BasicServiceContacts2, basicServiceContacts2);
        
        List<XContactData> basicDatasetContacts1 = _testCommon.GetMockContacts("doorce/contact_dataset", ["Marianna@Zestaw", "+48 123 456 789"]);
        _xContacts.Add(ContactsLabel.BasicDatasetContacts1, basicDatasetContacts1);
        
        List<XContactData> basicDatasetContacts2 = _testCommon.GetMockContacts("doorce/contact_dset", ["marianna@zestaw", "+48 123 456 789"]);
        _xContacts.Add(ContactsLabel.BasicDatasetContacts2, basicDatasetContacts2);
        
        List<XContactData> basicCatalogueContacts3 = _testCommon.GetMockContacts("doorce/contact_child", ["Andrzej@Kat", "+48 111 222 333"]);
        _xContacts.Add(ContactsLabel.BasicCatalogueContacts3, basicCatalogueContacts3);

        List<XContactData> basicOrgContacts3 =
            _testCommon.GetMockContacts("doorce/contact_org_test", ["Organizacja@Testowa", "+48 123 456 789"]);
        _xContacts.Add(ContactsLabel.BasicOrgContacts3, basicOrgContacts3);
        
        List<XContactData> basicPersonContacts3 =
            _testCommon.GetMockContacts("doorce/contact_person_test", ["Osoba@Testowa", "+48 987 654 321"]);
        _xContacts.Add(ContactsLabel.BasicPersonContacts3, basicPersonContacts3);
    }
}