using DoorCEModel.Infrastructure;
using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Tests.CataloguingManager.DtoHelpers;
using DoorCEServer.Tests.SchemaManager.DtoHelpers;
using DoorCEServer.Tests.SchemaManager.Labels;
using NetTopologySuite.Utilities;

namespace DoorCEServer.Tests.SchemaManager.TestHelpers;

public class MSchemasTestHelper(
    ApplicationDbContext context,
    SchemaSeriesHelper sSeriesHelper,
    SchemaHelper schemaHelper,
    OrganisationHelper organisationHelper,
    PersonHelper personHelper,
    CatalogueHelper catalogueHelper,
    DatasetSeriesHelper datasetSeriesHelper,
    DatasetHelper datasetHelper,
    DataServiceHelper dataServiceHelper,
    DistributionHelper distributionHelper)
    : BaseTestHelper(context, sSeriesHelper, schemaHelper, organisationHelper, personHelper, 
        catalogueHelper, datasetSeriesHelper, datasetHelper, dataServiceHelper, distributionHelper)
{
    private void ValidateInsertSchemaSeries(SSeriesLabel sseriesLabel)
    {
        XSchemaSeries? storedSSeries = SSeriesHelper.GetFromDb(sseriesLabel);
        
        // check if schema series was added
        Assert.IsTrue(null != storedSSeries);
        
        // check if schema series contains correct values
        Assert.IsTrue(SSeriesHelper.Validate(storedSSeries!, sseriesLabel, VariantLabel.AfterUpsert));
    }
    
    private void ValidateUpdateSchemaSeries(SSeriesLabel sseriesLabel)
    {
        XSchemaSeries? storedSSeries = SSeriesHelper.GetFromDb(sseriesLabel); //gets agent from db by Uri
        
        // check if schema series was added
        Assert.IsTrue(null != storedSSeries);
        
        // check if schema series was modified
        Assert.IsTrue(!SSeriesHelper.Validate(storedSSeries!, sseriesLabel, VariantLabel.AfterUpsert));

        // check if schema series contains correct values
        Assert.IsTrue(SSeriesHelper.Validate(storedSSeries!, sseriesLabel, VariantLabel.ModifiedAfterUpsert));
    }

    public bool SSeriesInsertBasicTest(SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUp(cleanupAllowed);
        
        // **** EXECUTION ****
        // add schema series
        SSeriesHelper.Upsert(sseriesLabel, VariantLabel.BeforeUpsert);
        
        // **** VALIDATION ****
        ValidateInsertSchemaSeries(sseriesLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void SetUpSSeriesUpdateTest(SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // *clear db
        SetUp(cleanupAllowed);

        // create schema series
        SSeriesHelper.Upsert(sseriesLabel, VariantLabel.BeforeUpsert);
    }

    public bool SSeriesUpdateBasicTest(SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpSSeriesUpdateTest(sseriesLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        SSeriesHelper.Upsert(sseriesLabel, VariantLabel.ModifiedBeforeUpsert);
        
        // **** VALIDATION ****
        ValidateUpdateSchemaSeries(sseriesLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
    
    private void ValidateInsertSchema(SchemaLabel schemaLabel)
    {
        XDataSchema? storedSchema = SchemaHelper.GetFromDb(schemaLabel); //gets agent from db by Uri
        
        // check if schema was added
        Assert.IsTrue(null != storedSchema);
        
        // check if schema contains correct values
        Assert.IsTrue(SchemaHelper.Validate(storedSchema!, schemaLabel, VariantLabel.AfterUpsert));
    }
    
    private void ValidateUpdateSchema(SchemaLabel schemaLabel)
    {
        XDataSchema? storedSchema = SchemaHelper.GetFromDb(schemaLabel); //gets agent from db by Uri
        
        // check if schema was added
        Assert.IsTrue(null != storedSchema);
        
        // check if schema was modified
        Assert.IsTrue(!SchemaHelper.Validate(storedSchema!, schemaLabel, VariantLabel.AfterUpsert));

        // check if schema contains correct values
        Assert.IsTrue(SchemaHelper.Validate(storedSchema!, schemaLabel, VariantLabel.ModifiedAfterUpsert));
    }

    private void SetUpSchemaInsertTest(SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);

        // create schema series
        SSeriesHelper.Upsert(sseriesLabel, VariantLabel.BeforeUpsert);
    }

    public bool SchemaInsertBasicTest(SchemaLabel schemaLabel, SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpSchemaInsertTest(sseriesLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // add schema
        SchemaHelper.Upsert(schemaLabel, VariantLabel.BeforeUpsert);
        
        // **** VALIDATION ****
        ValidateInsertSchema(schemaLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void SetUpSchemaUpdateTest(SchemaLabel schemaLabel, SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // *clear db, create schema series
        SetUpSchemaInsertTest(sseriesLabel, cleanupAllowed);

        // create schema
        SchemaHelper.Upsert(schemaLabel, VariantLabel.BeforeUpsert);

        // modify schema series
        SSeriesHelper.Upsert(sseriesLabel, VariantLabel.ModifiedBeforeUpsert);
    }

    public bool SchemaUpdateBasicTest(SchemaLabel schemaLabel, SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpSchemaUpdateTest(schemaLabel, sseriesLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        SchemaHelper.Upsert(schemaLabel, VariantLabel.ModifiedBeforeUpsert);
        
        // **** VALIDATION ****
        ValidateUpdateSchema(schemaLabel);
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }

    private void ValidateDeleteSchemaSeries(SSeriesLabel sseriesToDelete)
    {
        Assert.IsTrue(SSeriesHelper.ValidateDelete(sseriesToDelete));
    }
    
    private void SetUpDeleteSSeriesBasicTest(SSeriesLabel sseriesToUpsert, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);
        
        // create schema series
        SSeriesHelper.Upsert(sseriesToUpsert, VariantLabel.BeforeUpsert);
    }
    
    public bool DeleteSSeriesBasicTest(SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDeleteSSeriesBasicTest(sseriesLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // delete organisation
        SSeriesHelper.Delete(sseriesLabel); // deletes by Uri
        
        // **** VALIDATION ****
        ValidateDeleteSchemaSeries(sseriesLabel); // gets from db by Uri
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
    
    private void ValidateDeleteSchema(SchemaLabel schemaToDelete)
    {
        Assert.IsTrue(SchemaHelper.ValidateDelete(schemaToDelete));
    }
    
    private void SetUpDeleteSchemaTest(SchemaLabel schemaLabel,
        SSeriesLabel sseriesLabel, bool cleanupAllowed = true)
    {
        // make sure database is created (*and clear it)
        SetUp(cleanupAllowed);
        
        // create schema series
        SSeriesHelper.Upsert(sseriesLabel, VariantLabel.BeforeUpsert);
        
        // create schema
        SchemaHelper.Upsert(schemaLabel, VariantLabel.BeforeUpsert);
    }
    
    public bool DeleteSchemaTest(SchemaLabel schemaLabel, SSeriesLabel sseriesLabel,
        bool cleanupAllowed = true)
    {
        // **** SETUP ****
        SetUpDeleteSchemaTest(schemaLabel, sseriesLabel, cleanupAllowed);
        
        // **** EXECUTION ****
        // delete organisation
        SchemaHelper.Delete(schemaLabel); // deletes by Uri
        
        // **** VALIDATION ****
        ValidateDeleteSchema(schemaLabel); // gets from db by Uri
        
        // **** CLEANUP ****
        CleanUp(cleanupAllowed);
        
        return true;
    }
}