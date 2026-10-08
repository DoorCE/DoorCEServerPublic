using DoorCEGenerator.Application.Dtos;
using DoorCEGenerator.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEGenerator.WebApi.Controllers;

/// <summary>
/// The interface used for generating acquisition and access applications.
/// Provides operations for generating applications based on schemas, datasets, distributions and usage scenarios.
/// </summary>
[ApiController]
[Route("code")]
public class CodeProvisioningController(CodeProvisioningAPI api) : AuthControllerBase
{
    /// <summary>
    /// Returns a dictionary with framework codes and corresponding code package ids.
    /// </summary>
    /// <param name="templateId">URI of the application template.</param>
    /// <returns>Dictionary of strings containing codes and ids (200) or invalid argument (400)</returns>
    [HttpGet("GetTemplateCodePackages")]
    public Dictionary<string,string> GetTemplateCodePackages(string templateId){
        Log.Information("GetTemplateCodePackages called for template ID: {TemplateId}", templateId);
        Dictionary<string, string> result = api.GetTemplateCodePackages(templateId);
        Log.Debug("GetTemplateCodePackages returns code packages: {Packages}", string.Join(", ", result.Keys));
        return result;
    }
    
    /// <summary>
    /// Returns a dictionary of files containing application code.
    /// </summary>
    /// <param name="codePackageId">URI of the generated code package.</param>
    /// <returns>Dictionary of strings containing file names and code (200) or invalid argument (400)</returns>
    [HttpGet("GetTemplateCode")]
    public Dictionary<string,string> GetTemplateCode(string codePackageId){
        Log.Information("GetTemplateCode called for code package: {CodePackageId}", codePackageId);
        Dictionary<string, string> result = api.GetTemplateCode(codePackageId);
        Log.Debug("GetTemplateCode returns code for files: {Files}", string.Join(", ", result.Keys));
        return result;
    }

    /// <summary>
    /// Returns a dictionary of files containing application code.
    /// </summary>
    /// <param name="templateUri">URI of the application template.</param>
    /// <param name="codeFramework">Optional identifier of the code framework (FLT=Flutter-default, RCT=React)</param>
    /// <returns>Dictionary of strings containing file names and code (200) or invalid argument (400)</returns>
    [HttpGet("GetTemplateCodeByTemplateUri")]
    public Dictionary<string, string> GetTemplateCodeByTemplateUri(string templateUri, string codeFramework="FLT")
    {
        Log.Information("GetTemplateCodeByTemplateUri called for template URI: {TemplateUri} and code framework: {CodeFramework}",
            templateUri, codeFramework);
        Dictionary<string, string> result = api.GetTemplateCodeByTemplateUri(templateUri, codeFramework);
        Log.Debug("GetTemplateCodeByTemplateUri returns code for files: {Files}", string.Join(", ", result.Keys));
        return result;
    }

    [HttpPost("AppDeploymentFinished")]
    public void AppDeploymentFinished([FromBody] XDeploymentResult result)
    {
        Log.Information("AppDeploymentFinished called for app URI: {AppUri} with status: {Status}",
            result.AppUri, result.Status);
        api.AppDeploymentFinished(result);
    }
}