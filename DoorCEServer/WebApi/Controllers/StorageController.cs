using DoorCEServer.Application.StorageManager.Dtos;
using DoorCEServer.Application.StorageManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

[ApiController]
[Route("storage")]
public class StorageController(StorageAPI storageApi) : AuthControllerBase
{
    /// <summary>
    /// Insert a file. If the file already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="fileId">ID of the file.</param>
    /// <param name="fileName">User-defined name of the file.</param>
    /// <param name="file">File to be added or updated.</param>
    /// <returns>ID of the added or updated file (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertFile")]
    public int UpsertFile([FromForm] int? fileId, [FromForm] string fileName, IFormFile? file)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertFile called for {FileName} (id: {FileId}) by {Id}", fileName, fileId, userId);
        return storageApi.UpsertFile(new XFileData() { FileId = fileId, FileName = fileName }, file, userId);
    }
    
    /// <summary>
    /// Get a specific file (identified by ID).
    /// </summary>
    /// <param name="identifier">ID of the file.</param>
    /// <returns>FileContentResult (200) or not found (404)</returns>
    [HttpGet("GetFile")]
    public FileContentResult GetFile([FromQuery] int identifier)
    {
        Log.Debug("GetFile called for {Identifier}", identifier);
        return storageApi.GetFile(identifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Delete a specific file (identified by ID)
    /// </summary>
    /// <param name="identifier">ID of the file.</param>
    /// <returns>Acknowledge (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteFile")]
    public void DeleteFile([FromQuery] int identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteFile called for {Identifier} by {Id}", identifier, userId);
        storageApi.DeleteFile(identifier, userId);
    }

    /// <summary>
    /// Insert an Icon. If the Icon already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="iconName">User-defined name of the icon.</param>
    /// <param name="iconUri">Uri of the icon.</param>
    /// <param name="file"> Icon to be added or updated</param>
    /// <returns>URI of the added or updated icon (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertIcon")]
    public string UpsertIcon([FromForm] string iconName, [FromForm] string iconUri, IFormFile? file)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertIcon called for {IconUri} by {Id}", iconUri, userId);
        return storageApi.UpsertIcon(new XIconData() { IconName = iconName, IconUri = iconUri }, file, userId);
    }
    
    /// <summary>
    /// Get a list of files that have a name similar to the query.
    /// </summary>
    /// <param name="query">Fragment of the file name (at least 3 characters) or empty</param>
    /// <returns>Collection of XIconData (200)</returns>
    [Authorize]
    [HttpGet("GetIconList")]
    public IEnumerable<XIconData> GetIconList([FromQuery] string? query)
    {
        Log.Debug("GetIconList called with query: {Query}", query);
        return storageApi.GetIconList(query);
    }
    
    /// <summary>
    /// Get a specific Icon (identified by URI).
    /// </summary>
    /// <param name="identifier">URI identifier of the icon.</param>
    /// <returns>FileContentResult (200) or not found (404)</returns>
    [HttpGet("GetIcon")]
    public FileContentResult GetIcon([FromQuery] string identifier)
    {
        Log.Debug("GetIcon called for {Identifier}", identifier);
        return storageApi.GetIcon(identifier);
    }
    
    /// <summary>
    /// Delete a specific Icon (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the icon.</param>
    /// <returns>Acknowledge (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteIcon")]
    public void DeleteIcon([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteIcon called for {Identifier} by {Id}", identifier, userId);
        storageApi.DeleteIcon(identifier, userId);
    }
}