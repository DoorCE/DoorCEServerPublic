using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

/// <summary>
/// The interface for the management of DataSchema metadata. Allows for CRUD
/// operations on Data schemas and Data schema series.
/// </summary>
[ApiController]
[Route("schemas")]
public class SchemaController(SchemaAPI api) : AuthControllerBase
{
    /// <summary>
    /// Returns a list of schema series available to the current user.
    /// </summary>
    /// <param name="schemaId">Optional schema identifier for filtering results.</param>
    /// <returns>Collection of XSchemaSeries (200) or incorrect argument (400)</returns>
    [Authorize]
    [HttpGet("GetAllowedSchemaSeries")]
    public IEnumerable<XSchemaSeries> GetAllowedSchemaSeries([FromQuery] string? schemaId)
    {
        Log.Debug("GetAllowedSchemaSeries called for {SchemaId}", schemaId);
        return api.GetAllowedSchemaSeries(schemaId, GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Get a specific Data Schema Series (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the series.</param>
    /// <returns>XSchemaSeries (200) or not found (404)</returns>
    [HttpGet("GetSchemaSeries")]
    public XSchemaSeries GetSchemaSeries([FromQuery] string identifier)
    {
        Log.Debug("GetSchemaSeries called for {Identifier}", identifier);
        return api.GetSchemaSeries(identifier, GetCurrentUserId(false));
    }

    /// <summary>
    /// Get a list of Schema Series that have a title similar to the query.
    /// </summary>
    /// <param name="query">Fragment of the series title (at least 3 characters)</param>
    /// <returns>Collection of XSchemaSeries (200)</returns>
    [HttpGet("GetSchemaSeriesList")]
    public IEnumerable<XSchemaSeries> GetSchemaSeriesList([FromQuery] string? query)
    {
        Log.Debug("GetSchemaSeriesList called with query: {Query}", query);
        return api.GetSchemaSeriesList(query, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Insert a Data Schema Series. If the Series already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="xSchemaSeries">Data Schema Series to be added or updated.</param>
    /// <returns>URI of the series (200), or bad request or not authorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertSchemaSeries")]
    public string UpsertSchemaSeries([FromBody] XSchemaSeries xSchemaSeries)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertSchemaSeries called for {Identifier} by {Id}", xSchemaSeries.Uri, userId);
        return api.UpsertSchemaSeries(xSchemaSeries, userId);
    }
    
    /// <summary>
    /// Delete a specific Data Schema Series (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the series.</param>
    /// <returns>deleted (200), or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteSchemaSeries")]
    public void DeleteSchemaSeries([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteSchemaSeries called for {Identifier} by {Id}", identifier, userId);
        api.DeleteSchemaSeries(identifier, userId);
    }
    
    /// <summary>
    /// Check if a specific Schema Series id (Uri) is unique
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns>"true" if unique, "false" if not unique</returns>
    [HttpGet("CheckSchemaSeriesId")]
    public bool CheckSchemaSeriesId(string identifier)
    {
        Log.Debug("CheckSchemaSeriesId called for {Identifier}", identifier);
        return api.CheckSchemaSeriesId(identifier);
    }

    /// <summary>
    /// Get a list of Data Schemas that have a title similar to the query.
    /// </summary>
    /// <param name="query">Fragment of the schema title (at least 3 characters)</param>
    /// <returns>Collection of XDataSchemas (200)</returns>
    [HttpGet("GetDataSchemaList")]
    public IEnumerable<XDataSchema> GetDataSchemaList([FromQuery] string? query)
    {
        Log.Debug("GetDataSchemaList called with query: {Query}", query);
        return api.GetDataSchemaList(query, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Get the contents of a specific Data Schema Series (identified by URI)
    /// </summary>
    /// <param name="seriesIdentifier">URI identifier of the series</param>
    /// <returns>Collection of XDataSchemas (200), or bad request (400)</returns>
    [HttpGet ("GetSeriesDataSchemaList")]
    public IEnumerable<XDataSchema> GetSeriesDataSchemaList([FromQuery] string seriesIdentifier)
    {
        Log.Debug("GetSeriesDataSchemaList called for {SeriesIdentifier}", seriesIdentifier);
        return api.GetSeriesDataSchemaList(seriesIdentifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Get a specific Data Schema (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the data schema.</param>
    /// <returns>XDataSchema (200) or not found (404)</returns>
    [HttpGet("GetDataSchema")]
    public XDataSchema GetDataSchema([FromQuery] string identifier)
    {
        Log.Debug("GetDataSchema called for {Identifier}", identifier);
        return api.GetDataSchema(identifier, GetCurrentUserId(false));
    }
    
    /// <summary>
    /// Insert a Data Schema. If the Schema already exists, it will be updated (all fields will be overwritten). 
    /// </summary>
    /// <param name="xDataSchema">Data Schema to be added or updated.</param>
    /// <returns>URI of the schema (200), or bad request or not authorised (40X)</returns>
    [Authorize]
    [HttpPost("UpsertDataSchema")]
    public string UpsertDataSchema([FromBody] XDataSchema xDataSchema)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertDataSchema called for {Identifier} by {Id}", xDataSchema.Uri, userId);
        return api.UpsertDataSchema(xDataSchema, userId);
    }
    
    /// <summary>
    /// Delete a specific Data Schema (identified by URI)
    /// </summary>
    /// <param name="identifier">URI identifier of the schema.</param>
    /// <returns>deleted (200), or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete("DeleteDataSchema")]
    public void DeleteDataSchema([FromQuery] string identifier)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteDataSchema called for {Identifier} by {Id}", identifier, userId);
        api.DeleteDataSchema(identifier, userId);
    }
    
    /// <summary>
    /// Check if a specific Schema id (Uri) is unique
    /// </summary>
    /// <param name="identifier"></param>
    /// <returns>"true" if unique, "false" if not unique</returns>
    [HttpGet("CheckSchemaId")]
    public bool CheckSchemaId(string identifier)
    {
        Log.Debug("CheckSchemaId called for {Identifier}", identifier);
        return api.CheckSchemaId(identifier);
    }

    /// <summary>
    /// Get a list of standard namespaces available in the system.
    /// </summary>
    /// <returns>Collection of XNamespaces (200)</returns>
    [HttpGet("GetStandardNamespaceList")]
    public IEnumerable<XNamespace> GetStandardNamespaceList()
    {
        Log.Debug("GetStandardNamespaceList called");
        return api.GetStandardNamespaceList();
    }
}