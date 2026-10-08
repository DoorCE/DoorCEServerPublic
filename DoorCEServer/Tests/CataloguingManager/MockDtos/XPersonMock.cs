using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.Labels;

namespace DoorCEServer.Tests.CataloguingManager.MockDtos;

public class XPersonMock
{
    private readonly Dictionary<PersonLabel, List<Dictionary<VariantLabel, XPerson>>> _xPersons;
    private readonly Dictionary<PersonLabel, List<Dictionary<ContactsLabel, List<XContactData>>>> _xPersonContacts;
    private readonly TestCommon _testCommon;
    
    private readonly XOrganisationMock _orgMock;
    private readonly XContactsMock _contactsMock;
    
    public XPersonMock()
    {
        _xPersons = new();
        _xPersonContacts = new();
        _testCommon = new TestCommon();
        
        _contactsMock = new XContactsMock();
        _orgMock = new XOrganisationMock();
        
        Setup();
    }
    
    public XPerson Get(PersonLabel personLabel, VariantLabel variantLabel)
    {
        try
        {
            var personList = _xPersons[personLabel];
            var dictWithKey = personList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock person not found: {personLabel}, {variantLabel}");
        }
    }
    
    public List<XContactData> GetContacts(PersonLabel personLabel, ContactsLabel contactsLabel)
    {
        try
        {
            var contactList = _xPersonContacts[personLabel];
            var dictWithKey = contactList.First(d => d.ContainsKey(contactsLabel));
            return dictWithKey[contactsLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock person contacts not found: {personLabel}, {contactsLabel}");
        }
    }
    
    private void Add(XPerson xPerson, PersonLabel personLabel, VariantLabel variantLabel)
    {
        if (_xPersons.ContainsKey(personLabel))
        {
            _xPersons[personLabel].Add(new Dictionary<VariantLabel, XPerson>{{variantLabel, xPerson}});
        }
        else
        {
            _xPersons.Add(personLabel, [new Dictionary<VariantLabel, XPerson>{{variantLabel, xPerson}}]);
        }
    }
    
    private void AddToContacts(List<XContactData> xContacts, PersonLabel personLabel, ContactsLabel contactsLabel)
    {
        if (_xPersonContacts.ContainsKey(personLabel))
        {
            _xPersonContacts[personLabel].Add(new Dictionary<ContactsLabel, List<XContactData>>{{contactsLabel, xContacts}});
        }
        else
        {
            _xPersonContacts.Add(personLabel, [new Dictionary<ContactsLabel, List<XContactData>>{{contactsLabel, xContacts}}]);
        }
    }

    private void Setup()
    {
        // create contacts
        List<XContactData> basicContacts1 = _contactsMock.Get(ContactsLabel.BasicPersonContacts1);
        List<string> basicContacts1List = basicContacts1.Select(c => c.Uri).ToList()!;
        
        List<XContactData> basicContacts2 = _contactsMock.Get(ContactsLabel.BasicPersonContacts2);
        List<string> basicContacts2List = basicContacts2.Select(c => c.Uri).ToList()!;
        
        List<XContactData> basicContactsCombined = _contactsMock.Get(ContactsLabel.BasicPersonContacts1);
        List<string> basicContactsCombinedList = basicContactsCombined.Select(c => c.Uri).ToList()!;
        basicContactsCombinedList.AddRange(basicContacts2List);
        
        List<XContactData> extraContacts = _contactsMock.Get(ContactsLabel.BasicPersonContacts3);
        List<string> extraContactsList = extraContacts.Select(c => c.Uri).ToList()!;
        
        // create organisations
        XOrganisation basicOrg = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.AfterUpsert);
        XOrganisation basicOrgModified = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.ModifiedAfterUpsert);
        
        // **** create persons and add them to dict ****
        
        // Basic Person with organisation 
        XPerson basicPersonWithOrgBeforeUpsert = _testCommon.GetMockXPerson(
            "doorce/kowalski",
            "Kowalski",
            new List<string>(),
            new List<string> {basicOrg.Uri!}
        );
        Add(basicPersonWithOrgBeforeUpsert, PersonLabel.PersonWithOrg, VariantLabel.BeforeUpsert);
        AddToContacts(basicContacts1, PersonLabel.PersonWithOrg, ContactsLabel.Contacts1);
        
        var basicPersonWithOrgAfterUpsert = _testCommon.GetMockXPerson(
            "doorce/kowalski",
            "Kowalski",
            new List<string>(),
            new List<string> {basicOrg.Uri!}
        );
        basicPersonWithOrgAfterUpsert.ContactsUris = basicContacts1List;
        basicPersonWithOrgAfterUpsert.OrganisationsNames = new Dictionary<string, string> { {basicOrg.Uri!, basicOrg.Name} };
        Add(basicPersonWithOrgAfterUpsert, PersonLabel.PersonWithOrg, VariantLabel.AfterUpsert);
        
        XPerson basicPersonWithOrgModifiedBeforeUpsert =
            _testCommon.GetMockXPerson(
                "doorce/kowalski",
                "Kowalczyk", 
                basicContacts1List,
                new List<string> {basicOrgModified.Uri!}
                );
        
        basicPersonWithOrgModifiedBeforeUpsert.GivenNames = new List<string>() { "Krzysztof", "Andrzej" };
        Add(basicPersonWithOrgModifiedBeforeUpsert, PersonLabel.PersonWithOrg, VariantLabel.ModifiedBeforeUpsert);
        AddToContacts(basicContacts2, PersonLabel.PersonWithOrg, ContactsLabel.Contacts2);

        var basicPersonWithOrgModifiedAfterUpsert = _testCommon.GetMockXPerson(
            "doorce/kowalski",
            "Kowalczyk", 
            basicContactsCombinedList,
            new List<string> {basicOrgModified.Uri!}
        );
        basicPersonWithOrgModifiedAfterUpsert.GivenNames = new List<string>() { "Krzysztof", "Andrzej" };
        basicPersonWithOrgModifiedAfterUpsert.OrganisationsNames = new Dictionary<string, string> { {basicOrgModified.Uri!, basicOrgModified.Name} };
        Add(basicPersonWithOrgModifiedAfterUpsert, PersonLabel.PersonWithOrg, VariantLabel.ModifiedAfterUpsert);
        
        XPerson personWithoutOrgBeforeUpsert = _testCommon.GetMockXPerson(
            "doorce/craig", "Daniel Craig", new List<string>(), new List<string>());
        Add(personWithoutOrgBeforeUpsert, PersonLabel.BasicPersonWithoutOrg, VariantLabel.BeforeUpsert);
        AddToContacts(basicContacts1, PersonLabel.BasicPersonWithoutOrg, ContactsLabel.Contacts1);
        AddToContacts(basicContacts2, PersonLabel.BasicPersonWithoutOrg, ContactsLabel.Contacts2);
        
        // extra person
        XPerson extraPersonBeforeUpsert = _testCommon.GetMockXPerson(
            "doorce/craig", "Daniel Craig", new List<string>(), new List<string>());
        Add(extraPersonBeforeUpsert, PersonLabel.ExtraPerson, VariantLabel.BeforeUpsert);
        AddToContacts(extraContacts, PersonLabel.ExtraPerson, ContactsLabel.Contacts1);
        
        XPerson extraPersonAfterUpsert = _testCommon.GetMockXPerson(
            "doorce/craig", "Daniel Craig", extraContactsList, new List<string>());
        Add(extraPersonAfterUpsert, PersonLabel.ExtraPerson, VariantLabel.AfterUpsert);
        
        // TODO
        XPerson basicPersonWithoutOrgAfterUpsert;
        XPerson basicPersonWithoutOrgModifiedBeforeUpsert;
        XPerson basicPersonWithoutOrgModifiedAfterUpsert;
    }
}