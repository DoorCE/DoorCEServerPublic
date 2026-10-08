using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CkanProxy.Dtos;

namespace DoorCEServer.Application.CataloguingManager.Interfaces;

/// <summary>
/// Descriptions can be found in the WebApi/Controllers/DatasetManagementController.cs
/// </summary>
public interface DatasetManagementAPI
{
    IEnumerable<XCatalogueElement> GetAllowedCatalogues(string? resourceId, string userId);
    IEnumerable<XCatalogueElement> GetAllowedSeries(string? datasetId, string userId);
    IEnumerable<XCatalogueElement> GetAllowedDatasets(string? distributionId, string userId);
    IEnumerable<XCatalogueElement> GetAllowedServices(string? distributionId, string userId);
    IEnumerable<XCatalogueElement> GetSchemaDatasets(string? schemaUri, bool onlySourceDatasets, string? userId);
    XDataResources GetManagedDataResources(string userId);
    XCatalogue GetCatalogue(string identifier, string? userId);
    bool CheckCatalogueId(string identifier);
    string UpsertCatalogue(XCatalogue xCatalogue, IEnumerable<XContactData> newContacts, string userId);
    void DeleteCatalogue(string identifier, string userId);
    IEnumerable<XCatalogueElement> GetCatalogueContents(string identifier, string? userId, XCatalogueElementType? type = null);
    XCatalogue GetMainCatalogue(string? userId);
    XDatasetSeries GetSeries(string identifier, string? userId);
    bool CheckSeriesId(string identifier);
    string UpsertSeries(XDatasetSeries xSeries, IEnumerable<XContactData> newContacts, string userId);
    void DeleteSeries(string identifier, string userId);
    IEnumerable<XCatalogueElement> GetSeriesDatasetList(string identifier, string? userId);
    XDataset GetDataset(string identifier, string? userId);
    bool CheckDatasetId(string identifier);
    string UpsertDataset(XDataset xDataset, IEnumerable<XContactData> newContacts, string userId, bool addApp = false);
    void DeleteDataset(string identifier, string userId, bool force = false);
    XCkanResponse PublishDataset(string identifier, string userId, bool withData = false,
        bool createEmptyDatastore = false);
    XCkanResponse UnpublishDataset(string identifier, string userId);
}