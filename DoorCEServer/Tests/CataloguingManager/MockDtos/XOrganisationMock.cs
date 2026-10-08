using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.Labels;

namespace DoorCEServer.Tests.CataloguingManager.MockDtos;

public class XOrganisationMock
{
    private readonly Dictionary<OrgLabel, List<Dictionary<VariantLabel, XOrganisation>>> _xOrganisations;
    private readonly Dictionary<OrgLabel, List<Dictionary<ContactsLabel, List<XContactData>>>> _xOrgContacts;
    private readonly TestCommon _testCommon;
    private readonly XContactsMock _contactsMock;
    
    public XOrganisationMock()
    {
        _xOrganisations = new();
        _xOrgContacts = new();
        _testCommon = new TestCommon();

        _contactsMock = new XContactsMock();
        
        Setup();
    }

    public XOrganisation Get(OrgLabel organisationLabel, VariantLabel variantLabel)
    {
        try
        {
            var orgList = _xOrganisations[organisationLabel];
            var dictWithKey = orgList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock organisation not found: {organisationLabel}, {variantLabel}");
        }
    }
    
    public List<XContactData> GetContacts(OrgLabel orgLabel, ContactsLabel contactsLabel)
    {
        try
        {
            var contactList = _xOrgContacts[orgLabel];
            var dictWithKey = contactList.First(d => d.ContainsKey(contactsLabel));
            return dictWithKey[contactsLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock organisation contacts not found: {orgLabel}, {contactsLabel}");
        }
    }

    private void Add(XOrganisation xOrg, OrgLabel orgLabel, VariantLabel variantLabel)
    {
        if (_xOrganisations.TryGetValue(orgLabel, out var organisation))
        {
            organisation.Add(new Dictionary<VariantLabel, XOrganisation>{{variantLabel, xOrg}});
        }
        else
        {
            _xOrganisations.Add(orgLabel, [new Dictionary<VariantLabel, XOrganisation>{{variantLabel, xOrg}}]);
        }
    }
    
    private void AddToContacts(List<XContactData> xContacts, OrgLabel orgLabel, ContactsLabel contactsLabel)
    {
        if (_xOrgContacts.TryGetValue(orgLabel, out var contact))
        {
            contact.Add(new Dictionary<ContactsLabel, List<XContactData>>{{contactsLabel, xContacts}});
        }
        else
        {
            _xOrgContacts.Add(orgLabel, [new Dictionary<ContactsLabel, List<XContactData>>{{contactsLabel, xContacts}}]);
        }
    }

    private void Setup()
    {
        // create contacts
        List<XContactData> basicContacts1 = _contactsMock.Get(ContactsLabel.BasicOrgContacts1);
        List<string> basicContacts1List = basicContacts1.Select(c => c.Uri).ToList()!;
        AddToContacts(basicContacts1, OrgLabel.BasicOrg, ContactsLabel.Contacts1);
        
        List<XContactData> basicContacts2 = _contactsMock.Get(ContactsLabel.BasicOrgContacts2);
        List<string> basicContacts2List = basicContacts2.Select(c => c.Uri).ToList()!;
        AddToContacts(basicContacts2, OrgLabel.BasicOrg, ContactsLabel.Contacts2);
        
        var combinedContacts = _contactsMock.Get(ContactsLabel.BasicOrgContacts1);
        var combinedContactsList = combinedContacts.Select(c => c.Uri).ToList();
        combinedContactsList.AddRange(basicContacts2List);
        
        List<XContactData> basicContacts3 = _contactsMock.Get(ContactsLabel.BasicOrgContacts3);
        List<string> basicContacts3List = basicContacts3.Select(c => c.Uri).ToList()!;
        AddToContacts(basicContacts3, OrgLabel.BasicOrg, ContactsLabel.Contacts3);
        
        // create organisations and add them to dict
        XOrganisation basicOrgBeforeUpsert = _testCommon.GetMockXOrganisation(
            "doorce/WUT",
            "WUT",
            new List<string>()
        );
        Add(basicOrgBeforeUpsert, OrgLabel.BasicOrg, VariantLabel.BeforeUpsert);
       
        var basicOrgAfterUpsert = _testCommon.GetMockXOrganisation(
            "doorce/WUT",
            "WUT",
            basicContacts1List
        );
        Add(basicOrgAfterUpsert, OrgLabel.BasicOrg, VariantLabel.AfterUpsert);
        
        XOrganisation basicOrgModifiedBeforeUpsert =
            _testCommon.GetMockXOrganisation("doorce/WUT", "WUT2", basicContacts1List);
        Add(basicOrgModifiedBeforeUpsert, OrgLabel.BasicOrg, VariantLabel.ModifiedBeforeUpsert);

        var basicOrgModifiedAfterUpsert = _testCommon.GetMockXOrganisation(
            "doorce/WUT", "WUT2", combinedContactsList!);
        Add(basicOrgModifiedAfterUpsert, OrgLabel.BasicOrg, VariantLabel.ModifiedAfterUpsert);
        
        // Extra organisation
        XOrganisation extraOrgBeforeUpsert = _testCommon.GetMockXOrganisation(
            "doorce/WUT3", "WUT3", new List<string>());
        Add(extraOrgBeforeUpsert, OrgLabel.ExtraOrg, VariantLabel.BeforeUpsert);
        AddToContacts(basicContacts3, OrgLabel.ExtraOrg, ContactsLabel.Contacts1);
        
        XOrganisation extraOrgAfterUpsert = _testCommon.GetMockXOrganisation(
            "doorce/WUT3", "WUT3", basicContacts3List);
        Add(extraOrgAfterUpsert, OrgLabel.ExtraOrg, VariantLabel.AfterUpsert);

        // Minimal organisation
    }

    private XOrganisation GetMinimalXOrganisation()
    {
        return _testCommon.GetMockXOrganisation("doorce/minimal", "Minimal", new List<string>());
    }
}