using DoorCEGenerator.Application.InfoManager.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DoorCEGenerator.WebApi.Controllers;

[ApiController]
[Route("info")]
public class InfoController(InfoAPI api): ControllerBase
{
   /// <summary>
   /// Get the version information of the generator
   /// </summary>
   /// <returns>Version as string</returns>
   [HttpGet("GetVersion")]
   public string GetVersion()
   {
      return api.GetVersion();
   }
}