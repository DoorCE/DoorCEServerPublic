using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.CataloguingManager.TestHelpers;

namespace DoorCEServer.Tests.CataloguingManager.Tests;

public class MAgentsTest(MAgentsTestHelper testHelper)
{
    public bool InsertOrganisationBasicTest(bool cleanUpAllowed = true)
    {
        var orgLabel = OrgLabel.BasicOrg;
        
        return testHelper.OrgInsertBasicTest(orgLabel, cleanUpAllowed);
    }

    public bool UpdateOrganisationBasicTest(bool cleanUpAllowed = true)
    {
        var orgLabel = OrgLabel.BasicOrg;

        return testHelper.OrgUpdateBasicTest(orgLabel, cleanUpAllowed);
    }
    
    public bool InsertPersonWithOrgBasicTest(bool cleanUpAllowed = true)
    {
        var personLabel = PersonLabel.PersonWithOrg;

        var orgLabel = OrgLabel.BasicOrg;
        
        return testHelper.PersonInsertBasicTest(personLabel, orgLabel, cleanUpAllowed);
    }

    public bool UpdatePersonWithOrgBasicTest(bool cleanUpAllowed = true)
    {
        var person = PersonLabel.PersonWithOrg;
        var org = OrgLabel.BasicOrg;

        return testHelper.PersonUpdateBasicTest(person, org, cleanUpAllowed);
    }

    public bool DeleteOrganisationBasicTest(bool cleanupAllowed=true)
    {
        var orgToDelete = OrgLabel.BasicOrg;

        return testHelper.OrgDeleteBasicTest(orgToDelete, cleanupAllowed);
    }
    
    public bool DeletePersonWithOrgBasicTest(bool cleanupAllowed=true)
    {
        var person = PersonLabel.PersonWithOrg;
        
        var org = OrgLabel.BasicOrg;

        return testHelper.PersonDeleteBasicTest(person, org, cleanupAllowed);
    }

    public bool DeletePersonWithOrgTest_ManagesResources(bool cleanupAllowed = true)
    {
        var dataset = DatasetLabel.BasicDataset;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;
        
        return testHelper.DeletePersonTest_ManagesResources(person, dataset, cat, org, cleanupAllowed);
    }

    public bool DeleteOrgTest_ManagesResources(bool cleanupAllowed = true)
    {
        var dataset = DatasetLabel.BasicDataset;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;

        return testHelper.DeleteOrgTest_ManagesResources(org, dataset, cat, person, cleanupAllowed);
    }

    public bool DeletePersonTest_ContactsUsedByResources(bool cleanupAllowed = true)
    {
        var dataset = DatasetLabel.BasicDataset;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;

        var extraPerson = PersonLabel.ExtraPerson;
        
        return testHelper.TestDeletePerson_ContactsUsedByResources(person, extraPerson, dataset, cat, org, cleanupAllowed: cleanupAllowed);
    }

    public bool DeleteOrganisationTest_ContactsUsedByResources(bool cleanupAllowed = true)
    {
        var dataset = DatasetLabel.BasicDataset;
        var org = OrgLabel.BasicOrg;
        var person = PersonLabel.PersonWithOrg;
        var cat = CatLabel.BasicCat;
        
        var extraOrg = OrgLabel.ExtraOrg;

        return testHelper.TestDeleteOrg_ContactsUsedByResources(org, extraOrg, dataset, cat, person, cleanupAllowed);
    }
    
    public short PopulateWithTestData()
    {
        return 0;
    }
}