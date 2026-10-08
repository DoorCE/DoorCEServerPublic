using DoorCEModel.Infrastructure;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.DtoHelpers;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.SchemaManager.DtoHelpers;
using NetTopologySuite.Utilities;

namespace DoorCEServer.Tests.CataloguingManager.TestHelpers;

public class MAgentsTestHelper(
    ApplicationDbContext context,
    SchemaSeriesHelper sSeriesHelper,
    SchemaHelper schemaHelper,
    OrganisationHelper orgHelper,
    PersonHelper personHelper,
    CatalogueHelper catHelper,
    DatasetSeriesHelper dSeriesHelper,
    DatasetHelper datasetHelper,
    DataServiceHelper dataServiceHelper,
    DistributionHelper distributionHelper,
    MDatasetMgmtTestHelper datasetTestHelper)
    : BaseTestHelper(context, sSeriesHelper, schemaHelper, orgHelper, personHelper, 
        catHelper, dSeriesHelper, datasetHelper, dataServiceHelper, distributionHelper)
{
    private void ValidateUpdateOrganisation(OrgLabel orgTested)
    {
        XAgent? storedAgent = OrgHelper.GetFromDb(orgTested); //gets agent from db by Uri

        // check if organisation was added
        Assert.IsTrue(null != storedAgent);

        // check if organisation has a correct type
        Assert.IsTrue(storedAgent is XOrganisation);
        XOrganisation storedOrganisation = (storedAgent as XOrganisation)!;

        // check if organisation was modified
        Assert.IsTrue(!OrgHelper.Validate(storedOrganisation, orgTested, VariantLabel.AfterUpsert));

        // check if organisation contains correct values
        Assert.IsTrue(OrgHelper.Validate(storedOrganisation, orgTested, VariantLabel.ModifiedAfterUpsert));
    }

    private void ValidateInsertOrganisation(OrgLabel orgTested)
    {
        XAgent? storedAgent = OrgHelper.GetFromDb(orgTested); //gets agent from db by Uri

        // check if organisation was added
        Assert.IsTrue(null != storedAgent);

        // check if organisation has a correct type
        Assert.IsTrue(storedAgent is XOrganisation);
        XOrganisation storedOrganisation = (storedAgent as XOrganisation)!;

        // check if organisation contains correct values
        Assert.IsTrue(OrgHelper.Validate(storedOrganisation, orgTested, VariantLabel.AfterUpsert));
    }

    public bool OrgInsertBasicTest(OrgLabel orgLabel, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUp(cleanupAllowed);

        // **** EXECUTION ****
        // add organisation
        OrgHelper.Upsert(orgLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);

        // **** VALIDATION ****
        ValidateInsertOrganisation(orgLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

    private void SetUpOrgUpdateTest(OrgLabel orgLabel, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);

        // create organisation
        OrgHelper.Upsert(orgLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
    }

    public bool OrgUpdateBasicTest(OrgLabel orgLabel, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpOrgUpdateTest(orgLabel, cleanupAllowed);

        // **** EXECUTION ****
        OrgHelper.Upsert(orgLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);

        // **** VALIDATION ****
        ValidateUpdateOrganisation(orgLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

    private void ValidateInsertPerson(PersonLabel personLabel)
    {
        XAgent? storedAgent = PersonHelper.GetFromDb(personLabel);

        // check if person was added
        Assert.IsTrue(null != storedAgent);

        // check if person has a correct type
        Assert.IsTrue(storedAgent is XPerson);
        XPerson storedPerson = (storedAgent as XPerson)!;

        // check if person contains correct values
        Assert.IsTrue(PersonHelper.Validate(storedPerson, personLabel, VariantLabel.AfterUpsert));
    }
    
    private void ValidateUpdatePerson(PersonLabel personLabel)
    {
        XAgent? storedAgent = PersonHelper.GetFromDb(personLabel);

        // check if person was added
        Assert.IsTrue(null != storedAgent);

        // check if person has a correct type
        Assert.IsTrue(storedAgent is XPerson);
        XPerson storedPerson = (storedAgent as XPerson)!;

        // check if organisation was modified
        Assert.IsTrue(!PersonHelper.Validate(storedPerson, personLabel, VariantLabel.AfterUpsert));

        // check if person contains correct values
        Assert.IsTrue(PersonHelper.Validate(storedPerson, personLabel, VariantLabel.ModifiedAfterUpsert));
    }

    private void SetUpPersonInsertTest(OrgLabel? orgLabel=null, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);

        // *create organisation
        if (null != orgLabel)
        {
            OrgHelper.Upsert((OrgLabel)orgLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        }
    }

    public bool PersonInsertBasicTest(PersonLabel personLabel, OrgLabel? orgLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpPersonInsertTest(orgLabel, cleanupAllowed);

        // **** EXECUTION ****
        // add person
        PersonHelper.Upsert(personLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);

        // **** VALIDATION ****
        ValidateInsertPerson(personLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

    private void SetUpPersonUpdateTest(PersonLabel personLabel, OrgLabel? orgLabel=null, bool cleanupAllowed = true)
    {
        // (*clear db and upsert organisation)
        SetUpPersonInsertTest(orgLabel, cleanupAllowed);

        // create person
        PersonHelper.Upsert(personLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);

        // (*create modified organisation)
        if (null != orgLabel)
        {
            OrgHelper.Upsert((OrgLabel)orgLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);
        }
    }

    public bool PersonUpdateBasicTest(PersonLabel personLabel, OrgLabel? orgLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpPersonUpdateTest(personLabel, orgLabel, cleanupAllowed);

        // **** EXECUTION ****
        PersonHelper.Upsert(personLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);

        // **** VALIDATION ****
        ValidateUpdatePerson(personLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

    private void ValidateDeleteOrganisation(OrgLabel orgToDelete, VariantLabel variantLabel=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(OrgHelper.ValidateDelete(orgToDelete, variantLabel));
    }

    private void ValidateDeleteOrganisation_NotDeleted(OrgLabel orgToDelete, VariantLabel variantLabel=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(!OrgHelper.ValidateDelete(orgToDelete, variantLabel));
    }

    private void SetUpOrgDeleteBasicTest(OrgLabel orgToUpsert, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);

        // create organisation
        OrgHelper.Upsert(orgToUpsert, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
    }

    public bool OrgDeleteBasicTest(OrgLabel orgToDelete, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpOrgDeleteBasicTest(orgToDelete, cleanupAllowed);

        // **** EXECUTION ****
        // delete organisation
        OrgHelper.Delete(orgToDelete);

        // **** VALIDATION ****
        ValidateDeleteOrganisation(orgToDelete);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

    private void SetUpAgentDeleteTest_ManagesResources(DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // *clear db, create *organisation, *person, catalogue, dataset
        SetUp(cleanupAllowed);
        UpsertAll(orgLabel, personLabel, catLabel, datasetLabel: datasetLabel);
    }

    private void ValidateDeleteOrgTest_ManagesResources(OrgLabel orgToDelete, DatasetLabel datasetToDelete,
        CatLabel catToDelete)
    {
        // check if agent was not deleted
        ValidateDeleteOrganisation_NotDeleted(orgToDelete);

        // check if resources were not deleted
        datasetTestHelper.ValidateDeleteCatalogue_NotDeleted(catToDelete);
        datasetTestHelper.ValidateDeleteDataset_NotDeleted(datasetToDelete);
    }

    public bool DeleteOrgTest_ManagesResources(OrgLabel orgLabel, DatasetLabel datasetLabel, CatLabel catLabel,
        PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****

        // create agent with resources
        SetUpAgentDeleteTest_ManagesResources(datasetLabel, catLabel, orgLabel, personLabel, cleanupAllowed);

        // **** EXECUTION ****

        // try to delete agent
        try { OrgHelper.Delete(orgLabel); } catch (InvalidOperationException) { }

        // **** VALIDATION ****
        ValidateDeleteOrgTest_ManagesResources(orgLabel, datasetLabel, catLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

    private void SetUpPersonDeleteBasicTest(PersonLabel personToUpsert, OrgLabel? orgToUpsert = null, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);

        // create organisation
        if (null != orgToUpsert)
        {
            OrgHelper.Upsert((OrgLabel)orgToUpsert, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        }

        // create person
        PersonHelper.Upsert(personToUpsert, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
    }

    private void ValidateDeletePerson(PersonLabel personToDelete, VariantLabel variantBeforeDelete=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(PersonHelper.ValidateDelete(personToDelete, variantBeforeDelete));
    }

    private void ValidateDeletePerson_NotDeleted(PersonLabel personToDelete, VariantLabel variantBeforeDelete=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(!PersonHelper.ValidateDelete(personToDelete, variantBeforeDelete));
    }

    public bool PersonDeleteBasicTest(PersonLabel personToDelete, OrgLabel? orgToUpsert = null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpPersonDeleteBasicTest(personToDelete, orgToUpsert, cleanupAllowed);

        // **** EXECUTION ****
        // delete person
        PersonHelper.Delete(personToDelete);

        // **** VALIDATION ****
        ValidateDeletePerson(personToDelete);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

    private void ValidateDeletePersonTest_ManagesResources(PersonLabel personToDelete, DatasetLabel datasetToDelete,
        CatLabel catToDelete)
    {
        // check if agent was not deleted
        ValidateDeletePerson_NotDeleted(personToDelete);

        // check if resources were not deleted
        datasetTestHelper.ValidateDeleteCatalogue_NotDeleted(catToDelete);
        datasetTestHelper.ValidateDeleteDataset_NotDeleted(datasetToDelete);
    }

    public bool DeletePersonTest_ManagesResources(PersonLabel personLabel, DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****

        // create agent with resources
        SetUpAgentDeleteTest_ManagesResources(datasetLabel, catLabel, orgLabel, personLabel, cleanupAllowed);

        // **** EXECUTION ****

        // try to delete agent
        try { PersonHelper.Delete(personLabel); } catch (InvalidOperationException) { }

        // **** VALIDATION ****
        ValidateDeletePersonTest_ManagesResources(personLabel, datasetLabel, catLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }
    
    private void SetUpPersonDeleteTest_ContactsUsedByResources(PersonLabel personLabel, PersonLabel extraPersonLabel,
        DatasetLabel datasetLabel, CatLabel catLabel, OrgLabel? orgLabel=null, OrgLabel? extraOrgLabel=null, bool cleanupAllowed = true)
    {
        // *clear db, create *organisation, person and their resources
        SetUp(cleanupAllowed);
        UpsertAll(orgLabel, personLabel, catLabel, datasetLabel: datasetLabel);
        
        // create agent not managing these resources
        if (null != extraOrgLabel)
        {
            OrgHelper.Upsert((OrgLabel)extraOrgLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        }
        
        PersonHelper.Upsert(extraPersonLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // assign agent's contacts to a resource and update the resource
        DatasetHelper.Upsert(datasetLabel, VariantLabel.WithExtraPersonContacts, ContactsLabel.Contacts1);
    }
    
    public bool TestDeletePerson_ContactsUsedByResources(PersonLabel personLabel, PersonLabel extraPersonLabel,
        DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, OrgLabel? extraOrgLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpPersonDeleteTest_ContactsUsedByResources(personLabel, extraPersonLabel, datasetLabel, catLabel,
            orgLabel, extraOrgLabel, cleanupAllowed);

        // **** EXECUTION ****

        // try to delete agent
        try { PersonHelper.Delete(extraPersonLabel); } catch (InvalidOperationException) { }

        // **** VALIDATION ****
        ValidateDeletePerson_NotDeleted(extraPersonLabel);
        
        // *********************
        
        // unassign agent contacts from resources
        DatasetHelper.Upsert(datasetLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // try to delete agent
        PersonHelper.Delete(extraPersonLabel);

        // **** VALIDATION ****
        ValidateDeletePerson(extraPersonLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }
    
    private void SetUpOrgDeleteTest_ContactsUsedByResources(OrgLabel orgLabel, OrgLabel extraOrgLabel, 
        DatasetLabel datasetLabel, CatLabel catLabel, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // *clear db, create *organisation, *person, catalogue, dataset
        SetUp(cleanupAllowed);
        UpsertAll(orgLabel, personLabel, catLabel, datasetLabel: datasetLabel);
        
        // create agent not managing these resources
        OrgHelper.Upsert(extraOrgLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // update a resource with agent's contacts
        DatasetHelper.Upsert(datasetLabel, VariantLabel.WithExtraOrgContacts, ContactsLabel.Contacts1);
    }
    
    public bool TestDeleteOrg_ContactsUsedByResources(OrgLabel orgLabel, OrgLabel extraOrgLabel, DatasetLabel datasetLabel,
        CatLabel catLabel, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpOrgDeleteTest_ContactsUsedByResources(orgLabel, extraOrgLabel, 
            datasetLabel, catLabel, personLabel, cleanupAllowed);

        // **** EXECUTION ****

        // try to delete agent
        try { OrgHelper.Delete(extraOrgLabel); } catch (InvalidOperationException) { }

        // **** VALIDATION ****
        ValidateDeleteOrganisation_NotDeleted(extraOrgLabel);
        
        // *********************
        
        // unassign agent contacts from resource
        DatasetHelper.Upsert(datasetLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // try to delete agent
        OrgHelper.Delete(extraOrgLabel);

        // **** VALIDATION ****
        ValidateDeleteOrganisation(extraOrgLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }
}