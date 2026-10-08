using DoorCEServer.Application.AppGenProxy.Interfaces;
using DoorCEServer.Application.InfoManager.Dtos;
using DoorCEServer.Application.InfoManager.Interfaces;

namespace DoorCEServer.Application.InfoManager.Domain;

public class MInfo(string version, IAppGen genApi) :InfoAPI
{
    public XVersions GetVersion()
    {
        return new XVersions {
            ServerVersion = version,
            GeneratorVersion = genApi.GetVersion() ?? "???"
        };
    }
}