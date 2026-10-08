using DoorCEServer.Application.InfoManager.Dtos;
using DoorCEServer.Application.InfoManager.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DoorCEServer.WebApi.Controllers;

[ApiController]
[Route("info")]
public class InfoController(InfoAPI api): ControllerBase
{
   /// <summary>
   /// Get the version information of the server and the generator
   /// </summary>
   /// <returns>A pair strings representing versions: one for the server, one for the generator</returns>
   [HttpGet("GetVersion")]
   public XVersions GetVersion()
   {
      return api.GetVersion();
   }
}