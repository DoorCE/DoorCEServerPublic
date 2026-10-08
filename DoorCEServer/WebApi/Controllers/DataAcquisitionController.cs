using DoorCEServer.Application.DataContentsManager.Dtos;
using DoorCEServer.Application.DataContentsManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

/// <summary>
/// The interface dedicated to be used by the Acquisition Apps for the management
/// of structured data within Datasets. It offers CRUD operations on source
/// (working copies of) Dataset Items and publishing these working copies into
/// active ones.
/// </summary>
[ApiController]
[Route("acquisition")]
public class DataAcquisitionController(DataAcquisitionAPI api) : AuthControllerBase
{
    /// <summary>
    /// Retrieves a list of Dataset Items from the specified Dataset. If concept URI is not provided,
    /// items associated with the main concept of the associated Data Schema are returned. If criteria are provided,
    /// they are applied to filter the items.
    /// </summary>
    /// <param name="datasetUri">URI identifier of the dataset</param>
    /// <param name="conceptUri">Optional URI identifier of the concept</param>
    /// <param name="criteria">Criteria for filtering items on the list</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("GetDatasetItemList")]
    public IActionResult GetDatasetItemList([FromQuery] string datasetUri, [FromQuery] string? conceptUri, [FromQuery] XDataFilterParams? criteria)
    {
        Log.Debug("GetDatasetItemList called for {DatasetUri}, concept: {ConceptUri}", datasetUri, conceptUri);
        var items = api.GetDatasetItemList(datasetUri, conceptUri, criteria, GetCurrentUserId()!);
        return Ok(items);
    }

    /// <summary>
    /// Retrieves a DatasetItem with the given identifier from the given dataset and concept. If concept URI is not provided,
    /// the item is retrieved from the main concept.
    /// </summary>
    /// <param name="identifier">Identifier of the Dataset Item</param>
    /// <param name="datasetUri">URI identifier of the dataset</param>
    /// <param name="conceptUri">Optional identifier of the concept</param>
    /// <returns></returns>
    [Authorize]
    [HttpGet("GetDataItem")]
    public IActionResult GetDataItem([FromQuery] string identifier, [FromQuery] string datasetUri, [FromQuery] string? conceptUri)
    {
        Log.Debug("GetDataItem called for {Identifier}, dataset: {DatasetUri}, concept: {ConceptUri}", identifier, datasetUri, conceptUri);
        var item = api.GetDataItem(identifier, datasetUri, conceptUri, GetCurrentUserId()!);
        return Ok(item);
    }

    /// <summary>
    /// Insert a Dataset Item. If the item with the same identifier already exists, it will be updated
    /// (all fields will be overwritten). 
    /// </summary>
    /// <param name="xDataItem">Data Item to be inserted or updated.</param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("UpsertDataItem")]
    public IActionResult UpsertDataItem([FromBody] XDataItem xDataItem)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertDataItem called for {Identifier} by {Id}", xDataItem.Identifier, userId);
        api.UpsertDataItem(xDataItem, userId);
        return Ok();
    }

    /// <summary>
    /// Delete a specific Dataset Item with the given identifier from the given dataset and concept. If concept URI is not provided,
    /// the item is deleted from the main concept.
    /// </summary>
    /// <param name="identifier">Identifier of the Dataset Item</param>
    /// <param name="datasetUri">URI identifier of the dataset</param>
    /// <param name="conceptUri">Optional identifier of the concept</param>
    /// <returns></returns>
    [Authorize]
    [HttpDelete("DeleteDataItem")]
    public IActionResult DeleteDataItem([FromQuery] string identifier, [FromQuery] string datasetUri, [FromQuery] string? conceptUri)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteDataItem called for {Identifier} by {Id}, dataset: {DatasetUri}, concept: {ConceptUri}",
            identifier, userId, datasetUri, conceptUri);
        api.DeleteDataItem(identifier, datasetUri, conceptUri, userId);
        return Ok();
    }
    
    /// <summary>
    /// Check validity of a data item
    /// </summary>
    /// <param name="xDataItem">The data item to be checked</param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("CheckDataItem")]
    public IActionResult CheckDataItem([FromBody] XDataItem xDataItem)
    {
        Log.Debug("CheckDataItem called for {Identifier}", xDataItem.Identifier);
        int checkResult = api.CheckDataItem(xDataItem, GetCurrentUserId()!);
        return Ok(checkResult);
    }
    
    [Authorize]
    [HttpGet("CheckExistingDataItem")]
    public IActionResult CheckExistingDataItem([FromQuery] string identifier, [FromQuery] string datasetUri, [FromQuery] string? conceptUri)
    {
        Log.Debug("CheckExistingDataItem called for {Identifier}, dataset: {DatasetUri}, concept: {ConceptUri}",
            identifier, datasetUri, conceptUri);
        int checkResult = api.CheckExistingDataItem(identifier, datasetUri, conceptUri, GetCurrentUserId()!);
        return Ok(checkResult);
    }

    /// <summary>
    /// Transfers data from the given source dataset to the related active dataset 
    /// </summary>
    /// <param name="datasetUri">URI identifier of the source dataset</param>
    /// <returns></returns>
    [Authorize]
    [HttpPost("SubmitSourceDatasetContents")]
    public IActionResult SubmitSourceDatasetContents([FromQuery] string datasetUri)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("SubmitSourceDatasetContents called for {DatasetUri} by {Id}", datasetUri, userId);
        api.SubmitSourceDatasetContents(datasetUri, userId);
        return Ok();
    }
}