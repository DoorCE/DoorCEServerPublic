using DoorCEServer.Application.CataloguingManager.Dtos;

namespace DoorCEServer.Application.CataloguingManager.Interfaces;

/// <summary>
/// Descriptions can be found in the WebApi/Controllers/DistributionManagementController.cs
/// </summary>
public interface DistributionManagementAPI
{
    IEnumerable<XDistribution> GetDatasetDistributionList(string identifier, string? userId);
    XDistribution GetDistribution(string identifier, string? userId);
    bool CheckDistributionId(string identifier);
    string UpsertDistribution(XDistribution xDistribution, string userId);
    void DeleteDistribution(string identifier, string userId);
    XDataService GetDataService(string identifier, string? userId);
    bool CheckDataServiceId(string identifier);
    string UpsertDataService(XDataService dataService, IEnumerable<XContactData> newContacts, string userId);
    void DeleteDataService(string identifier, string userId);
    IEnumerable<XDistribution> GetServiceDistributionList(string identifier, string? userId);
}