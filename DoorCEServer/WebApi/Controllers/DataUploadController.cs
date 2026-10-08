using System.Text.Json;
using DoorCEServer.Application.DataContentsManager.Dtos;
using DoorCEServer.Application.DataContentsManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

[ApiController]
[Route("upload")]
public class DataUploadController(DataUploadAPI api) : AuthControllerBase
{
    [Authorize]
    [HttpPost("UploadData")]
    public XFileValidationResult UploadData([FromForm] string request, IFormFile? file=null)
    {
        var requestObj = JsonSerializer.Deserialize<XDataUploadRequest>(request);
        if (requestObj == null) throw new ArgumentException("Invalid request");
        string userId = GetCurrentUserId()!;
        Log.Information("UploadData called for dataset: {DatasetUri} by {Id}, file: {FileName}",
            requestObj.DatasetUri, userId, file?.FileName ?? "none");
        return api.UploadData(requestObj, userId, file);
    }

    [Authorize]
    [HttpDelete("ClearDataItemsByDatasetUri")]
    public void ClearDataItemsByDatasetUri([FromQuery] string datasetUri)
    {
        Log.Debug("ClearDataItemsByDatasetUri called for {DatasetUri}", datasetUri);
        api.ClearDataItemsByDatasetUri(datasetUri, GetCurrentUserId()!);
    }

    [HttpGet("GetDataItemCountsByConcept")]
    public Dictionary<string, int> GetDataItemCountsByConcept([FromQuery] string datasetUri)
    {
        Log.Debug("GetDataItemCountsByConcept called for {DatasetUri}", datasetUri);
        return api.GetDataItemCountsByConcept(datasetUri);
    }
}