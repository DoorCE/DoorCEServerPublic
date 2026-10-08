using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using DoorCEServer.Application.CkanProxy.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

/// <summary>
/// The interface for the management of Dataset metadata. Allows for CRUD
/// operations on Catalogues, Dataset Series and Datasets. It also allows for
/// assigning Dataset Schemas to Datasets.
/// </summary>
[ApiController]
[Route("datasets")]
public class DatasetManagementController(DatasetManagementAPI api): AuthControllerBase
{
    /// <summary>
    /// Get catalogues allowed to be set as parent for a specific data resource
    /// </summary>
    /// <param name="resourceId">URI identifier of the resource</param>
    /// <returns>Collection of XCatalogueElements (200) or incorrect argument (400)</returns>
    [Authorize]
    [HttpGet("GetAllowedCatalogues")]
    public IEnumerable<XCatalogueElement> GetAllowedCatalogues([FromQuery] string? resourceId)
    {
        Log.Debug("GetAllowedCatalogues called for {ResourceId}", resourceId);
        return api.GetAllowedCatalogues(resourceId, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Get dataset series allowed to be set as series for a specific dataset
    /// </summary>
    /// <param name="datasetId">URI identifier of the dataset</param>
    /// <returns>Collection of XCatalogueElements (200) or incorrect argument (400)</returns>
    [Authorize]
    [HttpGet("GetAllowedSeries")]
    public IEnumerable<XCatalogueElement> GetAllowedSeries([FromQuery] string? datasetId)
    {
        Log.Debug("GetAllowedSeries called for {DatasetId}", datasetId);
        return api.GetAllowedSeries(datasetId, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Get datasets allowed to be set as a parent for a specific distribution
    /// </summary>
    /// <param name="distributionId">URI identifier of the distribution</param>
    /// <returns>Collection of XCatalogueElements (200) or incorrect argument (400)</returns>
    [Authorize]
    [HttpGet("GetAllowedDatasets")]
    public IEnumerable<XCatalogueElement> GetAllowedDatasets([FromQuery] string? distributionId)
    {
        Log.Debug("GetAllowedDatasets called for {DistributionId}", distributionId);
        return api.GetAllowedDatasets(distributionId, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Get data services allowed to be set as a service for a specific dataset
    /// </summary>
    /// <param name="distributionId">URI identifier of the distribution</param>
    /// <returns>Collection of XCatalogueElements (200) or incorrect argument (400)</returns>
    [Authorize]
    [HttpGet("GetAllowedServices")]
    public IEnumerable<XCatalogueElement> GetAllowedServices([FromQuery] string? distributionId)
    {
        Log.Debug("GetAllowedServices called for {DistributionId}", distributionId);
        return api.GetAllowedServices(distributionId, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Get datasets declared as consistent with a given schema
    /// </summary>
    /// <param name="schemaUri">URI identifier of the schema</param>
    /// <param name="onlySourceDatasets">If true, only datasets that have status "Source" are returned.</param>
    /// <returns>Collection of XCatalogueElements (200) or incorrect argument (400)</returns>
    [HttpGet("GetSchemaDatasets")]
    public IEnumerable<XCatalogueElement> GetSchemaDatasets([FromQuery] string? schemaUri,
        bool onlySourceDatasets = false)
    {
        Log.Debug("GetSchemaDatasets called for {SchemaUri}, onlySource: {OnlySource}", schemaUri, onlySourceDatasets);
        return api.GetSchemaDatasets(schemaUri, onlySourceDatasets, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Get the resources (datasets and dataset series) managed by the current user
    /// </summary>
    /// <returns>Collection of XCatalogueElements (200)</returns>
    [Authorize]
    [HttpGet("GetManagedDataResources")]
    public XDataResources GetManagedDataResources()
    {
        Log.Debug("GetManagedDataResources called");
        return api.GetManagedDataResources(GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Get a specific Catalog (identified by URI) or the main user catalog (if uri==null)
    /// </summary>
    /// <param name="identifier">URI identifier of the catalogue or null.</param>
    /// <returns>XCatalogue (200) or not found (404)</returns>
    [HttpGet("GetCatalogue")]
    public XCatalogue GetCatalogue([FromQuery] string? identifier)
    {
        Log.Debug("GetCatalogue called for {Identifier}", identifier);
        string? userId = GetCurrentUserId(false);
        if (null == identifier) return api.GetMainCatalogue(userId);
        return api.GetCatalogue(identifier, userId);
    }
    
    /// <summary>
    /// Check if a specific Catalogue id (Uri) is unique
    /// </summary>
    /// <param name="identifier">Potential URI identifier of the catalogue.</param>
    /// <returns>"true" if unique, "false" if not unique</returns>
    [HttpGet("CheckCatalogueId")]
    public bool CheckCatalogueId([FromQuery] string identifier)
    {
        Log.Debug("CheckCatalogueId called for {Identifier}", identifier);
        return api.CheckCatalogueId(identifier);
    }

    /// <summary>
    /// Insert a Catalogue. If the Catalogue already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="catalogue">Catalogue to be added or updated with optional new contacts to be added.</param>
    /// <returns>URI of the added or updated resource (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertCatalogue")]
    public string UpsertCatalogue([FromBody] XCatalogueWithContacts catalogue)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertCatalogue called for {Identifier} by {Id}", catalogue.Catalogue.Uri, userId);
        return api.UpsertCatalogue(catalogue.Catalogue, catalogue.NewContacts, userId);
    }

    /// <summary>
    /// Delete a specific empty Catalogue (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the catalogue</param>
    /// <returns>deleted (200), or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteCatalogue")]
    public void DeleteCatalogue([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteCatalogue called for {Identifier} by {Id}", identifier, userId);
        api.DeleteCatalogue(identifier, userId);
    }

    /// <summary>
    /// Get the contents of a specific Catalog (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the catalogue</param>
    /// <returns>Collection of XCatalogueElements (200), not found if wrong URI (404)</returns>
    [HttpGet("GetCatalogueContents")]
    public IEnumerable<XCatalogueElement> GetCatalogueContents([FromQuery] string identifier)
    {
        Log.Debug("GetCatalogueContents called for {Identifier}", identifier);
        return api.GetCatalogueContents(identifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Get the catalogues inside a specific Catalog (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the catalogue</param>
    /// <returns>Collection of XCatalogueElements (200), not found if wrong URI (404)</returns>
    [HttpGet("GetCataloguesInCatalogue")]
    public IEnumerable<XCatalogueElement> GetCataloguesInCatalogue([FromQuery] string identifier)
    {
        Log.Debug("GetCataloguesInCatalogue called for {Identifier}", identifier);
        return api.GetCatalogueContents(identifier, GetCurrentUserId(false), XCatalogueElementType.Catalogue);
    }
    
    /// <summary>
    /// Get the datasets inside a specific Catalog (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the catalogue</param>
    /// <returns>Collection of XCatalogueElements (200), not found if wrong URI (404)</returns>
    [HttpGet("GetDatasetsInCatalogue")]
    public IEnumerable<XCatalogueElement> GetDatasetsInCatalogue([FromQuery] string identifier)
    {
        Log.Debug("GetDatasetsInCatalogue called for {Identifier}", identifier);
        return api.GetCatalogueContents(identifier, GetCurrentUserId(false), XCatalogueElementType.Dataset);
    }
    
    /// <summary>
    /// Get the dataset series inside a specific Catalog (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the catalogue</param>
    /// <returns>Collection of XCatalogueElements (200), not found if wrong URI (404)</returns>
    [HttpGet("GetSeriesInCatalogue")]
    public IEnumerable<XCatalogueElement> GetSeriesInCatalogue([FromQuery] string identifier)
    {
        Log.Debug("GetSeriesInCatalogue called for {Identifier}", identifier);
        return api.GetCatalogueContents(identifier, GetCurrentUserId(false), XCatalogueElementType.DatasetSeries);
    }
    
    /// <summary>
    /// Get the data services inside a specific Catalog (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the catalogue</param>
    /// <returns>Collection of XCatalogueElements (200), not found if wrong URI (404)</returns>
    [HttpGet("GetServicesInCatalogue")]
    public IEnumerable<XCatalogueElement> GetServicesInCatalogue([FromQuery] string identifier)
    {
        Log.Debug("GetServicesInCatalogue called for {Identifier}", identifier);
        return api.GetCatalogueContents(identifier, GetCurrentUserId(false), XCatalogueElementType.DataService);
    }
    
    /// <summary>
    /// Get a specific Dataset Series (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the series.</param>
    /// <returns>XDatasetSeries (200) or not found (404)</returns>
    [HttpGet("GetSeries")]
    public XDatasetSeries GetSeries([FromQuery] string identifier)
    {
        Log.Debug("GetSeries called for {Identifier}", identifier);
        return api.GetSeries(identifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Check if a specific Dataset Series id (Uri) is unique
    /// </summary>
    /// <param name="identifier">Potential URI identifier of the series.</param>
    /// <returns>"true" if unique, "false" if not unique</returns>
    [HttpGet("CheckSeriesId")]
    public bool CheckSeriesId([FromQuery] string identifier)
    {
        Log.Debug("CheckSeriesId called for {Identifier}", identifier);
        return api.CheckSeriesId(identifier);
    }

    /// <summary>
    /// Insert a Dataset Series. If the Series already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="datasetSeries">Dataset Series to be added or updated with optional new contacts to be added.</param>
    /// <returns>URI of the added or updated resource (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertSeries")]
    public string UpsertSeries([FromBody] XDatasetSeriesWithContacts datasetSeries)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertSeries called for {Identifier} by {Id}", datasetSeries.DatasetSeries.Uri, userId);
        return api.UpsertSeries(datasetSeries.DatasetSeries, datasetSeries.NewContacts, userId);
    }

    /// <summary>
    /// Delete a specific Dataset Series (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the series.</param>
    /// <returns>deleted (200), or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteSeries")]
    public void DeleteSeries([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteSeries called for {Identifier} by {Id}", identifier, userId);
        api.DeleteSeries(identifier, userId);
    }

    /// <summary>
    /// Get the contents of a specific Dataset Series (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the series</param>
    /// <returns>Collection of XCatalogueElements (200), or bad request (400)</returns>
    [HttpGet("GetSeriesDatasetList")]
    public IEnumerable<XCatalogueElement> GetSeriesDatasetList([FromQuery] string identifier)
    {
        Log.Debug("GetSeriesDatasetList called for {Identifier}", identifier);
        return api.GetSeriesDatasetList(identifier, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get a specific Dataset (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the dataset.</param>
    /// <returns>XDataset (200) or not found (404)</returns>
    [HttpGet("GetDataset")]
    public XDataset GetDataset([FromQuery] string identifier)
    {
        Log.Debug("GetDataset called for {Identifier}", identifier);
        return api.GetDataset(identifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Check if a specific Dataset id (Uri) is unique
    /// </summary>
    /// <param name="identifier">Potential URI identifier of the dataset.</param>
    /// <returns>"true" if unique, "false" if not unique</returns>
    [HttpGet("CheckDatasetId")]
    public bool CheckDatasetId([FromQuery] string identifier)
    {
        Log.Debug("CheckDatasetId called for {Identifier}", identifier);
        return api.CheckDatasetId(identifier);
    }

    /// <summary>
    /// Insert a Dataset. If the Dataset already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="xDataset">Dataset to be added or updated with optional new contacts to be added.</param>
    /// <param name="addApp">Optionally: add a default AcquisitionApp on creation of the dataset</param>
    /// <returns>URI of the added or updated resource (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertDataset")]
    public string UpsertDataset([FromBody] XDatasetWithContacts xDataset, [FromQuery] bool addApp = false)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertDataset called for {Identifier} by {Id}", xDataset.Dataset.Uri, userId);
        return api.UpsertDataset(xDataset.Dataset, xDataset.NewContacts, userId, addApp);
    }

    /// <summary>
    /// Delete the specified Dataset (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the dataset.</param>
    /// <param name="force">Force deletion even with distributions</param>
    /// <returns>deleted (200), or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteDataset")]
    public void DeleteDataset([FromQuery] string identifier, bool force = false)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteDataset called for {Identifier} by {Id}, force: {Force}",
            identifier, userId, force);
        api.DeleteDataset(identifier, userId, force);
    }

    /// <summary>
    /// Transfer the specified Dateset to CKAN and make it publicly accessible 
    /// </summary>
    /// <param name="identifier">URI identifier of the dataset.</param>
    /// <param name="withData">Specifies whether to upload also the data (default is false)</param>
    /// <returns>XCkanResponse, or bad request or unauthorised</returns>
    [Authorize]
    [HttpPost("PublishDataset")]
    public XCkanResponse PublishDataset([FromQuery] string identifier, 
        [FromQuery] bool withData=false)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("PublishDataset called for {Identifier} by {Id}, uploadToDatastore: {Upload}", 
            identifier, userId, withData);
        return api.PublishDataset(identifier, userId, withData);
    }
    
    /// <summary>
    /// Delete dataset from Ckan, along with all its resources and Datastore tables
    /// </summary>
    /// <param name="identifier">URI identifier of the dataset.</param>
    [Authorize]
    [HttpDelete("UnpublishDataset")]
    public XCkanResponse UnpublishDataset([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UnpublishDataset called for {Identifier} by {Id}", 
            identifier, userId);
        return api.UnpublishDataset(identifier, userId);
    }
}