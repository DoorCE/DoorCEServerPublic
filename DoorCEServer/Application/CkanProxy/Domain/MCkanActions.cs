using DoorCEModel.Infrastructure.DataModel.Agents;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CkanProxy.Common;
using DoorCEServer.Application.CkanProxy.Domain.Services;
using DoorCEServer.Application.CkanProxy.Dtos;
using DoorCEServer.Application.CkanProxy.Interfaces;
using Serilog;

namespace DoorCEServer.Application.CkanProxy.Domain;

public class MCkanActions(CkanDatastoreService datastore, CkanMetadataService metadata) : ICkanActions {

    public async Task<XCkanResponse> PublishMetadata(Dataset dataset, Organisation organisation, 
        bool ensureDatastoreExists = false) {
        
        XCkanResponse upsertResponse = metadata.UpsertDataset(dataset, organisation).GetAwaiter().GetResult();
        if  (!upsertResponse.IsSuccess || !ensureDatastoreExists) return upsertResponse;
        
        XCkanBulkResult ensureResults = await EnsureDatastoreExists(dataset);
        var allResults = ensureResults.Results.Concat([upsertResponse]).ToList();
        return new XCkanBulkResult(allResults, ensureResults.ExtractObjectId());
  
    }

    public async Task<XCkanBulkResult> PublishData(Dataset dataset, ICollection<XCkanResource> oldResources, 
        string packageId) {
        
            // delete old datastore resources (and corresponding ckan resources)
            XCkanBulkResult clearResult = await ClearDatasetDatastore(oldResources, packageId);

            if (!dataset.HasDataItems() || !clearResult.IsSuccess) {
                return clearResult;
            }
            
            if (dataset.Schema == null) {
                throw new ArgumentException("Failed to publish data to Datastore. Dataset has no schema assigned.");
            }
            
            // group data items by concepts and upload them into tables
            var results = new List<XCkanResponse>();
            var dataItems = dataset.Items.ToList();
            var itemGroups = dataItems.GroupBy(i => i.Concept).ToList();
            
            foreach (var itemGroup in itemGroups) {
                XDatastorePublishResult result = await PublishConceptDataItems(itemGroup.ToList()); 
                results.Add(result);
            }
            return new XCkanBulkResult(results, packageId);
    }
    
    // TODO - fix response inheritance

    public async Task<XDatastorePublishResult> PublishConceptDataItems(List<DataItem> dataItems) {
        // get dataset and concept from data items
        var dataset = dataItems.First().Dataset;
        var concept = dataItems.First().Concept;

        if (!dataItems.All(di => di.Dataset.Id == dataset.Id && di.Concept.Id == concept.Id))
            throw new ArgumentException("All data items must belong to the same dataset and concept");

        try
        {
            // check if table exists, if not create ckan resource and datastore table
            string? resourceId = await datastore.GetTableId(dataset, concept);

            if (resourceId != null) return await datastore.UpsertIntoTableInBatches(resourceId, dataItems, concept);
            XCkanResponse response = await CreateResourceAndTable(dataset, concept);
            if (!response.IsSuccess)
                return XDatastorePublishResult.FromXCkanResponse(concept.Uri!, 0, response);
            resourceId = response.ExtractObjectId();

            // upsert records into table in batches
            return await datastore.UpsertIntoTableInBatches(resourceId!, dataItems, concept);
                
        }
        catch (Exception ex)
        {
            Log.Error("Failed to publish concept {ConceptUri} data items: {ExMessage}", 
                concept.Uri, ex.Message);
            throw;
        }
    }

    public async Task<XCkanResponse> CreateResourceAndTable(Dataset dataset, Concept concept) {
        
        XCkanResponse resourceResponse = await metadata.CreateStructuredDataResource(dataset, concept);
        if (!resourceResponse.IsSuccess) return resourceResponse;
        XCkanResponse tableResponse =
            await datastore.UpsertTable(resourceResponse.ExtractObjectId()!, dataset, concept);
        if (tableResponse.IsSuccess) return tableResponse;
        
        var result = await metadata.DeleteCkanResource(resourceResponse.ExtractObjectId()!);
        return !result.IsSuccess ? result : tableResponse;
    }

    public async Task<XCkanResponse> PublishMetadataWithData(Dataset dataset, Organisation organisation) {
        
        XCkanResponse metadataResponse = await PublishMetadata(dataset, organisation);
        if (!metadataResponse.IsSuccess) return metadataResponse;
        XCkanResponse dataResponse = await
            PublishData(dataset, metadataResponse.ExtractResources() ?? new List<XCkanResource>(),
                metadataResponse.ExtractObjectId()!);
        return dataResponse;
    }

    public async Task<XCkanBulkResult> ClearDatasetDatastore(ICollection<XCkanResource> resources, 
        string parentObjectId) {
        
        var results = new List<XCkanResponse>();
        
        foreach (var resource in resources) {
            
            string? resourceId = resource.CkanId;

            if (null == resourceId) continue;
            
            XCkanResponse tableResult = await datastore.DeleteTable(resourceId);
            results.Add(tableResult);
            XCkanResponse ckanResult = await metadata.DeleteCkanResource(resourceId);
            results.Add(ckanResult);
        }
        
        return new XCkanBulkResult(results, parentObjectId);
    }

    public XCkanResponse UnpublishDataset(Dataset dataset) {
        // get package from ckan
        XCkanResponse packageResponse = metadata.GetPackage(dataset).GetAwaiter().GetResult();
        if (!packageResponse.IsSuccess) return packageResponse;

        ICollection<XCkanResource>? resources =
            packageResponse.ExtractResources(true);

        if (null == resources || resources.Count == 0)
            return metadata.PurgeCkanPackage(dataset).GetAwaiter().GetResult()!;
        foreach (var resource in resources) {
            if (resource.UdasType != "distribution") {
                string? tableId = datastore.GetTableId(resource.CkanId ?? throw new Exception("Resource not found"))
                    .GetAwaiter().GetResult();
                if (null != tableId) {
                    XCkanResponse tableResult = datastore
                        .DeleteTable(resource.CkanId
                                     ?? throw new ArgumentNullException(nameof(resource.CkanId))
                        ).GetAwaiter().GetResult();
                    if (!tableResult.IsSuccess)
                        return tableResult;
                }
            }
            XCkanResponse ckanResponse = metadata
                .DeleteCkanResource(resource.CkanId 
                                    ?? throw new Exception("Resource not found"))
                .GetAwaiter().GetResult();
            if (!ckanResponse.IsSuccess) return ckanResponse;
        }

        return metadata.PurgeCkanPackage(dataset).GetAwaiter().GetResult()!;
    }

    public async Task<XCkanBulkResult> EnsureDatastoreExists(Dataset dataset) {
        if (null == dataset.Schema) {
            throw new ArgumentException("Dataset must have a Schema");
        }

        XCkanResponse response = await metadata.GetPackage(dataset);
        if (!response.IsSuccess) throw new Exception($"Unable to fetch Ckan package for dataset {dataset.Uri}");

        ICollection<XCkanResource>? resources = response.ExtractResources(true);

        List<XCkanResponse> responses = new List<XCkanResponse>();
        foreach (var concept in dataset.Schema.Concepts) {
            string? resourceId = resources?.FirstOrDefault(r 
                => r.CkanName == MCkanProxyCommon.CreateCkanName(concept))?.CkanId;

            if (null != resourceId) {
                string? tableId = await datastore.GetTableId(resourceId);
                if (null == tableId) {
                    XCkanResponse tableResult = await datastore.UpsertTable(resourceId, dataset, concept);
                    responses.Add(tableResult);
                }
            } else {
                XCkanResponse createResponse = await CreateResourceAndTable(dataset, concept);
                responses.Add(createResponse);
            }
        }

        return new XCkanBulkResult(responses, response.ExtractObjectId());
    }
}