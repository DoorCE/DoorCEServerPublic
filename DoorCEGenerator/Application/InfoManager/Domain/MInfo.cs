using DoorCEGenerator.Application.InfoManager.Interfaces;

namespace DoorCEGenerator.Application.InfoManager.Domain;

public class MInfo(string version) :InfoAPI
{
    public string GetVersion()
    {
        return version;
    }
}