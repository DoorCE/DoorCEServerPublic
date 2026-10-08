using System.Net;
using System.Text.Json;
using DoorCEModel.Infrastructure.DataModel.DataSchemas;
using DoorCEModel.Infrastructure.DataModel.DatasetContents;
using DoorCEModel.Infrastructure.DataModel.Datasets;
using DoorCEServer.Application.CkanProxy.Common;
using DoorCEServer.Application.CkanProxy.Dtos;

namespace DoorCEServer.Application.CkanProxy.Domain.Services;

public class CkanDatastoreService(CkanClient client, RequestBuilder reqBuilder) {

    public async Task<string?> GetTableId(Dataset dataset, Concept concept) {
        XCkanResponse response = await GetTable(dataset, concept);
        if (!response.IsSuccess) return null;
        return response.ExtractObjectId();
    }

    public async Task<string?> GetTableId(string tableId) {
        XCkanResponse response = await GetTable(tableId);
        if (!response.IsSuccess) return null;
        return response.ExtractObjectId();
    }

    private async Task<XCkanResponse> GetTable(Dataset dataset, Concept concept)
    {
        XDatastoreRequest infoRequest 
            = RequestBuilder.BuildGetTable(MCkanProxyCommon.CreateDatastoreAlias(dataset.Uri, concept.Name));
        return await client.SendAsync(CkanActionNames.DatastoreSearch, infoRequest);
    }

    private async Task<XCkanResponse> GetTable(string tableId) {
        XDatastoreRequest infoRequest 
            = RequestBuilder.BuildGetTable(tableId);
        return await client.SendAsync(CkanActionNames.DatastoreSearch, infoRequest);
    }
    
    public async Task<XCkanResponse> UpsertTable(string resourceId, Dataset dataset, Concept concept)
    {
        XDatastoreResource request = reqBuilder.BuildUpsertEmptyTable(resourceId, dataset, concept);

        return await client.SendAsync(CkanActionNames.DatastoreCreate, request);
    }
    
    public async Task<XCkanResponse> DeleteTable(string resourceId)
    {
        XDatastoreRequest request = RequestBuilder.BuildDeleteTable(resourceId);
        return await client.SendAsync(CkanActionNames.DatastoreDelete, request);
    }

    private async Task<XCkanResponse> UpsertIntoTable(string resourceId,
        List<DataItem> dataItems) {

        XDatastoreRequest request = reqBuilder.BuildUpsertRecords(resourceId, dataItems);
        return await client.SendAsync(CkanActionNames.DatastoreUpsert, request);
    }
    
    public async Task<XDatastorePublishResult> UpsertIntoTableInBatches(string resourceId, List<DataItem> dataItems,
        Concept concept, int batchSize = 1000) {
        
        // create details for return objects so resource id can be later extracted from result
        var details = new Dictionary<string, string>() { { "resource_id", resourceId } };
        JsonElement detailsElement = JsonSerializer.SerializeToElement(details);
        
        if (dataItems.Count == 0) {
            return new XDatastorePublishResult {
                ConceptUri = concept.Uri!,
                IsSuccess = true,
                RowsUpserted = 0,
                StatusCode = HttpStatusCode.NoContent,
                Details = detailsElement
            };
        }

        int totalUpserted = 0;
        
        try {
            for (var offset = 0; offset < dataItems.Count; offset += batchSize)
            {
                var batch = dataItems
                    .Skip(offset)
                    .Take(batchSize)
                    .ToList();
                
                XCkanResponse result = await UpsertIntoTable(resourceId, batch);

                if (!result.IsSuccess) {
                    return XDatastorePublishResult.FromXCkanResponse(concept.Uri!, totalUpserted, result);
                }

                totalUpserted += batch.Count;
                
            }

            return new XDatastorePublishResult {
                IsSuccess = true,
                ConceptUri = concept.Uri!,
                RowsUpserted = totalUpserted,
                StatusCode = HttpStatusCode.OK,
                Details = detailsElement
            };
        } catch (Exception e) {
            return new XDatastorePublishResult {
                IsSuccess = false,
                ConceptUri = concept.Uri!,
                ErrorMessage = e.Message,
                RowsUpserted = totalUpserted,
                StatusCode = HttpStatusCode.InternalServerError,
                Details = detailsElement
            };
        }
    }
}