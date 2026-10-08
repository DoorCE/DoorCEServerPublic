using DoorCEGenerator.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DoorCEGenerator.WebApi.Controllers;

/// <summary>
/// The interface used for generating acquisition and access applications.
/// Provides operations for generating applications based on schemas, datasets, distributions and usage scenarios.
/// </summary>
[ApiController]
[Route("generate")]
public class AppGenerationController(AppGenerationAPI api) : AuthControllerBase
{
    /// <summary>
    /// Generates and saves code for the given application template in the given programming language 
    /// </summary>
    /// <param name="templateId">URI of the application template.</param>
    /// <param name="codeFramework">Short identifier of the target programming framework
    /// (supported: "FLT" - Flutter, "RCT" - React).</param>
    /// <returns>URI of the generated code package (200) or bad request or unauthorised (40X)</returns>
    //[Authorize]
    [HttpPost("GenerateCodeFromTemplate")]
    public string GenerateCodeFromTemplate(string templateId, string codeFramework)
    {
        Log.Information("GenerateCodeFromTemplate called for template ID: {TemplateId} and  CodeFramework: {CodeFramework}",
            templateId, codeFramework);
        return api.GenerateCodeFromTemplate(templateId, codeFramework);
    }
}