using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.Labels;

namespace DoorCEServer.Tests.CataloguingManager.MockDtos;

public class XCatalogueMock
{
    private readonly Dictionary<CatLabel, List<Dictionary<VariantLabel, XCatalogue>>> _xCatalogues;
    private readonly Dictionary<CatLabel, List<Dictionary<ContactsLabel, List<XContactData>>>> _xCatContacts;
    private readonly TestCommon _testCommon;
    
    private readonly XOrganisationMock _orgMock;
    private readonly XContactsMock _contactsMock;
    private readonly XPersonMock _personMock;
    
    public XCatalogueMock()
    {
        _xCatalogues = new Dictionary<CatLabel, List<Dictionary<VariantLabel, XCatalogue>>>();
        _xCatContacts = new Dictionary<CatLabel, List<Dictionary<ContactsLabel, List<XContactData>>>>();
        _testCommon = new TestCommon();
        
        _contactsMock = new XContactsMock();
        _orgMock = new XOrganisationMock();
        _personMock = new XPersonMock();
        
        Setup();
    }
    
    public XCatalogue Get(CatLabel catalogueLabel, VariantLabel variantLabel)
    {
        try
        {
            var catList = _xCatalogues[catalogueLabel];
            var dictWithKey = catList.First(d => d.ContainsKey(variantLabel));
            return dictWithKey[variantLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock catalogue not found: {catalogueLabel}, {variantLabel}");
        }
    }
    
    public List<XContactData> GetContacts(CatLabel catalogueLabel, ContactsLabel contactsLabel)
    {
        try
        {
            var contactList = _xCatContacts[catalogueLabel];
            var dictWithKey = contactList.First(d => d.ContainsKey(contactsLabel));
            return dictWithKey[contactsLabel];
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw new Exception($"Mock catalogue contacts not found: {catalogueLabel}, {contactsLabel}");
        }
    }
    
    private void Add(XCatalogue xCatalogue, CatLabel catLabel, VariantLabel variantLabel)
    {
        if (_xCatalogues.ContainsKey(catLabel))
        {
            _xCatalogues[catLabel].Add(new Dictionary<VariantLabel, XCatalogue>{{variantLabel, xCatalogue}});
        }
        else
        {
            _xCatalogues.Add(catLabel, [new Dictionary<VariantLabel, XCatalogue>{{variantLabel, xCatalogue}}]);
        }
    }
    
    private void AddToContacts(List<XContactData> xContacts, CatLabel catLabel, ContactsLabel contactsLabel)
    {
        if (_xCatContacts.ContainsKey(catLabel))
        {
            _xCatContacts[catLabel].Add(new Dictionary<ContactsLabel, List<XContactData>>{{contactsLabel, xContacts}});
        }
        else
        {
            _xCatContacts.Add(catLabel, [new Dictionary<ContactsLabel, List<XContactData>>{{contactsLabel, xContacts}}]);
        }
    }

    private Dictionary<string, string> NewCatalogue(string? addition)
    {
        return new() {
            { "en", "New Catalogue" + null != addition ? " (" + addition + ")" : "" },
            { "pl", "Nowy katalog" + null != addition ? " (" + addition + ")" : "" },
            { "it", "Nuovo catalogo" + null != addition ? " (" + addition + ")" : "" } 
        };
    }

    public void Setup()
    {
        // create contacts
        List<XContactData> basicContacts1 = _contactsMock.Get(ContactsLabel.BasicCatalogueContacts1);
        List<string> basicContacts1List = basicContacts1.Select(c => c.Uri).ToList()!;

        List<XContactData> basicContacts2 = _contactsMock.Get(ContactsLabel.BasicCatalogueContacts2);
        List<string> basicContacts2List = basicContacts2.Select(c => c.Uri).ToList()!;

        List<XContactData> basicContactsCombined = _contactsMock.Get(ContactsLabel.BasicCatalogueContacts1);
        List<string> basicContactsCombinedList = basicContactsCombined.Select(c => c.Uri).ToList()!;
        basicContactsCombinedList.AddRange(basicContacts2List);
        
        List<XContactData> basicContacts3 = _contactsMock.Get(ContactsLabel.BasicCatalogueContacts3);
        List<string> basicContacts3List = basicContacts3.Select(c => c.Uri).ToList()!;
        
        AddToContacts(basicContacts1, CatLabel.BasicCat, ContactsLabel.Contacts1);
        AddToContacts(basicContacts2, CatLabel.BasicCat, ContactsLabel.Contacts2);
        AddToContacts(basicContacts3, CatLabel.BasicCat, ContactsLabel.ChildCatContacts1);

        // create organisations and persons
        XOrganisation basicOrg = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.AfterUpsert);
        XOrganisation basicOrgModified = _orgMock.Get(OrgLabel.BasicOrg, VariantLabel.ModifiedAfterUpsert);
        XPerson basicPerson = _personMock.Get(PersonLabel.PersonWithOrg, VariantLabel.AfterUpsert);
        XPerson basicPersonModified = _personMock.Get(PersonLabel.PersonWithOrg, VariantLabel.ModifiedAfterUpsert);

        // create catalogues and add them to dict
        XCatalogue basicCatalogueBeforeUpsert = _testCommon.GetMockXCatalogue(
            "doorce/root",
            NewCatalogue(null),
            basicOrg.Uri!,
            basicPerson.Uri!
        );
        Add(basicCatalogueBeforeUpsert, CatLabel.BasicCat, VariantLabel.BeforeUpsert);
        
        var basicCatAfterUpsert = _testCommon.GetMockXCatalogue(
            "doorce/root",
            NewCatalogue(null),
            basicOrg.Uri!,
            basicPerson.Uri!
        );
        basicCatAfterUpsert.ContactsUris = basicContacts1List;
        basicCatAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPerson.GivenNames)} {basicPerson.FamilyName}";
        basicCatAfterUpsert.ResponsibleOrganisationName = basicOrg.Name;
        Add(basicCatAfterUpsert, CatLabel.BasicCat, VariantLabel.AfterUpsert);

        XCatalogue basicCatModifiedBeforeUpsert = _testCommon.GetMockXCatalogue(
            "doorce/root",
            NewCatalogue("upd"),
            basicOrgModified.Uri!,
            basicPersonModified.Uri!
        );
        
        basicCatModifiedBeforeUpsert.ContactsUris = basicContacts1List;
        Add(basicCatModifiedBeforeUpsert, CatLabel.BasicCat, VariantLabel.ModifiedBeforeUpsert);

        var basicCatModifiedAfterUpsert = _testCommon.GetMockXCatalogue(
            "doorce/root", 
            NewCatalogue("upd"),
            basicOrgModified.Uri!,
            basicPersonModified.Uri!
        );

        basicCatModifiedAfterUpsert.ContactsUris = basicContactsCombinedList;
        basicCatModifiedAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPersonModified.GivenNames)} {basicPersonModified.FamilyName}";
        basicCatModifiedAfterUpsert.ResponsibleOrganisationName = basicOrgModified.Name;
        Add(basicCatModifiedAfterUpsert, CatLabel.BasicCat, VariantLabel.ModifiedAfterUpsert);
        
        // child catalogue
        XCatalogue childCatalogueBeforeUpsert = _testCommon.GetMockXCatalogue(
            "doorce/second",
            NewCatalogue("child"),
            basicOrg.Uri!,
            basicPerson.Uri!,
            basicCatalogueBeforeUpsert.Uri
        );
        Add(childCatalogueBeforeUpsert, CatLabel.BasicCat, VariantLabel.ChildCatBeforeUpsert);
        
        XCatalogue childCatalogueAfterUpsert = _testCommon.GetMockXCatalogue(
            "doorce/second",
            NewCatalogue("child"),
            basicOrg.Uri!,
            basicPerson.Uri!,
            basicCatalogueBeforeUpsert.Uri
        );
        childCatalogueAfterUpsert.ContactsUris = basicContacts3List;
        childCatalogueAfterUpsert.ResponsiblePersonName =
            $"{string.Join(" ", basicPerson.GivenNames)} {basicPerson.FamilyName}";
        childCatalogueAfterUpsert.ResponsibleOrganisationName = basicOrg.Name;
        Add(childCatalogueAfterUpsert, CatLabel.BasicCat, VariantLabel.ChildCatAfterUpsert);
    }
}