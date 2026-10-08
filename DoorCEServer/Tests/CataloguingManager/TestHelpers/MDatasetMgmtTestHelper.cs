using DoorCEModel.Infrastructure;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.DtoHelpers;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.SchemaManager.DtoHelpers;
using DoorCEServer.Tests.SchemaManager.Labels;
using NetTopologySuite.Utilities;

namespace DoorCEServer.Tests.CataloguingManager.TestHelpers;

public class MDatasetMgmtTestHelper(
    ApplicationDbContext context,
    SchemaHelper schemaHelper,
    SchemaSeriesHelper sSeriesHelper,
    OrganisationHelper orgHelper,
    PersonHelper personHelper,
    CatalogueHelper catHelper,
    DatasetSeriesHelper dSeriesHelper,
    DatasetHelper datasetHelper,
    DataServiceHelper dataServiceHelper,
    DistributionHelper distributionHelper)
    : BaseTestHelper(context, sSeriesHelper, schemaHelper, orgHelper, personHelper, 
        catHelper, dSeriesHelper, datasetHelper, dataServiceHelper, distributionHelper)
{

    private void ValidateInsertCatalogue(CatLabel catLabel)
    {
        XCatalogue? storedCatalogue = CatHelper.GetFromDb(catLabel); //gets agent from db by Uri
        
        // check if catalogue exists
        Assert.IsTrue(null != storedCatalogue);

        // check if catalogue contains correct values
        Assert.IsTrue(CatHelper.Validate(storedCatalogue!, catLabel, VariantLabel.AfterUpsert));
    }
    
    private void ValidateUpdateCatalogue(CatLabel catLabel)
    {
        XCatalogue? storedCatalogue = CatHelper.GetFromDb(catLabel); //gets agent from db by Uri
        
        // check if catalogue exists
        Assert.IsTrue(null != storedCatalogue);
        
        // check if catalogue was modified
        Assert.IsTrue(!CatHelper.Validate(storedCatalogue!, catLabel, VariantLabel.AfterUpsert));

        // check if catalogue contains correct values
        Assert.IsTrue(CatHelper.Validate(storedCatalogue!, catLabel, VariantLabel.ModifiedAfterUpsert));
    }

    private void SetUpCatInsertTest(OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);
        
        // *create organisation
        if (null != orgLabel)
        {
            OrgHelper.Upsert((OrgLabel)orgLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        }
        
        // *create person
        if (null != personLabel)
        {
            PersonHelper.Upsert((PersonLabel)personLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        }
    }
    
    public bool CatInsertBasicTest(CatLabel catLabel, OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpCatInsertTest(orgLabel, personLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // add catalogue
        CatHelper.Upsert(catLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // **** VALIDATION ****
        ValidateInsertCatalogue(catLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void SetUpCatUpdateTest(CatLabel catLabel, OrgLabel? orgLabel=null, 
        PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // *clear db, create organisation and person
        SetUpCatInsertTest(orgLabel, personLabel, cleanupAllowed);
        
        // create catalogue
        CatHelper.Upsert(catLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // *modify organisation and person
        if (null != orgLabel)
        {
            OrgHelper.Upsert((OrgLabel)orgLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);
        }
        
        if (null != personLabel)
        {
            PersonHelper.Upsert((PersonLabel)personLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);
        }
    }
    
    public bool CatUpdateBasicTest(CatLabel catLabel, OrgLabel? orgLabel=null, 
        PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpCatUpdateTest(catLabel, orgLabel, personLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        CatHelper.Upsert(catLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);
        
        // **** VALIDATION ****
        ValidateUpdateCatalogue(catLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void ValidateInsertDataset(DatasetLabel datasetLabel)
    {
        XDataset? storedDataset = DatasetHelper.GetFromDb(datasetLabel); //gets agent from db by Uri
        
        // check if dataset exists
        Assert.IsTrue(null != storedDataset);

        // check if dataset contains correct values
        Assert.IsTrue(DatasetHelper.Validate(storedDataset!, datasetLabel, VariantLabel.AfterUpsert));
    }
    
    private void ValidateUpdateDataset(DatasetLabel datasetLabel)
    {
        XDataset? storedDataset = DatasetHelper.GetFromDb(datasetLabel); //gets agent from db by Uri
        
        // check if catalogue exists
        Assert.IsTrue(null != storedDataset);
        
        // check if catalogue was modified
        Assert.IsTrue(!DatasetHelper.Validate(storedDataset!, datasetLabel, VariantLabel.AfterUpsert));

        // check if catalogue contains correct values
        Assert.IsTrue(DatasetHelper.Validate(storedDataset!, datasetLabel, VariantLabel.ModifiedAfterUpsert));
    }

    private void SetUpDatasetInsertTest(CatLabel catLabel, OrgLabel? orgLabel=null, PersonLabel? personLabel=null,
        DSeriesLabel? dseriesLabel=null, SchemaLabel? schemaLabel=null, SSeriesLabel? sSeriesLabel=null, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue, *dataset series, *schema series and *schema
        UpsertAll(orgLabel, personLabel, catLabel, dseriesLabel, sSeriesLabel: sSeriesLabel, schemaLabel: schemaLabel);
    }
    
    public bool DatasetInsertBasicTest(DatasetLabel datasetLabel, CatLabel catLabel, 
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, DSeriesLabel? dseriesLabel=null,
        SchemaLabel? schemaLabel=null, SSeriesLabel? sseriesLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDatasetInsertTest(catLabel, orgLabel, personLabel, 
            dseriesLabel, schemaLabel, sseriesLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // add dataset
        DatasetHelper.Upsert(datasetLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // **** VALIDATION ****
        ValidateInsertDataset(datasetLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void SetUpDatasetUpdateTest(DatasetLabel datasetLabel,  CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null,
        DSeriesLabel? dseriesLabel=null, SchemaLabel? schemaLabel=null, SSeriesLabel? sseriesLabel=null,
        bool cleanupAllowed = true)
    {
        // (*clear db), create *organisation, *person, catalogue, *dataset series, dataset, *schema series and *schema
        SetUp(cleanupAllowed);
        UpsertAll(orgLabel, personLabel, catLabel, dseriesLabel, datasetLabel, sSeriesLabel: sseriesLabel, schemaLabel: schemaLabel);
        
        // modify organisation, person, catalogue, *dataset series, *schema series and schema
        UpsertAll(orgLabel, personLabel, catLabel, dseriesLabel: dseriesLabel, 
            schemaLabel: schemaLabel, sSeriesLabel: sseriesLabel, update: true);
    }

    public bool DatasetUpdateBasicTest(DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null,
        DSeriesLabel? dseriesLabel=null, SchemaLabel? schemaLabel=null,
        SSeriesLabel? sseriesLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDatasetUpdateTest(datasetLabel, catLabel, orgLabel, personLabel, dseriesLabel, schemaLabel, sseriesLabel, cleanupAllowed);

        // **** EXECUTION ****
        DatasetHelper.Upsert(datasetLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);

        // **** VALIDATION ****
        ValidateUpdateDataset(datasetLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }
    
    private void ValidateInsertDSeries(DSeriesLabel dseriesLabel)
    {
        XDatasetSeries? storedDSeries = DSeriesHelper.GetFromDb(dseriesLabel); //gets agent from db by Uri
        
        // check if catalogue exists
        Assert.IsTrue(null != storedDSeries);

        // check if catalogue contains correct values
        Assert.IsTrue(DSeriesHelper.Validate(storedDSeries!, dseriesLabel, VariantLabel.AfterUpsert));
    }
    
    private void ValidateUpdateDSeries(DSeriesLabel dSeriesLabel)
    {
        XDatasetSeries? storedDSeries = DSeriesHelper.GetFromDb(dSeriesLabel); //gets agent from db by Uri
        
        // check if catalogue exists
        Assert.IsTrue(null != storedDSeries);
        
        // check if catalogue was modified
        Assert.IsTrue(!DSeriesHelper.Validate(storedDSeries!, dSeriesLabel, VariantLabel.AfterUpsert));

        // check if catalogue contains correct values
        Assert.IsTrue(DSeriesHelper.Validate(storedDSeries!, dSeriesLabel, VariantLabel.ModifiedAfterUpsert));
    }
    
    private void SetUpDSeriesInsertTest(CatLabel catLabel, 
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue
        UpsertAll(orgLabel, personLabel, catLabel);
    }
    
    public bool DSeriesInsertBasicTest(DSeriesLabel dseriesLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDSeriesInsertTest(catLabel, orgLabel, personLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // add dataset series
        DSeriesHelper.Upsert(dseriesLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // **** VALIDATION ****
        ValidateInsertDSeries(dseriesLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void SetUpDSeriesUpdateTest(DSeriesLabel dseriesLabel, CatLabel catLabel, 
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // *clear db
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue, dataset series
        UpsertAll(orgLabel, personLabel, catLabel, dseriesLabel);
        
        // modify *organisation, *person and catalogue
        UpsertAll(orgLabel, personLabel, catLabel, update: true);
    }

    public bool DSeriesUpdateBasicTest(DSeriesLabel dseriesLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDSeriesUpdateTest(dseriesLabel, catLabel, orgLabel, personLabel, cleanupAllowed);

        // **** EXECUTION ****
        DSeriesHelper.Upsert(dseriesLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);

        // **** VALIDATION ****
        ValidateUpdateDSeries(dseriesLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

    private void ValidateDeleteCatalogue(CatLabel catLabel, VariantLabel variantLabel=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(CatHelper.ValidateDelete(catLabel, variantLabel));
    }

    public void ValidateDeleteCatalogue_NotDeleted(CatLabel catLabel, VariantLabel variantLabel=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(!CatHelper.ValidateDelete(catLabel, variantLabel));
    }
    
    private void SetUpDeleteEmptyCatalogueTest(CatLabel catLabel, OrgLabel? orgLabel=null, PersonLabel? personLabel=null,
        bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue
        UpsertAll(orgLabel, personLabel, catLabel);
    }
    
    public bool DeleteCatalogueTest_Empty(CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDeleteEmptyCatalogueTest(catLabel, orgLabel, personLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        CatHelper.Delete(catLabel);
        
        // **** VALIDATION ****
        ValidateDeleteCatalogue(catLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
    
    private void ValidateDeleteDSeries(DSeriesLabel dseriesLabel, VariantLabel variantLabel=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(DSeriesHelper.ValidateDelete(dseriesLabel, variantLabel));
    }

    private void SetUpDeleteDSeriesBasicTest(DSeriesLabel dseriesLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // *clear db
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue, dataset series
        UpsertAll(orgLabel, personLabel, catLabel, dseriesLabel);
    }
    
    public bool DeleteDSeriesBasicTest(DSeriesLabel dseriesLabel, CatLabel catLabel,
    OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDeleteDSeriesBasicTest(dseriesLabel, catLabel, orgLabel, personLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        DSeriesHelper.Delete(dseriesLabel);
        
        // **** VALIDATION ****
        ValidateDeleteDSeries(dseriesLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
    
    private void ValidateDeleteDataset(DatasetLabel datasetLabel, VariantLabel variantLabel=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(DatasetHelper.ValidateDelete(datasetLabel, variantLabel));
    }
    
    public void ValidateDeleteDataset_NotDeleted(DatasetLabel datasetLabel, VariantLabel variantLabel=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(!DatasetHelper.ValidateDelete(datasetLabel, variantLabel));
    }

    private void SetUpDeleteDatasetTest(DatasetLabel datasetLabel, CatLabel catLabel, 
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null,
        DSeriesLabel? dseriesLabel=null, SSeriesLabel? sseriesLabel=null,
        SchemaLabel? schemaLabel=null,  bool cleanupAllowed = true)
    {
        // *clear db
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue, dataset
        UpsertAll(orgLabel, personLabel, catLabel, dseriesLabel, datasetLabel,
            sSeriesLabel: sseriesLabel, schemaLabel: schemaLabel);
    }
    
    public bool DeleteDatasetTest(DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null,
        DSeriesLabel? dseriesLabel=null, SSeriesLabel? sseriesLabel=null,
        SchemaLabel? schemaLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDeleteDatasetTest(datasetLabel,  catLabel, orgLabel, personLabel,
            dseriesLabel, sseriesLabel, schemaLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        DatasetHelper.Delete(datasetLabel);
        
        // **** VALIDATION ****
        ValidateDeleteDataset(datasetLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
    
    private void ValidateDeleteCatalogue_WithChild(CatLabel catLabel)
    {
        ValidateDeleteCatalogue_NotDeleted(catLabel);
        ValidateDeleteCatalogue_NotDeleted(catLabel, VariantLabel.ChildCatAfterUpsert);
    }

    private void SetUpDeleteCatalogueTest_WithChild(CatLabel catLabel, OrgLabel? orgLabel=null, 
        PersonLabel? personLabel=null, bool cleanupAllowed=true)
    {
        // *clear db
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue
        UpsertAll(orgLabel, personLabel, catLabel);
        
        // create child catalogue
        CatHelper.Upsert(catLabel, VariantLabel.ChildCatBeforeUpsert, ContactsLabel.ChildCatContacts1);
    }

    public bool DeleteCatalogueTest_WithChild(CatLabel catLabel, OrgLabel? orgLabel=null,
        PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDeleteCatalogueTest_WithChild(catLabel, orgLabel, personLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // try to delete parent catalog
        try { CatHelper.Delete(catLabel); } catch (InvalidOperationException) {}
        
        // **** VALIDATION ****
        ValidateDeleteCatalogue_WithChild(catLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
    
    private void SetUpCatDeleteTest_WithResources(DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // *clear db
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue, dataset
        UpsertAll(orgLabel, personLabel, catLabel, datasetLabel: datasetLabel);
    }

    private void ValidateDeleteCatTest_WithResources(DatasetLabel datasetToDelete, CatLabel catToDelete)
    {
        // check if catalogue was not deleted
        ValidateDeleteCatalogue_NotDeleted(catToDelete);

        // check if resources were not deleted
        ValidateDeleteDataset_NotDeleted(datasetToDelete);
    }

    public bool DeleteCatTest_WithResources(DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed=true)
    {
        // **** SETUP ****

        // create catalogue with resources
        SetUpCatDeleteTest_WithResources(datasetLabel, catLabel, orgLabel, personLabel, cleanupAllowed);

        // **** EXECUTION ****

        // try to delete catalogue
        try { CatHelper.Delete(catLabel); } catch (InvalidOperationException) {}

        // **** VALIDATION ****
        ValidateDeleteCatTest_WithResources(datasetLabel, catLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }

}