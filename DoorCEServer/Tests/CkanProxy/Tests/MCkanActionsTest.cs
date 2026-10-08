using DoorCEModel.Infrastructure;
using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CkanProxy.Domain;
using DoorCEServer.Application.CkanProxy.Domain.Services;
using DoorCEServer.Application.CkanProxy.Dtos;
using DoorCEServer.Application.DataContentsManager.Dtos;
using DoorCEServer.Application.DataContentsManager.Interfaces;
using DoorCEServer.Tests.CataloguingManager.DtoHelpers;
using DoorCEServer.Tests.CataloguingManager.Labels;
using DoorCEServer.Tests.SchemaManager.DtoHelpers;
using DoorCEServer.Tests.SchemaManager.Labels;
using NetTopologySuite.Utilities;
using Serilog;

namespace DoorCEServer.Tests.CkanProxy.Tests;

public class MCkanActionsTest(ApplicationDbContext context, SchemaSeriesHelper schemaSeriesHelper,
    SchemaHelper schemaHelper, OrganisationHelper organisationHelper, PersonHelper personHelper,
    CatalogueHelper catalogueHelper, DatasetSeriesHelper datasetSeriesHelper, DatasetHelper datasetHelper,
    DataServiceHelper dataServiceHelper, DistributionHelper distributionHelper, TestCommon testCommon, 
    DataUploadAPI dataUpload, MCkanActions mCkanActions, CkanMetadataService ckanService, 
    CkanDatastoreService datastoreService)
        : BaseTestHelper(context, schemaSeriesHelper, schemaHelper, organisationHelper, personHelper, catalogueHelper, 
            datasetSeriesHelper, datasetHelper, dataServiceHelper, distributionHelper) {

    public string TestAll() {
        var r1 = ErrorCatcher(UnpublishDatasetTest);
        var r2 = ErrorCatcher(ClearDatasetDatastoreTest);
        var r3 = ErrorCatcher(CreateResourceAndTableTest);
        var r4 = ErrorCatcher(PublishConceptDataItemsTest);
        var r5 = ErrorCatcher(PublishDataTest);
        var r6 = ErrorCatcher(PublishMetadataTest);
        var r7A = ErrorCatcher(EnsureDatastoreExistsTest);
        var r7 = ErrorCatcher(PublishMetadataWithDataTest);
        var r8 = ErrorCatcher(PublishIndependentMetadataTest);
        
        return $"{r1}\n{r2}\n{r3}\n{r4}\n{r5}\n{r6}\n{r7A}\n{r7}\n{r8}";
    }

    private new void SetUp(bool independent = false) {
        // set up
        base.SetUp();
        DatasetLabel datasetLabel = independent 
            ? DatasetLabel.BasicDatasetIndependent : DatasetLabel.BasicDatasetWithSchema;
        
        UpsertAll(OrgLabel.BasicOrg, PersonLabel.PersonWithOrg, CatLabel.BasicCat, null,
            datasetLabel,null, DistrLabel.BasicDistr, SSeriesLabel.BasicSchemaSeries, 
            SchemaLabel.BasicSchema);
        Dataset dataset = DatasetHelper.GetModel(datasetLabel);

        if (!independent) {
            IFormFile file = testCommon.GetMockFile(testCommon.GetMockJsonData());
            XDataUploadRequest dataUploadRequest = testCommon.GetMockXDataUploadRequest(dataset.Uri,
                dataset.Schema!.Concepts.ToDictionary(x => x.Name, x => x.Uri!));
            XFileValidationResult result = dataUpload.UploadData(dataUploadRequest, "udas-admin", file);

            if (!result.IsValid)
                throw new Exception($"Upload failed: {result.Errors}");
        }

        mCkanActions.UnpublishDataset(dataset);
    }

    private void CleanUp(Dataset dataset) {
        mCkanActions.UnpublishDataset(dataset);
        base.CleanUp();
    }

    private bool EnsureDatastoreExistsTest() {
        SetUp(true);
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetIndependent);
        DataSchema schema = dataset.Schema!;
        XCkanResponse res1 = mCkanActions.PublishMetadata(dataset, organisation).GetAwaiter().GetResult();
        
        XCkanBulkResult response = mCkanActions.EnsureDatastoreExists(dataset).GetAwaiter().GetResult();
        
        Assert.IsTrue(response.IsSuccess);
        response.ExtractObjectId();
        var package = ckanService.GetPackage(dataset).GetAwaiter().GetResult();
        Assert.IsTrue(package.IsSuccess);
        var resources = package.ExtractResources(true);
        Assert.IsTrue(resources != null && resources.Count != 0);
        Assert.IsTrue(resources.Count > schema.Concepts.Count);
        foreach (var concept in schema.Concepts) { 
            var id = datastoreService.GetTableId(dataset, concept).GetAwaiter().GetResult();
            Assert.IsTrue(id != null);
            Assert.IsTrue(resources.Any(r => r.CkanId == id));
        }
        
        CleanUp(dataset);
        return true;
    }

    private bool PublishMetadataTest() {
        SetUp();
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetWithSchema);
        
        XCkanResponse response = mCkanActions.PublishMetadata(dataset, organisation).GetAwaiter().GetResult();
        
        Assert.IsTrue(response.IsSuccess);
        response.ExtractObjectId();
        ICollection<XCkanResource>? resources = response.ExtractResources(true);
        Assert.IsTrue(resources is { Count: > 0 });
        Assert.IsTrue(resources!.All(r => r.UdasType == "distribution"));
        
        CleanUp(dataset);
        return true;
    }

    private bool PublishIndependentMetadataTest() {
        SetUp(true);
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetIndependent);
        DataSchema schema = dataset.Schema!;
        
        XCkanResponse response = mCkanActions.PublishMetadata(dataset, organisation, true)
            .GetAwaiter().GetResult();
        XCkanBulkResult respons = (XCkanBulkResult) response;
        
        Assert.IsTrue(respons.IsSuccess);
        respons.ExtractObjectId();
        var package = ckanService.GetPackage(dataset).GetAwaiter().GetResult();
        Assert.IsTrue(package.IsSuccess);
        var resources = package.ExtractResources(true);
        Assert.IsTrue(resources != null && resources.Count != 0);
        Assert.IsTrue(resources.Count > schema.Concepts.Count);
        foreach (var concept in schema.Concepts) { 
            var id = datastoreService.GetTableId(dataset, concept).GetAwaiter().GetResult();
            Assert.IsTrue(id != null);
            Assert.IsTrue(resources.Any(r => r.CkanId == id));
        };
        
        CleanUp(dataset);
        return true;
    }

    private bool PublishDataTest() {
        SetUp();
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetWithSchema);
        if (!dataset.HasDataItems()) throw new Exception("No dataset data available");
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        DataSchema schema = dataset.Schema!;
        var jsonData = testCommon.GetMockJsonData();
        XCkanResponse res = mCkanActions.PublishMetadata(dataset, organisation).GetAwaiter().GetResult();
        if (!res.IsSuccess) throw new Exception(res.ErrorMessage);
        
        XCkanBulkResult response = mCkanActions.PublishData(dataset, 
            new List<XCkanResource>(), res.ExtractObjectId()).GetAwaiter().GetResult();
        
        Assert.IsTrue(response.IsSuccess);
        response.ExtractObjectId();
        
        foreach (var concept in schema.Concepts) {
            Assert.IsEquals(response.UpsertedRows[concept.Uri], jsonData[concept.Name].Count);
        }
        
        CleanUp(dataset);
        return true;
    }

    private bool PublishMetadataWithDataTest() {
        SetUp();
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetWithSchema);
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        DataSchema schema = dataset.Schema!;
        var jsonData = testCommon.GetMockJsonData();
        
        XCkanResponse response = mCkanActions.PublishMetadataWithData(dataset, organisation).GetAwaiter().GetResult();
        var respons = (XCkanBulkResult)response;
        
        Assert.IsTrue(respons.IsSuccess);
        respons.ExtractObjectId();
        foreach (var concept in schema.Concepts) {
            Assert.IsEquals(respons.UpsertedRows[concept.Uri], jsonData[concept.Name].Count);
        }
        
        CleanUp(dataset);
        return true;
    }

    private bool PublishConceptDataItemsTest() {
        SetUp();
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetWithSchema);
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        var jsonData = testCommon.GetMockJsonData();
        XCkanResponse res = mCkanActions.PublishMetadata(dataset, organisation).GetAwaiter().GetResult();
        if (!res.IsSuccess) throw new Exception(res.ErrorMessage);
        
        // group data items by concepts and upload them into tables
        var dataItems = dataset.Items.ToList();
        var itemGroups = dataItems.GroupBy(i => i.Concept).ToList();
            
        foreach (var itemGroup in itemGroups) {
            XDatastorePublishResult result = mCkanActions.PublishConceptDataItems(itemGroup.ToList())
                .GetAwaiter().GetResult();
            Assert.IsTrue(result.IsSuccess);
            result.ExtractObjectId();
            Assert.IsEquals(result.RowsUpserted, jsonData[itemGroup.Key.Name].Count);
        }
        
        CleanUp(dataset);
        return true;
    }

    private bool CreateResourceAndTableTest() {
        SetUp();
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetWithSchema);
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        DataSchema schema = dataset.Schema!;
        XCkanResponse res = mCkanActions.PublishMetadata(dataset, organisation).GetAwaiter().GetResult();
        if (!res.IsSuccess) throw new Exception(res.ErrorMessage);
        
        XCkanResponse response = mCkanActions.CreateResourceAndTable(dataset, schema.Concepts.First())
            .GetAwaiter().GetResult();
        Assert.IsTrue(response.IsSuccess);
        response.ExtractObjectId();
        
        CleanUp(dataset);
        return true;
    }

    private bool ClearDatasetDatastoreTest() {
        SetUp();
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetWithSchema);
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        XCkanResponse res = mCkanActions.PublishMetadataWithData(dataset, organisation).GetAwaiter().GetResult();
        if (!res.IsSuccess) throw new Exception(res.ErrorMessage);
        ICollection<XCkanResource>? resources = ckanService.GetPackageResources(dataset).GetAwaiter().GetResult();
        if (resources == null) throw new Exception("No resources found");
        
        XCkanResponse result = mCkanActions.ClearDatasetDatastore(resources, res.ExtractObjectId()).GetAwaiter().GetResult();
        Assert.IsTrue(result.IsSuccess);
        
        var getResponse = ckanService.GetPackage(dataset).GetAwaiter().GetResult(); // make sure package still exists
        Assert.IsTrue(getResponse.ExtractObjectId() != null);
        
        ICollection<XCkanResource>? resourcesNull = ckanService.GetPackageResources(dataset).GetAwaiter().GetResult();
        Assert.IsTrue(resourcesNull == null || resourcesNull.Count == 0);
        Log.Debug($"[ClearDatasetDatastoreTest]: getting deleted table id from response: {res.ExtractResources()}");
        if (res is not XCkanBulkResult bulkResult) {
            throw new Exception("Bulk result not found");
        }
        var tabledIdBefore = bulkResult.Results.First().ExtractObjectId();
        var tableId = datastoreService.GetTableId(tabledIdBefore).GetAwaiter().GetResult();
        Assert.IsTrue(tableId == null);

        CleanUp(dataset);
        return true;
    }

    private bool UnpublishDatasetTest() {
        SetUp();
        Dataset dataset = DatasetHelper.GetModel(DatasetLabel.BasicDatasetWithSchema);
        Organisation organisation = OrgHelper.GetModel(OrgLabel.BasicOrg);
        XCkanResponse res = mCkanActions.PublishMetadataWithData(dataset, organisation).GetAwaiter().GetResult();
        if (!res.IsSuccess) throw new Exception(res.ErrorMessage);
        ICollection<XCkanResource>? resources = ckanService.GetPackageResources(dataset).GetAwaiter().GetResult();
        if (resources == null) throw new Exception("No resources found");
        
        XCkanResponse result = mCkanActions.UnpublishDataset(dataset);
        Assert.IsTrue(result.IsSuccess);
        
        var id = ckanService.GetPackageId(dataset).GetAwaiter().GetResult();
        Assert.IsTrue(id == null);
        if (res is not XCkanBulkResult bulkResult) {
            throw new Exception("Bulk result not found");
        }
        var tabledIdBefore = bulkResult.Results.First().ExtractObjectId();
        var tableId = datastoreService.GetTableId(tabledIdBefore).GetAwaiter().GetResult();
        Assert.IsTrue(tableId == null);
        
        CleanUp(dataset);
        return true;
    }
}