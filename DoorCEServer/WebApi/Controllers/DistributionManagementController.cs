using DoorCEServer.Application.CataloguingManager.Dtos;
using DoorCEServer.Application.CataloguingManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

/// <summary>
/// The interface for the management of dataset Distribution metadata. Allows for
/// CRUD operations on Distributions and Data Services.
/// </summary>
[ApiController]
[Route("distributions")]
public class DistributionManagementController(DistributionManagementAPI api) : AuthControllerBase
{
    /// <summary>
    /// Get the distributions inside a specific dataset (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the dataset</param>
    /// <returns>Collection of XDistributions (200), not found if wrong URI (404)</returns>
    [HttpGet("GetDatasetDistributionList")]
    public IEnumerable<XDistribution> GetDatasetDistributionList([FromQuery] string identifier)
    {
        Log.Debug("GetDatasetDistributionList called for {Identifier}", identifier);
        return api.GetDatasetDistributionList(identifier, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get a specific Distribution (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the distribution.</param>
    /// <returns>XDistribution (200) or not found (404)</returns>
    [HttpGet("GetDistribution")]
    public XDistribution GetDistribution([FromQuery] string identifier)
    {
        Log.Debug("GetDistribution called for {Identifier}", identifier);
        return api.GetDistribution(identifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Check if a specific Distribution id (Uri) is unique
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns>"true" if unique, "false" if not unique</returns>
    [HttpGet("CheckDistributionId")]
    public bool CheckDistributionId([FromQuery] string identifier)
    {
        Log.Debug("CheckDistributionId called for {Identifier}", identifier);
        return api.CheckDistributionId(identifier);
    }

    /// <summary>
    /// Insert a Distribution. If the Distribution already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="distribution">Distribution to be added or updated.</param>
    /// <returns>URI of the added or updated resource (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertDistribution")]
    public string UpsertDistribution([FromBody] XDistribution distribution)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertDistribution called for {Identifier} by {Id}", distribution.Uri, userId);
        return api.UpsertDistribution(distribution, userId);
    }

    /// <summary>
    /// Delete the specified Distribution (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the distribution.</param>
    /// <returns>deleted (200), or not found or bad request (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteDistribution")]
    public void DeleteDistribution([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteDistribution called for {Identifier} by {Id}", identifier, userId);
        api.DeleteDistribution(identifier, userId);
    }

    /// <summary>
    /// Get a specific DataService (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the data service.</param>
    /// <returns>XDataService (200) or not found (404)</returns>
    [HttpGet("GetDataService")]
    public XDataService GetDataService([FromQuery] string identifier)
    {
        Log.Debug("GetDataService called for {Identifier}", identifier);
        return api.GetDataService(identifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Check if a specific Data Service id (Uri) is unique
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns>"true" if unique, "false" if not unique</returns>
    [HttpGet("CheckDataServiceId")]
    public bool CheckDataServiceId([FromQuery] string identifier)
    {
        Log.Debug("CheckDataServiceId called for {Identifier}", identifier);
        return api.CheckDataServiceId(identifier);
    }

    /// <summary>
    /// Insert a DataService. If the DataService already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="dataService">Data service to be added or updated with optional new contacts to be added.</param>
    /// <returns>URI of the added or updated resource (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertDataService")]
    public string UpsertDataService([FromBody] XDataServiceWithContacts dataService)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertDataService called for {Identifier} by {Id}", dataService.DataService.Uri, userId);
        return api.UpsertDataService(dataService.DataService, dataService.NewContacts, userId);
    }

    /// <summary>
    /// Delete the specified DataService (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the data service.</param>
    /// <returns>deleted (200), or not found or bad request (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteDataService")]
    public void DeleteDataService([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteDataService called for {Identifier} by {Id}", identifier, userId);
        api.DeleteDataService(identifier, userId);
    }

    /// <summary>
    /// Get all the Distributions that are part of a specific DataService (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the data service</param>
    /// <returns>Collection of XDistributions (200), or bad request (400)</returns>
    [HttpGet("GetServiceDistributionList")]
    public IEnumerable<XDistribution> GetServiceDistributionList([FromQuery] string identifier)
    {
        Log.Debug("GetServiceDistributionList called for {Identifier}", identifier);
        return api.GetServiceDistributionList(identifier, GetCurrentUserId(false));
    }
}