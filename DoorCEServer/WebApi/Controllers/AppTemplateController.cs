using DoorCEServer.Application.DataTemplateManager.Dtos;
using DoorCEServer.Application.DataTemplateManager.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEServer.WebApi.Controllers;

/// <summary>
/// The interface for the management of AppTemplate and AcquisitionApp metadata.
/// Allows for CRUD operations on App templates and Acquisition apps.
/// </summary>
[ApiController]
[Route("template")]
public class AppTemplateController(AppTemplateAPI api) : AuthControllerBase
{
    /// <summary>
    /// Get the list of Application Templates available to the user.
    /// </summary>
    /// <returns>Collection of XAppTemplates (200), or not found (404)</returns>
    [Authorize]
    [HttpGet ("GetAppTemplateList")]
    public IEnumerable<XAppTemplate> GetAppTemplateList()
    {
        Log.Debug("GetAppTemplateList called");
        return api.GetAppTemplateList(GetCurrentUserId()!);
    }
    
    /// <summary>
    /// Get a specific Application Template by identifier.
    /// </summary >
    /// <param name="identifier">Identifier of the application template.</param>
    /// <returns>XAppTemplate (200), or not found (404)</returns>
    [Authorize]
    [HttpGet ("GetAppTemplate")]
    public XAppTemplate GetAppTemplate(string identifier)
    {
        Log.Debug("GetAppTemplate called for {Identifier}", identifier);
        //return api.GetAppTemplate(identifier, "udas-admin");
        return api.GetAppTemplate(identifier, GetCurrentUserId()!);
    }

    /// <summary>
    /// Create or update an Application Template.
    /// </summary>
    /// <param name="xTemplate">XAppTemplate to create or update.</param>
    /// <returns>Identifier of the created or updated Application Template (200)</returns>
    [Authorize]
    [HttpPost ("UpsertAppTemplate")]
    public string UpsertAppTemplate(XAppTemplate xTemplate)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertAppTemplate called for {Identifier} by {Id}", xTemplate.Uri, userId);
        return api.UpsertAppTemplate(xTemplate, userId);
    }

    /// <summary>
    /// Delete an Application Template.
    /// </summary>
    /// <param name="templateUri">Identifier of the Application Template to delete.</param>
    /// <returns>deleted (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete ("DeleteAppTemplate")]
    public void DeleteAppTemplate(string templateUri)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteAppTemplate called for {TemplateUri} by {Id}", templateUri, userId);
        api.DeleteAppTemplate(templateUri, userId);
    }

    /// <summary>
    /// Check if an Application Template identifier is already in use.
    /// </summary>
    /// <param name="identifier">Identifier to check.</param>
    /// <returns>True if the identifier is in use (200), false otherwise (200)</returns>
    [HttpGet ("CheckAppTemplateId")]
    public bool CheckAppTemplateId(string identifier)
    {
        Log.Debug("CheckAppTemplateId called for {Identifier}", identifier);
        return api.CheckAppTemplateId(identifier);
    }

    /// <summary>
    ///  Generate App Template code
    /// </summary>
    /// <param name="templateUri">Identifier of the Application Template to generate</param>
    /// <returns>Identifier of the generated code package (200)</returns>
    [Authorize]
    [HttpPost ("GenerateAppTemplate")]
    public string GenerateAppTemplate(string templateUri)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("GenerateAppTemplate called for {TemplateUri} by {Id}", templateUri, userId);
        return api.GenerateAppTemplate(templateUri, userId);
    }
    
    /// <summary>
    /// Get the list of Applications available to be run by the user.
    /// </summary>
    /// <returns>Collection of XAcquisitionApps (200), or not found (404)</returns>
    [Authorize]
    [HttpGet ("GetPlatformAppList")]
    public IEnumerable<XAcquisitionApp> GetPlatformAppList()
    {
        Log.Debug("GetPlatformAppList called");
        return api.GetAppList(GetCurrentUserId()!,true);
    }
    
    /// <summary>
    /// Get the list of Applications editable by the user.
    /// </summary>
    /// <returns>Collection of XAcquisitionApps (200), or not found (404)</returns>
    [Authorize]
    [HttpGet ("GetAppList")]
    public IEnumerable<XAcquisitionApp> GetAppList()
    {
        Log.Debug("GetAppList called");
        return api.GetAppList(GetCurrentUserId()!, false);
    }
    
    /// <summary>
    /// Get a specific Application available to be run by the user.
    /// </summary>
    /// <param name="identifier">Identifier of the application.</param>
    /// <returns>XAcquisitionApp (200), or not found (404)</returns>
    [Authorize]
    [HttpGet ("GetPlatformApp")]
    public XAcquisitionApp GetPlatformApp(string identifier)
    {
        Log.Debug("GetPlatformApp called for {Identifier}", identifier);
        return api.GetApp(identifier, GetCurrentUserId()!, true);
    }
    
    /// <summary>
    /// Get a specific Application editable by the user.
    /// </summary>
    /// <param name="identifier">Identifier of the application.</param>
    /// <returns>XAcquisitionApp (200), or not found (404)</returns>
    [Authorize]
    [HttpGet ("GetApp")]
    public XAcquisitionApp GetApp(string identifier)
    {
        Log.Debug("GetApp called for {Identifier}", identifier);
        return api.GetApp(identifier, GetCurrentUserId()!, false);
    }

    /// <summary>
    /// Create or update an Application.
    /// </summary>
    /// <param name="xApp">XAppCreationRequest to create or update.</param>
    /// <returns>Identifier of the created or updated Application (200)</returns>
    [Authorize]
    [HttpPost ("UpsertApp")]
    public string UpsertApp(XAppCreationRequest xApp)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("UpsertApp called for {Identifier} by {Id}", xApp.Uri, userId);
        return api.UpsertApp(xApp, userId);
    }

    /// <summary>
    /// Delete an Application.
    /// </summary>
    /// <param name="appUri">Identifier of the Application to delete.</param>
    /// <returns>deleted (200) or bad request or unauthorised (40X)</returns>
    [Authorize]
    [HttpDelete ("DeleteApp")]
    public void DeleteApp(string appUri)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeleteApp called for {AppUri} by {Id}", appUri, userId);
        api.DeleteApp(appUri, userId);
    }

    /// <summary>
    /// Check if an Application Template identifier is already in use.
    /// </summary>
    /// <param name="identifier">Identifier to check.</param>
    /// <returns>True if the identifier is in use (200), false otherwise (200)</returns>
    [HttpGet ("CheckAppId")]
    public bool CheckAppId(string identifier)
    {
        Log.Debug("CheckAppId called for {Identifier}", identifier);
        return api.CheckAppId(identifier);
    }

    /// <summary>
    /// Deploy an Application to the App Platform
    /// </summary>
    /// <param name="appUri">Identifier of the Application to deploy</param>
    [Authorize]
    [HttpPost ("DeployApp")]
    public void DeployApp(string appUri)
    {
        string userId = GetCurrentUserId()!;
        Log.Information("DeployApp called for {AppUri} by {Id}", appUri, userId);
        api.DeployApp(appUri, userId);
    }

    /// <summary>
    /// Returns a default app template for acquiring data conforming to a specified schema.
    /// </summary>
    /// <param name="schemaId">URI of the schema for which the default template is fetched.</param>
    /// <param name="conceptName">Optional name of the concept for which the default template is fetched.</param>
    /// <param name="language">Optional language code for the template (en, pl, it, sv, sk, hr, de, etc.)</param>
    /// <returns> Default template (200) or invalid argument (400)</returns>
    [HttpGet("GetDefaultAppTemplate")]
    public XDefaultAppTemplate GetDefaultAppTemplate(string schemaId, string? conceptName, string? language)
    {
        Log.Debug("GetDefaultAppTemplate called for {SchemaId}, concept: {ConceptName}, language: {Language}",
            schemaId, conceptName, language);
        return api.GetDefaultAppTemplate(schemaId, conceptName, language);
    }

    /// <summary>
    /// Get the list of Applications available to be run by the user for a specific dataset.
    /// </summary>
    /// <param name="datasetUri">Uri of the dataset</param>
    /// <returns>Collection of XAcquisitionApp (200), or not found (404)</returns>
    [Authorize]
    [HttpGet("GetDatasetApps")]
    public IEnumerable<XAcquisitionApp> GetDatasetApps(string datasetUri)
    {
        string userId = GetCurrentUserId()!;
        Log.Debug("GetDatasetApps called for {DatasetUri} by {UserId}", datasetUri, userId);
        return api.GetDatasetApps(datasetUri, userId);
    }

    /// <summary>
    /// Get the default Application available to be run by the user for a specific dataset.
    /// </summary>
    /// <param name="datasetUri">Uri of the dataset</param>
    /// <returns>XAcquisitionApp (200), or not found (404), or not exists (204)</returns>
    [Authorize]
    [HttpGet("GetDefaultDatasetApp")]
    public XAcquisitionApp? GetDefaultDatasetApp(string datasetUri)
    {
        string userId = GetCurrentUserId()!;
        Log.Debug("GetDefaultDatasetApp called for {DatasetUri} by {UserId}", datasetUri, userId);
        return api.GetDefaultDatasetApp(datasetUri, userId);
    }

}