using DoorCEModel.Infrastructure;
using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.DtoHelpers;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.SchemaManager.DtoHelpers;
using DoorCEServer.Tests.SchemaManager.Labels;
using NetTopologySuite.Utilities;

namespace DoorCEServer.Tests.CataloguingManager.TestHelpers;

public class MDistrMgmtTestHelper(
    ApplicationDbContext context,
    SchemaSeriesHelper sSeriesHelper,
    SchemaHelper schemaHelper,
    OrganisationHelper orgHelper,
    CatalogueHelper catHelper,
    PersonHelper personHelper,
    DatasetSeriesHelper dSeriesHelper,
    DatasetHelper datasetHelper,
    DataServiceHelper serviceHelper,
    DistributionHelper distrHelper)
    : BaseTestHelper(context, sSeriesHelper, schemaHelper, orgHelper, personHelper, 
        catHelper, dSeriesHelper, datasetHelper, serviceHelper, distrHelper)
{
    private void ValidateInsertService(ServiceLabel serviceLabel)
    {
        XDataService? storedService = ServiceHelper.GetFromDb(serviceLabel); //gets agent from db by Uri
        
        // check if service exists
        Assert.IsTrue(null != storedService);

        // check if service contains correct values
        Assert.IsTrue(ServiceHelper.Validate(storedService!, serviceLabel, VariantLabel.AfterUpsert));
    }    
    
    private void ValidateUpdateService(ServiceLabel serviceLabel)
    {
        XDataService? storedService = ServiceHelper.GetFromDb(serviceLabel); //gets agent from db by Uri
        
        // check if service exists
        Assert.IsTrue(null != storedService);
        
        // check if service was modified
        Assert.IsTrue(!ServiceHelper.Validate(storedService!, serviceLabel, VariantLabel.AfterUpsert));

        // check if service contains correct values
        Assert.IsTrue(ServiceHelper.Validate(storedService!, serviceLabel, VariantLabel.ModifiedAfterUpsert));
    }

    private void SetUpServiceInsertTest(CatLabel catLabel, OrgLabel? orgLabel=null, PersonLabel? personLabel=null,
        bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);
        
        // create *organisation, *person and catalogue
        UpsertAll(orgLabel: orgLabel, personLabel: personLabel, catLabel: catLabel);
    }
    
    public bool ServiceInsertBasicTest(ServiceLabel serviceLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpServiceInsertTest(catLabel, orgLabel, personLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // add data service
        ServiceHelper.Upsert(serviceLabel, VariantLabel.BeforeUpsert, ContactsLabel.Contacts1);
        
        // **** VALIDATION ****
        ValidateInsertService(serviceLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void SetUpServiceUpdateTest(ServiceLabel serviceLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // *clear db
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue, data service
        UpsertAll(orgLabel: orgLabel, personLabel: personLabel, 
            catLabel: catLabel, serviceLabel: serviceLabel);
        
        // modify organisation, person, catalogue
        UpsertAll(update: true, orgLabel: orgLabel, personLabel: personLabel,
            catLabel: catLabel);
    }

    public bool ServiceUpdateBasicTest(ServiceLabel serviceLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpServiceUpdateTest(serviceLabel, catLabel, orgLabel, personLabel, cleanupAllowed);

        // **** EXECUTION ****
        ServiceHelper.Upsert(serviceLabel, VariantLabel.ModifiedBeforeUpsert, ContactsLabel.Contacts2);

        // **** VALIDATION ****
        ValidateUpdateService(serviceLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }
    
    private void ValidateInsertDistribution(DistrLabel distrLabel)
    {
        XDistribution? storedDistr = DistrHelper.GetFromDb(distrLabel); //gets agent from db by Uri
        
        // check if distribution exists
        Assert.IsTrue(null != storedDistr);
        
        // check if distribution contains correct values
        Assert.IsTrue(DistrHelper.Validate(storedDistr!, distrLabel, VariantLabel.AfterUpsert));
    }
    
    private void ValidateUpdateDistribution(DistrLabel distrLabel)
    {
        XDistribution? storedDistr = DistrHelper.GetFromDb(distrLabel); //gets agent from db by Uri
        
        // check if distribution exists
        Assert.IsTrue(null != storedDistr);
        
        // check if distribution was modified
        Assert.IsTrue(!DistrHelper.Validate(storedDistr!, distrLabel, VariantLabel.AfterUpsert));

        // check if distribution contains correct values
        Assert.IsTrue(DistrHelper.Validate(storedDistr!, distrLabel, VariantLabel.ModifiedAfterUpsert));
    }

    private void SetUpDistrInsertTest(DatasetLabel datasetLabel, CatLabel catLabel, OrgLabel? orgLabel=null, PersonLabel? personLabel=null,
        ServiceLabel? serviceLabel=null, SchemaLabel? schemaLabel=null, SSeriesLabel? sseriesLabel=null, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, dataset, *data service, *schema series and *schema
        UpsertAll(orgLabel: orgLabel, personLabel: personLabel, catLabel: catLabel,
            datasetLabel: datasetLabel, serviceLabel: serviceLabel, schemaLabel: schemaLabel, sSeriesLabel: sseriesLabel);
    }
    
    public bool DistrInsertBasicTest(DistrLabel distrLabel, CatLabel catLabel, DatasetLabel datasetLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, ServiceLabel? serviceLabel=null,
        SchemaLabel? schemaLabel=null, SSeriesLabel? sseriesLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDistrInsertTest(datasetLabel, catLabel, orgLabel, personLabel, serviceLabel, schemaLabel, sseriesLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // add distribution
        DistrHelper.Upsert(distrLabel, VariantLabel.BeforeUpsert);
        
        // **** VALIDATION ****
        ValidateInsertDistribution(distrLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void SetUpDistrUpdateTest(DistrLabel distrLabel, CatLabel catLabel, DatasetLabel datasetLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, ServiceLabel? serviceLabel=null,
        SchemaLabel? schemaLabel=null, SSeriesLabel? sseriesLabel=null, bool cleanupAllowed = true)
    {
        // (*clear db), create dataset, distribution, *data service, *schema series and schema
        SetUp(cleanupAllowed);
        UpsertAll(
            orgLabel: orgLabel, 
            personLabel: personLabel,
            catLabel: catLabel,
            datasetLabel: datasetLabel,
            distributionLabel: distrLabel,
            serviceLabel: serviceLabel, 
            schemaLabel: schemaLabel, 
            sSeriesLabel: sseriesLabel
        );
        
        // modify dataset, *data service, *schema series and schema
        UpsertAll(
            update: true,
            orgLabel: orgLabel,
            personLabel: personLabel,
            catLabel: catLabel,
            datasetLabel: datasetLabel,
            serviceLabel: serviceLabel, 
            schemaLabel: schemaLabel, 
            sSeriesLabel: sseriesLabel
        );
    }

    public bool DistrUpdateBasicTest(DistrLabel distrLabel, DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, ServiceLabel? serviceLabel=null,
        SchemaLabel? schemaLabel=null, SSeriesLabel? sseriesLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDistrUpdateTest(
            distrLabel: distrLabel,
            orgLabel: orgLabel,
            personLabel: personLabel,
            catLabel: catLabel,
            datasetLabel: datasetLabel,
            serviceLabel: serviceLabel,
            schemaLabel: schemaLabel,
            sseriesLabel: sseriesLabel,
            cleanupAllowed: cleanupAllowed
        );

        // **** EXECUTION ****
        DistrHelper.Upsert(distrLabel, VariantLabel.ModifiedBeforeUpsert);

        // **** VALIDATION ****
        ValidateUpdateDistribution(distrLabel);

        // **** CLEANUP ****
        CleanUp(cleanupAllowed);

        return true;
    }
    
    private void ValidateDeleteDataService(ServiceLabel serviceLabel, VariantLabel variantLabel=VariantLabel.AfterUpsert)
    {
        Assert.IsTrue(ServiceHelper.ValidateDelete(serviceLabel, variantLabel));
    }

    private void SetUpDeleteServiceTest(ServiceLabel serviceLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // *clear db
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue, data service
        UpsertAll(orgLabel, personLabel, catLabel, serviceLabel: serviceLabel);
    }
    
    public bool DeleteServiceTest(ServiceLabel serviceLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDeleteServiceTest(serviceLabel, catLabel, orgLabel, personLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        ServiceHelper.Delete(serviceLabel);
        
        // **** VALIDATION ****
        ValidateDeleteDataService(serviceLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
    
    private void ValidateDeleteDistribution(DistrLabel distrLabel)
    {
        Assert.IsTrue(DistrHelper.ValidateDelete(distrLabel));
    }

    private void SetUpDeleteDistrTest(DistrLabel distrLabel, DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, ServiceLabel? serviceLabel=null,
        SchemaLabel? schemaLabel=null, SSeriesLabel? sseriesLabel=null, bool cleanupAllowed = true)
    {
        // *clear db,
        SetUp(cleanupAllowed);
        
        // create *organisation, *person, catalogue, dataset, *data service, *schema series and *schema, distribution
        UpsertAll(orgLabel, personLabel, catLabel, datasetLabel: datasetLabel, serviceLabel: serviceLabel, 
            distributionLabel: distrLabel, sSeriesLabel: sseriesLabel, schemaLabel: schemaLabel);
    }
    
    public bool DeleteDistrTest(DistrLabel distrLabel, DatasetLabel datasetLabel, CatLabel catLabel,
        OrgLabel? orgLabel=null, PersonLabel? personLabel=null, ServiceLabel? serviceLabel=null, 
        SchemaLabel? schemaLabel=null, SSeriesLabel? sseriesLabel=null, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDeleteDistrTest(distrLabel, datasetLabel, catLabel, orgLabel, personLabel, serviceLabel, schemaLabel, sseriesLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        DistrHelper.Delete(distrLabel); // deletes by Uri
        
        // **** VALIDATION ****
        ValidateDeleteDistribution(distrLabel); // gets from db by Uri
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
}