using DoorCEServer.Application.InfoManager.Dtos;

namespace DoorCEServer.Application.InfoManager.Interfaces;

public interface InfoAPI
{
    XVersions GetVersion();
}